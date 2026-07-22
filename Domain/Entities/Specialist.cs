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
    public double Rating { get; private set; } = 4.5;
    public decimal StartingPrice { get; private set; } = 40;
    public string AvailabilityStatus { get; private set; } = "AVAILABLE TODAY";
    public Guid? SalonId { get; private set; }

    private readonly List<ServiceItem> _services = new();
    public IReadOnlyCollection<ServiceItem> Services => _services.AsReadOnly();

    protected Specialist() : base() { }

    public Specialist(string email, string passwordHash, string fullName, string role, string? phone, string? deviceId,
        string address, double? latitude, double? longitude, string? description, string? socialMedias, string? logoUrl, string? preferredColors, string? workingHours,
        double rating = 4.5, decimal startingPrice = 40, string availabilityStatus = "AVAILABLE TODAY", DateTime? birthday = null, string? gender = null, Guid? salonId = null)
        : base(email, passwordHash, fullName, role, phone, gender, birthday, deviceId)
    {
        Address = address;
        Latitude = latitude;
        Longitude = longitude;
        Description = description;
        SocialMedias = socialMedias;
        LogoUrl = logoUrl;
        PreferredColors = preferredColors;
        WorkingHours = workingHours;
        Rating = rating;
        StartingPrice = startingPrice;
        AvailabilityStatus = availabilityStatus;
        SalonId = salonId;
    }

    public void AddService(ServiceItem service)
    {
        _services.Add(service);
    }

    public void UpdateSpecialistProfile(string address, double? latitude, double? longitude, string? description, string? socialMedias, string? logoUrl, string? preferredColors, string? workingHours, Guid? salonId = null)
    {
        Address = address;
        Latitude = latitude;
        Longitude = longitude;
        Description = description;
        SocialMedias = socialMedias;
        LogoUrl = logoUrl;
        PreferredColors = preferredColors;
        WorkingHours = workingHours;
        SalonId = salonId;
    }
}
