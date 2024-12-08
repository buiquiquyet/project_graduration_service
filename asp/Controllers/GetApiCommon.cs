/*using asp.Services;*/
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using asp.Helper;
using asp.Services.User;
using asp.Services.JWT;
using asp.Models.User;
using asp.Helper.ApiResponse;
using System.Data;
using asp.Services.ProjectFundDone;

namespace asp.Controllers
{
    [ApiController]
    [Route("api/statistics")]
    public class GetApiCommon : Controller
    {
        private readonly GetCommonApiService _resp;


       
        public GetApiCommon(GetCommonApiService resp)
        {
            _resp = resp;
        }
        [HttpGet]
        public async Task<IActionResult> GetFundsStatistics()
        {
            var statistics = await _resp.GetFundsStatisticsAsync();
            return Ok(new ApiResponseDTO<Object> { data = statistics, message = "Success" });
        }
    }
}
