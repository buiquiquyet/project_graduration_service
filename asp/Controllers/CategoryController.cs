/*using asp.Services;*/
using asp.Models;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using asp.Services;
using asp.Helper;
using MongoDB.Bson;
using asp.Services.Category;
using asp.Helper.ApiResponse;
namespace asp.Controllers
{
    [ApiController]
    [Route("api/category")]
    public class CategoryController : Controller
    {
        private readonly CategoryService _resp;


       
        public CategoryController(CategoryService resp )
        {
            _resp = resp;
        }
        [HttpPost]
        public async Task<IActionResult> Create([FromForm] Categorys request)
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
        public async Task<IActionResult> GetAllCategorys(int page = 1, int size = 10, string? searchValue = null)
        {
            var skipAmount = (page - 1) * size;
            List<Categorys> datas;
            long totalCharityFunds;
            datas = await _resp.GetAllAsync(skipAmount, size, searchValue);
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
       
        [HttpGet("{id}")]
        public async Task<IActionResult> GetCategoryById(string id)
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
        public async Task<IActionResult> updateCategory(string id, [FromForm] Categorys updatedCharityFund)
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

        
    }
}
