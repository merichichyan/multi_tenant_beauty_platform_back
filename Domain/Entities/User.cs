namespace multi_tenant_beauty_platform_back.Domain.Entities;

public class User
{
    public Guid Id { get; private set; }
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public string FullName { get; private set; } = string.Empty;
    public string Role { get; private set; } = "user";
    public string? Phone { get; private set; }
    public string? Gender { get; private set; }
    public DateTime? Birthday { get; private set; }
    public string? DeviceId { get; private set; }
    public bool IsOnboardingCompleted { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    public SpecialistProfile? SpecialistProfile { get; private set; }
    public SalonProfile? SalonProfile { get; private set; }

    protected User() { }

    public User(string email, string passwordHash, string fullName, string role, string? phone = null, string? gender = null, DateTime? birthday = null, string? deviceId = null)
    {
        Id = Guid.NewGuid();
        Email = email;
        PasswordHash = passwordHash;
        FullName = fullName;
        Role = role;
        Phone = phone;
        Gender = gender;
        Birthday = birthday;
        DeviceId = deviceId;
        IsOnboardingCompleted = false;
        CreatedAt = DateTime.UtcNow;
    }

    public void CompleteOnboarding()
    {
        IsOnboardingCompleted = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateRole(string role)
    {
        Role = role;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetSpecialistProfile(SpecialistProfile profile)
    {
        SpecialistProfile = profile;
    }

    public void SetSalonProfile(SalonProfile profile)
    {
        SalonProfile = profile;
    }
}
