using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace QuanLySanPhamApp.Models;

public class Category
{
    [Key]
    public int CategoryId { get; set; }
    
    [Required]
    [StringLength(50)]
    public string Name { get; set; } = string.Empty;
    
    public string? Description { get; set; }
    
    public int? ParentCategoryId { get; set; }
    
    public int DisplayOrder { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    // Navigation properties
    [ForeignKey("ParentCategoryId")]
    public Category? ParentCategory { get; set; }
    
    public ICollection<Category>? ChildCategories { get; set; }
    
    // Prevents serialization cycle by not including Products collection when Category is serialized
    [JsonIgnore]
    public ICollection<Product>? Products { get; set; }
}
