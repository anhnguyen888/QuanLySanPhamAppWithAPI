using System;

namespace QuanLySanPhamApp.Models
{
    // This model will be serialized to/from session
    [Serializable]
    public class CartItem
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public DateTime DateCreated { get; set; }
        
        // We don't include navigation properties when storing in session
        // The Product property is removed to avoid serialization issues
    }
}
