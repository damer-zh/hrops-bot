using HROpsBot.API.BackgroundServices;
using HROpsBot.Core.Interfaces;
using HROpsBot.Core.Dialog;
using HROpsBot.Core.Handlers;
using HROpsBot.Core.NLU;
using HROpsBot.Core.Services;
using HROpsBot.Infrastructure.Cache;
using HROpsBot.Infrastructure.Persistence;
using HROpsBot.Infrastructure.Services;
using HROpsBot.Infrastructure.Telegram;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using StackExchange.Redis;
using Telegram.Bot;
using Telegram.Bot.Types;

var builder = WebApplication.CreateBuilder(args);

// ---- Configuration ----
var telegramToken = builder.Configuration["Telegram:BotToken"]
    ?? throw new InvalidOperationException("Telegram:BotToken is required");
var webhookUrl = builder.Configuration["Telegram:WebhookUrl"];
var geminiApiKey = builder.Configuration["Gemini:ApiKey"]
    ?? throw new InvalidOperationException("Gemini:ApiKey is required");
var postgresConn = builder.Configuration.GetConnectionString("Postgres");
var redisConn = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";

// ---- Database ----
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseNpgsql(postgresConn));

// ---- Redis ----
builder.Services.AddSingleton<IConnectionMultiplexer>(
    ConnectionMultiplexer.Connect(redisConn));
builder.Services.AddSingleton<RedisSessionStore>();

// ---- Telegram Bot ----
builder.Services.AddSingleton<ITelegramBotClient>(new TelegramBotClient(telegramToken));

// ---- Gemini NLU ----
builder.Services.Configure<GeminiOptions>(opt =>
{
    opt.ApiKey = geminiApiKey;
    opt.Model = builder.Configuration["Gemini:Model"] ?? "gemini-flash-latest";
});
builder.Services.AddHttpClient<GeminiNluClient>();

// ---- Real DB Services ----
builder.Services.AddScoped<IHrService, HrService>();
builder.Services.AddScoped<IDocService, DocService>();
builder.Services.AddScoped<IEquipmentService, EquipmentService>();
builder.Services.AddScoped<ITaskService, TaskService>();
builder.Services.AddScoped<IItRequestService, ItRequestService>();

// ---- i18n ----
builder.Services.AddSingleton<I18nService>();
builder.Services.AddSingleton<CsatService>();

// ---- Handlers ----
builder.Services.AddScoped<VacationHandler>();
builder.Services.AddScoped<CertificateHandler>();
builder.Services.AddScoped<RegulationSearchHandler>();
builder.Services.AddScoped<EquipmentHandler>();
builder.Services.AddScoped<TaskHandler>();
builder.Services.AddScoped<AppointmentHandler>();
builder.Services.AddScoped<FaqHandler>();
builder.Services.AddScoped<OnboardingHandler>();

// ---- Dialog Manager ----
builder.Services.AddScoped<DialogManager>();
builder.Services.AddScoped<TelegramBotAdapter>();

// ---- Background Services ----
builder.Services.AddHostedService<TaskPollingService>();

// ---- API ----
builder.Services.AddControllers();
builder.Services.AddSignalR();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(opt =>
{
    opt.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "HROpsBot API",
        Version = "v1",
        Description = "REST API для Telegram Mini App HROpsBot — автоматизация HR-процессов: " +
                      "онбординг, отпуска, справки, IT-заявки, оборудование.",
        Contact = new OpenApiContact { Name = "HROpsBot Team" }
    });

    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (System.IO.File.Exists(xmlPath))
        opt.IncludeXmlComments(xmlPath);
});

// ---- CORS (для TMA) ----
builder.Services.AddCors(opt => opt.AddDefaultPolicy(p =>
    p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

// ---- Middleware ----
app.UseCors();
app.UseSwagger();
app.UseSwaggerUI(opt =>
{
    opt.SwaggerEndpoint("/swagger/v1/swagger.json", "HROpsBot API v1");
    opt.RoutePrefix = "swagger";
    opt.DocumentTitle = "HROpsBot API Docs";
});

app.MapControllers();
app.MapHub<HROpsBot.API.Hubs.NotificationHub>("/notifications");

// ---- Migrate DB & Seed ----
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    // Сначала пытаемся создать БД и основные таблицы, если БД не существовала вообще
    await db.Database.EnsureCreatedAsync();

    // Если БД уже существовала, EnsureCreatedAsync не создаст новые таблицы (например, ItRequests или OnboardingProgresses).
    // Поэтому принудительно прогоняем скрипт создания таблиц с IF NOT EXISTS.
    try
    {
        var script = db.Database.GenerateCreateScript();
        var statements = script.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var statement in statements)
        {
            var cleanStatement = statement.Trim();
            if (string.IsNullOrWhiteSpace(cleanStatement)) continue;

            if (cleanStatement.StartsWith("CREATE TABLE", StringComparison.OrdinalIgnoreCase))
            {
                cleanStatement = cleanStatement.Replace("CREATE TABLE \"", "CREATE TABLE IF NOT EXISTS \"");
            }
            else if (cleanStatement.StartsWith("CREATE INDEX", StringComparison.OrdinalIgnoreCase) ||
                     cleanStatement.StartsWith("CREATE UNIQUE INDEX", StringComparison.OrdinalIgnoreCase))
            {
                cleanStatement = cleanStatement.Replace("CREATE INDEX \"", "CREATE INDEX IF NOT EXISTS \"")
                                               .Replace("CREATE UNIQUE INDEX \"", "CREATE UNIQUE INDEX IF NOT EXISTS \"");
            }

            try
            {
                await db.Database.ExecuteSqlRawAsync(cleanStatement);
            }
            catch (Exception ex)
            {
                // Игнорируем ошибки вроде "relation already exists" или дублирования ограничений,
                // так как это ожидаемо для уже существующих таблиц.
                app.Logger.LogDebug("DB Init Statement: {Message}", ex.Message);
            }
        }
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Ошибка при автоматической миграции схемы БД");
    }

    await SeedData.SeedAsync(db);
}

// ---- Register Telegram Webhook ----
if (!string.IsNullOrEmpty(webhookUrl))
{
    var botClient = app.Services.GetRequiredService<ITelegramBotClient>();
    await botClient.SetWebhookAsync(url: $"{webhookUrl}/api/bot/webhook");
    app.Logger.LogInformation("Webhook set to {Url}", webhookUrl);
}

app.Run();
