using Microsoft.AspNetCore.Mvc;
using Supabase;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Caching.Memory;
using UrlShortener.Utilities;
using UrlShortener.Models;
using UrlShortener.Services;

namespace UrlShortener.Controllers;

[ApiController]
[Route("[controller]")]
public class UrlController : ControllerBase
{
    private readonly Client _supabaseClient;
    private readonly ILogger<UrlController> _logger;
    private readonly RateLimitService _rateLimitService;
    private readonly CodeGenerator _generator;

    public UrlController(SupabaseService supabaseService, ILogger<UrlController> logger, RateLimitService rateLimitService, CodeGenerator generator)
    {
        _supabaseClient = supabaseService.Client;
        _logger = logger;
        _rateLimitService = rateLimitService;
        _generator = generator;
    }

    [HttpGet("/{shortCode}")]
    public async Task<IActionResult> RedirectUrl(string shortCode)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

        if (string.IsNullOrEmpty(ipAddress))
        {
            return BadRequest("Unable to determine IP address.");
        }

        var cacheKey = $"rate_limit:{ipAddress}";
        if (_rateLimitService.IsRateLimited(cacheKey))
        {
            _logger.LogWarning("Rate limit exceeded for IP: {IpAddress}", ipAddress);
            return StatusCode(429); // too many requests
        }

        _logger.LogInformation("Received request to redirect for short code: {ShortCode}", shortCode);

        var response = await _supabaseClient
            .From<UrlEntry>()
            .Where(x => x.Id == shortCode)
            .Single();

        if (response == null)
        {
            _logger.LogWarning("Short code not found: {ShortCode}", shortCode);
            return NotFound("Shortened URL not found.");
        }

        // Increment access count
        response.AccessCount += 1;

        try
        {
            await _supabaseClient.From<UrlEntry>().Update(response);
            _logger.LogInformation("Incremented access count for short code: {ShortCode}", shortCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update access count for short code: {ShortCode}", shortCode);
        }

        if (string.IsNullOrEmpty(response.FullUrl))
        {
            _logger.LogError("URL entry found, but FullUrl is null or empty for short code: {ShortCode}", shortCode);
            return BadRequest("URL entry found, but FullUrl is invalid.");
        }

        return Redirect(response.FullUrl);
    }

    [HttpGet("/api/my-urls")]
    public async Task<IActionResult> GetMyUrls()
    {
        var authHeader = HttpContext.Request.Headers["Authorization"].ToString();
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
        {
            _logger.LogWarning("Authorization token missing or invalid.");
            return Unauthorized();
        }
        var token = authHeader["Bearer ".Length..].Trim();

        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);
            var email = jwtToken.Claims.FirstOrDefault(c => c.Type == "email")?.Value;
            if (string.IsNullOrEmpty(email))
            {
                _logger.LogWarning("Invalid token: Missing user.");
                return Unauthorized();
            }

            var response = await _supabaseClient
                .From<UrlEntry>()
                .Filter("created_by", Supabase.Postgrest.Constants.Operator.Equals, email)
                .Get();

            if (response == null)
            {
                _logger.LogWarning("No URL entries found.");
                return NotFound("No URL entries found.");
            }
            
            // https://github.com/supabase-community/supabase-csharp/issues/78#issuecomment-1605684300
            var urlEntries = response.Models;  
            var mapped = urlEntries.Select(data => new Dictionary<string, object>
            {
                { "id", data.Id! },
                { "full_url", data.FullUrl! },
                { "created_at", data.CreatedAt },
                { "created_by", data.CreatedBy ?? "default_user" },
                { "access_count", data.AccessCount }
            }).ToList();

            return Ok(mapped);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching URL entries.");
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("/api/shorten")]
    public async Task<IActionResult> ShortenUrl([FromBody] ShortenRequest request)
    {
        _logger.LogInformation("Received request to shorten URL: {Url}", request.Url);

        if (string.IsNullOrEmpty(request.Url))
        {
            _logger.LogWarning("URL is required.");
            return BadRequest("URL is required.");
        }

        var authHeader = HttpContext.Request.Headers["Authorization"].ToString();
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
        {
            _logger.LogWarning("Authorization token missing or invalid.");
            return Unauthorized();
        }
        var token = authHeader["Bearer ".Length..].Trim(); // Remove "Bearer " prefix

        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);
            var email = jwtToken.Claims.FirstOrDefault(c => c.Type == "email")?.Value; // Extract email from the token
            if (string.IsNullOrEmpty(email))
            {
                _logger.LogWarning("Invalid token: Missing user."); // it should be null if user is unauthorized
                return Unauthorized();
            }

            _logger.LogInformation("Authenticated user: {email}", email);
            if (string.IsNullOrEmpty(request.Url))
            {
                _logger.LogWarning("URL is required.");
                return BadRequest("URL is required.");
            }

            string cacheKey = $"rate_limit:{email}";
            if (_rateLimitService.IsRateLimited(cacheKey))
            {
                _logger.LogWarning("Rate limit exceeded for user: {email}", email);
                return StatusCode(429); // Too Many Requests
            }

            string shortCode = await _generator.GenerateShortCode();
            _logger.LogInformation("Generated short code: {ShortCode}", shortCode);

            var newUrl = new UrlEntry
            {
                Id = shortCode,
                FullUrl = request.Url,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = email,
                AccessCount = 0
            };

            try
            {
                _logger.LogInformation("Inserting new URL entry into Supabase...");
                var response = await _supabaseClient.From<UrlEntry>().Insert(newUrl);
                _logger.LogInformation("URL entry inserted successfully: {ShortCode}", shortCode);
                return Ok(new { code = shortCode });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inserting URL entry into Supabase: {Message}", ex.Message);
                return BadRequest(new { error = ex.Message });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Invalid token format or validation failed.");
            return Unauthorized();
        }
    }
}