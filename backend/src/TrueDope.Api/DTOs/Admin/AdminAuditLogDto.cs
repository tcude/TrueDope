namespace TrueDope.Api.DTOs.Admin;

public class AdminAuditLogDto
{
    public int Id { get; set; }
    public string AdminUserId { get; set; } = null!;
    public string? AdminEmail { get; set; }
    public string ActionType { get; set; } = null!;
    public string? TargetUserId { get; set; }
    public string? TargetUserEmail { get; set; }
    public string? TargetEntityType { get; set; }
    public int? TargetEntityId { get; set; }
    public string? Details { get; set; }
    public string? IpAddress { get; set; }
    public DateTime Timestamp { get; set; }
}
