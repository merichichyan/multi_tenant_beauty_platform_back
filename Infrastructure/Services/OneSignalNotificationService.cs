using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using multi_tenant_beauty_platform_back.Domain.Services;

namespace multi_tenant_beauty_platform_back.Infrastructure.Services;

public class OneSignalNotificationService : INotificationService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OneSignalNotificationService> _logger;
    private readonly string _appId;
    private readonly string _restApiKey;

    public OneSignalNotificationService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<OneSignalNotificationService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _appId = configuration["OneSignal:AppId"] ?? "";
        _restApiKey = configuration["OneSignal:RestApiKey"] ?? "";
    }

    public async Task SendNotificationToUserAsync(Guid userId, string title, string message, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_appId) || string.IsNullOrWhiteSpace(_restApiKey))
        {
            _logger.LogWarning("OneSignal is not configured. Missing AppId or RestApiKey. Skipping push notification.");
            return;
        }

        try
        {
            var payload = new
            {
                app_id = _appId,
                contents = new { en = message },
                headings = new { en = title },
                include_aliases = new
                {
                    external_id = new[] { userId.ToString() }
                },
                target_channel = "push"
            };

            var json = JsonSerializer.Serialize(payload);
            using var request = new HttpRequestMessage(HttpMethod.Post, "https://onesignal.com/api/v1/notifications");
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", _restApiKey);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request, ct);
            var responseContent = await response.Content.ReadAsStringAsync(ct);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Successfully sent OneSignal notification to user {UserId}. Response: {Response}", userId, responseContent);
            }
            else
            {
                _logger.LogError("Failed to send OneSignal notification to user {UserId}. Status: {Status}, Response: {Response}", 
                    userId, response.StatusCode, responseContent);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error sending push notification via OneSignal for user {UserId}", userId);
        }
    }
}
