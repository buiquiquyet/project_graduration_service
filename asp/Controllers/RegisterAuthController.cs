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
        public async Task<IActionResult> Create([FromBody] string email)
        {
            try
            {
                // Tạo mã xác thực ngẫu nhiên
                string verificationCode = _resp.GenerateVerificationCode();

                // Gửi mã xác thực qua email
                 _resp.SendVerificationEmail(email, verificationCode);

                // Lưu mã xác thực vào cơ sở dữ liệu hoặc bộ nhớ tạm (tuỳ ý)
                await _resp.Create(email, verificationCode);

                return Ok("Đăng ký thành công. Vui lòng kiểm tra email để lấy mã xác thực.");
            }
            catch (Exception ex)
            {
                // Log lỗi nếu có
                return BadRequest("Error sending verification email: " + ex.Message);
            }
        }

        // POST: api/registerAuth/verify
        [HttpPost("verify")] 
        public IActionResult Verify(string email, string code)
        {
            // Kiểm tra mã xác thực
            bool isValid = _resp.CheckVerificationCode(email, code);
            if (isValid)
            {
                // Xác thực thành công, hoàn tất đăng ký
                return Ok("Xác thực thành công.");
            }

            // Mã xác thực không hợp lệ
            return BadRequest("Mã xác thực không hợp lệ.");
        }
    }
}
