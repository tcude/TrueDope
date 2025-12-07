using System.ComponentModel.DataAnnotations;

namespace TrueDope.Api.DTOs.Auth;

public class RefreshRequest
{
    [Required(ErrorMessage = "Refresh token is required")]
    public string RefreshToken { get; set; } = string.Empty;
}
