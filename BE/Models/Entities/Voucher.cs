using System;

namespace ClothingShop.Domain.Entities
{
    public class Voucher
    {
        public int Id { get; set; }
        public string Code { get; set; }
        public int? DiscountPercent { get; set; }
        public decimal? DiscountAmount { get; set; }
        public DateTime ExpiryDate { get; set; }
        public int UsageLimit { get; set; }
        public int UsedCount { get; set; }
    }
}
