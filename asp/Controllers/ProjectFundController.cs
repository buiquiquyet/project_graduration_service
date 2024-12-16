/*using asp.Services;*/
using asp.Models;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using asp.Services;
using asp.Helper;
using MongoDB.Bson;
using asp.Services.ProjectFundDone;
using asp.Helper.ApiResponse;
using asp.Constants.ProjectFundConst;
using asp.Respositories;
using asp.Models.ProjectFund;
namespace asp.Controllers
{
    [ApiController]
    [Route("api/project-fund")]
    public class ProjectFundController : Controller
    {
        private readonly ProjectFundService _resp;


       
        public ProjectFundController(ProjectFundService resp )
        {
            _resp = resp;
        }
        [HttpPost]
        public async Task<IActionResult> Create([FromForm] ProjectFunds request)
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
                return Ok(new ApiResponseDTO<object> { data = new { success = "Sucess" }, message = "Tạo dự án mới thành công." });
            }
            catch (ArgumentNullException ex)
            {
                // Nếu có lỗi về việc thiếu tham số, trả về mã lỗi 400
                return BadRequest(new ApiResponseDTO<object> { data = new { error = "Error" }, message = "Tạo dự án mới thất bại." });
            }
            catch (Exception ex)
            {
                // Nếu có lỗi không xác định, trả về mã lỗi 500
                return StatusCode(500, "Có lỗi xảy ra trong quá trình xử lý yêu cầu: " + ex.Message);
            }
        }
        [HttpGet]
        public async Task<IActionResult> GetAllProjectFunds(int page = 1, int size = 10, string filterType = FilterListProjectFund.ALL, string? fundId = null, string? searchValue = null)
        {
            var skipAmount = (page - 1) * size;
            List<ProjectFunds> datas;
            long totalProjectFunds;
            datas = await _resp.GetAllAsync(skipAmount, size, filterType, fundId, searchValue);
            totalProjectFunds = await _resp.CountAsync();

            if (datas != null)
            {
                var response = new
                {
                    message = "success",
                    datas,
                    totalPages = (int)Math.Ceiling((double)totalProjectFunds / size),
                    currentPage = page,
                    totalRecords = totalProjectFunds
                };

                return Ok(response);
            }
            else
            {
                return BadRequest(new ApiResponseDTO<object> { data = new { error = "Error" }, message = "Đã xảy ra lỗi." });
            }
        }

        //[HttpGet]
        //public async Task<IActionResult> GetAllProjectFunds(int page = 1, int size = 10, string filterType = FilterListProjectFund.ALL)
        //{
        //    var skipAmount = (page - 1) * size;
        //    List<ProjectFunds> datas;
        //    long totalProjectFunds;
        //    datas = await _resp.GetAllAsync(skipAmount, size, filterType);
        //    totalProjectFunds = await _resp.CountAsync();

        //    if (datas != null )
        //    {
        //        var response = new
        //        {
        //            message = "success",
        //            datas,
        //            totalPages = (int)Math.Ceiling((double)totalProjectFunds / size),
        //            currentPage = page,
        //            totalRecords = totalProjectFunds
        //        };

        //        return Ok(response);
        //    }
        //    else
        //    {
        //        return BadRequest(new ApiResponseDTO<object> { data = new { error = "Error" }, message = "Đã xảy ra lỗi." });
        //    }
        //}

        [HttpGet("{id}")]
        public async Task<IActionResult> GetProjectFundById(string id)
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
        public async Task<IActionResult> updateProjectFund(string id, [FromForm] ProjectFunds updatedProjectFund)
        {

            try
            {
                await _resp.UpdateAsync(id, updatedProjectFund);
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
                return Ok(new ApiResponseDTO<object> { data = new { error = "Success" }, message = $"Xóa thành công {deletedCount} dự án." });
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

        // API like project fund
        [HttpPost("like")]
        public async Task<IActionResult> LikeProject([FromBody] LikeProjectFund dto)
        {
            bool result = await _resp.LikeProjectAsync(dto);
            if (result)
            {
                return Ok(new ApiResponseDTO<object> { data = new { success = "Success" }, message = "Thả tim thành công." });
            }
            return BadRequest(new { message = "Dự án không tồn tại hoặc đã thả tim rồi" });
        }

        // API unlike project fund
        [HttpPost("unlike")]
        public async Task<IActionResult> UnlikeProject([FromBody] LikeProjectFund dto)
        {
            bool result = await _resp.UnlikeProjectAsync(dto);
            if (result)
            {
                return Ok(new ApiResponseDTO<object> { data = new { success = "Success" }, message = "Bỏ thả tim thành công." });
            }
            return BadRequest(new { message = "Dự án không tồn tại hoặc bạn chưa thả tim" });
        }

        // API lấy số lượng thả tim
        [HttpGet("countLikes/{projectId}")]
        public async Task<IActionResult> GetLikesCount(string projectId)
        {
            int likesCount = await _resp.GetLikesCountAsync(projectId);
            if (likesCount >= 0)
            {
                return Ok(new ApiResponseDTO<object> { data = likesCount, message = "Bỏ thả tim thành công." });
            }
            return BadRequest(new { message = "Dự án không tồn tại" });
        }
    }
}
