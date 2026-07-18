using System;
using System.Collections.Generic;
using System.Text.Json;

namespace multi_tenant_beauty_platform_back.Application.Services;

public static class LocalizationHelper
{
    public static string LocalizeString(string? jsonOrRaw, string? lang)
    {
        if (string.IsNullOrWhiteSpace(jsonOrRaw)) return string.Empty;
        
        var trimmed = jsonOrRaw.Trim();
        if (!trimmed.StartsWith("{") || !trimmed.EndsWith("}"))
        {
            return jsonOrRaw; // fallback if not JSON
        }

        try
        {
            var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(trimmed);
            if (dict != null)
            {
                var requestedLang = lang?.ToLower().Trim() ?? "en";
                if (dict.TryGetValue(requestedLang, out var value) && !string.IsNullOrWhiteSpace(value))
                {
                    return value;
                }
                
                // Fallbacks:
                if (dict.TryGetValue("en", out var enVal) && !string.IsNullOrWhiteSpace(enVal)) return enVal;
                if (dict.TryGetValue("hy", out var hyVal) && !string.IsNullOrWhiteSpace(hyVal)) return hyVal;
                if (dict.TryGetValue("ru", out var ruVal) && !string.IsNullOrWhiteSpace(ruVal)) return ruVal;
            }
        }
        catch
        {
            // ignore and return raw string
        }

        return jsonOrRaw;
    }
}
