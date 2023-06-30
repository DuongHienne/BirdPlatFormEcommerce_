using BirdPlatFormEcommerce.DEntity;
using BirdPlatFormEcommerce.Payment.Requests;

namespace BirdPlatFormEcommerce.Payment
{
    public interface IVnPayService
    {
        string CreatePaymentUrl(TbPayment payment);
        
    }
}
