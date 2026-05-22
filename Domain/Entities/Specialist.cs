namespace multi_tenant_beauty_platform_back.Domain.Entities;

public class Specialist : User
{
    public string Address { get; private set; } = string.Empty;
    public double? Latitude { get; private set; }
    public double? Longitude { get; private set; }
    public string? Description { get; private set; }
    public string? SocialMedias { get; private set; }
    public string? LogoUrl { get; private set; }
    public string? PreferredColors { get; private set; }
    public string? WorkingHours { get; private set; }

    private readonly List<ServiceItem> _services = new();
    public IReadOnlyCollection<ServiceItem> Services => _services.AsReadOnly();

    protected Specialist() : base() { }

    public Specialist(string email, string passwordHash, string fullName, string role, string? phone, string? deviceId,
        string address, double? latitude, double? longitude, string? description, string? socialMedias, string? logoUrl, string? preferredColors, string? workingHours)
        : base(email, passwordHash, fullName, role, phone, null, null, deviceId)
    {
        Address = address;
        Latitude = latitude;
        Longitude = longitude;
        Description = description;
        SocialMedias = socialMedias;
        LogoUrl = logoUrl;
        PreferredColors = preferredColors;
        WorkingHours = workingHours;
    }

    public void AddService(ServiceItem service)
    {
        _services.Add(service);
    }
}
