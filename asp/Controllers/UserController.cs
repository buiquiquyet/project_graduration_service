/*using asp.Services;*/
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using asp.Helper;
using asp.Services.User;
using asp.Services.JWT;
using asp.Models.User;
using asp.Helper.ApiResponse;
using System.Data;
using asp.Constants;

namespace asp.Controllers
{
    [ApiController]
    [Route("api/user")]
    public class UserController : Controller
    {
        private readonly UserService _resp;

        private readonly JWTService _jwtService;

       
        public UserController(UserService resp, JWTService jwtService)
        {
            _resp = resp;
            _jwtService = jwtService;
        }
        [HttpGet("validate")]
        public async Task<IActionResult> ValidateToken(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                return BadRequest(new ApiResponseDTO<object> { data = new { error = "Error" }, message = "Xác thực thất bại." });
            }

            var userId = _jwtService.DecodeToken(token);
            if (string.IsNullOrEmpty(userId))
            {
                return BadRequest(new ApiResponseDTO<object> { data = new { error = "Error" }, message = "Xác thực thất bại." });
            }
            var dataUser = await _resp.GetByIdAsync(userId);

            return Ok(new ApiResponseDTO<Object> { data = new { userId, dataUser }, message = "Xác thực thành công." });
        }
        [HttpPut("{id}")]
        public async Task<IActionResult> updateUser(string id, [FromForm] Users updatedUser)
        {

            try
            {
                await _resp.UpdateAsync(id, updatedUser);
                return Ok(new ApiResponseDTO<object> { data = new { error = "Success" }, message = "Cập nhật thành công." });
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponseDTO<object> { data = new { error = "Error" }, message = "Cập nhật thất bại." });
            }
        }
        [HttpGet("{id}")]
        public async Task<IActionResult> GetUserById(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return BadRequest(new ApiResponseDTO<object> { data = new { error = "Error" }, message = "Lấy thông tin thất bại." });
            }

            var user = await _resp.GetByIdAsync(id);
            if (user == null)
            {
                return BadRequest(new ApiResponseDTO<object> { data = new { error = "Error" }, message = "Lấy thông tin thất bại." });
            }
            return Ok(new ApiResponseDTO<Object> { data = user , message = "Lấy thông tin thành công." });
        }
        [HttpPost("update-avatar/{id}")]
        public async Task<IActionResult> UpdateAvatarAsync(string id, [FromForm] IFormFile avatar)
        {
            // Kiểm tra file và id có hợp lệ không
            if (avatar == null)
            {
                return Ok(new ApiResponseDTO<object> { data = new { error = "Error" }, message = "Upload thất bại." });
            }

            if (string.IsNullOrEmpty(id))
            {
                return BadRequest(new ApiResponseDTO<object> { data = new { error = "Error" }, message = "Upload thất bại." });
            }

            try
            {
                // Gọi service để cập nhật avatar
                var isUpdated = await _resp.UpdateAvatarAsync(id, avatar);

                if (isUpdated)
                {
                    return Ok(new ApiResponseDTO<object> { data = new { success = "Success" }, message = "Cập nhật thành công." });
                }
                else
                {
                    return BadRequest(new ApiResponseDTO<object> { data = new { error = "Error" }, message = "Cập nhật thất bại." });
                }
            }
            catch (Exception ex)
            {
                // Xử lý lỗi
                return BadRequest(new ApiResponseDTO<object> { data = new { error = "Error" }, message = "Upload thất bại." });
            }
        }
        [HttpGet]
        public async Task<IActionResult> GetAllUsers(int page = 1, int size = 10, string? searchValue =  null)
        {
            var skipAmount = (page - 1) * size;
            List<Users> datas;
            long totalCharityFunds;
            datas = await _resp.GetAllAsync(skipAmount, size, searchValue);
            totalCharityFunds = await _resp.CountAsync();

            if (datas != null)
            {
                var response = new
                {
                    message = "success",
                    datas,
                    totalPages = (int)Math.Ceiling((double)totalCharityFunds / size),
                    currentPage = page,
                    totalRecords = totalCharityFunds
                };

                return Ok(response);
            }
            else
            {
                return BadRequest(new ApiResponseDTO<object> { data = new { error = "Error" }, message = "Đã xảy ra lỗi." });
            }
        }

        [HttpGet("emissary")]
        public async Task<IActionResult> GetAllUsersEmissary(int page = 1, int size = 10, string? searchValue = null, string? isEmissaryApproved = ApprovedConst.PROCESSING)
        {
            var skipAmount = (page - 1) * size;
            List<Users> datas;
            long totalCharityFunds;
            datas = await _resp.GetAllUserEmissaryAsync(skipAmount, size, searchValue, isEmissaryApproved);
            totalCharityFunds = await _resp.CountAsync();

            if (datas != null)
            {
                var response = new
                {
                    message = "success",
                    datas,
                    totalPages = (int)Math.Ceiling((double)totalCharityFunds / size),
                    currentPage = page,
                    totalRecords = totalCharityFunds
                };

                return Ok(response);
            }
            else
            {
                return BadRequest(new ApiResponseDTO<object> { data = new { error = "Error" }, message = "Đã xảy ra lỗi." });
            }
        }
        [HttpPut("update-emissary")]
        public async Task<IActionResult> UpdateApprovalStatus([FromBody] UserUpdateEmissary request)
        {
            if (request.userIds == null || request.userIds.Count == 0)
            {
                return BadRequest(new { message = "Duyệt thất bại." });
            }

            if (string.IsNullOrEmpty(request.newApprovalStatus))
            {
                return BadRequest(new { message = "Duyệt thất bại." });
            }

            // Kiểm tra trạng thái có hợp lệ không
            if (request.newApprovalStatus != ApprovedConst.APPROVED &&
                request.newApprovalStatus != ApprovedConst.PROCESSING &&
                request.newApprovalStatus != ApprovedConst.REJECTED)
            {
                return BadRequest(new { message = "Duyệt thất bại." });
            }

            // Gọi service để cập nhật nhiều người dùng
            await _resp.UpdateIsEmissaryApprovedAsync(request.userIds, request.newApprovalStatus);

            return Ok(new { message = "Duyệt thành công." });
        }
        [HttpGet("history-donate")]
        public async Task<IActionResult> GetDonatesByUserIdAsync(int page = 1, int size = 10, string? userId = "", string? searchValue = "")
        {
            var skipAmount = (page - 1) * size;
            List<MomoExecuteResponseModel> datas;
            long totalCharityFunds;
             (datas, totalCharityFunds) = await _resp.GetDonatesByUserIdAsync(userId, skipAmount, size, searchValue);

            if (datas != null)
            {
                var response = new
                {
                    message = "success",
                    datas,
                    totalPages = (int)Math.Ceiling((double)totalCharityFunds / size),
                    currentPage = page,
                    totalRecords = totalCharityFunds
                };

                return Ok(response);
            }
            else
            {
                return BadRequest(new ApiResponseDTO<object> { data = new { error = "Error" }, message = "Đã xảy ra lỗi." });
            }
        }
        [HttpDelete("deleteByIds")]
        public async Task<IActionResult> DeleteRecords([FromBody] List<string> ids)
        {
            if (ids == null || ids.Count == 0)
            {
                return BadRequest(new ApiResponseDTO<object> { data = new { error = "Error" }, message = "Danh sách ID không được để trống." });
            }

            try
            {
                var deletedCount = await _resp.DeleteByIdsAsync(ids);
                return Ok(new ApiResponseDTO<object> { data = new { error = "Success" }, message = $"Xóa thành công {deletedCount} người dùng." });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new ApiResponseDTO<object> { data = new { error = "Error" }, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponseDTO<object> { data = new { error = "Error" }, message = "Đã xảy ra lỗi trong quá trình xử lý yêu cầu." });
            }
        }
       
    }
}
