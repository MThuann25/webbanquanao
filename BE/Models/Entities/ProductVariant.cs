namespace ClothingShop.Domain.Entities
{
    public class ProductVariant
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string Size { get; set; }
        public string Color { get; set; }
        public int Quantity { get; set; }
        public string SKU { get; set; }

        // Navigation property
        public virtual Product Product { get; set; }
    }
}
