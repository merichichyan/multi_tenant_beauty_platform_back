using multi_tenant_beauty_platform_back.Application.DTOs;
using multi_tenant_beauty_platform_back.Domain.Entities;
using multi_tenant_beauty_platform_back.Domain.Exceptions;
using multi_tenant_beauty_platform_back.Domain.Repositories;

namespace multi_tenant_beauty_platform_back.Application.Services;

public class OnboardingService : IOnboardingService
{
    private readonly IOnboardingRepository _repository;

    public OnboardingService(IOnboardingRepository repository)
    {
        _repository = repository;
    }

    public async Task<OnboardingResponseDto> SubmitOnboardingAsync(OnboardingRequestDto request, CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);

        var existingSubmission = await _repository.GetByDeviceIdAsync(request.DeviceId, cancellationToken);

        if (existingSubmission != null)
        {
            existingSubmission.UpdatePreferences(request.Language, request.Role, request.Timezone, request.NotificationsAllowed);
            await _repository.UpdateAsync(existingSubmission, cancellationToken);
            return MapToDto(existingSubmission);
        }

        var newSubmission = new OnboardingSubmission(
            request.DeviceId,
            request.ProgramId,
            request.Language,
            request.Role,
            request.Timezone,
            request.NotificationsAllowed
        );

        var savedSubmission = await _repository.AddAsync(newSubmission, cancellationToken);
        return MapToDto(savedSubmission);
    }

    public async Task<OnboardingResponseDto> GetOnboardingByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var submission = await _repository.GetByIdAsync(id, cancellationToken);
        if (submission == null)
        {
            throw new NotFoundException(nameof(OnboardingSubmission), id);
        }

        return MapToDto(submission);
    }

    public async Task<IEnumerable<OnboardingResponseDto>> GetAllOnboardingsAsync(CancellationToken cancellationToken = default)
    {
        var submissions = await _repository.GetAllAsync(cancellationToken);
        return submissions.Select(MapToDto);
    }

    private static void ValidateRequest(OnboardingRequestDto request)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.DeviceId))
        {
            errors.Add(nameof(request.DeviceId), new[] { "Device ID is required." });
        }
        if (string.IsNullOrWhiteSpace(request.Language))
        {
            errors.Add(nameof(request.Language), new[] { "Language is required." });
        }
        if (string.IsNullOrWhiteSpace(request.Role))
        {
            errors.Add(nameof(request.Role), new[] { "Role is required." });
        }

        if (errors.Count > 0)
        {
            throw new ValidationException(errors);
        }
    }

    private static OnboardingResponseDto MapToDto(OnboardingSubmission entity)
    {
        return new OnboardingResponseDto(
            entity.Id,
            entity.DeviceId,
            entity.ProgramId,
            entity.Language,
            entity.Role,
            entity.Timezone,
            entity.NotificationsAllowed,
            entity.CreatedAt,
            entity.UpdatedAt
        );
    }
}
