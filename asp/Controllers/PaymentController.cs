using asp.Helper;
using asp.Models;
using asp.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.Data;
using System.Threading.Tasks;

namespace asp.Controllers
{
    [ApiController]
    [Route("api/payment")]
    public class PaymentController : ControllerBase
    {
        private readonly IMomoService _momoService;

        public PaymentController(IMomoService momoService)
        {
            _momoService = momoService;
        }

        [HttpPost]
        [Route("createPaymentUrl")]
        public async Task<IActionResult> CreatePaymentUrl(OrderInfoModel model)
        {
            var response = await _momoService.CreatePaymentAsync(model);
            if (response == null)
            {
                return BadRequest(new { message = response?.Message ?? "Error processing payment" });
            }
            if(response.PayUrl != null)
            {
            return Ok(new ApiResponseDTO<Object> { data = new { response }, message = "Success." });

            }
            return BadRequest(new ApiResponseDTO<Object> { data = new { response }, message = "Error." });


        }
        [Route("paymentCallback")]
        public async Task<IActionResult> PaymentCallBack()
        {
            var response = _momoService.PaymentExecuteAsync(HttpContext.Request.Query);

            // Kiểm tra nếu response là null (có thể là do thiếu tham số trong query)
            if (response == null)
            {
                return BadRequest(new { message = "Error processing payment: Missing parameters" });
            }

            return Ok(new ApiResponseDTO<Object> { data = new { response }, message = "Success." });
        }

    }
}
