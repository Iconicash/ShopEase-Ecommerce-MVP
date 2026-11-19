using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopEase.Application.DTOs.Products;
using ShopEase.Application.Interfaces;

namespace ShopEase.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
	private readonly IProductService _productService;

	public ProductsController(IProductService productService)
	{
		_productService = productService;
	}

	// GET: api/products
	[HttpGet]
	public async Task<IActionResult> GetAll()
	{
		var products = await _productService.GetAllAsync();
		return Ok(products);
	}

	// GET: api/products/1
	[HttpGet("{id}")]
	public async Task<IActionResult> GetById(int id)
	{
		var product = await _productService.GetByIdAsync(id);

		if (product == null)
			return NotFound(new { message = "Product not found." });

		return Ok(product);
	}

	// POST: api/products
	[Authorize(Roles = "Admin")]
	[HttpPost]
	public async Task<IActionResult> Create(CreateProductDto dto)
	{
		try
		{
			var product = await _productService.CreateAsync(dto);

			return CreatedAtAction(
				nameof(GetById),
				new { id = product.Id },
				product
			);
		}
		catch (ArgumentException ex)
		{
			return BadRequest(new { message = ex.Message });
		}
		catch (InvalidOperationException ex)
		{
			return Conflict(new { message = ex.Message });
		}
		catch (DbUpdateException)
		{
			return Conflict(new
			{
				message = "Could not create product. Check whether the SKU already exists."
			});
		}
	}

	// PUT: api/products/1
	[Authorize(Roles = "Admin")]
	[HttpPut("{id}")]
	public async Task<IActionResult> Update(int id, UpdateProductDto dto)
	{
		try
		{
			var product = await _productService.UpdateAsync(id, dto);

			if (product == null)
				return NotFound(new { message = "Product not found." });

			return Ok(product);
		}
		catch (ArgumentException ex)
		{
			return BadRequest(new { message = ex.Message });
		}
		catch (InvalidOperationException ex)
		{
			return Conflict(new { message = ex.Message });
		}
		catch (DbUpdateException)
		{
			return Conflict(new
			{
				message = "Could not update product. Check whether the SKU already exists."
			});
		}
	}

	// DELETE: api/products/1
	[Authorize(Roles = "Admin")]
	[HttpDelete("{id}")]
	public async Task<IActionResult> Delete(int id)
	{
		try
		{
			var deleted = await _productService.DeleteAsync(id);

			if (!deleted)
				return NotFound(new { message = "Product not found." });

			return NoContent();
		}
		catch (DbUpdateException)
		{
			return Conflict(new { message = "Cannot delete this product because it is referenced by a cart or order." });
		}
	}
}