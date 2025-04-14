using AppInfrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MigApp.Logic;

namespace MigApp
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var builder = Host.CreateApplicationBuilder(args);
            
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

            builder.Services.AddDbContext<AppDbContext>(
                options => options.UseNpgsql(connectionString
                    , b => b.MigrationsAssembly("AppInfrastructure")
                    ).EnableSensitiveDataLogging()
                , ServiceLifetime.Scoped
            );

            builder.Services.AddScoped<App>();
            builder.Services.AddScoped<ImporterService>();
            builder.Services.AddScoped<PrinterService>();

            using var host = builder.Build();

            using (var scope = host.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                try
                {
                    var dbContext = services.GetRequiredService<AppDbContext>();
                    //await dbContext.Database.MigrateAsync();

                    var app = services.GetRequiredService<App>();
                    await app.RunAsync(args);
                }
                catch (Exception ex)
                {
                    await Console.Error.WriteLineAsync($"Fatal error: {ex.ToString()}");
                    Environment.ExitCode = 1;
                    throw;
                }
            }
        }
    }
}
