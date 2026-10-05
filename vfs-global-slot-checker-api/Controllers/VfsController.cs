using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using vfs_global_slot_checker_api.Models;
using vfs_global_slot_checker_api.Services;

namespace vfs_global_slot_checker_api.Controllers;

[ApiController]
[Route("api/vfs")]
public sealed class VfsController(
    VfsAvailabilityClient client,
    IOptions<VfsOptions> options,
    VfsSessionState session) : ControllerBase
{
    private static readonly ApplicationCenter[] AllCenters =
    [
        new("Edirne", "EDR", "tur/tr/bgr"),
        new("Izmir", "ADB", "tur/tr/bgr"),
        new("Gaziantep", "GAZ", "tur/tr/bgr"),
        new("Bursa", "BUR", "tur/tr/bgr"),
        new("Istanbul-Altunizade", "ALT", "tur/tr/bgr"),
        new("Istanbul-Beyoglu", "IBY", "tur/tr/bgr"),
        new("Trabzon", "TZX", "tur/tr/bgr"),
        new("Ankara", "ESB", "tur/tr/bgr"),
        new("Antalya", "AYT", "tur/tr/bgr"),
        new("Diyarbakir", "DIY", "tur/tr/bgr"),
        new("Istanbul Consulate", "BCONS", "tur/tr/bgr"),
        new("Ankara Embassy", "EMBANK", "tur/tr/bgr")
    ];

    [HttpGet("getCenters")]
    [ProducesResponseType(typeof(IReadOnlyList<CenterSummary>), StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyList<CenterSummary>> GetCenters()
    {
        return Ok(AllCenters.Select(center => new CenterSummary(center.Name, center.Code)).ToArray());
    }

    [HttpPost("session")]
    [ProducesResponseType(typeof(SetVfsSessionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<SetVfsSessionResponse> SetSession(SetVfsSessionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.LoginUser) ||
            string.IsNullOrWhiteSpace(request.Authorize) ||
            string.IsNullOrWhiteSpace(request.ClientSource))
        {
            return BadRequest("LoginUser, Authorize and ClientSource are required.");
        }

        session.Set(request.LoginUser, request.Authorize, request.ClientSource);
        return Ok(new SetVfsSessionResponse(true));
    }

    [HttpGet("availability")]
    [ProducesResponseType(typeof(CheckAvailabilityResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public Task<ActionResult<CheckAvailabilityResponse>> GetAvailability(CancellationToken cancellationToken)
    {
        var settings = options.Value;
        if (!session.TryGet(out var loginUser, out var authorize, out var clientSource))
        {
            return Task.FromResult<ActionResult<CheckAvailabilityResponse>>(
                StatusCode(StatusCodes.Status500InternalServerError,
                    "Call POST /api/vfs/session from Swagger with per-center credentials first."));
        }

        var request = new CheckAvailabilityRequest(
            settings.CountryCode,
            settings.MissionCode,
            loginUser,
            settings.VisaCategoryCode,
            settings.RoleName,
            string.Empty,
            settings.PayCode,
            clientSource,
            authorize,
            AllCenters);

        return CheckCentersAsync(request, cancellationToken);
    }

    [HttpGet("availability/{centerCode}")]
    [ProducesResponseType(typeof(CheckAvailabilityResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<CheckAvailabilityResponse>> GetCenterAvailability(
        string centerCode,
        CancellationToken cancellationToken)
    {
        var center = AllCenters.FirstOrDefault(
            value => value.Code.Equals(centerCode, StringComparison.OrdinalIgnoreCase));

        if (center is null)
        {
            return BadRequest($"Unknown center code '{centerCode}'.");
        }

        if (!session.TryGet(out var loginUser, out var authorize, out var clientSource))
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                "Call POST /api/vfs/session from Swagger first.");
        }

        var settings = options.Value;
        var request = new CheckAvailabilityRequest(
            settings.CountryCode,
            settings.MissionCode,
            string.Empty,
            settings.VisaCategoryCode,
            settings.RoleName,
            loginUser,
            settings.PayCode,
            clientSource,
            authorize,
            [center]);

        return await CheckCentersAsync(request, cancellationToken);
    }

    private async Task<ActionResult<CheckAvailabilityResponse>> CheckCentersAsync(
        CheckAvailabilityRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var results = new List<CenterAvailabilityResult>(request.Centers.Count);
            for (var index = 0; index < request.Centers.Count; index++)
            {
                if (index > 0)
                {
                    var delay = Math.Max(0, options.Value.DelayBetweenRequestsSeconds);
                    await Task.Delay(TimeSpan.FromSeconds(delay), cancellationToken);
                }

                var center = request.Centers[index];
                try
                {
                    results.Add(await client.CheckAsync(request, center, cancellationToken));
                }
                catch (HttpRequestException exception)
                {
                    results.Add(new CenterAvailabilityResult(center.Name, center.Code, false, exception.Message));
                }
                catch (InvalidOperationException exception)
                {
                    results.Add(new CenterAvailabilityResult(center.Name, center.Code, false, exception.Message));
                }
            }

            return Ok(new CheckAvailabilityResponse(results));
        }
        catch (HttpRequestException exception)
        {
            return StatusCode(StatusCodes.Status502BadGateway, exception.Message);
        }
        catch (InvalidOperationException exception)
        {
            return StatusCode(StatusCodes.Status502BadGateway, exception.Message);
        }
    }
}
