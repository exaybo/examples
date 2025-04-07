using BotLib;
using BotLibrary;
using Logics.Statistics;
using Logics;
using Telegram.Bot.Types;
using static BotLib.BotConstants;
using System.Threading.Channels;
using BotAsWebhook;
using NLog.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// Configure the HTTP request pipeline.
#region logging
builder.Services.AddLogging(loggingBuilder =>
{
    loggingBuilder.ClearProviders();
    loggingBuilder.AddNLog("NLog.config");
});
#endregion

builder.Services.ConfigureTelegramBot<Microsoft.AspNetCore.Http.Json.JsonOptions>(opt => opt.SerializerOptions);

// Конфигурируем зависимости
builder.AddMongoDBClient("mongodb");

builder.Services.AddSingleton(Channel.CreateUnbounded<Update>());
builder.Services.AddHostedService<WebhookProcessingService>();


builder.Services.AddSingleton<BotService>();
builder.Services.AddScoped<CommandHandler>();
builder.Services.AddScoped<PlaceLogic>();
builder.Services.AddScoped<EventLogic>();
builder.Services.AddScoped<UseStatLogic>();
builder.Services.AddScoped<MkEventWrapperLogic>();
builder.Services.AddScoped<MkRankRuleLogic>();
builder.Services.AddMemoryCache();


var app = builder.Build();

app.MapPost("/botwebhook", (HttpContext context, Update update) => 
HandleUpdate(context, update));

app.Run();

async Task<IResult> HandleUpdate(HttpContext context, Update update)
{
    if (context.Request.Headers.TryGetValue("X-Telegram-Bot-Api-Secret-Token", out var secretToken))
    {
        if(secretToken == BotService.webhookSecret)
        {
            var updateQueue = app.Services.GetRequiredService<Channel<Update>>();
            await updateQueue.Writer.WriteAsync(update);
        }
    }
    else
    {
        Console.WriteLine("Header 'X-Telegram-Bot-Api-Secret-Token' not found.");
    }
    return Results.Ok();
}