using Aspire.Hosting;
using YamlDotNet.Core.Tokens;

var builder = DistributedApplication.CreateBuilder(args);



#region MONGO
var mongo = builder.AddMongoDB("mongo")
    .WithDataVolume();
var mongodb = mongo.AddDatabase("mongodb");
#endregion

#region WEB FRONTEND
builder.AddProject<Projects.TgUralWithLove_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithReference(mongodb)
    .WaitFor(mongodb)

    .WithEnvironment("Telegram__Token", "xxx")
    .WithEnvironment("Telegram__BotName", "xxx")
    .WithEnvironment("Auth__TgAdminsList", "xxx,")
    .WithEnvironment("Auth__LocalAdminLogin", "xxx")
    .WithEnvironment("Auth__LocalAdminPassword", "xxx")
    .WithEnvironment("Auth__LocalCustomerLogin", "xxx")
    .WithEnvironment("Auth__LocalCustomerPassword", "xxx");
#endregion

#region BOT AS CONSOLE
//builder.AddProject<Projects.BotAsConsole>("botascons")
//    .WithEnvironment("Telegram__Token", "xxx")
//    .WithReference(mongodb)
//    .WaitFor(mongodb)
//;
#endregion

#region MKRF LOADER
//builder.AddProject<Projects.MkrfLoader>("mkrfloader")
//    .WithReference(mongodb)
//    .WaitFor(mongodb)
//;
#endregion

#region BOT AS WEB
builder.AddProject<Projects.BotAsWebhook>("botasweb")
    .WithEnvironment("Telegram__BotName", "xxx")
    .WithEnvironment("Telegram__Token", "xxx")
    .WithEnvironment("Telegram__WebhookUri", "https://elf-tough-thoroughly.ngrok-free.app/botwebhook")
    .WithReference(mongodb)
    .WaitFor(mongodb);
#endregion

#region NGROK
//важно аргументы передавать по отдельности, а не в одну строку
builder.AddContainer("ngrok", "ngrok/ngrok")
    .WithEnvironment("NGROK_AUTHTOKEN", "xxx")
    .WithArgs("http", "--domain=elf-tough-thoroughly.ngrok-free.app", "http://host.docker.internal:5112");
#endregion

#region MONGO DUMP
//builder.AddContainer("mongodumper", "localhost:5000/barracuda/mongodump")
//    .WithReference(mongodb)
//    .WaitFor(mongodb);
#endregion

builder.Build().Run();
