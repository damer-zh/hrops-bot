using System.Text.Json;

namespace HROpsBot.Core.Services;

/// <summary>
/// Формирует параллельные RU+KK сообщения из шаблонов.
/// </summary>
public class I18nService
{
    private readonly Dictionary<string, string> _ru;
    private readonly Dictionary<string, string> _kk;

    public I18nService()
    {
        _ru = LoadMessages("ru");
        _kk = LoadMessages("kk");
    }

    private static Dictionary<string, string> LoadMessages(string lang)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Resources", $"messages.{lang}.json");
        if (!File.Exists(path)) return [];
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? [];
    }

    /// <summary>
    /// Возвращает строку с обоими языками: "RU текст\n\nKK текст"
    /// </summary>
    public string Get(string key, object? args = null)
    {
        var ru = Format(_ru.GetValueOrDefault(key, key), args);
        var kk = Format(_kk.GetValueOrDefault(key, key), args);
        return $"{ru}\n\n{kk}";
    }

    /// <summary>
    /// Возвращает только RU строку.
    /// </summary>
    public string GetRu(string key, object? args = null) =>
        Format(_ru.GetValueOrDefault(key, key), args);

    /// <summary>
    /// Возвращает только KK строку.
    /// </summary>
    public string GetKk(string key, object? args = null) =>
        Format(_kk.GetValueOrDefault(key, key), args);

    private static string Format(string template, object? args)
    {
        if (args == null) return template;
        // Поддержка {PropertyName} подстановок через рефлексию
        var type = args.GetType();
        var result = template;
        foreach (var prop in type.GetProperties())
        {
            var value = prop.GetValue(args)?.ToString() ?? "";
            result = result.Replace($"{{{prop.Name}}}", value);
        }
        return result;
    }
}
