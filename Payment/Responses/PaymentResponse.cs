using BirdPlatFormEcommerce.DEntity;

namespace BirdPlatFormEcommerce.Payment.Responses
{
    public class PaymentResponse
    {
        public int PaymentId { get; set; }

        public int UserId { get; set; }

        public string? PaymentMethod { get; set; }

        public DateTime? PaymentDate { get; set; }

        public decimal? Amount { get; set; }

        public string? PaymentUrl { get; set; }

    }
}
