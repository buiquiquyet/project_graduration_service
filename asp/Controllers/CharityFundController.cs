/*using asp.Services;*/
using asp.Models;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using asp.Services;
using asp.Helper;
using MongoDB.Bson;
using asp.Services.Fund;
using asp.Helper.ApiResponse;
namespace asp.Controllers
{
    [ApiController]
    [Route("api/charity")]
    public class CharityFundController : Controller
    {
        private readonly CharityFundService _resp;


       
        public CharityFundController(CharityFundService resp )
        {
            _resp = resp;
        }
        [HttpPost]
        public async Task<IActionResult> Create([FromForm] CharityFunds request)
        {
            if (request == null)
            {
                return BadRequest(new ApiResponseDTO<object> { data = new { success = "Sucess" }, message = "Dữ liệu không hợp lệ." });
            }

            try
            {
                // Gọi service để tạo quỹ từ thiện
                var result = await _resp.Create(request);

                // Trả về mã 201 nếu tạo thành công
                return Ok(new ApiResponseDTO<object> { data = new { success = "Sucess" }, message = "Tạo quỹ mới thành công." });
            }
            catch (ArgumentNullException ex)
            {
                // Nếu có lỗi về việc thiếu tham số, trả về mã lỗi 400
                return BadRequest(new ApiResponseDTO<object> { data = new { error = "Error" }, message = "Tạo quỹ mới thất bại." });
            }
            catch (Exception ex)
            {
                // Nếu có lỗi không xác định, trả về mã lỗi 500
                return StatusCode(500, "Có lỗi xảy ra trong quá trình xử lý yêu cầu: " + ex.Message);
            }
        }
        [HttpGet]
        public async Task<IActionResult> GetAllCharityFunds(int page = 1, int size = 10)
        {
            var skipAmount = (page - 1) * size;
            List<CharityFunds> datas;
            long totalCharityFunds;
            datas = await _resp.GetAllAsync(skipAmount, size);
            totalCharityFunds = await _resp.CountAsync();

            if (datas != null )
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
        [HttpGet("selectionOptions")]
        public async Task<IActionResult> GetAllCharityFundsForOptions(int page = 1, int size = 10)
        {
            var skipAmount = (page - 1) * size;
            List<CharityFundsv2> datas;
            long totalCharityFunds;
            datas = await _resp.GetAllAsyncForOptions(skipAmount, size);
            totalCharityFunds = await _resp.CountAsync();

            if (datas != null && datas.Count > 0)
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
        [HttpGet("{id}")]
        public async Task<IActionResult> GetCharityFundById(string id)
        {
            if (!ObjectId.TryParse(id, out _))
            {
                return BadRequest(new ApiResponseDTO<object> { data = new { error = "Error" }, message = "Lấy thông tin thất bại." });
            }

            var user = await _resp.GetByIdAsync(id);
            if (user == null)
            {
                return BadRequest(new ApiResponseDTO<object> { data = new { error = "Error" }, message = "Lấy thông tin thất bại." });
            }
            var response = new
            {
                message = "success",
                data = user
            };
            return Ok(response);
        }
        [HttpPut("{id}")]
        public async Task<IActionResult> updateCharityFund(string id, [FromForm] CharityFunds updatedCharityFund)
        {

            try
            {
                await _resp.UpdateAsync(id, updatedCharityFund);
                return Ok(new ApiResponseDTO<object> { data = new { error = "Success" }, message = "Cập nhật thành công." });
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponseDTO<object> { data = new { error = "Error" }, message = "Cập nhật thất bại." });
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
                return Ok(new ApiResponseDTO<object> { data = new { error = "Success" }, message = $"Xóa thành công {deletedCount} quỹ." });
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

        //[HttpPut("{id}")]
        //public async Task<IActionResult> updateUser(string id, [FromBody] Users updatedUser)
        //{

        //    try
        //    {
        //        await _resp.UpdateAsync(id, updatedUser);
        //        return Ok(new ApiResponseDTO<object> { data = new { error = "Success" }, message = "Cập nhật thành công." });
        //    }
        //    catch (Exception ex)
        //    {
        //        return Ok(new ApiResponseDTO<object> { data = new { error = "Error" }, message = "Cập nhật thất bại." });
        //    }
        //}
        //[HttpPost("update-avatar/{id}")]
        //public async Task<IActionResult> UpdateAvatarAsync(string id, [FromForm] IFormFile avatar)
        //{
        //    // Kiểm tra file và id có hợp lệ không
        //    if (avatar == null)
        //    {
        //        return Ok(new ApiResponseDTO<object> { data = new { error = "Error" }, message = "Upload thất bại." });
        //    }

        //    if (string.IsNullOrEmpty(id))
        //    {
        //        return Ok(new ApiResponseDTO<object> { data = new { error = "Error" }, message = "Upload thất bại." });
        //    }

        //    try
        //    {
        //        // Gọi service để cập nhật avatar
        //        var isUpdated = await _resp.UpdateAvatarAsync(id, avatar);

        //        if (isUpdated)
        //        {
        //            return Ok(new ApiResponseDTO<object> { data = new { error = "Success" }, message = "Cập nhật thành công." });
        //        }
        //        else
        //        {
        //            return Ok(new ApiResponseDTO<object> { data = new { error = "Error" }, message = "Cập nhật thất bại." });
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        // Xử lý lỗi
        //        return Ok(new ApiResponseDTO<object> { data = new { error = "Error" }, message = "Upload thất bại." });
        //    }
        //}


        //[HttpGet]
        //public async Task<IActionResult> GetAllUsers(int page = 1 , int size = 10 )
        //{
        //    var skipAmount = (page - 1) * size;
        //    List<Users> datas;
        //    long totalUsers ;
        //    datas = await _resp.GetAllAsync(skipAmount, size);
        //    totalUsers = await _resp.CountAsync();

        //    if (datas != null && datas.Count > 0)
        //    {
        //        var response = new
        //        {
        //            message = "success",
        //            datas,
        //            totalPages = (int)Math.Ceiling((double)totalUsers / size),
        //            currentPage = page,
        //            totalRecords = totalUsers
        //        };

        //        return Ok(response);
        //    }
        //    else
        //    {
        //        var errorObject = new { error = "Đã xảy ra lỗi" };
        //        return Json(errorObject);
        //    }
        //}

        //[HttpGet("tendangnhap/{tendangnhap}")]
        //public async Task<IActionResult> GetByTenDangNhapAsync(string tendangnhap)
        //{


        //    var user = await _resp.GetByTenDangNhapAsync(tendangnhap);
        //    if (user == null)
        //    {
        //        return NotFound();
        //    }
        //    var response = new {
        //        message = "success",
        //        data = user,
        //    };
        //    return Ok(response);
        //}
        //[HttpGet("department/{idDepartment}")]
        //public async Task<IActionResult> GetByIdDepartmentAsync(string idDepartment)
        //{
        //    var user = await _resp.GetByIdDepartmentAsync(idDepartment);
        //    if (user == null)
        //    {
        //        return NotFound();
        //    }
        //    return Ok(user);
        //}
        //private bool IsValidObjectId(string id)
        //{
        //    return ObjectId.TryParse(id, out _);
        //}
        //[HttpPost("/api/auth/login")]
        //public async Task<IActionResult> GetUser([FromBody] Users model)
        //{
        //    if (!ModelState.IsValid)
        //    {
        //        return BadRequest(new { errorMessage = "Tên đăng nhập hoặc mật khẩu không chính xác. Xin vui lòng thử lại." });
        //    }

        //    var user = await _resp.GetUserByTenDangNhapAndPassword(model.email, model.pass);
        //    if (user == null)
        //    {
        //        return Ok(new { errorMessage = "Tên đăng nhập hoặc mật khẩu không chính xác. Xin vui lòng thử lại." });
        //    }
        //    var token = _jwtService.GenerateToken(user.Id);
        //    return Ok(new { token, role = user.Id });

        //}

        //[HttpPost]
        //public async Task<IActionResult> CreateAsync([FromBody] Users newEntity)
        //{
        //    if (newEntity == null)
        //    {
        //        return BadRequest();
        //    }
        //    try
        //    {
        //        await _resp.CreateAsync(newEntity);
        //        var response = new { message = "Thêm người dùng thành công" };
        //        return Ok(response);
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, "Internal server error");
        //    }
        //}
        //[HttpPost("createMany")]
        //public async Task<IActionResult> InsertManyUsers(List<Users> users)
        //{
        //    try
        //    {
        //        if(users.Count > 0)
        //        {
        //            long insertedCount = await _resp.CreatetManyAsync(users);
        //            return Ok(new { message = $"Đã thêm thành công {insertedCount} người dùng." });

        //        }
        //        else
        //        {
        //            return BadRequest("Danh sách người dùng rỗng.");
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(StatusCodes.Status500InternalServerError, $"Đã xảy ra lỗi: {ex.Message}");
        //    }
        //}




        //[HttpDelete("{id}")]
        //public async Task<IActionResult> DeleteUser(string id)
        //{
        //    try
        //    {
        //        await _resp.RemoveAsync(id);
        //        return Ok(new {message = "Xóa người dùng thành công."}); 
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, $"Internal server error: {ex.Message}");
        //    }
        //}
        //[HttpDelete("deleteByIds")]
        //public async Task<IActionResult> DeleteUsers(List<string> ids)
        //{
        //    try
        //    {
        //        var deletedCount = await _resp.DeleteByIdsAsync(ids);
        //        var response = new { message = $"Xóa thành công {deletedCount} người dùng" };
        //        return Ok(response);
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, ex.Message); 
        //    }
        //}
    }
}
