namespace multi_tenant_beauty_platform_back.Domain.Entities;

public class OnboardingSubmission
{
    public Guid Id { get; private set; }
    public string DeviceId { get; private set; } = string.Empty;
    public string ProgramId { get; private set; } = string.Empty;
    public string Language { get; private set; } = string.Empty;
    public string Role { get; private set; } = string.Empty;
    public string Timezone { get; private set; } = string.Empty;
    public bool NotificationsAllowed { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    protected OnboardingSubmission() { }

    public OnboardingSubmission(string deviceId, string programId, string language, string role, string timezone, bool notificationsAllowed)
    {
        Id = Guid.NewGuid();
        DeviceId = deviceId;
        ProgramId = programId;
        Language = language;
        Role = role;
        Timezone = timezone;
        NotificationsAllowed = notificationsAllowed;
        CreatedAt = DateTime.UtcNow;
    }

    public void UpdatePreferences(string language, string role, string timezone, bool notificationsAllowed)
    {
        Language = language;
        Role = role;
        Timezone = timezone;
        NotificationsAllowed = notificationsAllowed;
        UpdatedAt = DateTime.UtcNow;
    }
}
