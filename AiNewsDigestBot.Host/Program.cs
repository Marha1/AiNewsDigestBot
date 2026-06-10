using AiNewsDigestBot.Host.Features.MySubs;
using AiNewsDigestBot.Host.Features.Start;
using AiNewsDigestBot.Host.Features.Subscribe;
using AiNewsDigestBot.Host.Features.Topics;
using AiNewsDigestBot.Host.Features.Unsubscribe;
using AiNewsDigestBot.Host.Services.TelegramBot;
using AiNewsDigestBot.Host.Shared.Data;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
// Telegram Bot
builder.Services.AddSingleton<ITelegramBotClient>(sp =>
    new TelegramBotClient(builder.Configuration["Telegram:Token"]));
//сервисы фич
builder.Services.AddHostedService<TelegramBotService>();
builder.Services.AddScoped<StartHandler>();
builder.Services.AddScoped<TopicsHandler>();
builder.Services.AddScoped<SubscribeHandler>();
builder.Services.AddScoped<UnsubscribeHandler>();
builder.Services.AddHttpClient();
builder.Services.AddScoped<MySubsHandler>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// Применяем миграции
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

app.Run();