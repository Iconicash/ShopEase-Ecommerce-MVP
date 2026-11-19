using Microsoft.EntityFrameworkCore;
using ShopEase.Application.Interfaces;
using ShopEase.Domain.Entities;
using ShopEase.Infrastructure.Data;

namespace ShopEase.Infrastructure.Repositories;

public class ProductRepository : IProductRepository
{
    private readonly ApplicationDbContext _context;

    public ProductRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    // Get all products
    public async Task<List<Product>> GetAllAsync()
    {
        return await _context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .ToListAsync();
    }

    // Get product by ID
    public async Task<Product?> GetByIdAsync(int id)
    {
        return await _context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    // Check duplicate SKU
    public async Task<bool> ExistsBySkuAsync(
        string sku,
        int? excludeProductId = null)
    {
        return await _context.Products.AnyAsync(p =>
            p.SKU == sku &&
            (!excludeProductId.HasValue ||
             p.Id != excludeProductId.Value));
    }

    // Check category existence
    public async Task<bool> CategoryExistsAsync(int categoryId)
    {
        return await _context.Categories
            .AnyAsync(c => c.Id == categoryId);
    }

    // Add product
    public async Task AddAsync(Product product)
    {
        await _context.Products.AddAsync(product);
        await _context.SaveChangesAsync();
    }

    // Update product
    public async Task UpdateAsync(Product product)
    {
        _context.Products.Update(product);
        await _context.SaveChangesAsync();
    }

    // Delete product
    public async Task DeleteAsync(Product product)
    {
        _context.Products.Remove(product);
        await _context.SaveChangesAsync();
    }
}