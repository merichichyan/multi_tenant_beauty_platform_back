namespace multi_tenant_beauty_platform_back.Domain.Entities;

public class FavoriteSpecialist
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public Guid SpecialistId { get; private set; }

    protected FavoriteSpecialist() { }

    public FavoriteSpecialist(Guid userId, Guid specialistId)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        SpecialistId = specialistId;
    }
}
