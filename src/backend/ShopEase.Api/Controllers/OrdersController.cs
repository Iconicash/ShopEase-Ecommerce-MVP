using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopEase.Application.DTOs.Orders;
using ShopEase.Domain.Entities;
using ShopEase.Infrastructure.Data;

namespace ShopEase.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    public OrdersController(ApplicationDbContext db) => _db = db;

    [HttpPost("checkout")]
    public async Task<IActionResult> Checkout(CheckoutRequestDto dto)
    {
        var userId = GetUserId();
        await using var transaction = await _db.Database.BeginTransactionAsync();

        var cart = await _db.Carts.Include(c => c.Items).ThenInclude(i => i.Product)
            .SingleOrDefaultAsync(c => c.UserId == userId);
        if (cart is null || cart.Items.Count == 0)
            return BadRequest(new { message = "Your cart is empty." });

        foreach (var item in cart.Items)
        {
            if (!item.Product.IsActive)
                return Conflict(new { message = $"{item.Product.Name} is no longer available." });
            if (item.Quantity > item.Product.StockQuantity)
                return Conflict(new { message = $"Insufficient stock for {item.Product.Name}. Available: {item.Product.StockQuantity}." });
        }

        var order = new Order
        {
            OrderNumber = $"SE-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}",
            UserId = userId,
            Status = "Pending",
            PaymentMethod = "CashOnDelivery",
            ShippingName = dto.ShippingName.Trim(),
            ShippingAddress = dto.ShippingAddress.Trim(),
            ShippingCity = dto.ShippingCity.Trim(),
            ShippingPostalCode = dto.ShippingPostalCode.Trim(),
            Phone = dto.Phone.Trim()
        };

        foreach (var item in cart.Items)
        {
            var price = item.Product.Price;
            item.Product.StockQuantity -= item.Quantity;
            order.Items.Add(new OrderItem
            {
                ProductId = item.ProductId,
                Product = item.Product,
                ProductName = item.Product.Name,
                SKU = item.Product.SKU,
                UnitPrice = price,
                Quantity = item.Quantity,
                LineTotal = price * item.Quantity
            });
        }
        order.TotalAmount = order.Items.Sum(i => i.LineTotal);

        _db.Orders.Add(order);
        _db.CartItems.RemoveRange(cart.Items);
        await _db.SaveChangesAsync();
        await transaction.CommitAsync();

        return CreatedAtAction(nameof(GetMyOrder), new { id = order.Id }, ToDto(order));
    }

    [HttpGet("my")]
    public async Task<IActionResult> GetMyOrders()
    {
        var userId = GetUserId();
        var orders = await _db.Orders.AsNoTracking().Include(o => o.Items)
            .Where(o => o.UserId == userId).OrderByDescending(o => o.OrderDate).ToListAsync();
        return Ok(orders.Select(o => ToDto(o)).ToList());
    }

    [HttpGet("my/{id:int}")]
    public async Task<IActionResult> GetMyOrder(int id)
    {
        var order = await _db.Orders.AsNoTracking().Include(o => o.Items)
            .SingleOrDefaultAsync(o => o.Id == id && o.UserId == GetUserId());
        return order is null ? NotFound(new { message = "Order not found." }) : Ok(ToDto(order));
    }

    [HttpGet("admin")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAllOrders()
    {
        var orders = await _db.Orders.AsNoTracking().Include(o => o.Items).Include(o => o.User)
            .OrderByDescending(o => o.OrderDate).ToListAsync();
        return Ok(orders.Select(o => ToDto(o, o.User.Email)).ToList());
    }

    [HttpPatch("admin/{id:int}/status")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateStatus(int id, UpdateOrderStatusDto dto)
    {
        var allowed = new[] { "Pending", "Confirmed", "Shipped", "Delivered", "Cancelled" };
        var requested = allowed.FirstOrDefault(s => s.Equals(dto.Status.Trim(), StringComparison.OrdinalIgnoreCase));
        if (requested is null) return BadRequest(new { message = "Status must be Pending, Confirmed, Shipped, Delivered or Cancelled." });

        var order = await _db.Orders.Include(o => o.Items).ThenInclude(i => i.Product)
            .SingleOrDefaultAsync(o => o.Id == id);
        if (order is null) return NotFound(new { message = "Order not found." });
        if (order.Status == "Delivered" && requested != "Delivered")
            return Conflict(new { message = "A delivered order cannot be changed." });
        if (order.Status == "Cancelled" && requested != "Cancelled")
            return Conflict(new { message = "A cancelled order cannot be reopened." });
        if (requested == "Cancelled" && (order.Status is "Shipped" or "Delivered"))
            return Conflict(new { message = "A shipped or delivered order cannot be cancelled." });

        if (requested == "Cancelled" && order.Status != "Cancelled")
        {
            foreach (var item in order.Items)
                item.Product.StockQuantity += item.Quantity;
        }
        order.Status = requested;
        await _db.SaveChangesAsync();
        return Ok(ToDto(order));
    }

    private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private static OrderResponseDto ToDto(Order order, string? email = null) => new()
    {
        Id = order.Id,
        OrderNumber = order.OrderNumber,
        OrderDate = order.OrderDate,
        Status = order.Status,
        PaymentMethod = order.PaymentMethod,
        TotalAmount = order.TotalAmount,
        ShippingName = order.ShippingName,
        ShippingAddress = order.ShippingAddress,
        ShippingCity = order.ShippingCity,
        ShippingPostalCode = order.ShippingPostalCode,
        Phone = order.Phone,
        CustomerEmail = email,
        Items = order.Items.Select(i => new OrderLineDto
        {
            ProductId = i.ProductId,
            ProductName = i.ProductName,
            SKU = i.SKU,
            UnitPrice = i.UnitPrice,
            Quantity = i.Quantity,
            LineTotal = i.LineTotal
        }).ToList()
    };
}
