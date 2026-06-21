namespace ClothingShop.Domain.Entities
{
    public class ProductImage
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string ImageUrl { get; set; }
        public bool IsMain { get; set; } = false;

        // Navigation property
        public virtual Product Product { get; set; }
    }
}
