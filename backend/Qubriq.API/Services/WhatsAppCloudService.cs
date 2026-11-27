using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Qubriq.API.Services;

public class WhatsAppCloudService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _config;
    private readonly ILogger<WhatsAppCloudService> _logger;

    public WhatsAppCloudService(HttpClient httpClient, IConfiguration config, ILogger<WhatsAppCloudService> logger)
    {
        _httpClient = httpClient;
        _config = config;
        _logger = logger;
    }

    public async Task<string?> SendTextMessageAsync(string toPhone, string bodyText, CancellationToken ct = default)
    {
        var payload = new
        {
            messaging_product = "whatsapp",
            recipient_type = "individual",
            to = toPhone,
            type = "text",
            text = new { preview_url = false, body = bodyText }
        };

        return await PostToMetaAsync(payload, ct);
    }

    public async Task<string?> SendTemplateMessageAsync(string toPhone, string templateName, string languageCode, string[] bodyParameters, CancellationToken ct = default)
    {
        var payload = new
        {
            messaging_product = "whatsapp",
            to = toPhone,
            type = "template",
            template = new
            {
                name = templateName,
                language = new { code = languageCode },
                components = bodyParameters.Length > 0
                    ? new[]
                    {
                        new
                        {
                            type = "body",
                            parameters = bodyParameters.Select(p => new { type = "text", text = p }).ToArray()
                        }
                    }
                    : Array.Empty<object>()
            }
        };

        return await PostToMetaAsync(payload, ct);
    }

    private async Task<string?> PostToMetaAsync(object payload, CancellationToken ct)
    {
        var version = _config["WhatsApp:ApiVersion"] ?? "v24.0";
        var phoneNumberId = _config["WhatsApp:PhoneNumberId"];
        var accessToken = _config["WhatsApp:AccessToken"];

        var url = $"https://graph.facebook.com/{version}/{phoneNumberId}/messages";
        var json = JsonSerializer.Serialize(payload);

        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.SendAsync(request, ct);
        var responseBody = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Meta WhatsApp API Error ({StatusCode}): {Body}", response.StatusCode, responseBody);
            return null;
        }

        using var doc = JsonDocument.Parse(responseBody);
        if (doc.RootElement.TryGetProperty("messages", out var msgs) && msgs.GetArrayLength() > 0)
        {
            return msgs[0].GetProperty("id").GetString(); // Returns wamid
        }

        return null;
    }
}