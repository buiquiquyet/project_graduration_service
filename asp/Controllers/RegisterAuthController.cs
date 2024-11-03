using asp.Helper;
using asp.Models;
using asp.Respositories;
using asp.Services;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Net.Mail;
namespace asp.Controllers
{
    [ApiController]
    [Route("api/registerAuth")]
    public class RegisterAuthController : Controller 
    {
        private readonly RegisterAuthService _resp;
        public RegisterAuthController(RegisterAuthService resp)
        {
            _resp = resp;
        }

        // POST: api/registerAuth/create
        [HttpPost("create")]
        public async Task<ActionResult<ApiResponseDTO<string>>> Create([FromBody] string email)
        {
            try
            {
                if (string.IsNullOrEmpty(email))
                {
                    return BadRequest(new ApiResponseDTO<string> { message = "Email không được để trống." });
                }

                // Kiểm tra sự tồn tại của email
                bool checkEmailExists = await _resp.EmailExistsAsync(email);
                if (checkEmailExists)
                {
                    // Nếu email đã tồn tại, cập nhật mã xác thực nếu hết hạn
                    bool isUpdated = await _resp.UpdateVerificationCodeIfExpiredAsync(email);
                    if (!isUpdated)
                    {
                        return BadRequest(new ApiResponseDTO<string> { message = "Email đã được dùng để đăng ký." });
                    }
                }
                else
                {
                    // Tạo mã xác thực mới
                    string verificationCode = _resp.GenerateVerificationCode();
                    DateTime expirationTime = DateTime.UtcNow.AddSeconds(20); // Thời gian mới

                    // Lưu mã xác thực mới vào cơ sở dữ liệu
                    await _resp.Create(email, verificationCode); // Cần cập nhật phương thức Create để nhận thêm expirationTime
                }

                return Ok(new ApiResponseDTO<Object> { data = new { email }, message = "Kiểm tra email để nhận mã xác thực." });
            }
            catch (Exception ex)
            {
                // Log lỗi nếu có
                return BadRequest(new ApiResponseDTO<string> { message = "Lỗi khi gửi mã xác thực qua email: " + ex.Message });
            }
        }


        // POST: api/registerAuth/verify
        [HttpPost("verify")]
        public IActionResult Verify([FromBody] Dictionary<string, object> request)
        {
            // Kiểm tra nếu request null hoặc không có giá trị email và code
            if (request == null ||
                !request.ContainsKey("email") ||
                !request.ContainsKey("code") ||
                string.IsNullOrWhiteSpace(request["email"]?.ToString()) ||
                string.IsNullOrWhiteSpace(request["code"]?.ToString()))
            {
                return BadRequest(new ApiResponseDTO<string> { message = "Email và mã xác thực không được để trống." });
            }

            // Truyền các giá trị vào biến
            string email = request["email"].ToString();
            string code = request["code"].ToString();

            // Kiểm tra mã xác thực
            bool isValid = _resp.CheckVerificationCode(email, code);
            if (isValid)
            {
                return Ok(new ApiResponseDTO<Object> { message = "Xác thực thành công." });
            }

            return BadRequest(new ApiResponseDTO<string> { message = "Mã xác thực không hợp lệ." });
        }

    }
}
