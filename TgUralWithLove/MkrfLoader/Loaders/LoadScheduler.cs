
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MkrfLoader.Loaders
{
    public class LoadScheduler : BackgroundService
    {
        private ILogger _logger;
        private IServiceProvider _serviceProvider;
        public LoadScheduler(ILogger<LoadScheduler> logger, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            DateTime lastExecution = default;
            while (!stoppingToken.IsCancellationRequested)
            {
                var now = DateTime.Now;
                if (now.Date > lastExecution.Date 
                    && now.Hour >= 2
                    //&& now.Hour <= 6
                    )
                {
                    try
                    {
                        using (var scope = _serviceProvider.CreateScope())
                        {
                            var eventsLoader = scope.ServiceProvider.GetRequiredService<EventsLoader>();
                            await eventsLoader.StartLoadSession();
                        }

                        lastExecution = now;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"{ex.Message}; {ex.InnerException?.Message}");
                    }
                }

                await Task.Delay(TimeSpan.FromHours(1));
            }
        }
    }
}
