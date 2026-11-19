using System.ComponentModel.DataAnnotations;

namespace ShopEase.Application.DTOs.Cart;

public class AddCartItemDto
{
    [Range(1, int.MaxValue)]
    public int ProductId { get; set; }

    [Range(1, 100)]
    public int Quantity { get; set; } = 1;
}
