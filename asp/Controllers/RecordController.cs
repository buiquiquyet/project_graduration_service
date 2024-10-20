/*using asp.Services;*/
using asp.Models;
using asp.Respositories;
using asp.Models;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using System.Threading.Tasks;
using MongoDB.Bson;
using asp.Services;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using System.Data;

namespace asp.Controllers
{
    [ApiController]
    [Route("api/record")]
    public class RecordController : Controller
    {
        private readonly RecordService _resp;

        public RecordController(RecordService resp)
        {
            _resp = resp;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllRecords(int page = 1, int size = 10)
        {
            var skipAmount = (page - 1) * size;
            List<Records> datas;
            long totalRecords;
            datas = await _resp.GetAllAsync(skipAmount, size);
            totalRecords = await _resp.CountAsync();

            if (datas != null && datas.Count > 0)
            {
                var response = new
                {
                    message = "success",
                    datas,
                    totalPages = (int)Math.Ceiling((double)totalRecords / size),
                    currentPage = page,
                    totalRecords
                };

                return Ok(response);
            }
            else
            {
                var errorObject = new { error = "Đã xảy ra lỗi" };
                return Json(errorObject);
            }
        }
        [HttpGet("{id}")]
        public async Task<IActionResult> GetRecordById(string id)
        {
            if (!IsValidObjectId(id))
            {
                return BadRequest("Invalid ObjectId format.");
            }

            var record = await _resp.GetByIdAsync(id);
            if (record == null)
            {
                return NotFound();
            }
            var response = new
            {
                message = "success",
                data = record
            };
            return Ok(response);
        }
        private bool IsValidObjectId(string id)
        {
            return ObjectId.TryParse(id, out _);
        }
        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetRecordByUserId(string userId, int page = 1, int size = 10)
        {
            var skipAmount = (page - 1) * size;
            List<Records> datas;
            long totalRecords;
            datas = await _resp.GetByUserIdAsync(userId,skipAmount, size);
            totalRecords = await _resp.CountAsync();

            if (datas != null && datas.Count > 0)
            {
                var response = new
                {
                    message = "success",
                    datas,
                    totalPages = (int)Math.Ceiling((double)totalRecords / size),
                    currentPage = page,
                    totalRecords
                };

                return Ok(response);
            }
            else
            {
                var errorObject = new { error = "Đã xảy ra lỗi" };
                return Json(errorObject);
            }
        }
        [HttpGet("tbt/{departmentId}/{subjectId}")]
        public async Task<IActionResult> GetRecordByDepartmentSubjectId(string departmentId, string subjectId, int page = 1, int size = 10)
        {
            var skipAmount = (page - 1) * size;
            List<Records> datas;
            long totalRecords;
            datas = await _resp.GetByDepartmentSubjectIdAsync(departmentId, subjectId, skipAmount, size);

            if (datas != null && datas.Count > 0)
            {
                var response = new
                {
                    message = "success",
                    datas,
                    totalPages = (int)Math.Ceiling((double)datas.Count / size),
                    currentPage = page,
                    totalRecords = datas.Count
                };

                return Ok(response);
            }
            else
            {
                var errorObject = new { error = "Đã xảy ra lỗi" };
                return Json(errorObject);
            }
        }
        [HttpPost]
        public async Task<IActionResult> CreateAsync([FromBody] Records newEntity)
        {
            if (newEntity == null)
            {
                return BadRequest();
            }
            try
            {
                var profileId = await _resp.CreateAsync(newEntity);
                var response = new { message = "Thêm hồ sơ thành công", profileId };
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateRecord(string id, [FromBody] Records updatedRecord)
        {

            try
            {
                await _resp.UpdateAsync(id, updatedRecord);
                return Ok(new { message = "Cập nhật hồ sơ thành công." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
        [HttpPut("note/{id}")]
        public async Task<IActionResult> UpdateNoteRecord(string id,[FromBody] string note)
        {

            try
            {
                await _resp.UpdateNoteAsync(id, note);
                return Ok(new { message = "Cập nhật ghi chú hồ sơ thành công." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
        [HttpPut("updateChecks")]
        public async Task<IActionResult> UpdateCheckRecords(List<string> ids, string updateChecks)
        {
            try
            {
                var count = await _resp.UpdateCheckAsync(ids, updateChecks);
                return Ok(new { message = $"Duyệt thành công {count}  hồ sơ." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRecord(string id)
        {
            try
            {
                await _resp.RemoveAsync(id);
                return Ok(new { message = "Xóa hồ sơ  thành công." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
        [HttpDelete("deleteByIds")]
        public async Task<IActionResult> DeleteRecords(List<string> ids)
        {
            try
            {
                var deletedCount = await _resp.DeleteByIdsAsync(ids);
                var response = new { message = $"Xóa thành công {deletedCount} hồ sơ" };
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
    }
}
