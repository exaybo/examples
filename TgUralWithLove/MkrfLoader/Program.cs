using Logics;
using Logics.utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MkrfLoader.Loaders;
using NLog.Extensions.Logging;
using System.Net;
using System.Net.Sockets;

var builder = Host.CreateApplicationBuilder(args);

#region logging
builder.Services.AddLogging(loggingBuilder =>
{
    loggingBuilder.ClearProviders();
    loggingBuilder.AddNLog("NLog.config");
});
#endregion

// Конфигурируем зависимости
builder.AddMongoDBClient("mongodb");

//внедрение преднастроенного httpclient для EventsLoader
builder.Services.AddHttpClient("riko", //- именованный, т.к. AddHttpClient<EventsLoader> не работает
    client =>
    {
        //Таймаут
        client.Timeout = TimeSpan.FromMinutes(20); 
        //запрашиваем сжатие
        client.DefaultRequestHeaders.AcceptEncoding.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("gzip"));
        client.DefaultRequestHeaders.AcceptEncoding.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("deflate"));
        client.DefaultRequestHeaders.AcceptEncoding.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("br"));
    }
    )
    .ConfigurePrimaryHttpMessageHandler(config => new SocketsHttpHandler
    {
        AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate | DecompressionMethods.Brotli,

        ConnectCallback = async (context, cancellationToken) =>
        {
            IPHostEntry ipHostEntry = await Dns.GetHostEntryAsync(context.DnsEndPoint.Host);

            IPAddress ipAddress = ipHostEntry
                .AddressList
                .FirstOrDefault(i => i.AddressFamily == AddressFamily.InterNetworkV6);

            if (ipAddress == null)
            {
                ipAddress = ipHostEntry
                    .AddressList
                    .FirstOrDefault(i => i.AddressFamily == AddressFamily.InterNetwork);
            }

            if (ipAddress == null)
                throw new Exception($"no address for {context.DnsEndPoint.Host}");

            Console.WriteLine($"address for {context.DnsEndPoint.Host} - {ipAddress}");

            TcpClient tcp = new();
            await tcp.ConnectAsync(ipAddress, context.DnsEndPoint.Port, cancellationToken);

            return tcp.GetStream();
        }
    });
//автоматическая декомпрессия
//.ConfigurePrimaryHttpMessageHandler(config => new HttpClientHandler
//{
//    AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate | DecompressionMethods.Brotli
//})
//;


//.ConfigurePrimaryHttpMessageHandler(() =>
//{
//    // Создаем и настраиваем SocketsHttpHandler
//    var handler = new SocketsHttpHandler
//    {
//        ConnectCallback
//    };

//    // Возвращаем настроенный SocketsHttpHandler
//    return handler;
//});

builder.Services.AddScoped<PlaceLogic>();
builder.Services.AddScoped<EventLogic>();
builder.Services.AddScoped<EventsLoader>();
builder.Services.AddScoped<MkEventWrapperLogic>();
builder.Services.AddScoped<MkRankRuleLogic>();
builder.Services.AddHostedService<LoadScheduler>();
builder.Services.AddHostedService<LogicIndexesCreatorHostedService>();

var app = builder.Build();
await app.RunAsync();
//// Обработка завершения приложения через Ctrl+C
//var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
//var tcs = new TaskCompletionSource();
//lifetime.ApplicationStopping.Register(() => tcs.SetResult());

//// Ожидаем завершения
//await tcs.Task;