using asp.Helper;
using asp.Helper.ApiResponse;
using asp.Models;
using asp.Services.Momo;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.Data;
using System.Drawing;
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

            if (datas != null)
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
            // Tạo HTML đẹp với nội dung thông báo thanh toán thành công hoặc thất bại
            string htmlResponse = "<html lang='en'>";
            htmlResponse += "<head>";
            htmlResponse += "<meta charset='UTF-8' />";
            htmlResponse += "<meta name='viewport' content='width=device-width, initial-scale=1.0' />";
            htmlResponse += "<title>Payment Status</title>";
            htmlResponse += "<style>";
            htmlResponse += "body { font-family: Arial, sans-serif; margin: 0; padding: 0; background-color: #f4f4f9; color: #333; display: flex; justify-content: center; align-items: center; height: 100vh;}";
            htmlResponse += "h1 { color: #27ae60; font-size: 2rem; text-align: center; margin-bottom: 20px;}";
            htmlResponse += "p { color: #555; text-align: center; font-size: 1.2rem; margin-bottom: 30px;}";
            htmlResponse += ".btn { background-color: #3498db; color: white; border: none; padding: 10px 20px; font-size: 1rem; cursor: pointer; border-radius: 5px; text-align: center;}";
            htmlResponse += ".btn:hover { background-color: #2980b9; }";
            htmlResponse += "</style>";
            htmlResponse += "</head>";
            htmlResponse += "<body>";

            //if (response.Amount != null && response.Amount == "ExpectedAmount") // Điều kiện thanh toán thành công
            //{
            htmlResponse += "<h1>Thanh toán thành công!</h1>";
            htmlResponse += "<p>Cảm ơn bạn đã đóng góp cho dự án.</p>";
            htmlResponse += "<p>Vui lòng quay lại trang dự án để xem chi tiết.</p>";
            htmlResponse += $"<a href='http://localhost:5173/project-fund-detail/{response.ProjectFundId}' class='btn'>Quay lại trang Donate</a>";
            //}
           

            htmlResponse += "</body>";
            htmlResponse += "</html>";

            return Content(htmlResponse, "text/html");
            //return Ok(new ApiResponseDTO<Object> { data = new { response }, message = "success" });
        }
        [HttpGet("export-excel/{projectFundId}")]
        public async Task<IActionResult> ExportDonatesToExcelAsync(string projectFundId, int page, int size = 10)
        {
            try
            {
                var skipAmount = (page - 1) * size;
                // Call the service to generate the Excel file as a byte array
                var fileBytes = await _momoService.GenerateDonatesExcelAsync(projectFundId, skipAmount, size);

                // Return the file as a response with the correct Excel MIME type
                return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Donates.xlsx");
            }
            catch (Exception ex)
            {
                // Handle errors and log or return a friendly message
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

    }
}
