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
            return Ok(new ApiResponseDTO<Object> { data = new { response }, message = "success." });

            }
            return BadRequest(new ApiResponseDTO<Object> { data = new { response }, message = "Error." });


        }

        [HttpGet]
        [Route("byProjectFundId/{projectFundId}")]
        public async Task<ActionResult<List<MomoExecuteResponseModel>>> GetDonatesByProjectFundId(string projectFundId, int page = 1, int size = 10)
        {
            var skipAmount = (page - 1) * size;
            List<MomoExecuteResponseModel> datas;
            long totalDonates;
            datas = await _momoService.GetDonatesByProjectFundIdAsync(projectFundId, skipAmount, size);
            totalDonates = await _momoService.CountAsync(projectFundId);

            if (datas != null && datas.Count > 0)
            {
                var response = new
                {
                    message = "success",
                    datas,
                    totalPages = (int)Math.Ceiling((double)totalDonates / size),
                    currentPage = page,
                    totalRecords = totalDonates
                };

                return Ok(response);
            }
            else
            {
                return BadRequest(new ApiResponseDTO<object> { data = new { error = "Error" }, message = "Đã xảy ra lỗi." });
            }
        }

        [HttpGet("top-donors")]
        public async Task<ActionResult<List<MomoExecuteResponseModel>>> GetTop3Donors()
        {
            var topDonors = await _momoService.GetTop3DonorsAsync();
            return Ok(new ApiResponseDTO<Object> { data = new { topDonors }, message = "success" });
        }
        [Route("paymentCallback")]
        public async Task<IActionResult> PaymentCallBack()
        {
            var response = await _momoService.PaymentExecuteAsync(HttpContext.Request.Query);

            // Kiểm tra nếu response là null (có thể là do thiếu tham số trong query)
            if (response == null)
            {
                return BadRequest(new { message = "Error processing payment: Missing parameters" });
            }

            return Ok(new ApiResponseDTO<Object> { data = new { response }, message = "success" });
        }

    }
}
