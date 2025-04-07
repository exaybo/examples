using Logics;
using Logics.Statistics;
using Logics.utils;
using Microsoft.AspNetCore.Components.Authorization;
using MudBlazor.Services;
using NLog.Extensions.Logging;
using System.Globalization;
using TgUralWithLove.Web;
using TgUralWithLove.Web.Auth;
using TgUralWithLove.Web.Components;

var builder = WebApplication.CreateBuilder(args);

CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;


// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

#region logging
//if (!Directory.Exists("logs"))
//{
//    Directory.CreateDirectory("logs");
//    string cur = Directory.GetCurrentDirectory();
//}

builder.Services.AddLogging(loggingBuilder =>
{
    loggingBuilder.ClearProviders();
    loggingBuilder.AddNLog("NLog.config");
});
#endregion

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

//auth
{
    builder.Services.AddAuthorizationCore();
    builder.Services.AddCascadingAuthenticationState();
    builder.Services.AddScoped<AuthService>();
    builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthenticationStateProvider>();//AuthenticationStateProvider - важно, так вызывается GetAuthenticationStateAsync при рендре страницы
    //хранилище статусов аутентификации. ключ - куки с ид. девайса
    builder.Services.AddMemoryCache();
    //для чтения куки
    builder.Services.AddHttpContextAccessor();
}

builder.Services.AddOutputCache();

builder.Services.AddMudServices();

builder.Services.AddScoped<PlaceLogic>();
builder.Services.AddScoped<EventLogic>();
builder.Services.AddScoped<UseStatLogic>(); 
builder.Services.AddScoped<MkEventWrapperLogic>();
builder.Services.AddScoped<MkRankRuleLogic>();
builder.Services.AddHostedService<LogicIndexesCreatorHostedService>();

builder.AddMongoDBClient("mongodb");


var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

//app.UseAuthorization();


app.UseAntiforgery();

app.UseOutputCache();

app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapDefaultEndpoints();


//auth
{
    //ставим куки для идентификации девайса. к идентификатору привязываем состояние аутентификации
    app.Use(async (context, next) =>
    {
        if (!context.Request.Cookies.ContainsKey(CustomAuthenticationStateProvider.DeviceCookieKey))
        {
            context.Response.Cookies.Append(CustomAuthenticationStateProvider.DeviceCookieKey
                , $"{Guid.NewGuid()}-{Guid.NewGuid()}"
                , new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict
                });
        }

        await next.Invoke();
    });

}

app.Run();
