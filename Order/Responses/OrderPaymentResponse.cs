using BirdPlatFormEcommerce.DEntity;

namespace BirdPlatFormEcommerce.Order.Responses
{
    public class OrderPaymentResponse
    {
        public int PaymentId { get; set; }

        public int UserId { get; set; }

        public string? PaymentMethod { get; set; }

        public DateTime? PaymentDate { get; set; }

        public decimal? Amount { get; set; }

    }
}
