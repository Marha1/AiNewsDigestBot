using System.Net;
using AiNewsDigestBot.Host.Features.Digest;
using AiNewsDigestBot.Host.Features.Help;
using AiNewsDigestBot.Host.Features.Latest;
using AiNewsDigestBot.Host.Features.MySubs;
using AiNewsDigestBot.Host.Features.Search;
using AiNewsDigestBot.Host.Features.Settings;
using AiNewsDigestBot.Host.Features.Start;
using AiNewsDigestBot.Host.Features.Subscribe;
using AiNewsDigestBot.Host.Features.Subscribe.Unsubscribe;
using AiNewsDigestBot.Host.Features.Topics;
using AiNewsDigestBot.Host.Shared.Data;
using AiNewsDigestBot.Host.Shared.Data.Configurations;
using AiNewsDigestBot.Host.Shared.Services;
using AiNewsDigestBot.Host.Shared.Services.ChatService.Implementations;
using AiNewsDigestBot.Host.Shared.Services.ChatService.Interfaces;
using AiNewsDigestBot.Host.Shared.Services.Parser;
using AiNewsDigestBot.Host.Shared.Services.Parser.TopicDetected;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
    .AddEnvironmentVariables(); // <- Переменные окружения имеют приоритет

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));


builder.Services.Configure<TelegramConfig>(
    builder.Configuration.GetSection(TelegramConfig.SectionName));

builder.Services.AddOptions<TelegramConfig>()
    .Validate(config => !string.IsNullOrWhiteSpace(config.Token), 
        "❌ Telegram Bot Token is not configured. " +
        "Set 'Telegram:Token' in appsettings.json or TELEGRAM__TOKEN environment variable.")
    .ValidateOnStart();

builder.Services.AddSingleton<ITelegramBotClient>(sp =>
{
    var config = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<TelegramConfig>>().Value;
    var token = config.Token;
    
    if (string.IsNullOrWhiteSpace(token))
    {
        throw new InvalidOperationException(
            "❌ Telegram Bot Token is not configured.\n" +
            "Please set:\n" +
            "  - In appsettings.json: 'Telegram:Token'\n" +
            "  - Or environment variable: TELEGRAM__TOKEN\n" +
            "  - Or in Docker: -e TELEGRAM__TOKEN=your_token");
    }
    
    return new TelegramBotClient(token);
});
builder.Services.AddHostedService<TelegramBotService>();
builder.Services.AddScoped<StartHandler>();
builder.Services.AddScoped<TopicsHandler>();
builder.Services.AddScoped<SubscribeHandler>();
builder.Services.AddScoped<UnsubscribeHandler>();
builder.Services.AddScoped<MySubsHandler>();
builder.Services.AddScoped<HelpHandler>();
builder.Services.AddScoped<TopicDetector>();
builder.Services.AddScoped<MainNewsParser>();
builder.Services.AddScoped<DigestHandler>();
builder.Services.AddScoped<IChatService, ChatService>();
builder.Services.AddScoped<ArticleService>();
builder.Services.AddScoped<SourceService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<SubscriptionService>();
builder.Services.AddScoped<SearchHandler>();
builder.Services.AddScoped<CancelHandler>();
builder.Services.AddScoped<DigestSchedulerService>();
builder.Services.AddScoped<LatestHandler>();
builder.Services.AddScoped<SettingsHandler>();

builder.Services.AddSingleton<SearchStateService>();
builder.Services.AddSingleton<SettingsStateService>();

builder.Services.AddHttpClient<NewsParser>((serviceProvider, client) =>
{
    client.DefaultRequestHeaders.Add("User-Agent",
        "Mozilla/5.0 (compatible; AiNewsDigestBot/1.0; +https://github.com/Marha1/AiNewsDigestBot)");
    client.Timeout = TimeSpan.FromSeconds(30);
})
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
    AllowAutoRedirect = true,
    MaxAutomaticRedirections = 5
})
.SetHandlerLifetime(TimeSpan.FromMinutes(5));

builder.Services.AddHangfire(config => config
    .UseRecommendedSerializerSettings()
    .UsePostgreSqlStorage(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddHangfireServer();
builder.Services.AddSingleton<HangfireJobScheduler>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.UseHangfireDashboard();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

using (var scope = app.Services.CreateScope())
{
    var scheduler = scope.ServiceProvider.GetRequiredService<HangfireJobScheduler>();
    scheduler.StartScheduler();
    scheduler.EnqueueInitialParse();
}

app.Run();