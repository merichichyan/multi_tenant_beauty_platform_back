namespace multi_tenant_beauty_platform_back.Domain.Entities;

public class ServiceCategory
{
    public Guid Id { get; private set; }
    public string NameHy { get; private set; } = string.Empty;
    public string NameRu { get; private set; } = string.Empty;
    public string NameEn { get; private set; } = string.Empty;

    protected ServiceCategory() { }

    public ServiceCategory(string nameHy, string nameRu, string nameEn)
    {
        Id = Guid.NewGuid();
        NameHy = nameHy;
        NameRu = nameRu;
        NameEn = nameEn;
    }

    public void Update(string nameHy, string nameRu, string nameEn)
    {
        NameHy = nameHy;
        NameRu = nameRu;
        NameEn = nameEn;
    }
}
