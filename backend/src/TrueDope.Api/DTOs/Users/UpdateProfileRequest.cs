using System.ComponentModel.DataAnnotations;

namespace TrueDope.Api.DTOs.Users;

public class UpdateProfileRequest
{
    [MaxLength(100)]
    public string? FirstName { get; set; }

    [MaxLength(100)]
    public string? LastName { get; set; }
}
