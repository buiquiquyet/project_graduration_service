using asp.Respositories;
using Microsoft.AspNetCore.Mvc;

namespace asp.Services
{
    public interface IMomoService
    {
        Task<MomoCreatePaymentResponseModel> CreatePaymentAsync(OrderInfoModel orderInfo);
        Task<MomoExecuteResponseModel> PaymentExecuteAsync(IQueryCollection collection);
        Task<List<MomoExecuteResponseModel>> GetDonatesByProjectFundIdAsync(string projectFundId, int skipAmount, int pageSize);
        Task<long> CountAsync(string projectFundId);

        Task<List<MomoExecuteResponseModel>> GetTop3DonorsAsync();

        Task<byte[]> GenerateDonatesExcelAsync(string projectFundId, int skipAmount, int pageSize);
    }
}
