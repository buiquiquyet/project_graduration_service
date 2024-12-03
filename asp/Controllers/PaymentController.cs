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
    [Route("api/payment")]
    public class PaymentController : Controller
    {



        private IMomoService _momoService;
        //private readonly IVnPayService _vnPayService;
        public PaymentController(IMomoService momoService)
        {
            _momoService = momoService;

        }
        [HttpPost]
        [Route("CreatePaymentUrl")]
        public async Task<IActionResult> CreatePaymentUrl(OrderInfoModel model)
        {
            var response = await _momoService.CreatePaymentAsync(model);
            return Redirect(response.PayUrl);
        }

    }
}
