/*using asp.Services;*/
using asp.Models;
using asp.Respositories;
using asp.Models;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using System.Threading.Tasks;
using MongoDB.Bson;

namespace asp.Controllers 
{
    [ApiController]
    [Route("api/class")]
    public class ClassController : Controller
    {
        private readonly ClassService _resp;

        public ClassController(ClassService resp)
        {
            _resp = resp;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllClasses()
        {
            List<Classes> datas;
            datas = await _resp.GetAllAsync();

            if (datas != null && datas.Count > 0)
            {
                var response = new
                {
                    message = "success",
                    datas
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
        public async Task<IActionResult> GetClass(string id)
        {
            if (!IsValidObjectId(id))
            {
                return BadRequest("Invalid ObjectId format.");
            }

            var classes = await _resp.GetByIdAsync(id);
            if (classes == null)
            {
                return NotFound();
            }
            var response = new
            {
                message = "success",
                datas = classes
            };
            return Ok(response);
        }
        [HttpGet("user/{tendangnhapUser}")]
        public async Task<IActionResult> GetClassByIdKhoa(string tendangnhapUser)
        {
            var classes = await _resp.GetByUserAsync(tendangnhapUser);
            if (classes == null)
            {
                return NotFound();
            }
            var response = new {
                message = "success",
                datas = classes
            };
            return Ok(response);
        }
        [HttpGet("department/{idKhoa}")]
        public async Task<IActionResult> GetByIdKhoaAsync(string idKhoa)
        {
            var classes = await _resp.GetByIdKhoaAsync(idKhoa);
            if (classes == null)
            {
                return NotFound();
            }
            var response = new
            {
                message = "success",
                datas = classes
            };
            return Ok(response);
        }
        private bool IsValidObjectId(string id)
        {
            return ObjectId.TryParse(id, out _);
        }
        /*[HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(string id, Users updatedUser)
        {
            await _resp.UpdateAsync(id, updatedUser);
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(string id)
        {
            await _resp.RemoveAsync(id);
            return NoContent();
        }*/
    }
}
