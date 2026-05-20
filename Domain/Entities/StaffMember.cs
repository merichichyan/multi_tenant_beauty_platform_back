namespace multi_tenant_beauty_platform_back.Domain.Entities;

public class StaffMember
{
    public Guid Id { get; private set; }
    public Guid SalonProfileId { get; private set; }
    public string FullName { get; private set; } = string.Empty;
    public string? Title { get; private set; }
    public string? GraphicsUrl { get; private set; }
    public string? WorkingHours { get; private set; }

    private readonly List<ServiceItem> _services = new();
    public IReadOnlyCollection<ServiceItem> Services => _services.AsReadOnly();

    protected StaffMember() { }

    public StaffMember(Guid salonProfileId, string fullName, string? title, string? graphicsUrl, string? workingHours = null)
    {
        Id = Guid.NewGuid();
        SalonProfileId = salonProfileId;
        FullName = fullName;
        Title = title;
        GraphicsUrl = graphicsUrl;
        WorkingHours = workingHours;
    }

    public void AddService(ServiceItem service)
    {
        _services.Add(service);
    }
}
