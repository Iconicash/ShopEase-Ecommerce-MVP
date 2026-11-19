using ShopEase.Domain.Entities;

namespace ShopEase.Application.Interfaces;

public interface IProductRepository
{
    Task<List<Product>> GetAllAsync();

    Task<Product?> GetByIdAsync(int id);

    Task<bool> ExistsBySkuAsync(string sku, int? excludeProductId = null);

    Task<bool> CategoryExistsAsync(int categoryId);

    Task AddAsync(Product product);

    Task UpdateAsync(Product product);

    Task DeleteAsync(Product product);
}