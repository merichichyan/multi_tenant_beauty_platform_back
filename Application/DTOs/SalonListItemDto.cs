namespace multi_tenant_beauty_platform_back.Application.DTOs;

public class SalonListItemDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string OwnerFullName { get; set; } = string.Empty;
    public string SalonName { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? Description { get; set; }
    public string? LogoUrl { get; set; }
    public string? OperatingHours { get; set; }
    public string? SocialMedias { get; set; }
    public string? PreferredColors { get; set; }
    public double Rating { get; set; }
    public decimal StartingPrice { get; set; }
    public string AvailabilityStatus { get; set; } = string.Empty;
    public List<StaffMemberDto> StaffMembers { get; set; } = [];
}

public class StaffMemberDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Title { get; set; }
    public string? GraphicsUrl { get; set; }
    public string? WorkingHours { get; set; }
    public string Status { get; set; } = "Active";
    public List<ServiceItemDto> Services { get; set; } = [];
}
