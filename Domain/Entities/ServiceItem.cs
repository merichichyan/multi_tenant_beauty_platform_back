namespace multi_tenant_beauty_platform_back.Domain.Entities;

public class ServiceItem
{
    public Guid Id { get; private set; }
    public Guid? SpecialistId { get; private set; }
    public Guid? StaffMemberId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Category { get; private set; } = string.Empty;
    public decimal Price { get; private set; }
    public int DurationMinutes { get; private set; }

    public bool IsActive { get; private set; } = true;

    protected ServiceItem() { }

    public ServiceItem(string name, string category, decimal price, int durationMinutes, Guid? specialistId = null, Guid? staffMemberId = null, bool isActive = true)
    {
        Id = Guid.NewGuid();
        Name = name;
        Category = category;
        Price = price;
        DurationMinutes = durationMinutes;
        SpecialistId = specialistId;
        StaffMemberId = staffMemberId;
        IsActive = isActive;
    }

    public void SetActive(bool isActive)
    {
        IsActive = isActive;
    }
}
