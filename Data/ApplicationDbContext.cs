using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using QuanLySanPhamApp.Models.Identity;
using QuanLySanPhamApp.Models;

namespace QuanLySanPhamApp.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, string>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Product> Products { get; set; }
    public DbSet<Category> Categories { get; set; }
    // We've removed CartItems as it's now managed through session

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        
        // Custom configurations for your identity models can be added here
        
        // Configure unique constraint for Product SKU
        // builder.Entity<Product>()
        //     .HasIndex(p => p.SKU)
        //     .IsUnique();
        
        // Configure one-to-many relationship between Category and Product
        builder.Entity<Category>()
            .HasMany(c => c.Products)
            .WithOne(p => p.Category)
            .HasForeignKey(p => p.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
        
        // Configure self-referencing relationship for hierarchical categories
        builder.Entity<Category>()
            .HasMany(c => c.ChildCategories)
            .WithOne(c => c.ParentCategory)
            .HasForeignKey(c => c.ParentCategoryId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);
    }
}
