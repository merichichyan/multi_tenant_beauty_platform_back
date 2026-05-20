namespace multi_tenant_beauty_platform_back.Domain.Entities;

public class SalonProfile
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
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

    protected SalonProfile() { }

    public SalonProfile(Guid userId, string salonName, string address, double? latitude, double? longitude, string? description, string? socialMedias, string? logoUrl, string? preferredColors, string? operatingHours)
    {
        Id = Guid.NewGuid();
        UserId = userId;
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
