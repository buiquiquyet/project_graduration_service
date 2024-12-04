using asp.Respositories;

namespace asp.Services
{
    public interface IMomoService
    {
        Task<MomoCreatePaymentResponseModel> CreatePaymentAsync(OrderInfoModel orderInfo);
        Task<MomoExecuteResponseModel> PaymentExecuteAsync(IQueryCollection collection);
    }
}
