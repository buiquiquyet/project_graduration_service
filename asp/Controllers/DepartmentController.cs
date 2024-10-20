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
    [Route("api/department")]
    public class DepartmentController : Controller
    {
        private readonly DepartmentService _resp;

        public DepartmentController(DepartmentService resp)
        {
            _resp = resp;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllDepartment()
        {
            List<Departments> datas;
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
        public async Task<IActionResult> GetDepartment(string id)
        {
            if (!IsValidObjectId(id))
            {
                return BadRequest("Invalid ObjectId format.");
            }

            var department = await _resp.GetByIdAsync(id);
            if (department == null)
            {
                return NotFound();
            }
            var response = new
            {
                message = "success",
                datas = department
            };
            return Ok(response);
        }
        [HttpGet("departmentId/{idDepartment}")]
        public async Task<IActionResult> GetByIdDepartmentAsync(string idDepartment)
        {
            

            var department = await _resp.GetByIdDepartmentAsync(idDepartment);
            if (department == null)
            {
                return NotFound();
            }
            var response = new
            {
                message = "success",
                datas = department
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
