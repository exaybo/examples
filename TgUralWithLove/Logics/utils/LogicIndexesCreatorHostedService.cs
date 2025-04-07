using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logics.utils
{
    public class LogicIndexesCreatorHostedService : BackgroundService
    {
        IServiceProvider serviceProvider;
        public LogicIndexesCreatorHostedService(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using (var scope = serviceProvider.CreateScope())
            {
                var EventLogic = scope.ServiceProvider.GetRequiredService<EventLogic>();
                var PlaceLogic = scope.ServiceProvider.GetRequiredService<PlaceLogic>();
                var MkEventWrapperLogic = scope.ServiceProvider.GetRequiredService<MkEventWrapperLogic>();
                await EventLogic.CreateIndexes();
                await PlaceLogic.CreateIndexes();
                await MkEventWrapperLogic.CreateIndexes();
            }
        }
    }
}
