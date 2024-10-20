/*using asp.Services;*/
using asp.Models;
using asp.Respositories;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;


namespace asp.Controllers
{
    [ApiController]
    [Route("api/subject")]
    public class SubjecController : Controller
    {
        private readonly SubjectService _resp;



        public SubjecController(SubjectService resp)
        {
            _resp = resp;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllSubjects()
        {
            
            List<Subjects> datas;
            datas = await _resp.GetAllAsync();
            

            if (datas != null && datas.Count > 0)
            {
                var response = new
                {
                    message = "success",
                    datas,
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
        public async Task<IActionResult> GetSubjectById(string id)
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
        [HttpGet("user/{userTdn}")]
        public async Task<IActionResult> GetSubjectByUserId(string userTdn)
        {
            var record = await _resp.GetByUserIdAsync(userTdn);
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
        [HttpGet("department/{departmentId}")]
        public async Task<IActionResult> GetSubjectByDepartmentId(string departmentId)
        {
            var record = await _resp.GetByDepartmentIdAsync(departmentId);
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

    }
}
