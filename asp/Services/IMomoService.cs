using asp.Respositories;

namespace asp.Services
{
    public interface IMomoService
    {
        Task<MomoCreatePaymentResponseModel> CreatePaymentAsync(OrderInfoModel orderInfo);
        MomoExecuteResponseModel PaymentExecuteAsync(IQueryCollection collection);
    }
}
