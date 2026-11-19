using ShopEase.Application.DTOs.Categories;
using ShopEase.Application.Interfaces;
using ShopEase.Domain.Entities;

namespace ShopEase.Application.Services;

public class CategoryService : ICategoryService
{
    private readonly ICategoryRepository _categoryRepository;

    public CategoryService(ICategoryRepository categoryRepository)
    {
        _categoryRepository = categoryRepository;
    }

    public async Task<List<CategoryResponseDto>> GetAllAsync()
    {
        var categories = await _categoryRepository.GetAllAsync();

        return categories.Select(c => new CategoryResponseDto
        {
            Id = c.Id,
            Name = c.Name,
            Description = c.Description,
            IsActive = c.IsActive,
            CreatedAt = c.CreatedAt
        }).ToList();
    }

    public async Task<CategoryResponseDto?> GetByIdAsync(int id)
    {
        var category = await _categoryRepository.GetByIdAsync(id);

        if (category == null)
            return null;

        return new CategoryResponseDto
        {
            Id = category.Id,
            Name = category.Name,
            Description = category.Description,
            IsActive = category.IsActive,
            CreatedAt = category.CreatedAt
        };
    }

    public async Task<CategoryResponseDto> CreateAsync(CreateCategoryDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            throw new ArgumentException("Category name is required.");

        var name = dto.Name.Trim();
        var description = dto.Description?.Trim() ?? string.Empty;

        if (await _categoryRepository.ExistsByNameAsync(name))
            throw new InvalidOperationException("Category name already exists.");

        var category = new Category
        {
            Name = name,
            Description = description,
            IsActive = true
        };

        await _categoryRepository.AddAsync(category);

        return new CategoryResponseDto
        {
            Id = category.Id,
            Name = category.Name,
            Description = category.Description,
            IsActive = category.IsActive,
            CreatedAt = category.CreatedAt
        };
    }

    public async Task<CategoryResponseDto?> UpdateAsync(
        int id,
        UpdateCategoryDto dto)
    {
        var category = await _categoryRepository.GetByIdAsync(id);

        if (category == null)
            return null;

        if (string.IsNullOrWhiteSpace(dto.Name))
            throw new ArgumentException("Category name is required.");

        var name = dto.Name.Trim();
        var description = dto.Description?.Trim() ?? string.Empty;

        if (await _categoryRepository.ExistsByNameAsync(name, id))
            throw new InvalidOperationException("Category name already exists.");

        category.Name = name;
        category.Description = description;
        category.IsActive = dto.IsActive;

        await _categoryRepository.UpdateAsync(category);

        return new CategoryResponseDto
        {
            Id = category.Id,
            Name = category.Name,
            Description = category.Description,
            IsActive = category.IsActive,
            CreatedAt = category.CreatedAt
        };
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var category = await _categoryRepository.GetByIdAsync(id);

        if (category == null)
            return false;

        await _categoryRepository.DeleteAsync(category);

        return true;
    }
}