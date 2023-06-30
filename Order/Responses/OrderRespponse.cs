using BirdPlatFormEcommerce.DEntity;
using BirdPlatFormEcommerce.Payment.Responses;

namespace BirdPlatFormEcommerce.Order.Responses
{
    public class OrderRespponse
    {
        public int OrderId { get; set; }

        public bool? Status { get; set; }

        public int UserId { get; set; }

        public string? Note { get; set; }
        public int? AddressID { get; set; }
        public decimal TotalPrice { get; set; }

        public DateTime? OrderDate { get; set; }
        public int ShopId { get; set; }
        public virtual PaymentResponse? Payment { get; set; }

        public virtual ICollection<OrderDetailResponse> Items { get; set; } = new List<OrderDetailResponse>();

       
    }
    
    public class OrderResponses
    {
        public int OrderId { get; set; }
        public decimal TotalPrice { get; set; }
        public decimal SubTotal { get; set; }
        public List<OrderItemResponse> Items { get; set; }
    }

    public class OrderItemResponse
    {
        public decimal Price { get; set; }
        public decimal? SoldPrice { get; set; }
        public string? ShopName { get; set; }
        public int ShopId { get; set; }
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public string? ProductName { get; set; }
        public string? ImagePath { get; set; }
    }

    public class OrderDetailResponse
    {
        public int Id { get; set; }

        public int ProductId { get; set; }

        public int? Quantity { get; set; }

        public int? Discount { get; set; }

        public decimal? ProductPrice { get; set; }

        public decimal? DiscountPrice { get; set; }

        public decimal? SubTotal { get; set; }
    }
    public class OrderResult
    {
        public int OrderID { get; set; }
        public List<ShopOrder> Shops { get; set; }
    }

    public class ShopOrder
    {
        public int ShopID { get; set; }
        public string PaymentMethod { get; set; }
        public string ShopName { get; set; }
        public DateTime DateOrder { get; set; }
        public int AddressId { get; set; }
        public string Note { get; set; }
        public string Address { get; set; }
        public string? AddressDetail { get; set; }
        public string? Phone { get; set; }
        public string? NameRg { get; set; }
        public List<OrderItem> Items { get; set; }
    }

    public class OrderItem
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public int Quantity { get; set; }
        public decimal ProductPrice { get; set; }
        public decimal DiscountPrice { get; set; }
        public decimal Total { get; set; }
        public string FirstImagePath { get; set; }
        
    }
    public class OrderInfo
    {
        public int orderId { get; set; }
      
        public DateTime OrderDate { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public bool Status { get; set; }
       
        public TbUser User { get; set; }
    }
    public class UserOder
    {
        public int UserId { get; set; }
        public string PaymentMethod { get; set; }
       
        public DateTime DateOrder { get; set; }
        public int AddressId { get; set; }
        public string Note { get; set; }
        public string Address { get; set; }
        public string? AddressDetail { get; set; }
        public string? Phone { get; set; }
        public string? NameRg { get; set; }
        public List<OrderItem> Items { get; set; }
    }

    public class UserItem
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public int Quantity { get; set; }
        public decimal ProductPrice { get; set; }
        public decimal DiscountPrice { get; set; }
        public decimal Total { get; set; }
        public string FirstImagePath { get; set; }

    }
    public class UserResult
    {
        public int OrderID { get; set; }
        public List<UserOder> useroder { get; set; }
    }
}
