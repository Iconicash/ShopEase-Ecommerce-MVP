using ShopEase.Domain.Entities;

namespace ShopEase.Application.Interfaces;

public interface ICategoryRepository
{
	Task<List<Category>> GetAllAsync();

	Task<Category?> GetByIdAsync(int id);

	Task<bool> ExistsByNameAsync(string name, int? excludeCategoryId = null);

	Task AddAsync(Category category);

	Task UpdateAsync(Category category);

	Task DeleteAsync(Category category);
}