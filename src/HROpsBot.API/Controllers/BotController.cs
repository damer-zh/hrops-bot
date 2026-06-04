using HROpsBot.Infrastructure.Telegram;
using Microsoft.AspNetCore.Mvc;
using Telegram.Bot.Types;

namespace HROpsBot.API.Controllers;

/// <summary>
/// Telegram Bot webhook и служебные эндпоинты
/// </summary>
[ApiController]
[Route("api/bot")]
[Produces("application/json")]
public class BotController(TelegramBotAdapter botAdapter, ILogger<BotController> logger) : ControllerBase
{
    /// <summary>Получить Telegram update через webhook</summary>
    /// <remarks>Эндпоинт вызывается Telegram-серверами автоматически. Не вызывайте его вручную.</remarks>
    /// <response code="200">Update успешно обработан</response>
    [HttpPost("webhook")]
    [ProducesResponseType(StatusCodes.Status200OK)]
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

    /// <summary>Проверка доступности API</summary>
    /// <response code="200"><c>{ "status": "ok", "time": "2024-..." }</c></response>
    [HttpGet("health")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Health() => Ok(new { status = "ok", time = DateTime.UtcNow });
}
