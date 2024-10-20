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
    [Route("api/instructor")]
    public class InstructorController : Controller
    {
        private readonly InstructorService _resp;

        public InstructorController(InstructorService resp)
        {
            _resp = resp;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllInstructors(int page = 1, int size = 10)
        {
            var skipAmount = (page - 1) * size;
            List<Instructors> datas;
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
        public async Task<IActionResult> GetIntstructorById(string id)
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
            List<Instructors> datas;
            long totalRecords;
            datas = await _resp.GetByUserIdAsync(userId, skipAmount, size);
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
        [HttpGet("department/{departmentId}")]
        public async Task<IActionResult> GetRecordByDepartmentId(string departmentId, int page = 1, int size = 10)
        {
            var skipAmount = (page - 1) * size;
            List<Instructors> datas;
            long totalRecords;
            datas = await _resp.GetByDepartmentIdAsync(departmentId, skipAmount, size);

            if (datas != null && datas.Count > 0)
            {
                var response = new
                {
                    message = "success",
                    datas,
                    totalPages = (int)Math.Ceiling((double)datas.Count / size),
                    currentPage = page,
                    totalRecords=datas.Count
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
        public async Task<IActionResult> CreateAsync([FromBody] Instructors newEntity)
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
        public async Task<IActionResult> UpdateIntstructor(string id, [FromBody] Instructors updatedIntstructor)
        {

            try
            {
                await _resp.UpdateAsync(id, updatedIntstructor);
                return Ok(new { message = "Cập nhật hồ sơ thành công." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteIntstructor(string id)
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
        public async Task<IActionResult> DeleteIntstructors(List<string> ids)
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
