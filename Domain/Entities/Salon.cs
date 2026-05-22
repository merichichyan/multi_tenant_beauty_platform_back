namespace multi_tenant_beauty_platform_back.Domain.Entities;

public class Salon : User
{
    public string SalonName { get; private set; } = string.Empty;
    public string Address { get; private set; } = string.Empty;
    public double? Latitude { get; private set; }
    public double? Longitude { get; private set; }
    public string? Description { get; private set; }
    public string? SocialMedias { get; private set; }
    public string? LogoUrl { get; private set; }
    public string? PreferredColors { get; private set; }
    public string? OperatingHours { get; private set; }

    private readonly List<StaffMember> _staffMembers = new();
    public IReadOnlyCollection<StaffMember> StaffMembers => _staffMembers.AsReadOnly();

    protected Salon() : base() { }

    public Salon(string email, string passwordHash, string salonName, string role, string? phone, string? deviceId,
        string address, double? latitude, double? longitude, string? description, string? socialMedias, string? logoUrl, string? preferredColors, string? operatingHours)
        : base(email, passwordHash, salonName, role, phone, null, null, deviceId)
    {
        SalonName = salonName;
        Address = address;
        Latitude = latitude;
        Longitude = longitude;
        Description = description;
        SocialMedias = socialMedias;
        LogoUrl = logoUrl;
        PreferredColors = preferredColors;
        OperatingHours = operatingHours;
    }

    public void AddStaffMember(StaffMember staffMember)
    {
        _staffMembers.Add(staffMember);
    }
}
