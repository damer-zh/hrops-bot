using HROpsBot.Infrastructure.Telegram;
using Microsoft.AspNetCore.Mvc;
using Telegram.Bot.Types;

namespace HROpsBot.API.Controllers;

[ApiController]
[Route("api/bot")]
public class BotController(TelegramBotAdapter botAdapter, ILogger<BotController> logger) : ControllerBase
{
    [HttpPost("webhook")]
    public async Task<IActionResult> Webhook()
    {
        using var reader = new System.IO.StreamReader(Request.Body);
        var body = await reader.ReadToEndAsync();
        var update = Newtonsoft.Json.JsonConvert.DeserializeObject<Update>(body);
        
        if (update != null)
        {
            logger.LogDebug("Webhook received: {UpdateType}", update.Type);
            await botAdapter.HandleUpdateAsync(update);
        }
        return Ok();
    }

    [HttpGet("health")]
    public IActionResult Health() => Ok(new { status = "ok", time = DateTime.UtcNow });
}
