using BotLib;
using System.Threading.Channels;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using static BotLib.BotConstants;

namespace BotAsWebhook
{
    public class WebhookProcessingService : BackgroundService
    {
        private readonly BotService _botService;
        private readonly Channel<Update> _updateQueue;

        public WebhookProcessingService(Channel<Update> updateQueue, BotService botService)
        {
            _updateQueue = updateQueue;
            _botService = botService;
        }

        //обработка по одному за раз может стать узким местом
        //тогад можно попробовать
        // Параллельная обработка: Запускайте несколько задач для обработки элементов одновременно.
        // var tasks = Enumerable.Range(0, Environment.ProcessorCount) // или фиксированное число потоков
        //  .Select(_ => ProcessUpdates(stoppingToken))
        //  .ToArray();
        //await Task.WhenAll(tasks);
        // Обработка партиями: Читайте несколько элементов из канала за один раз и обрабатывайте их вместе.

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            //init
            await _botService.Init(BotModeKinds.webhook);


            await foreach (var update in _updateQueue.Reader.ReadAllAsync(stoppingToken))
            {
                try
                {
                    await _botService.OnUpdate(update);
                }
                catch (Exception ex)
                {
                    // Логирование ошибки
                    Console.WriteLine($"Error processing update: {ex.Message}");
                }
            }
        }
    }

}
