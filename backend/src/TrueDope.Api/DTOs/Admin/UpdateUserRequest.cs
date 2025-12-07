using System.ComponentModel.DataAnnotations;

namespace TrueDope.Api.DTOs.Admin;

public class UpdateUserRequest
{
    [MaxLength(100)]
    public string? FirstName { get; set; }

    [MaxLength(100)]
    public string? LastName { get; set; }

    public bool? IsAdmin { get; set; }
}
