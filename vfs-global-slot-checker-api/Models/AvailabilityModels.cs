namespace vfs_global_slot_checker_api.Models;

public sealed record CheckAvailabilityRequest(
    string CountryCode,
    string MissionCode,
    string VacCode,
    string VisaCategoryCode,
    string RoleName,
    string LoginUser,
    string? PayCode,
    string ClientSource,
    string Authorize,
    IReadOnlyList<ApplicationCenter> Centers);

public sealed record ApplicationCenter(string Name, string Code, string Route);

public sealed record CenterSummary(string Name, string Code);

public sealed record CenterAvailabilityResult(string Name, string Code, bool Available, string? Error = null);

public sealed record CheckAvailabilityResponse(IReadOnlyList<CenterAvailabilityResult> Centers);

public sealed record SetVfsSessionRequest(string LoginUser, string Authorize, string ClientSource);

public sealed record SetVfsSessionResponse(bool Configured);

public sealed class VfsOptions
{
    public string CountryCode { get; set; } = "tur";
    public string MissionCode { get; set; } = "bgr";
    public string VisaCategoryCode { get; set; } = "30ev";
    public string RoleName { get; set; } = "Individual";
    public string LoginUser { get; set; } = string.Empty;
    public string PayCode { get; set; } = string.Empty;
    public string ClientSource { get; set; } = string.Empty;
    public string Authorize { get; set; } = string.Empty;
    public string Route { get; set; } = "tur/tr/bgr";
    public int DelayBetweenRequestsSeconds { get; set; } = 60;
}
