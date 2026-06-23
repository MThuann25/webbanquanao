using System;

namespace ClothingShop.Domain.Entities
{
    public class Review
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string UserId { get; set; }
        public int? OrderId { get; set; }
        public int Rating { get; set; }
        public string Comment { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public bool IsApproved { get; set; } = true;

        // Navigation properties
        public virtual Product Product { get; set; }
        public virtual ApplicationUser User { get; set; }
        public virtual Order Order { get; set; }
    }
}
