using System.Text.Json;
using Aegis.Server.Attributes;
using Aegis.Server.DTOs;
using Aegis.Server.Services;
using Microsoft.AspNetCore.Mvc;

namespace Aegis.Server.Controllers;

[Route("api/[controller]")]
[ApiController]
public class LicensesController(LicenseService licenseService) : ControllerBase
{
    [HttpPost("generate")]
    [AuthorizeMiddleware(["Admin"])]
    public async Task<IActionResult> Generate([FromBody] LicenseGenerationRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var license = await licenseService.GenerateLicenseAsync(request);
        return Ok(license);
    }

    [HttpPost("validate")]
    [AuthorizeMiddleware(["Admin", "User"])]
    public async Task<IActionResult> Validate([FromForm] string licenseKey, [FromForm] string validationParams = "{}",
        [FromForm] IFormFile? licenseFile = null)
    {
        if (string.IsNullOrEmpty(licenseKey))
            return BadRequest("License key is required.");

        if (licenseFile == null || licenseFile.Length == 0)
            return BadRequest("License file is required.");

        using var ms = new MemoryStream();
        await licenseFile.OpenReadStream().CopyToAsync(ms);
        var licenseFileBytes = ms.ToArray();

        var result = await licenseService.ValidateLicenseAsync(licenseKey, licenseFileBytes,
            JsonSerializer.Deserialize<Dictionary<string, string?>>(validationParams));

        return result.IsValid ? Ok("License is valid") : BadRequest(result.Exception);
    }

    [HttpPost("activate")]
    [AuthorizeMiddleware(["Admin"])]
    public async Task<IActionResult> Activate([FromQuery] string licenseKey, [FromQuery] string? hardwareId = null)
    {
        var result = await licenseService.ActivateLicenseAsync(licenseKey, hardwareId);
        return result.IsSuccessful ? Ok() : BadRequest(result.Exception);
    }

    [HttpPost("revoke")]
    [AuthorizeMiddleware(["Admin"])]
    public async Task<IActionResult> Revoke([FromQuery] string licenseKey, [FromQuery] string? hardwareId = null)
    {
        var result = await licenseService.RevokeLicenseAsync(licenseKey, hardwareId);
        return result.IsSuccessful ? Ok() : NotFound(result.Exception);
    }

    [HttpPost("disconnect")]
    [AuthorizeMiddleware(["User"])]
    public async Task<IActionResult> Disconnect([FromQuery] string licenseKey, [FromQuery] string? hardwareId = null)
    {
        var result = await licenseService.DisconnectConcurrentLicenseUser(licenseKey, hardwareId);
        return result.IsSuccessful ? Ok() : NotFound(result.Exception);
    }

    [HttpPost("renew")]
    [AuthorizeMiddleware(["Admin"])]
    public async Task<IActionResult> RenewLicense([FromBody] RenewLicenseRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);
        
        var result = await licenseService.RenewLicenseAsync(request.LicenseKey, request.NewExpirationDate);
        return result.IsSuccessful ? Ok(result.LicenseFile) : BadRequest(result.Message);
    }

    [HttpPost("heartbeat")]
    [RateLimitingMiddleware(5, "00:10:00")]
    [AuthorizeMiddleware(["User"])]
    public async Task<IActionResult> Heartbeat([FromBody] HeartbeatRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);
        
        var result = await licenseService.HeartbeatAsync(request.LicenseKey, request.MachineId);
        return result ? Ok() : NotFound();
    }
}