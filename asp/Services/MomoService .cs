using asp.Helper;
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

namespace asp.Respositories
{


    public class MomoService : IMomoService
    {
        private readonly IOptions<MomoOptionModel> _options;
        private readonly IMongoCollection<MomoExecuteResponseModel> _collection;
        public MomoService(IOptions<MomoOptionModel> options, IOptions<MongoDbSetting> databaseSettings)
        {
            var client = new MongoClient(databaseSettings.Value.ConnectionURI);
            var database = client.GetDatabase(databaseSettings.Value.DatabaseName);
            _collection = database.GetCollection<MomoExecuteResponseModel>(typeof(MomoExecuteResponseModel).Name.ToLower());
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

        //public async Task<MomoExecuteResponseModel>  PaymentExecuteAsync(IQueryCollection collection)
        //{
        //    // Sử dụng FirstOrDefault để tránh ngoại lệ nếu không có tham số
        //    var amountPair = collection.FirstOrDefault(s => s.Key == "amount");
        //    var orderInfoPair = collection.FirstOrDefault(s => s.Key == "orderInfo");
        //    var storeId = collection.FirstOrDefault(s => s.Key == "extraData");
        //    var orderIdPair = collection.FirstOrDefault(s => s.Key == "orderId");

        //    // Kiểm tra nếu không tìm thấy phần tử
        //    if (amountPair.Equals(default(KeyValuePair<string, StringValues>)) ||
        //        orderInfoPair.Equals(default(KeyValuePair<string, StringValues>)) ||
        //        orderIdPair.Equals(default(KeyValuePair<string, StringValues>)))
        //    {
        //        // Trả về null nếu không tìm thấy bất kỳ tham số nào
        //        return null;
        //    }

        //    var data =   new MomoExecuteResponseModel()
        //    {
        //        Amount = amountPair.Value.ToString(),  // Chuyển đổi giá trị sang chuỗi
        //        //OrderId = orderIdPair.Value.ToString(),
        //        //OrderInfo = orderInfoPair.Value.ToString(),
        //        UserId = storeId.Value.ToString(), // id của người dùng
        //        ProjectFundId = orderInfoPair.Value.ToString(), // id của người từ thiện
        //         CreatedAt = DateTime.Now // Ghi nhận thời gian tạo
        //    };
        //    try
        //    {
        //        // Chèn dữ liệu vào collection MongoDB
        //        await _collection.InsertOneAsync(data);

        //        // Trả về đối tượng đã chèn, bao gồm cả Id từ MongoDB nếu có
        //        return data;
        //    }
        //    catch (Exception ex)
        //    {
        //        // Xử lý lỗi (ghi log, ném ngoại lệ, v.v.)
        //        // Ví dụ: ghi log chi tiết (có thể dùng log framework như NLog, Serilog)
        //        throw new Exception("Có lỗi xảy ra trong quá trình chèn dữ liệu vào cơ sở dữ liệu: " + ex.Message, ex);
        //    }
        //}
        public async Task<MomoExecuteResponseModel> PaymentExecuteAsync(IQueryCollection collection)
        {
            // Sử dụng FirstOrDefault để tránh ngoại lệ nếu không có tham số
            var amountPair = collection.FirstOrDefault(s => s.Key == "amount");
            var projectFundId = collection.FirstOrDefault(s => s.Key == "orderInfo"); // id của 1 bản ghi từ thiện
            var userId = collection.FirstOrDefault(s => s.Key == "extraData"); // id của user 
            var resultCodePair = collection.FirstOrDefault(s => s.Key == "resultCode"); // Thêm resultCode để kiểm tra thành công

            // Kiểm tra nếu không tìm thấy các tham số cần thiết
            if (amountPair.Equals(default(KeyValuePair<string, StringValues>)) ||
                projectFundId.Equals(default(KeyValuePair<string, StringValues>)) ||
                userId.Equals(default(KeyValuePair<string, StringValues>)) ||
                resultCodePair.Equals(default(KeyValuePair<string, StringValues>))) // Kiểm tra xem có resultCode không
            {
                // Trả về null nếu không tìm thấy bất kỳ tham số nào
                return null;
            }

            // Kiểm tra resultCode để xác định trạng thái giao dịch
            bool isSuccess = resultCodePair.Value.ToString() == "0"; // Thông thường, resultCode = 0 có nghĩa là thành công trong MoMo

            var data = new MomoExecuteResponseModel()
            {
                Amount = amountPair.Value.ToString(),
                UserId = userId.Value.ToString(),
                ProjectFundId = projectFundId.Value.ToString(),
                CreatedAt = DateTime.Now,
            };

            try
            {
                if (isSuccess)
                {
                    // Chèn dữ liệu vào MongoDB nếu giao dịch thành công
                    await _collection.InsertOneAsync(data);
                }

                return data; // Trả về đối tượng đã chèn vào DB (hoặc thông báo thành công/không thành công)
            }
            catch (Exception ex)
            {
                // Xử lý lỗi (ghi log, ném ngoại lệ, v.v.)
                throw new Exception("Có lỗi xảy ra trong quá trình chèn dữ liệu vào cơ sở dữ liệu: " + ex.Message, ex);
            }
        }






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
    }




}

