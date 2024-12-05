﻿using asp.Helper;
using asp.Models;
using asp.Services;
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

namespace asp.Respositories
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
                signature = signature
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
        public async Task<MomoExecuteResponseModel> PaymentExecuteAsync(IQueryCollection collection)
        {
            // Sử dụng FirstOrDefault để tránh ngoại lệ nếu không có tham số
            var amountPair = collection.FirstOrDefault(s => s.Key == "amount");
            var orderInfoPair = collection.FirstOrDefault(s => s.Key == "orderInfo");
            var storeId = collection.FirstOrDefault(s => s.Key == "extraData");
            var orderIdPair = collection.FirstOrDefault(s => s.Key == "orderId");
            Console.WriteLine("The answer is: " + collection);
            // Kiểm tra nếu không tìm thấy phần tử
            if (amountPair.Equals(default(KeyValuePair<string, StringValues>)) ||
                orderInfoPair.Equals(default(KeyValuePair<string, StringValues>)) ||
                orderIdPair.Equals(default(KeyValuePair<string, StringValues>)))
            {
                // Trả về null nếu không tìm thấy bất kỳ tham số nào
                return null;
            }

            var data = new MomoExecuteResponseModel()
            {
                Amount = amountPair.Value.ToString(),  // Chuyển đổi giá trị sang chuỗi
                //OrderId = orderIdPair.Value.ToString(),
                //OrderInfo = orderInfoPair.Value.ToString(),
                UserId = storeId.Value.ToString(), // id của người dùng
                ProjectFundId = orderInfoPair.Value.ToString(), // id của người từ thiện
                CreatedAt = DateTime.Now // Ghi nhận thời gian tạo
            };
            try
            {
                // Chèn dữ liệu vào collection MongoDB
                await _collection.InsertOneAsync(data);
                // Tìm bản ghi ProjectFunds dựa trên ProjectFundId
                var projectFund = await _collectionProjectFund
                    .Find(pf => pf.Id == data.ProjectFundId)
                    .FirstOrDefaultAsync();
                
                if (projectFund != null)
                {
                    // Kiểm tra và xử lý các giá trị currentAmount không hợp lệ
                    if (string.IsNullOrEmpty(projectFund.currentAmount) || !decimal.TryParse(projectFund.currentAmount, out var currentAmount))
                    {
                        currentAmount = 0;
                    }
                    // Cộng số tiền giao dịch vào currentAmount
                    if (decimal.TryParse(data.Amount, out var transactionAmount))
                    {
                        projectFund.currentAmount = (currentAmount + transactionAmount).ToString();
                        projectFund.updatedAt = DateTime.Now;

                        // Cập nhật bản ghi ProjectFunds trong cơ sở dữ liệu
                        await _collectionProjectFund.ReplaceOneAsync(
                            pf => pf.Id == projectFund.Id,
                            projectFund);
                    }
                }
                // Trả về đối tượng đã chèn, bao gồm cả Id từ MongoDB nếu có
                return data;
            }
            catch (Exception ex)
            {
                // Xử lý lỗi (ghi log, ném ngoại lệ, v.v.)
                // Ví dụ: ghi log chi tiết (có thể dùng log framework như NLog, Serilog)
                throw new Exception("Có lỗi xảy ra trong quá trình chèn dữ liệu vào cơ sở dữ liệu: " + ex.Message, ex);
            }
        }
        // dếm số lượng donate
        public async Task<List<MomoExecuteResponseModel>> GetDonatesByProjectFundIdAsync(string projectFundId, int skipAmount, int pageSize)
        {
            // Lấy danh sách các donate dựa theo projectFundId
            var donates = await _collection
                .Find(comment => comment.ProjectFundId == projectFundId)
                .Skip(skipAmount)
                .Limit(pageSize)
                .SortByDescending(comment => comment.CreatedAt)
                .ToListAsync();

            // Lấy danh sách userId duy nhất từ donate
            var userIds = donates.Select(donate => donate.UserId).Distinct().ToList();

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
                }else
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

        public async Task<List<MomoExecuteResponseModel>> GetTop3DonorsAsync()
        {
            var pipeline = new[]
            {
                // Nhóm theo UserId và tính tổng số tiền quyên góp
                new BsonDocument("$group", new BsonDocument
                    {
                        { "_id", "$UserId" },  // UserId trong MomoExecuteResponseModel
                        { "totalAmount", new BsonDocument("$sum", new BsonDocument("$toDecimal", "$Amount")) },
                    }
                ),
                // Chuyển UserId thành ObjectId nếu cần thiết
                new BsonDocument("$addFields", new BsonDocument
                    {
                        { "UserId", new BsonDocument("$toObjectId", "$_id") }
                    }
                ),
                // Thực hiện phép nối (join) với bảng Users để lấy thông tin từ bảng Users
                new BsonDocument("$lookup", new BsonDocument
                    {
                        { "from", "users" }, // Tên bảng (collection) Users
                        { "localField", "UserId" }, // Trường trong MomoExecuteResponseModel (UserId)
                        { "foreignField", "_id" }, // Trường trong bảng Users để nối với UserId
                        { "as", "userInfo" } // Tên trường mới để lưu kết quả nối
                    }
                ),
                // Chỉ lấy thông tin của người quyên góp đầu tiên trong userInfo
                new BsonDocument("$unwind", new BsonDocument { { "path", "$userInfo" } }),
                // Sắp xếp theo tổng số tiền quyên góp giảm dần
                new BsonDocument("$sort", new BsonDocument("totalAmount", -1)),
                // Lấy ra 3 người đứng đầu
                new BsonDocument("$limit", 3)
            };

            var result = await _collection.AggregateAsync<BsonDocument>(pipeline);
            var topDonors = new List<MomoExecuteResponseModel>();

            await result.ForEachAsync(doc =>
            {
                topDonors.Add(new MomoExecuteResponseModel
                {
                    UserId = doc["_id"].BsonType == BsonType.Null ? null : doc["_id"].AsString,
                    Amount = doc["totalAmount"].ToString(),
                    FullName = doc["userInfo"]["fullName"].BsonType == BsonType.Null ? null : doc["userInfo"]["fullName"].AsString, // Lấy fullName từ userInfo
                    Avatar = doc["userInfo"]["avatar"].BsonType == BsonType.Null ? null : doc["userInfo"]["avatar"].AsString  // Lấy avatar từ userInfo
                });
            });

            return topDonors;
        }
    }




}

