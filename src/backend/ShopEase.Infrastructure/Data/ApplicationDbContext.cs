using Microsoft.EntityFrameworkCore;
using ShopEase.Domain.Entities;

namespace ShopEase.Infrastructure.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Product> Products => Set<Product>();

    public DbSet<Category> Categories => Set<Category>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Cart> Carts => Set<Cart>();
    public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Category configuration
        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(c => c.Id);

            entity.Property(c => c.Name)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(c => c.Description)
                .HasMaxLength(500);
        });

        // Product configuration
        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(p => p.Id);

            entity.Property(p => p.Name)
                .IsRequired()
                .HasMaxLength(150);

            entity.Property(p => p.SKU)
                .IsRequired()
                .HasMaxLength(50);

            entity.HasIndex(p => p.SKU)
                .IsUnique();

            entity.Property(p => p.Price)
                .HasPrecision(18, 2);

            entity.Property(p => p.Description)
                .HasMaxLength(2000);

            entity.HasOne(p => p.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.Id);
            entity.Property(u => u.FullName).IsRequired().HasMaxLength(120);
            entity.Property(u => u.Email).IsRequired().HasMaxLength(256);
            entity.HasIndex(u => u.Email).IsUnique();
            entity.Property(u => u.PasswordHash).IsRequired().HasMaxLength(500);
            entity.Property(u => u.Role).IsRequired().HasMaxLength(30);
        });

        modelBuilder.Entity<Cart>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.HasIndex(c => c.UserId).IsUnique();
            entity.HasOne(c => c.User).WithOne(u => u.Cart)
                .HasForeignKey<Cart>(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CartItem>(entity =>
        {
            entity.HasKey(i => i.Id);
            entity.HasIndex(i => new { i.CartId, i.ProductId }).IsUnique();
            entity.HasOne(i => i.Cart).WithMany(c => c.Items)
                .HasForeignKey(i => i.CartId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(i => i.Product).WithMany()
                .HasForeignKey(i => i.ProductId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(o => o.Id);
            entity.HasIndex(o => o.OrderNumber).IsUnique();
            entity.Property(o => o.OrderNumber).IsRequired().HasMaxLength(40);
            entity.Property(o => o.Status).IsRequired().HasMaxLength(30);
            entity.Property(o => o.PaymentMethod).IsRequired().HasMaxLength(30);
            entity.Property(o => o.TotalAmount).HasPrecision(18, 2);
            entity.Property(o => o.ShippingName).IsRequired().HasMaxLength(120);
            entity.Property(o => o.ShippingAddress).IsRequired().HasMaxLength(300);
            entity.Property(o => o.ShippingCity).IsRequired().HasMaxLength(100);
            entity.Property(o => o.ShippingPostalCode).IsRequired().HasMaxLength(20);
            entity.Property(o => o.Phone).IsRequired().HasMaxLength(20);
            entity.HasOne(o => o.User).WithMany(u => u.Orders)
                .HasForeignKey(o => o.UserId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.HasKey(i => i.Id);
            entity.Property(i => i.ProductName).IsRequired().HasMaxLength(150);
            entity.Property(i => i.SKU).IsRequired().HasMaxLength(50);
            entity.Property(i => i.UnitPrice).HasPrecision(18, 2);
            entity.Property(i => i.LineTotal).HasPrecision(18, 2);
            entity.HasOne(i => i.Order).WithMany(o => o.Items)
                .HasForeignKey(i => i.OrderId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(i => i.Product).WithMany()
                .HasForeignKey(i => i.ProductId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}