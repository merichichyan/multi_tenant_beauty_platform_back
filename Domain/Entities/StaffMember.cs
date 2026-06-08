namespace multi_tenant_beauty_platform_back.Domain.Entities;

public class StaffMember
{
    public Guid Id { get; private set; }
    public Guid SalonId { get; private set; }
    public string FullName { get; private set; } = string.Empty;
    public string? Title { get; private set; }
    public string? GraphicsUrl { get; private set; }
    public string? WorkingHours { get; private set; }
    public string Status { get; private set; } = "Active";
    public Guid? SpecialistId { get; private set; }

    private readonly List<ServiceItem> _services = new();
    public IReadOnlyCollection<ServiceItem> Services => _services.AsReadOnly();

    protected StaffMember() { }

    public StaffMember(Guid salonId, string fullName, string? title, string? graphicsUrl, string? workingHours = null, string status = "Active", Guid? specialistId = null)
    {
        Id = Guid.NewGuid();
        SalonId = salonId;
        FullName = fullName;
        Title = title;
        GraphicsUrl = graphicsUrl;
        WorkingHours = workingHours;
        Status = status;
        SpecialistId = specialistId;
    }

    public void AddService(ServiceItem service)
    {
        _services.Add(service);
    }

    public void UpdateStatus(string status)
    {
        Status = status;
    }

    public void UpdateStaffMember(string fullName, string? title, string? graphicsUrl, string? workingHours)
    {
        FullName = fullName;
        Title = title;
        GraphicsUrl = graphicsUrl;
        WorkingHours = workingHours;
    }
}
