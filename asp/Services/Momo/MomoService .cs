using asp.Helper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Microsoft.OpenApi.Models;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json;
using RestSharp;
using System.Linq.Expressions;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using ZstdSharp.Unsafe;
using static MongoDB.Bson.Serialization.Serializers.SerializerHelper;
using System.IO;
using OfficeOpenXml;
using asp.Models.User;
using asp.Models.MongoSetting;
namespace asp.Services.Momo
{


    public class MomoService : IMomoService
    {
        private readonly IOptions<MomoOptionModel> _options;
        private readonly IMongoCollection<MomoExecuteResponseModel> _collection;
        private readonly IMongoCollection<ProjectFunds> _collectionProjectFund;
        private readonly IMongoCollection<Users> _collectionUser;
        public MomoService(IOptions<MomoOptionModel> options, IOptions<MongoDbSetting> databaseSettings)
        {
            var client = new MongoClient(databaseSettings.Value.ConnectionURI);
            var database = client.GetDatabase(databaseSettings.Value.DatabaseName);
            _collection = database.GetCollection<MomoExecuteResponseModel>(typeof(MomoExecuteResponseModel).Name.ToLower());
            _collectionProjectFund = database.GetCollection<ProjectFunds>(typeof(ProjectFunds).Name.ToLower());
            _collectionUser = database.GetCollection<Users>(typeof(Users).Name.ToLower());
            _options = options;
        }

        public async Task<MomoCreatePaymentResponseModel> CreatePaymentAsync(OrderInfoModel model)
        {
            model.OrderId = DateTime.UtcNow.Ticks.ToString();
            //model.OrderInfo = "Khách hàng: " + model.FullName + ". Nội dung: " + model.OrderInfo;

            var rawData =
              $"partnerCode={_options.Value.PartnerCode}" +
               $"&accessKey={_options.Value.AccessKey}" +
               $"&requestId={model.OrderId}" +
               $"&amount={model.Amount}" +
               $"&orderId={model.OrderId}" + // id của bản ghi từ thiện
               $"&orderInfo={model.OrderInfo}" +
               $"&returnUrl={_options.Value.ReturnUrl}" +
               $"&notifyUrl={_options.Value.NotifyUrl}" +
               $"&extraData={model.UserId}";  // id của user
            var signature = ComputeHmacSha256(rawData, _options.Value.SecretKey);

            var client = new RestClient(_options.Value.MomoApiUrl);
            var request = new RestRequest() { Method = Method.Post };
            request.AddHeader("Content-Type", "application/json; charset=UTF-8");

            // Create an object representing the request data
            var requestData = new
            {
                accessKey = _options.Value.AccessKey,
                partnerCode = _options.Value.PartnerCode,
                requestType = _options.Value.RequestType,
                notifyUrl = _options.Value.NotifyUrl,
                returnUrl = _options.Value.ReturnUrl,
                orderId = model.OrderId,
                amount = model.Amount.ToString(),
                orderInfo = model.OrderInfo,
                requestId = model.OrderId,
                extraData = model.UserId,
                signature
            };

            request.AddParameter("application/json", JsonConvert.SerializeObject(requestData), ParameterType.RequestBody);

            var response = await client.ExecuteAsync(request);

            return JsonConvert.DeserializeObject<MomoCreatePaymentResponseModel>(response.Content);
        }
        // hàm băm
        private string ComputeHmacSha256(string message, string secretKey)
        {
            var keyBytes = Encoding.UTF8.GetBytes(secretKey);
            var messageBytes = Encoding.UTF8.GetBytes(message);

            byte[] hashBytes;

            using (var hmac = new HMACSHA256(keyBytes))
            {
                hashBytes = hmac.ComputeHash(messageBytes);
            }

            var hashString = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();

            return hashString;
        }
        // lưu giao dịch donate
        public async Task<MomoExecuteResponseModel> PaymentExecuteAsync(IQueryCollection collection)
        {
            var amountPair = collection.FirstOrDefault(s => s.Key == "amount");
            var orderInfoPair = collection.FirstOrDefault(s => s.Key == "orderInfo");
            var storeId = collection.FirstOrDefault(s => s.Key == "extraData");
            var orderIdPair = collection.FirstOrDefault(s => s.Key == "orderId");

            Console.WriteLine("The query parameters: " + string.Join(", ", collection.Select(kv => $"{kv.Key}: {kv.Value}")));

            if (string.IsNullOrEmpty(amountPair.Value) ||
                string.IsNullOrEmpty(orderInfoPair.Value) ||
                string.IsNullOrEmpty(orderIdPair.Value) ||
                string.IsNullOrEmpty(storeId.Value))
            {
                return null;
            }

            var data = new MomoExecuteResponseModel()
            {
                Amount = amountPair.Value.ToString(),
                UserId = storeId.Value.ToString(),
                ProjectFundId = orderInfoPair.Value.ToString(),
                CreatedAt = DateTime.Now
            };

            try
            {
                // Insert transaction data into MongoDB collection
                await _collection.InsertOneAsync(data);

                // Calculate totalAmount for the store/user (storeId or userId)
                var pipeline = new[] {
                    new BsonDocument("$match", new BsonDocument("UserId", data.UserId)),  // Match documents by UserId (storeId)
                    new BsonDocument("$group", new BsonDocument {
                        { "_id", "$UserId" },  // Group by UserId (or storeId)
                        { "totalAmount", new BsonDocument("$sum", new BsonDocument("$toDecimal", "$Amount")) }  // Sum up the Amount
                    })
                };

                var result = await _collection.AggregateAsync<BsonDocument>(pipeline);
                var sumResult = await result.FirstOrDefaultAsync();

                decimal totalAmount = 0;
                if (sumResult != null && sumResult.Contains("totalAmount"))
                {
                    totalAmount = sumResult["totalAmount"].ToDecimal();
                }

                Console.WriteLine($"Total Amount for User {data.UserId}: {totalAmount}");

                // Update the user record if totalAmount exceeds 100000
                if (totalAmount >= 100000)
                {
                    var user = await _collectionUser
                        .Find(u => u.Id == data.UserId)
                        .FirstOrDefaultAsync();

                    if (user != null)
                    {
                        // Check and update isEmissary status if not already true
                        if (user.isEmissary != true)
                        {
                            user.isEmissary = true;
                            user.updatedAt = DateTime.Now;

                            var updateResult = await _collectionUser.ReplaceOneAsync(u => u.Id == user.Id, user);
                            Console.WriteLine($"User status updated to Emissary: {updateResult.ModifiedCount} records updated");
                        }
                    }
                }
                return data;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Error occurred while processing payment: " + ex.Message);
                throw new Exception("Có lỗi xảy ra trong quá trình chèn dữ liệu vào cơ sở dữ liệu: " + ex.Message, ex);
            }
        }






        private bool IsValidObjectId(string id)
        {
            return ObjectId.TryParse(id, out _);
        }

        // lấy số lượng donate
        public async Task<List<MomoExecuteResponseModel>> GetDonatesByProjectFundIdAsync(string projectFundId, int skipAmount, int pageSize)
        {
            // Lấy danh sách các donate dựa theo projectFundId
            var donates = await _collection
                .Find(comment => comment.ProjectFundId == projectFundId)
                .Skip(skipAmount)
                .Limit(pageSize)
                .SortByDescending(comment => comment.CreatedAt)
                .ToListAsync();

            // Lấy danh sách userId duy nhất từ donate và chỉ giữ lại các ObjectId hợp lệ
            var userIds = donates
                .Select(donate => donate.UserId)
                .Where(userId => IsValidObjectId(userId))
                .Distinct()
                .ToList();

            // Lấy thông tin người dùng từ collection "Users"
            var users = await _collectionUser
                .Find(user => userIds.Contains(user.Id))
                .ToListAsync();

            // Kết hợp thông tin người dùng vào các comment
            foreach (var donate in donates)
            {
                var user = users.FirstOrDefault(u => u.Id == donate.UserId);
                if (user != null)
                {
                    donate.FullName = user.fullName; // Gán tên người dùng
                }
                else
                {
                    donate.FullName = "Nhà hảo tâm ẩn danh"; // Gán tên người dùng
                }
            }

            return donates;
        }


        // đếm số lượng bản ghi
        public async Task<long> CountAsync(string projectFundId)
        {
            return await _collection.CountDocumentsAsync(comment => comment.ProjectFundId == projectFundId);
        }


        // lấy 3 người donate nhiều nhất
        public async Task<List<MomoExecuteResponseModel>> GetTop3DonorsAsync()
        {
            var pipeline = new[]
            {
                // Nhóm theo UserId và tính tổng số tiền quyên góp
                new BsonDocument("$group", new BsonDocument
                {
                    { "_id", "$UserId" },  // UserId trong MomoExecuteResponseModel
                    { "totalAmount", new BsonDocument("$sum", new BsonDocument("$toDecimal", "$Amount")) }
                }),
                // Đổi tên _id thành UserId
                new BsonDocument("$project", new BsonDocument
                {
                    { "UserId", "$_id" },
                    { "totalAmount", 1 }
                }),
                // Sắp xếp theo tổng số tiền quyên góp giảm dần
                new BsonDocument("$sort", new BsonDocument("totalAmount", -1)),
                // Lấy ra 3 người đứng đầu
                new BsonDocument("$limit", 3)
            };

            var result = await _collection.AggregateAsync<BsonDocument>(pipeline);
            var topDonors = new List<MomoExecuteResponseModel>();

            var userIds = new List<string>();

            await result.ForEachAsync(doc =>
            {
                var userId = doc.Contains("UserId") ? doc["UserId"].AsString : null;
                if (userId != null && ObjectId.TryParse(userId, out _))
                {
                    userIds.Add(userId);
                }
                topDonors.Add(new MomoExecuteResponseModel
                {
                    UserId = userId,
                    Amount = doc.Contains("totalAmount") ? doc["totalAmount"].ToString() : null,
                    FullName = null,  // Mặc định FullName là null
                    Avatar = null     // Mặc định Avatar là null
                });
            });

            if (userIds.Count > 0)
            {
                var filter = Builders<Users>.Filter.In(u => u.Id, userIds);
                var users = await _collectionUser.Find(filter).ToListAsync();

                foreach (var donor in topDonors)
                {
                    if (!string.IsNullOrEmpty(donor.UserId))
                    {
                        var user = users.FirstOrDefault(u => u.Id == donor.UserId);
                        if (user != null)
                        {
                            donor.FullName = user.fullName;
                            donor.Avatar = user.avatar;
                        }
                    }
                }
            }

            return topDonors;
        }

        //export excel danh sách người donate
        public async Task<byte[]> GenerateDonatesExcelAsync(string projectFundId, int skipAmount, int pageSize)
        {
            // Fetch the donations based on the projectFundId
            var donates = await _collection
                .Find(comment => comment.ProjectFundId == projectFundId)
                .Skip(skipAmount)
                .Limit(pageSize)
                .SortByDescending(comment => comment.CreatedAt)
                .ToListAsync();

            // Create the Excel file using EPPlus
            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Donates");

                // Check if there are no donations
                if (donates == null || donates.Count == 0)
                {
                    // If no donations, set a message in the Excel file
                    worksheet.Cells[1, 1].Value = "Chưa có dữ liệu lịch sử ủng hộ.";
                    worksheet.Cells[1, 1].Style.Font.Bold = true;
                    worksheet.Cells[1, 1].Style.Font.Size = 14;
                    worksheet.Cells[1, 1].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                    worksheet.Cells[1, 1].Style.VerticalAlignment = OfficeOpenXml.Style.ExcelVerticalAlignment.Center;
                    worksheet.Cells[1, 1, 1, 4].Merge = true;  // Merge all columns to show the message

                    // Return the file as a byte array
                    return package.GetAsByteArray();
                }

                // Get unique userIds from donations, filter valid ObjectIds
                var userIds = donates
                    .Select(donate => donate.UserId)
                    .Where(userId => IsValidObjectId(userId))
                    .Distinct()
                    .ToList();

                // Fetch user data from the "Users" collection
                var users = await _collectionUser
                    .Find(user => userIds.Contains(user.Id))
                    .ToListAsync();

                // Create a dictionary to map UserId to FullName
                var userDictionary = users.ToDictionary(user => user.Id, user => user.fullName);

                // Combine user info into donations
                foreach (var donate in donates)
                {
                    if (userDictionary.TryGetValue(donate.UserId, out var fullName))
                    {
                        donate.FullName = fullName;
                    }
                    else
                    {
                        donate.FullName = "Nhà hảo tâm ẩn danh"; // Set default name if not found
                    }
                }

                // Set column headers
                worksheet.Cells[1, 1].Value = "Id người dùng";
                worksheet.Cells[1, 2].Value = "Họ và tên";
                worksheet.Cells[1, 3].Value = "Số tiền ủng hộ";
                worksheet.Cells[1, 4].Value = "Thời gian";

                // Fill data rows
                for (int i = 0; i < donates.Count; i++)
                {
                    var donate = donates[i];
                    worksheet.Cells[i + 2, 1].Value = donate.UserId;
                    worksheet.Cells[i + 2, 2].Value = donate.FullName;
                    worksheet.Cells[i + 2, 3].Value = donate.Amount;
                    worksheet.Cells[i + 2, 4].Value = donate.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss");
                }

                // Return the file as a byte array
                return package.GetAsByteArray();
            }
        }



    }




}

