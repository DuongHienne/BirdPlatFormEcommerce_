namespace BirdPlatFormEcommerce.Product
{
    public class HomeViewProductModel
    {
        public int ProductId { get; set; }

        public string ProductName { get; set; }

        public string? CateName { get; set; }
        public bool? Status { get; set; }

        public decimal Price { get; set; }
        public decimal? DiscountPercent { get; set; }
        public decimal? SoldPrice { get; set; }
        public int ShopId { get; set; }
        public string ShopName { get; set; }
        public int? QuantitySold { get; set; }
        public int? Quantity { get; set; }
        public int? Rate { get; set; }

        public string? Address { get; set; }
        public string? Thumbnail { get; set; }






    }
}
