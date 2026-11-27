using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Qubriq.API.Data;
using Qubriq.API.Models;
using Qubriq.API.Services;

namespace Qubriq.API.Controllers;

[ApiController]
[Route("api/webhooks/whatsapp")]
[AllowAnonymous]
public class WhatsAppWebhookController : ControllerBase
{
    private readonly QubriqDbContext _db;
    private readonly WhatsAppCloudService _whatsAppService;
    private readonly IConfiguration _config;
    private readonly ILogger<WhatsAppWebhookController> _logger;

    public WhatsAppWebhookController(
        QubriqDbContext db,
        WhatsAppCloudService whatsAppService,
        IConfiguration config,
        ILogger<WhatsAppWebhookController> logger)
    {
        _db = db;
        _whatsAppService = whatsAppService;
        _config = config;
        _logger = logger;
    }

    // 1. Meta Webhook Verification Handshake
    [HttpGet]
    public IActionResult VerifyWebhook(
        [FromQuery(Name = "hub.mode")] string? mode,
        [FromQuery(Name = "hub.verify_token")] string? verifyToken,
        [FromQuery(Name = "hub.challenge")] string? challenge)
    {
        var expectedToken = _config["WhatsApp:VerifyToken"];

        if (mode == "subscribe" && !string.IsNullOrEmpty(expectedToken) && verifyToken == expectedToken)
        {
            _logger.LogInformation("WhatsApp webhook verified successfully.");
            return Ok(challenge);
        }

        _logger.LogWarning("WhatsApp webhook verification failed. Received token: {Token}", verifyToken);
        return Forbid();
    }

    // 2. Receive Incoming Messages & Delivery Statuses
    [HttpPost]
    public async Task<IActionResult> ReceiveWebhook(CancellationToken ct)
    {
        Request.EnableBuffering();
        using var reader = new StreamReader(Request.Body, Encoding.UTF8, leaveOpen: true);
        var rawBody = await reader.ReadToEndAsync(ct);
        Request.Body.Position = 0;

        var signatureHeader = Request.Headers["X-Hub-Signature-256"].FirstOrDefault();
        if (!IsValidSignature(rawBody, signatureHeader))
        {
            _logger.LogWarning("Rejected WhatsApp webhook due to invalid X-Hub-Signature-256.");
            return Unauthorized();
        }

        using var doc = JsonDocument.Parse(rawBody);
        var root = doc.RootElement;

        if (!root.TryGetProperty("entry", out var entries))
            return Ok();

        foreach (var entry in entries.EnumerateArray())
        {
            if (!entry.TryGetProperty("changes", out var changes)) continue;

            foreach (var change in changes.EnumerateArray())
            {
                if (!change.TryGetProperty("value", out var value)) continue;

                // Handle Delivery Status Updates (sent, delivered, read, failed)
                if (value.TryGetProperty("statuses", out var statuses))
                {
                    foreach (var statusObj in statuses.EnumerateArray())
                    {
                        var wamid = statusObj.GetProperty("id").GetString();
                        var newStatus = statusObj.GetProperty("status").GetString();

                        if (statusObj.TryGetProperty("errors", out var errorsProp))
                        {
                            _logger.LogError("Meta delivery failure for {Wamid}: {Errors}", wamid, errorsProp.GetRawText());
                        }

                        if (!string.IsNullOrEmpty(wamid) && !string.IsNullOrEmpty(newStatus))
                        {
                            var msg = await _db.WhatsAppMessages.FirstOrDefaultAsync(m => m.MetaMessageId == wamid, ct);
                            if (msg != null)
                            {
                                msg.DeliveryStatus = char.ToUpper(newStatus[0]) + newStatus[1..];
                            }
                        }
                    }
                    await _db.SaveChangesAsync(ct);
                }

                // Handle Inbound Customer Messages
                if (value.TryGetProperty("messages", out var messages))
                {
                    string? profileName = null;
                    string? bsuid = null;

                    if (value.TryGetProperty("contacts", out var contacts) && contacts.GetArrayLength() > 0)
                    {
                        var contact = contacts[0];
                        if (contact.TryGetProperty("profile", out var prof) && prof.TryGetProperty("name", out var nameProp))
                            profileName = nameProp.GetString();
                        if (contact.TryGetProperty("user_id", out var userIdProp))
                            bsuid = userIdProp.GetString();
                    }

                    foreach (var msgObj in messages.EnumerateArray())
                    {
                        var wamid = msgObj.GetProperty("id").GetString();
                        if (string.IsNullOrEmpty(wamid)) continue;

                        // Idempotency check against Meta retries
                        if (await _db.WhatsAppMessages.AnyAsync(m => m.MetaMessageId == wamid, ct))
                            continue;

                        var rawFromPhone = msgObj.TryGetProperty("from", out var fromProp) ? fromProp.GetString() ?? "" : "";
                        var fromPhone = rawFromPhone.Trim().TrimStart('+').Replace(" ", "");

                        if (msgObj.TryGetProperty("from_user_id", out var fromUserProp) && string.IsNullOrEmpty(bsuid))
                            bsuid = fromUserProp.GetString();

                        var msgType = msgObj.GetProperty("type").GetString();
                        var textBody = msgType == "text"
                            ? msgObj.GetProperty("text").GetProperty("body").GetString() ?? ""
                            : $"[{msgType} message received]";

                        var conversation = await GetOrCreateConversationAsync(fromPhone, bsuid, profileName, ct);
                        conversation.LastCustomerMessageAt = DateTime.UtcNow;

                        var inboundEntity = new WhatsAppMessage
                        {
                            ConversationId = conversation.Id,
                            MetaMessageId = wamid,
                            Direction = "Inbound",
                            SenderType = "Customer",
                            Content = textBody,
                            DeliveryStatus = "Received",
                            Timestamp = DateTime.UtcNow
                        };
                        _db.WhatsAppMessages.Add(inboundEntity);
                        await _db.SaveChangesAsync(ct);

                        // Stage 1 Smoke-Test Reply (Replaced by QubriqAiAgentService in Stage 2)
                        if (conversation.Status == "AI_Active" && !string.IsNullOrEmpty(fromPhone))
                        {
                            var replyText = $"👋 Welcome to *QUBRIQ Connect*{(profileName != null ? $", {profileName}" : "")}!\n\nWe received: \"{textBody}\"\n_(Stage 1 Webhook active — AI Logistics Agent connecting next.)_";
                            var sentWamid = await _whatsAppService.SendTextMessageAsync(fromPhone, replyText, ct);

                            if (!string.IsNullOrEmpty(sentWamid))
                            {
                                _db.WhatsAppMessages.Add(new WhatsAppMessage
                                {
                                    ConversationId = conversation.Id,
                                    MetaMessageId = sentWamid,
                                    Direction = "Outbound",
                                    SenderType = "System",
                                    Content = replyText,
                                    DeliveryStatus = "Sent",
                                    Timestamp = DateTime.UtcNow
                                });
                                await _db.SaveChangesAsync(ct);
                            }
                        }
                    }
                }
            }
        }

        return Ok();
    }

    private async Task<WhatsAppConversation> GetOrCreateConversationAsync(string phone, string? bsuid, string? profileName, CancellationToken ct)
    {
        var conv = await _db.WhatsAppConversations
            .FirstOrDefaultAsync(c => (!string.IsNullOrEmpty(phone) && c.PhoneNumber == phone) || (!string.IsNullOrEmpty(bsuid) && c.Bsuid == bsuid), ct);

        if (conv != null)
        {
            if (!string.IsNullOrEmpty(profileName)) conv.ProfileName = profileName;
            if (!string.IsNullOrEmpty(bsuid) && string.IsNullOrEmpty(conv.Bsuid)) conv.Bsuid = bsuid;
            return conv;
        }

        var last10 = phone.Length >= 10 ? phone[^10..] : phone;

        var matchedCustomer = !string.IsNullOrEmpty(last10)
            ? await _db.Customers.FirstOrDefaultAsync(c => c.Phone != null && c.Phone.EndsWith(last10), ct)
            : null;

        var matchedRecipient = matchedCustomer != null
            ? await _db.ShipmentRecipients.FirstOrDefaultAsync(r => r.UserId == matchedCustomer.UserId, ct)
            : null;

        conv = new WhatsAppConversation
        {
            PhoneNumber = phone,
            Bsuid = bsuid,
            ProfileName = profileName,
            CustomerId = matchedCustomer?.Id,
            RecipientId = matchedRecipient?.Id,
            Status = "AI_Active"
        };

        _db.WhatsAppConversations.Add(conv);
        await _db.SaveChangesAsync(ct);
        return conv;
    }

    private bool IsValidSignature(string rawBody, string? signatureHeader)
    {
        var appSecret = _config["WhatsApp:AppSecret"];
        if (string.IsNullOrEmpty(appSecret)) return true; // Bypass only if AppSecret is unset in local dev
        if (string.IsNullOrEmpty(signatureHeader) || !signatureHeader.StartsWith("sha256=")) return false;

        var expectedHex = signatureHeader["sha256=".Length..];
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(appSecret));
        var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(rawBody));
        var computedHex = Convert.ToHexString(hashBytes).ToLowerInvariant();

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(computedHex),
            Encoding.UTF8.GetBytes(expectedHex.ToLowerInvariant()));
    }
}