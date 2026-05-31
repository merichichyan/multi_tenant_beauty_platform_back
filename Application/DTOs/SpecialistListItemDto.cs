namespace multi_tenant_beauty_platform_back.Application.DTOs;

public class SpecialistListItemDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? Description { get; set; }
    public string? LogoUrl { get; set; }
    public string? WorkingHours { get; set; }
    public string? SocialMedias { get; set; }
    public string? PreferredColors { get; set; }
    public double Rating { get; set; }
    public decimal StartingPrice { get; set; }
    public string AvailabilityStatus { get; set; } = string.Empty;
    public List<ServiceItemDto> Services { get; set; } = [];
}

public class ServiceItemDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public decimal? Price { get; set; }
    public int? DurationMinutes { get; set; }
    public bool IsActive { get; set; } = true;
}
