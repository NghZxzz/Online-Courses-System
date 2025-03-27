using DoAnChuyennNganh.Models.Vnpay;

namespace DoAnChuyennNganh.Services.VnPay
{
    public interface IVnPayService
    {
        Task<string> CreatePaymentUrl(PaymentInformationModel model, HttpContext context,  string userId);
        PaymentResponseModel PaymentExecute(IQueryCollection collections);

    }
}
