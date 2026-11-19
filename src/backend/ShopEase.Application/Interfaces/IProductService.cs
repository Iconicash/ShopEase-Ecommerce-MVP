using ShopEase.Application.DTOs.Products;

namespace ShopEase.Application.Interfaces;

public interface IProductService
{
    Task<List<ProductResponseDto>> GetAllAsync();

    Task<ProductResponseDto?> GetByIdAsync(int id);

    Task<ProductResponseDto> CreateAsync(CreateProductDto dto);

    Task<ProductResponseDto?> UpdateAsync(int id, UpdateProductDto dto);

    Task<bool> DeleteAsync(int id);
}