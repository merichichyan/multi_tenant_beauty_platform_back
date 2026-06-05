namespace multi_tenant_beauty_platform_back.Domain.Entities;

public class FavoriteSalon
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public Guid SalonId { get; private set; }

    protected FavoriteSalon() { }

    public FavoriteSalon(Guid userId, Guid salonId)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        SalonId = salonId;
    }
}
