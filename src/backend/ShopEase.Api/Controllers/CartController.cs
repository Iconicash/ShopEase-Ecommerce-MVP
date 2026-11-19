using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopEase.Application.DTOs.Cart;
using ShopEase.Domain.Entities;
using ShopEase.Infrastructure.Data;

namespace ShopEase.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class CartController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    public CartController(ApplicationDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetCart()
    {
        var cart = await GetCartAsync();
        return Ok(ToDto(cart));
    }

    [HttpPost("items")]
    public async Task<IActionResult> AddItem(AddCartItemDto dto)
    {
        var product = await _db.Products.SingleOrDefaultAsync(p => p.Id == dto.ProductId && p.IsActive);
        if (product is null) return NotFound(new { message = "Product not found or inactive." });

        var cart = await GetCartAsync();
        var existing = cart.Items.SingleOrDefault(i => i.ProductId == dto.ProductId);
        var newQuantity = (existing?.Quantity ?? 0) + dto.Quantity;
        if (newQuantity > product.StockQuantity)
            return Conflict(new { message = $"Only {product.StockQuantity} item(s) are currently in stock." });

        if (existing is null)
            cart.Items.Add(new CartItem { ProductId = product.Id, Quantity = dto.Quantity, Product = product });
        else
            existing.Quantity = newQuantity;

        await _db.SaveChangesAsync();
        return Ok(ToDto(cart));
    }

    [HttpPut("items/{productId:int}")]
    public async Task<IActionResult> UpdateItem(int productId, UpdateCartItemDto dto)
    {
        var cart = await GetCartAsync();
        var item = cart.Items.SingleOrDefault(i => i.ProductId == productId);
        if (item is null) return NotFound(new { message = "Cart item not found." });
        if (dto.Quantity > item.Product.StockQuantity)
            return Conflict(new { message = $"Only {item.Product.StockQuantity} item(s) are currently in stock." });
        item.Quantity = dto.Quantity;
        await _db.SaveChangesAsync();
        return Ok(ToDto(cart));
    }

    [HttpDelete("items/{productId:int}")]
    public async Task<IActionResult> RemoveItem(int productId)
    {
        var cart = await GetCartAsync();
        var item = cart.Items.SingleOrDefault(i => i.ProductId == productId);
        if (item is null) return NotFound(new { message = "Cart item not found." });
        _db.CartItems.Remove(item);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete]
    public async Task<IActionResult> ClearCart()
    {
        var cart = await GetCartAsync();
        _db.CartItems.RemoveRange(cart.Items);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    private async Task<ShopEase.Domain.Entities.Cart> GetCartAsync()
    {
        var userId = GetUserId();
        var cart = await _db.Carts.Include(c => c.Items).ThenInclude(i => i.Product)
            .SingleOrDefaultAsync(c => c.UserId == userId);
        if (cart is not null) return cart;
        cart = new ShopEase.Domain.Entities.Cart { UserId = userId };
        _db.Carts.Add(cart);
        await _db.SaveChangesAsync();
        return cart;
    }

    private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private static CartResponseDto ToDto(ShopEase.Domain.Entities.Cart cart)
    {
        var lines = cart.Items.Select(i => new CartLineDto
        {
            ProductId = i.ProductId,
            Name = i.Product.Name,
            SKU = i.Product.SKU,
            ImageUrl = i.Product.ImageUrl,
            UnitPrice = i.Product.Price,
            Quantity = i.Quantity,
            AvailableStock = i.Product.StockQuantity,
            LineTotal = i.Product.Price * i.Quantity
        }).ToList();
        return new CartResponseDto
        {
            Id = cart.Id,
            Items = lines,
            TotalItems = lines.Sum(i => i.Quantity),
            Subtotal = lines.Sum(i => i.LineTotal)
        };
    }
}
