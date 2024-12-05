/*using asp.Services;*/
using asp.Models;
using asp.Respositories;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using asp.Services;
using asp.Helper;

namespace asp.Controllers 
{
    [ApiController]
    [Route("api/comment")]
    public class CommentController : Controller
    {
        private readonly CommentService _resp;


       
        public CommentController(CommentService resp)
        {
            _resp = resp;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromForm] Comments request)
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
                return Ok(new ApiResponseDTO<object> { data = new { success = "Sucess" }, message = "Gửi bình luận thành công." });
            }
            catch (ArgumentNullException ex)
            {
                // Nếu có lỗi về việc thiếu tham số, trả về mã lỗi 400
                return BadRequest(new ApiResponseDTO<object> { data = new { error = "Error" }, message = "Đã có lỗi xảy ra." });
            }
            catch (Exception ex)
            {
                // Nếu có lỗi không xác định, trả về mã lỗi 500
                return StatusCode(500, "Có lỗi xảy ra trong quá trình xử lý yêu cầu: " + ex.Message);
            }
        }
        [HttpGet("byProjectFundId/{projectFundId}")]
        public async Task<ActionResult<List<Comments>>> GetCommentsByProjectFundId(string projectFundId, int page = 1, int size = 10)
        {
            var skipAmount = (page - 1) * size;
            List<Comments> datas;
            long totalProjectFunds;
            datas = await _resp.GetCommentsByProjectFundIdAsync(projectFundId, skipAmount, size);
            totalProjectFunds = await _resp.CountAsync(projectFundId);

            if (datas != null )
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
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteComment(string id)
        {
            try
            {
                await _resp.RemoveAsync(id);
                return Ok(new ApiResponseDTO<object> { data = new { success = "Success" }, message = "Xóa thành công." });
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponseDTO<object> { data = new { error = "Error" }, message = "Đã xảy ra lỗi." });
            }
        }

    }
}
