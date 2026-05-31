using HROpsBot.API.BackgroundServices;
using HROpsBot.Core.Dialog;
using HROpsBot.Core.Handlers;
using HROpsBot.Core.NLU;
using HROpsBot.Core.Services;
using HROpsBot.Infrastructure.Cache;
using HROpsBot.Infrastructure.Persistence;
using HROpsBot.Infrastructure.Telegram;
using HROpsBot.MockAPI;
using Microsoft.EntityFrameworkCore;
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

// ---- Mock API Services ----
builder.Services.AddSingleton<MockHRService>();
builder.Services.AddSingleton<MockDocService>();
builder.Services.AddSingleton<MockEquipmentService>();
builder.Services.AddSingleton<MockTaskService>();

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

// ---- Dialog Manager ----
builder.Services.AddScoped<DialogManager>();
builder.Services.AddScoped<TelegramBotAdapter>();

// ---- Background Services ----
builder.Services.AddHostedService<TaskPollingService>();

// ---- API ----
builder.Services.AddControllers();
builder.Services.AddSignalR();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ---- CORS (для TMA) ----
builder.Services.AddCors(opt => opt.AddDefaultPolicy(p =>
    p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

// ---- Middleware ----
app.UseCors();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();
app.MapHub<HROpsBot.API.Hubs.NotificationHub>("/notifications");

// ---- Migrate DB & Seed ----
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    // Очищаем БД перед созданием, чтобы сбросить старое состояние (__EFMigrationsHistory)
    await db.Database.EnsureDeletedAsync(); 
    await db.Database.EnsureCreatedAsync(); // Создаст таблицы, если их нет
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
