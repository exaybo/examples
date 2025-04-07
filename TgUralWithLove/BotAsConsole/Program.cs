using BotLib;
using BotLibrary;
using Logics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading.Tasks;
using Aspire.MongoDB.Driver;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using Logics.Statistics;
using static BotLib.BotConstants;

class Program
{
    static async Task Main(string[] args)
    {
        //using var host = CreateHostBuilder(args).Build();

        //// Получение корневого сервиса
        //var app = host.Services.GetRequiredService<App>();
        //await app.RunAsync();

        var builder = Host.CreateApplicationBuilder(args);

        #region logging
        if (!Directory.Exists("log"))
        {
            Directory.CreateDirectory("log");
        }

        builder.Services.AddLogging(loggingBuilder =>
        {
            loggingBuilder.ClearProviders();
            loggingBuilder.AddNLog("NLog.config");
        });
        #endregion

        // Конфигурируем зависимости
        builder.AddMongoDBClient("mongodb");

        builder.Services.AddTransient<App>();
        builder.Services.AddSingleton<IMyService, MyService>();
        builder.Services.AddSingleton<BotService>();
        builder.Services.AddScoped<CommandHandler>();
        builder.Services.AddScoped<PlaceLogic>();
        builder.Services.AddScoped<EventLogic>();
        builder.Services.AddScoped<UseStatLogic>();


        // Получаем IConfiguration
        //var configuration = builder.Configuration;
        //var mySetting = configuration["MySetting"];
        //Console.WriteLine($"MySetting: {mySetting}");

        var app = builder.Services.BuildServiceProvider().GetRequiredService<App>();
        await app.RunAsync();
    }

    static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                // Регистрация зависимостей
                services.AddTransient<App>(); // Основной класс приложения
                services.AddScoped<IMyService, MyService>();
                services.AddSingleton<BotService>();
                services.AddScoped<CommandHandler>();
                services.AddScoped<PlaceLogic>();
                services.AddScoped<EventLogic>();
                
            });
}

public class App
{
    private readonly IMyService _myService;
    private readonly BotService _botService;
    public App(IMyService myService, BotService botService)
    {
        _myService = myService;
        _botService = botService;
    }

    public async Task RunAsync()
    {
        Console.WriteLine("Application started. Press Ctrl+C to exit.");

        var tcs = new TaskCompletionSource();
        Console.CancelKeyPress += (sender, eventArgs) =>
        {
            eventArgs.Cancel = true; // Предотвращает немедленное завершение приложения.
            tcs.SetResult();
        };

        await _myService.DoWorkAsync();

        await _botService.Init(BotModeKinds.polling);

        // Ожидаем завершения через Ctrl+C
        await tcs.Task;

        Console.WriteLine("Application shutting down.");
    }
}

public interface IMyService
{
    Task DoWorkAsync();
}

public class MyService : IMyService
{
    public Task DoWorkAsync()
    {
        Console.WriteLine("MyService is working...");
        return Task.CompletedTask;
    }
}
