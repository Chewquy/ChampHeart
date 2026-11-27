using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using ChampHeart.MVC.Models;

namespace ChampHeart.MVC.Services;

// Shared code for the services that call the WebAPI
public abstract class ApiServiceBase
{
    protected readonly HttpClient _httpClient;

    protected ApiServiceBase(HttpClient httpClient, IHttpContextAccessor httpContextAccessor)
    {
        _httpClient = httpClient;

        // Send the JWT saved in session as a Bearer token
        var session = httpContextAccessor.HttpContext?.Session;

        if (session != null)
        {
            var token = new ChampHeartSession(session).GetToken();

            if (token != null)
            {
                _httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", token.Token);
            }
        }
    }

    // Read the error message returned by the API ({ message } or { errors })
    protected static async Task<string> ReadErrorAsync(HttpResponseMessage response)
    {
        try
        {
            var body = await response.Content.ReadAsStringAsync();

            if (!string.IsNullOrWhiteSpace(body))
            {
                using var document = JsonDocument.Parse(body);
                var root = document.RootElement;

                if (root.ValueKind == JsonValueKind.Object)
                {
                    if (root.TryGetProperty("message", out var message) &&
                        message.ValueKind == JsonValueKind.String)
                    {
                        return message.GetString() ?? string.Empty;
                    }

                    if (root.TryGetProperty("errors", out var errors))
                    {
                        // Identity errors: ["...", "..."]
                        if (errors.ValueKind == JsonValueKind.Array)
                        {
                            return string.Join(" ", errors.EnumerateArray()
                                .Select(e => e.GetString()));
                        }

                        // Validation errors: { "Field": ["..."] }
                        if (errors.ValueKind == JsonValueKind.Object)
                        {
                            return string.Join(" ", errors.EnumerateObject()
                                .SelectMany(field => field.Value.EnumerateArray())
                                .Select(e => e.GetString()));
                        }
                    }
                }
            }
        }
        catch (JsonException)
        {
            // Body was not JSON, fall back to a generic message
        }

        return response.StatusCode switch
        {
            HttpStatusCode.Unauthorized => "Your session has expired. Please log in again.",
            HttpStatusCode.Forbidden => "You do not have permission to do this.",
            HttpStatusCode.NotFound => "The requested item was not found.",
            _ => "Something went wrong. Please try again."
        };
    }
}
