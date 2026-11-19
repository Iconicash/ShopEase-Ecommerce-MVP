using System.ComponentModel.DataAnnotations;

namespace ShopEase.Application.DTOs.Orders;

public class UpdateOrderStatusDto
{
    [Required]
    public string Status { get; set; } = string.Empty;
}
