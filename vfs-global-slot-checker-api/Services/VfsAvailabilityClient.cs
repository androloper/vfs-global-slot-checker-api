using System.Text;
using System.Text.Json;
using vfs_global_slot_checker_api.Models;

namespace vfs_global_slot_checker_api.Services;

public sealed class VfsAvailabilityClient(HttpClient httpClient, ILogger<VfsAvailabilityClient> logger)
{
    public async Task<CenterAvailabilityResult> CheckAsync(
        CheckAvailabilityRequest request,
        ApplicationCenter center,
        CancellationToken cancellationToken)
    {
        var payload = new
        {
            countryCode = request.CountryCode,
            missionCode = request.MissionCode,
            vacCode = center.Code,
            visaCategoryCode = request.VisaCategoryCode,
            roleName = request.RoleName,
            loginUser = request.LoginUser,
            payCode = request.PayCode ?? string.Empty
        };

        using var upstreamRequest = new HttpRequestMessage(HttpMethod.Post, "appointment/CheckIsSlotAvailable")
        {
            Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
        };

        upstreamRequest.Headers.TryAddWithoutValidation("authorize", request.Authorize);
        upstreamRequest.Headers.TryAddWithoutValidation("clientsource", request.ClientSource);
        upstreamRequest.Headers.TryAddWithoutValidation("route", center.Route);
        upstreamRequest.Headers.TryAddWithoutValidation("origin", "https://visa.vfsglobal.com");
        upstreamRequest.Headers.TryAddWithoutValidation("referer", "https://visa.vfsglobal.com/");
        upstreamRequest.Headers.TryAddWithoutValidation(
            "user-agent",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/154.0.0.0 Safari/537.36");

        using var response = await httpClient.SendAsync(upstreamRequest, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("VFS request failed for center {CenterCode} with status {StatusCode}", center.Code, (int)response.StatusCode);
            throw new HttpRequestException(
                $"VFS request failed for center '{center.Code}' with status {(int)response.StatusCode}: {responseBody}");
        }

        if (!TryReadAvailability(responseBody, out var available))
        {
            throw new InvalidOperationException($"VFS returned an unsupported response for center '{center.Code}'.");
        }

        return new CenterAvailabilityResult(center.Name, center.Code, available);
    }

    private static bool TryReadAvailability(string body, out bool available)
    {
        available = false;

        try
        {
            using var document = JsonDocument.Parse(body);
            var root = document.RootElement;

            var hasDate = root.TryGetProperty("earliestDate", out var date) &&
                          date.ValueKind is not JsonValueKind.Null and not JsonValueKind.Undefined &&
                          !string.IsNullOrWhiteSpace(date.ToString());

            var hasSlots = root.TryGetProperty("earliestSlotLists", out var slots) &&
                           slots.ValueKind == JsonValueKind.Array &&
                           slots.GetArrayLength() > 0;

            available = hasDate || hasSlots;
            return root.TryGetProperty("earliestDate", out _) && root.TryGetProperty("earliestSlotLists", out _);
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
