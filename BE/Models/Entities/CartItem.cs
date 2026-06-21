namespace ClothingShop.Domain.Entities
{
    public class CartItem
    {
        public int Id { get; set; }
        public int CartId { get; set; }
        public int ProductVariantId { get; set; }
        public int Quantity { get; set; } = 1;

        // Navigation properties
        public virtual Cart Cart { get; set; }
        public virtual ProductVariant ProductVariant { get; set; }
    }
}
