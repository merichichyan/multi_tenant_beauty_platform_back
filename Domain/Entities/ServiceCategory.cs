namespace multi_tenant_beauty_platform_back.Domain.Entities;

public class ServiceCategory
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;

    protected ServiceCategory() { }

    public ServiceCategory(string name)
    {
        Id = Guid.NewGuid();
        Name = name;
    }

    public void Update(string name)
    {
        Name = name;
    }
}
