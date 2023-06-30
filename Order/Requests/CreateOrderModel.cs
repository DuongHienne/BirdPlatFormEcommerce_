using System.ComponentModel.DataAnnotations;

namespace BirdPlatFormEcommerce.Order.Requests
{
    public class CreateOrderModel
    {
        [Required]
        [MinLength(1)]
        public IList<OrderDetailModel> Items { get; set; } = new List<OrderDetailModel>();

        public string? Note { get; set; }
        public int? AddressID { get; set; }
        
    }

    public class OrderDetailModel
    {
        [Required]
        public int ProductId { get; set; }

        [Required]
        [Range(1, Int32.MaxValue)]
        public int Quantity { get; set; }
       
    }
}
