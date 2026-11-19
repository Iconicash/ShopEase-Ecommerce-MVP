using System.ComponentModel.DataAnnotations;

namespace ShopEase.Application.DTOs.Orders;

public class CheckoutRequestDto
{
    [Required, StringLength(120)]
    public string ShippingName { get; set; } = string.Empty;

    [Required, StringLength(300)]
    public string ShippingAddress { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string ShippingCity { get; set; } = string.Empty;

    [Required, StringLength(20)]
    public string ShippingPostalCode { get; set; } = string.Empty;

    [Required, Phone, StringLength(20)]
    public string Phone { get; set; } = string.Empty;
}
