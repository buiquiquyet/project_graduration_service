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

        public MomoService(IOptions<MomoOptionModel> options)
        {
            _options = options;
        }
        public async Task<MomoCreatePaymentResponseModel> CreatePaymentAsync(OrderInfoModel model)
        {
            model.OrderId = DateTime.UtcNow.Ticks.ToString();
            model.OrderInfo = "Khách hàng: " + model.FullName + ". Nội dung: " + model.OrderInfo;
           
            var rawData =
                $"partnerCode={_options.Value.PartnerCode}" +
                $"&accessKey={_options.Value.AccessKey}" +
                $"&requestId={model.OrderId}" +
                $"&amount={model.Amount}" +
                $"&orderId={model.OrderId}" +
                $"&orderInfo={model.OrderInfo}" +
                $"&returnUrl={_options.Value.ReturnUrl}" +
                $"&notifyUrl={_options.Value.NotifyUrl}" +
                $"&extraData=";
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
                extraData = "",
                signature = signature
            };

            request.AddParameter("application/json", JsonConvert.SerializeObject(requestData), ParameterType.RequestBody);

            var response = await client.ExecuteAsync(request);

            return JsonConvert.DeserializeObject<MomoCreatePaymentResponseModel>(response.Content);
        }

        public MomoExecuteResponseModel PaymentExecuteAsync(IQueryCollection collection)
        {
            // Sử dụng FirstOrDefault để tránh ngoại lệ nếu không có tham số
            var amountPair = collection.FirstOrDefault(s => s.Key == "amount");
            var userId = collection.FirstOrDefault(s => s.Key == "userId");
            var orderInfoPair = collection.FirstOrDefault(s => s.Key == "orderInfo");
            var orderIdPair = collection.FirstOrDefault(s => s.Key == "orderId");

            // Kiểm tra nếu không tìm thấy phần tử
            if (amountPair.Equals(default(KeyValuePair<string, StringValues>)) ||
                orderInfoPair.Equals(default(KeyValuePair<string, StringValues>)) ||
                userId.Equals(default(KeyValuePair<string, StringValues>)) ||
                orderIdPair.Equals(default(KeyValuePair<string, StringValues>)))
            {
                // Trả về null nếu không tìm thấy bất kỳ tham số nào
                return null;
            }

            return new MomoExecuteResponseModel()
            {
                Amount = amountPair.Value.ToString(),  // Chuyển đổi giá trị sang chuỗi
                OrderId = orderIdPair.Value.ToString(),
                OrderInfo = orderInfoPair.Value.ToString(),
            };
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

