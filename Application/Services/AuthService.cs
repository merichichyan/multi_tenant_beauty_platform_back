using multi_tenant_beauty_platform_back.Application.DTOs.Auth;
using multi_tenant_beauty_platform_back.Domain.Entities;
using multi_tenant_beauty_platform_back.Domain.Exceptions;
using multi_tenant_beauty_platform_back.Domain.Repositories;
using multi_tenant_beauty_platform_back.Domain.Services;

namespace multi_tenant_beauty_platform_back.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly multi_tenant_beauty_platform_back.Infrastructure.Data.ApplicationDbContext _context;

    public AuthService(IUserRepository userRepository, IJwtTokenGenerator jwtTokenGenerator, multi_tenant_beauty_platform_back.Infrastructure.Data.ApplicationDbContext context)
    {
        _userRepository = userRepository;
        _jwtTokenGenerator = jwtTokenGenerator;
        _context = context;
    }

    public async Task<RegisterResponseDto> RegisterAsync(RegisterRequestDto request, CancellationToken cancellationToken = default)
    {
        ValidateRegisterRequest(request);

        var existingUser = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (existingUser != null)
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                { nameof(request.Email), new[] { "Email is already registered." } }
            });
        }

        var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
        var user = new User(request.Email, passwordHash, request.FullName, request.Role.ToLower().Trim());

        var savedUser = await _userRepository.AddAsync(user, cancellationToken);

        return new RegisterResponseDto(savedUser.Id, savedUser.IsOnboardingCompleted);
    }

    public async Task<RegisterResponseDto> RegisterUserAsync(UserRegisterRequestDto request, CancellationToken cancellationToken = default)
    {
        ValidateUserRegisterRequest(request);

        var existingUser = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (existingUser != null)
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                { nameof(request.Email), new[] { "Email is already registered." } }
            });
        }

        var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
        var user = new User(request.Email, passwordHash, request.FullName, request.Role.ToLower().Trim(), request.Phone, request.Gender, request.Birthday, request.DeviceId);

        var savedUser = await _userRepository.AddAsync(user, cancellationToken);

        return new RegisterResponseDto(savedUser.Id, savedUser.IsOnboardingCompleted);
    }

    public async Task<RegisterResponseDto> RegisterSpecialistAsync(SpecialistRegisterRequestDto request, CancellationToken cancellationToken = default)
    {
        ValidateSpecialistRegisterRequest(request);

        var existingUser = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (existingUser != null)
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                { nameof(request.Email), new[] { "Email is already registered." } }
            });
        }

        var password = string.IsNullOrWhiteSpace(request.Password) ? "Meri.12345" : request.Password;
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(password);
        var specialist = new Specialist(
            request.Email,
            passwordHash,
            request.FullName,
            request.Role.ToLower().Trim(),
            request.Phone,
            request.DeviceId,
            request.Address,
            request.Latitude ?? 40.1792,
            request.Longitude ?? 44.5152,
            request.Description,
            request.SocialMedias,
            request.LogoUrl,
            request.PreferredColors,
            request.WorkingHours,
            birthday: request.Birthday,
            gender: request.Gender,
            salonId: request.SalonId
        );

        if (request.Services != null)
        {
            foreach (var s in request.Services)
            {
                specialist.AddService(new ServiceItem(s.Name, s.Category, s.Price, s.DurationMinutes, specialistId: specialist.Id));
            }
        }

        var savedUser = await _userRepository.AddAsync(specialist, cancellationToken);

        if (request.SalonId.HasValue)
        {
            var exists = await _context.StaffMembers.AnyAsync(sm => sm.SpecialistId == savedUser.Id && sm.SalonId == request.SalonId.Value, cancellationToken);
            if (!exists)
            {
                var newStaff = new multi_tenant_beauty_platform_back.Domain.Entities.StaffMember(
                    request.SalonId.Value,
                    request.FullName,
                    request.Description ?? "Specialist",
                    request.LogoUrl,
                    request.WorkingHours ?? "09:00-18:00",
                    "Active",
                    savedUser.Id);
                _context.StaffMembers.Add(newStaff);
                await _context.SaveChangesAsync(cancellationToken);
            }
        }

        return new RegisterResponseDto(savedUser.Id, savedUser.IsOnboardingCompleted);
    }

    public async Task<RegisterResponseDto> RegisterSalonAsync(SalonRegisterRequestDto request, CancellationToken cancellationToken = default)
    {
        ValidateSalonRegisterRequest(request);

        var existingUser = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (existingUser != null)
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                { nameof(request.Email), new[] { "Email is already registered." } }
            });
        }

        var password = string.IsNullOrWhiteSpace(request.Password) ? "Meri.12345" : request.Password;
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(password);
        var ownerName = string.IsNullOrWhiteSpace(request.OwnerFullName) ? request.Email : request.OwnerFullName;
        var salon = new Salon(
            request.Email,
            passwordHash,
            request.SalonName,
            ownerName,
            request.Role.ToLower().Trim(),
            request.Phone,
            request.DeviceId,
            request.Address,
            request.Latitude ?? 40.1792,
            request.Longitude ?? 44.5152,
            request.Description,
            request.SocialMedias,
            request.LogoUrl,
            request.PreferredColors,
            request.OperatingHours
        );

        if (request.StaffMembers != null)
        {
            foreach (var sm in request.StaffMembers)
            {
                var staffMember = new StaffMember(salon.Id, sm.FullName, sm.Title, sm.GraphicsUrl);
                if (sm.Services != null)
                {
                    foreach (var s in sm.Services)
                    {
                        staffMember.AddService(new ServiceItem(s.Name, s.Category, s.Price, s.DurationMinutes, staffMemberId: staffMember.Id));
                    }
                }
                salon.AddStaffMember(staffMember);
            }
        }

        var savedUser = await _userRepository.AddAsync(salon, cancellationToken);

        return new RegisterResponseDto(savedUser.Id, savedUser.IsOnboardingCompleted);
    }

    public async Task<AuthResponseDto> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default)
    {
        ValidateLoginRequest(request);

        var user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                { "Credentials", new[] { "Invalid email or password." } }
            });
        }

        var token = _jwtTokenGenerator.GenerateToken(user);

        return new AuthResponseDto(token, user.IsOnboardingCompleted, user.Role, user.Id, user.Email);
    }

    public async Task CompleteOnboardingAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user == null)
        {
            throw new NotFoundException(nameof(User), userId);
        }

        user.CompleteOnboarding();
        await _userRepository.UpdateAsync(user, cancellationToken);
    }

    public async Task SelectRoleAsync(SelectRoleRequestDto request, CancellationToken cancellationToken = default)
    {
        var errors = new Dictionary<string, string[]>();

        if (request.UserId == Guid.Empty)
        {
            errors.Add(nameof(request.UserId), new[] { "User ID is required." });
        }

        if (string.IsNullOrWhiteSpace(request.Role))
        {
            errors.Add(nameof(request.Role), new[] { "Role is required." });
        }
        else
        {
            var normalizedRole = request.Role.ToLower().Trim();
            if (normalizedRole != "user" && normalizedRole != "specialist" && normalizedRole != "salon")
            {
                errors.Add(nameof(request.Role), new[] { "Role must be 'user', 'specialist', or 'salon'." });
            }
        }

        if (errors.Count > 0)
        {
            throw new ValidationException(errors);
        }

        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user == null)
        {
            throw new NotFoundException(nameof(User), request.UserId);
        }

        user.UpdateRole(request.Role.ToLower().Trim());
        await _userRepository.UpdateAsync(user, cancellationToken);
    }

    private static void ValidateRegisterRequest(RegisterRequestDto request)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.Email))
        {
            errors.Add(nameof(request.Email), new[] { "Email is required." });
        }
        if (string.IsNullOrWhiteSpace(request.Password))
        {
            errors.Add(nameof(request.Password), new[] { "Password is required." });
        }
        if (string.IsNullOrWhiteSpace(request.FullName))
        {
            errors.Add(nameof(request.FullName), new[] { "Full name is required." });
        }
        if (string.IsNullOrWhiteSpace(request.Role) || 
            !(request.Role.ToLower().Trim() == "user" || 
              request.Role.ToLower().Trim() == "specialist" || 
              request.Role.ToLower().Trim() == "salon"))
        {
            errors.Add(nameof(request.Role), new[] { "Valid role (user, specialist, salon) is required." });
        }

        if (errors.Count > 0)
        {
            throw new ValidationException(errors);
        }
    }

    private static void ValidateUserRegisterRequest(UserRegisterRequestDto request)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.Email))
        {
            errors.Add(nameof(request.Email), new[] { "Email is required." });
        }
        if (string.IsNullOrWhiteSpace(request.Password))
        {
            errors.Add(nameof(request.Password), new[] { "Password is required." });
        }
        if (string.IsNullOrWhiteSpace(request.FullName))
        {
            errors.Add(nameof(request.FullName), new[] { "Full name is required." });
        }
        if (string.IsNullOrWhiteSpace(request.Role) || 
            !(request.Role.ToLower().Trim() == "user" || 
              request.Role.ToLower().Trim() == "specialist" || 
              request.Role.ToLower().Trim() == "salon"))
        {
            errors.Add(nameof(request.Role), new[] { "Valid role (user, specialist, salon) is required." });
        }

        if (errors.Count > 0)
        {
            throw new ValidationException(errors);
        }
    }

    private static void ValidateSpecialistRegisterRequest(SpecialistRegisterRequestDto request)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.Email))
        {
            errors.Add(nameof(request.Email), new[] { "Email is required." });
        }
        if (string.IsNullOrWhiteSpace(request.FullName))
        {
            errors.Add(nameof(request.FullName), new[] { "Full name is required." });
        }
        if (string.IsNullOrWhiteSpace(request.Phone))
        {
            errors.Add(nameof(request.Phone), new[] { "Phone is required." });
        }
        if (string.IsNullOrWhiteSpace(request.Address))
        {
            errors.Add(nameof(request.Address), new[] { "Address is required." });
        }
        if (string.IsNullOrWhiteSpace(request.Role) || 
            !(request.Role.ToLower().Trim() == "user" || 
              request.Role.ToLower().Trim() == "specialist" || 
              request.Role.ToLower().Trim() == "salon"))
        {
            errors.Add(nameof(request.Role), new[] { "Valid role (user, specialist, salon) is required." });
        }

        if (errors.Count > 0)
        {
            throw new ValidationException(errors);
        }
    }

    private static void ValidateSalonRegisterRequest(SalonRegisterRequestDto request)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.Email))
        {
            errors.Add(nameof(request.Email), new[] { "Email is required." });
        }
        if (string.IsNullOrWhiteSpace(request.SalonName))
        {
            errors.Add(nameof(request.SalonName), new[] { "Salon name is required." });
        }
        if (string.IsNullOrWhiteSpace(request.Phone))
        {
            errors.Add(nameof(request.Phone), new[] { "Phone is required." });
        }
        if (string.IsNullOrWhiteSpace(request.Address))
        {
            errors.Add(nameof(request.Address), new[] { "Address is required." });
        }
        if (string.IsNullOrWhiteSpace(request.Role) || 
            !(request.Role.ToLower().Trim() == "user" || 
              request.Role.ToLower().Trim() == "specialist" || 
              request.Role.Trim().ToLower() == "salon"))
        {
            errors.Add(nameof(request.Role), new[] { "Valid role (user, specialist, salon) is required." });
        }

        if (errors.Count > 0)
        {
            throw new ValidationException(errors);
        }
    }

    private static void ValidateLoginRequest(LoginRequestDto request)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.Email))
        {
            errors.Add(nameof(request.Email), new[] { "Email is required." });
        }
        if (string.IsNullOrWhiteSpace(request.Password))
        {
            errors.Add(nameof(request.Password), new[] { "Password is required." });
        }

        if (errors.Count > 0)
        {
            throw new ValidationException(errors);
        }
    }

    public async Task<bool> ActivateAccountAsync(ActivateRequestDto request, CancellationToken cancellationToken = default)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(request.Email))
        {
            errors.Add(nameof(request.Email), new[] { "Email is required." });
        }
        if (string.IsNullOrWhiteSpace(request.Password))
        {
            errors.Add(nameof(request.Password), new[] { "Password is required." });
        }
        if (errors.Count > 0)
        {
            throw new ValidationException(errors);
        }

        var email = request.Email.Trim().ToLower();
        var user = await _userRepository.GetByEmailAsync(email, cancellationToken);
        if (user == null)
        {
            throw new NotFoundException(nameof(User), email);
        }

        bool isUnactivated = string.IsNullOrEmpty(user.PasswordHash) || 
                             BCrypt.Net.BCrypt.Verify("Meri.12345", user.PasswordHash);

        if (!isUnactivated)
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                { "Account", new[] { "This account is already activated." } }
            });
        }

        var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
        user.UpdatePasswordHash(passwordHash);
        user.UpdateStatus("Verified");
        await _userRepository.UpdateAsync(user, cancellationToken);

        return true;
    }
}
