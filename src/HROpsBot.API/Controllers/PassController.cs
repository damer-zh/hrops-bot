using System.Security.Cryptography;
using System.Text;
using HROpsBot.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HROpsBot.API.Controllers;

/// <summary>
/// Цифровой пропуск сотрудника — генерация и верификация QR-токена
/// </summary>
[ApiController]
[Route("api/pass")]
[Produces("application/json")]
public class PassController(AppDbContext dbContext, IConfiguration configuration) : ControllerBase
{
    // Секрет для подписи токена (настраивается в appsettings)
    private string Secret => configuration["Pass:Secret"] ?? "hropsbot-pass-secret-default-key";

    // TTL токена в секундах (5 минут)
    private const int TokenTtlSeconds = 300;

    /// <summary>Сгенерировать токен цифрового пропуска для сотрудника</summary>
    /// <param name="employeeId">ID сотрудника</param>
    /// <response code="200">token, expiresIn (сек), employee</response>
    /// <response code="404">Сотрудник не найден</response>
    [HttpGet("generate/{employeeId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GeneratePass(int employeeId)
    {
        var employee = await dbContext.Employees
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == employeeId);

        if (employee == null) return NotFound();

        var expiry = DateTimeOffset.UtcNow.AddSeconds(TokenTtlSeconds).ToUnixTimeSeconds();
        var payload = $"{employeeId}~{expiry}";
        var hmac    = ComputeHmac(payload);
        var raw     = $"{payload}~{hmac}";

        // URL-safe base64
        var token = Convert.ToBase64String(Encoding.UTF8.GetBytes(raw))
            .Replace('+', '-').Replace('/', '_').TrimEnd('=');

        return Ok(new
        {
            token,
            expiresIn  = TokenTtlSeconds,
            expiresAt  = DateTimeOffset.UtcNow.AddSeconds(TokenTtlSeconds).ToUnixTimeMilliseconds(),
            employee   = new
            {
                employee.NameRu,
                employee.Department,
                employee.Position
            }
        });
    }

    /// <summary>
    /// Верифицировать QR-токен пропуска.
    /// Вызывается охранником после сканирования — без авторизации.
    /// Проверяет: подпись токена, срок действия, статус сотрудника в системе.
    /// </summary>
    /// <param name="t">URL-safe base64 токен</param>
    /// <response code="200">valid + employee data или valid=false + error message</response>
    [HttpGet("verify")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> VerifyPass([FromQuery] string t)
    {
        if (string.IsNullOrWhiteSpace(t))
            return Ok(new { valid = false, error = "Токен не указан" });

        try
        {
            // Decode URL-safe base64
            var padded = t.Replace('-', '+').Replace('_', '/');
            var rem = padded.Length % 4;
            if (rem > 0) padded += new string('=', 4 - rem);

            var raw = Encoding.UTF8.GetString(Convert.FromBase64String(padded));
            var parts = raw.Split('~');

            if (parts.Length != 3)
                return Ok(new { valid = false, error = "Недействительный пропуск" });

            if (!int.TryParse(parts[0], out var employeeId))
                return Ok(new { valid = false, error = "Недействительный пропуск" });

            if (!long.TryParse(parts[1], out var expiry))
                return Ok(new { valid = false, error = "Недействительный пропуск" });

            // Проверка срока действия
            if (DateTimeOffset.UtcNow.ToUnixTimeSeconds() > expiry)
                return Ok(new { valid = false, error = "Пропуск истёк. Попросите сотрудника обновить QR." });

            // Проверка HMAC (защита от подделки)
            var expectedHmac = ComputeHmac($"{parts[0]}~{parts[1]}");
            var actualBytes   = Encoding.UTF8.GetBytes(parts[2]);
            var expectedBytes = Encoding.UTF8.GetBytes(expectedHmac);

            if (actualBytes.Length != expectedBytes.Length ||
                !CryptographicOperations.FixedTimeEquals(actualBytes, expectedBytes))
            {
                return Ok(new { valid = false, error = "Подпись не совпадает. Пропуск недействителен." });
            }

            // Получаем данные сотрудника
            var employee = await dbContext.Employees
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.Id == employeeId);

            if (employee == null)
                return Ok(new { valid = false, error = "Сотрудник не найден в системе" });

            // Простая проверка: сотрудник активен (не уволен)
            var isActive = !string.IsNullOrEmpty(employee.Department);

            if (!isActive)
                return Ok(new { valid = false, error = "Сотрудник больше не работает в компании" });

            return Ok(new
            {
                valid = true,
                verifiedAt = DateTime.UtcNow,
                employee = new
                {
                    employee.Id,
                    employee.NameRu,
                    employee.NameKk,
                    employee.Department,
                    employee.Position,
                    employee.HiredAt,
                    employee.IsHrAdmin,
                }
            });
        }
        catch
        {
            return Ok(new { valid = false, error = "Ошибка разбора токена" });
        }
    }

    private string ComputeHmac(string data)
    {
        var keyBytes  = Encoding.UTF8.GetBytes(Secret);
        var dataBytes = Encoding.UTF8.GetBytes(data);
        using var hmac = new HMACSHA256(keyBytes);
        return Convert.ToBase64String(hmac.ComputeHash(dataBytes))
            .Replace('+', 'A').Replace('/', 'B').TrimEnd('=');
    }
}
