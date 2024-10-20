using asp.DTO;
using asp.Models;
using asp.Respositories;
using asp.Services;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;

namespace asp.Controllers
{

    [ApiController]
    [Route("api/file")]
    public class FileController : Controller
    {
            private readonly FileService _resp;

            public FileController(FileService resp)
            {
                _resp = resp;
            }

            [HttpGet("{id}")]
            public async Task<IActionResult> GetFileById(string id)
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
            [HttpGet("count")]
            public async Task<long> GetFileCount()
            {
                return await _resp.CountAsync();
            }

            [HttpGet("record/{id}")]
            public async Task<IActionResult> GetFilesByRecordId(string id)
            {
                try
                {
                    var files = await _resp.GetByRecordIdAsync(id);
                    if (files == null || files.Count == 0)
                    {
                    return Ok(new { error="Error" });
                }
                    return Ok(new { message="Success", countFile= files.Count, files });
                }
                catch (Exception ex)
                {
                    return BadRequest(ex.Message);
                }
            }
            [HttpPost]
            public async Task<IActionResult> CreateAsync([FromForm] FileDTO newEntities)
            {
                try
                {
                    await _resp.CreateAsync(newEntities);
                    var response = new { message = "Success" };
                    return Ok(response);
                }
                catch (Exception ex)
                {
                    return StatusCode(500, "Lỗi khi thêm file: " + ex.Message);
                }
            }


            [HttpPut("{id}")]
            public async Task<IActionResult> UpdateFile(string id, [FromBody] Files updatedRecord)
            {

                try
                {
                    await _resp.UpdateAsync(id, updatedRecord);
                    return Ok(new { message = "Cập nhật file thành công." });
                }
                catch (Exception ex)
                {
                    return StatusCode(500, $"Internal server error: {ex.Message}");
                }
            }

            [HttpDelete("{id}")]
            public async Task<IActionResult> DeleteFile(string id)
            {
                try
                {
                    await _resp.RemoveAsync(id);
                    return Ok(new { message = "Xóa file  thành công." });
                }
                catch (Exception ex)
                {
                    return StatusCode(500, $"Internal server error: {ex.Message}");
                }
            }
            [HttpDelete("deleteByIds")]
            public async Task<IActionResult> DeleteFiles(List<string> ids)
            {
                try
                {
                    var deletedCount = await _resp.DeleteByIdsAsync(ids);
                    var response = new { message = $"Xóa thành công {deletedCount} file" };
                    return Ok(response);
                }
                catch (Exception ex)
                {
                    return StatusCode(500, ex.Message);
                }
            }
        [HttpDelete("profile/deleteByIds")]
        public async Task<IActionResult> DeleteFileByProfileIds(List<string> profileIds)
        {
            try
            {
                var deletedCount = await _resp.DeleteByProfileIdsAsync(profileIds);
                var response = new { message = $"Xóa thành công {deletedCount} file" };
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
    }
}
