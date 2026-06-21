using System.Collections.Generic;

namespace ClothingShop.Domain.Entities
{
    public class Brand
    {
        public int Id { get; set; }
        public string Name { get; set; }

        // Navigation properties
        public virtual ICollection<Product> Products { get; set; } = new List<Product>();
    }
}
