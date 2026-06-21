using System;
using System.Collections.Generic;

namespace ClothingShop.Domain.Entities
{
    public class Order
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public DateTime OrderDate { get; set; } = DateTime.UtcNow;
        public string Status { get; set; } // Chờ xác nhận, Đang giao, Hoàn thành, Đã hủy
        public decimal TotalAmount { get; set; }
        public decimal DiscountAmount { get; set; } = 0;
        public string ShippingAddress { get; set; }
        public string PaymentMethod { get; set; } // COD, VNPay
        public string VoucherCode { get; set; }

        // Navigation properties
        public virtual ApplicationUser User { get; set; }
        public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
    }
}
