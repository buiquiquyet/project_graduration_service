using asp.Constants.User;
using asp.Helper.ApiResponse;
using asp.Models.User;
using asp.Services.JWT;
using asp.Services.LoginGoogle;
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
        private readonly JWTService _jwtService;
        public RegisterAuthController(RegisterAuthService resp, JWTService jwtService)
        {
            _resp = resp;
            _jwtService = jwtService;
        }

        // POST: api/registerAuth/create
        [HttpPost("create")]
        public async Task<ActionResult<ApiResponseDTO<string>>> Create([FromBody] Dictionary<string, object> request)
        {
            try
            {
                if (request == null ||
                !request.ContainsKey("email") ||
                !request.ContainsKey("passWord") ||
                string.IsNullOrWhiteSpace(request["email"]?.ToString()) ||
                string.IsNullOrWhiteSpace(request["passWord"]?.ToString()))
                {
                    return BadRequest(new ApiResponseDTO<string> { message = "Thông tin không được để trống." });
                }
                // Truyền các giá trị vào biến
                string email = request["email"].ToString();
                string passWord = request["passWord"].ToString();

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
                    // Gửi mã xác thực mới qua email
                    _resp.SendVerificationEmail(email, verificationCode);

                    // Lưu mã xác thực mới vào cơ sở dữ liệu
                    Users user = new Users
                    {
                        email = email,
                        passWord = passWord,
                        verificationCode = verificationCode,
                        role = UserRole.USER
                    };
                    await _resp.Create(user); // Cần cập nhật phương thức Create để nhận thêm expirationTime
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
        public async Task<IActionResult> Verify([FromBody] Dictionary<string, object> request)
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
            var isValid = await  _resp.CheckVerificationCode(email, code);
            if (isValid != null)
            {
                var token = _jwtService.GenerateToken(isValid.Id);
                return Ok(new ApiResponseDTO<Object> { data = new { token }, message = "Xác thực thành công." });
            }

            return BadRequest(new ApiResponseDTO<string> { message = "Mã xác thực không hợp lệ." });
        }
        // POST: api/registerAuth/verify
        [HttpPost("login")]
        public async Task<ActionResult<ApiResponseDTO<string>>> Login([FromBody] Dictionary<string, object> request)
        {
            // Kiểm tra nếu request null hoặc không có giá trị email và code
            if (request == null ||
                !request.ContainsKey("email") ||
                !request.ContainsKey("passWord") ||
                string.IsNullOrWhiteSpace(request["email"]?.ToString()) ||
                string.IsNullOrWhiteSpace(request["passWord"]?.ToString()))
            {
                return BadRequest(new ApiResponseDTO<string> { message = "Thông tin không được để trống." });
            }

            // Truyền các giá trị vào biến
            string email = request["email"].ToString() ?? "";
            string passWord = request["passWord"].ToString() ?? "";

            // Kiểm tra mã xác thực
            var isValid =  await _resp.Login(email, passWord);
            if (isValid != null)
            {
                var token = _jwtService.GenerateToken(isValid.Id);
                return Ok(new ApiResponseDTO<Object> { data = new { token }, message = "Đăng nhập thành công." });
            }

            return BadRequest(new ApiResponseDTO<string> { message = "Đăng nhập thất bại." });
        }
        [HttpPost("loginWithGoogle")]
        public async Task<IActionResult> LoginWithGoogle([FromBody] Users userRequest)
        {
            try
            {
                // Kiểm tra nếu request null hoặc không có giá trị email và code
                if (string.IsNullOrEmpty(userRequest.email))
                {
                    return BadRequest(new ApiResponseDTO<string> { message = "Thông tin không được để trống." });
                }
                // Kiểm tra sự tồn tại của email
                bool checkEmailExists = await _resp.EmailExistsAsync(userRequest.email);
                if (checkEmailExists)
                {
                    // Nếu email đã tồn tại
                    bool isUpdated = await _resp.UpdateVerificationCodeIfExpiredAsync(userRequest.email);
                    if (!isUpdated)
                    {
                        var isValid = await _resp.Login(userRequest.email, null, false);
                        if (isValid != null)
                        {
                            var token = _jwtService.GenerateToken(isValid?.Id);
                            return Ok(new ApiResponseDTO<Object> { data = new { token }, message = "Đăng nhập thành công." });
                        }
                    }
                }
                else
                {
                    // Lưu mã xác thực mới vào cơ sở dữ liệu
                    //Users user = new Users
                    //{
                    //    email = userRequest.email,
                    //    passWord = "",
                    //    verificationCode = "",
                    //    isVerified = true
                    //};
                    var repUser = await _resp.Create(userRequest); // Cần cập nhật phương thức Create để nhận thêm expirationTime
                    if (repUser != null)
                    {
                        var token = _jwtService.GenerateToken(repUser?.Id);
                        return Ok(new ApiResponseDTO<Object> { data = new { token }, message = "Đăng nhập thành công." });
                    }

                }
                return BadRequest(new ApiResponseDTO<string> { message = "Đăng nhập thất bại." });
            }
            catch (Exception ex) { 
                return BadRequest(new ApiResponseDTO<string> { message = "Đăng nhập thất bại." });
            }

        }


    }
}
