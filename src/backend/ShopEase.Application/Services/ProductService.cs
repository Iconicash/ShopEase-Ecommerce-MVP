using ShopEase.Application.DTOs.Products;
using ShopEase.Application.Interfaces;
using ShopEase.Domain.Entities;

namespace ShopEase.Application.Services;

public class ProductService : IProductService
{
    private readonly IProductRepository _productRepository;

    public ProductService(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    // Get all products
    public async Task<List<ProductResponseDto>> GetAllAsync()
    {
        var products = await _productRepository.GetAllAsync();

        return products.Select(p => new ProductResponseDto
        {
            Id = p.Id,
            Name = p.Name,
            Description = p.Description,
            SKU = p.SKU,
            Price = p.Price,
            StockQuantity = p.StockQuantity,
            ImageUrl = p.ImageUrl,
            IsActive = p.IsActive,
            CategoryId = p.CategoryId,
            CategoryName = p.Category.Name,
            CreatedAt = p.CreatedAt
        }).ToList();
    }

    // Get product by ID
    public async Task<ProductResponseDto?> GetByIdAsync(int id)
    {
        var product = await _productRepository.GetByIdAsync(id);

        if (product == null)
            return null;

        return MapToResponse(product);
    }

    // Create product
    public async Task<ProductResponseDto> CreateAsync(CreateProductDto dto)
    {
        ValidateProduct(
            dto.Name,
            dto.SKU,
            dto.Price,
            dto.StockQuantity
        );

        var name = dto.Name.Trim();
        var sku = dto.SKU.Trim();
        var description = dto.Description?.Trim() ?? string.Empty;

        if (await _productRepository.ExistsBySkuAsync(sku))
            throw new InvalidOperationException("SKU already exists.");

        if (!await _productRepository.CategoryExistsAsync(dto.CategoryId))
            throw new ArgumentException("Invalid category ID.");

        var product = new Product
        {
            Name = name,
            Description = description,
            SKU = sku,
            Price = dto.Price,
            StockQuantity = dto.StockQuantity,
            ImageUrl = dto.ImageUrl,
            CategoryId = dto.CategoryId,
            IsActive = true
        };

        await _productRepository.AddAsync(product);

        var createdProduct =
            await _productRepository.GetByIdAsync(product.Id);

        return MapToResponse(createdProduct!);
    }

    // Update product
    public async Task<ProductResponseDto?> UpdateAsync(
        int id,
        UpdateProductDto dto)
    {
        var product = await _productRepository.GetByIdAsync(id);

        if (product == null)
            return null;

        ValidateProduct(
            dto.Name,
            dto.SKU,
            dto.Price,
            dto.StockQuantity
        );

        var name = dto.Name.Trim();
        var sku = dto.SKU.Trim();
        var description = dto.Description?.Trim() ?? string.Empty;

        if (await _productRepository.ExistsBySkuAsync(sku, id))
            throw new InvalidOperationException("SKU already exists.");

        if (!await _productRepository.CategoryExistsAsync(dto.CategoryId))
            throw new ArgumentException("Invalid category ID.");

        product.Name = name;
        product.Description = description;
        product.SKU = sku;
        product.Price = dto.Price;
        product.StockQuantity = dto.StockQuantity;
        product.ImageUrl = dto.ImageUrl;
        product.CategoryId = dto.CategoryId;
        product.IsActive = dto.IsActive;

        await _productRepository.UpdateAsync(product);

        var updatedProduct =
            await _productRepository.GetByIdAsync(id);

        return MapToResponse(updatedProduct!);
    }

    // Delete product
    public async Task<bool> DeleteAsync(int id)
    {
        var product = await _productRepository.GetByIdAsync(id);

        if (product == null)
            return false;

        await _productRepository.DeleteAsync(product);

        return true;
    }

    // Validate product data
    private static void ValidateProduct(
        string name,
        string sku,
        decimal price,
        int stockQuantity)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Product name is required.");

        if (string.IsNullOrWhiteSpace(sku))
            throw new ArgumentException("SKU is required.");

        if (price <= 0)
            throw new ArgumentException("Price must be greater than zero.");

        if (stockQuantity < 0)
            throw new ArgumentException("Stock quantity cannot be negative.");
    }

    // Convert Product entity to response DTO
    private static ProductResponseDto MapToResponse(Product product)
    {
        return new ProductResponseDto
        {
            Id = product.Id,
            Name = product.Name,
            Description = product.Description,
            SKU = product.SKU,
            Price = product.Price,
            StockQuantity = product.StockQuantity,
            ImageUrl = product.ImageUrl,
            IsActive = product.IsActive,
            CategoryId = product.CategoryId,
            CategoryName = product.Category.Name,
            CreatedAt = product.CreatedAt
        };
    }
}