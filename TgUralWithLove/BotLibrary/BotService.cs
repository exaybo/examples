using Amazon.Runtime;
using BotLibrary;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InlineQueryResults;
using static BotLib.BotConstants;

namespace BotLib
{
    public class BotService : IDisposable
    {
        IConfiguration configuration;
        ILogger logger;
        IServiceProvider serviceProvider;
        IMemoryCache memoryCache;
        CancellationTokenSource cts = new CancellationTokenSource();

        public static string webhookSecret = Guid.NewGuid().ToString(); //секрет чтоб удостоверится, чоо вебхук пришел из телеграма


        Tuple<BotConstants.UBotCommands, string>[] commandsAry =
        {
            new(BotConstants.UBotCommands.uwl_choise,$"Рекомендации канала на следующие {BotConstants.SoonDays} дней"),
            new(BotConstants.UBotCommands.mkrf_events,$"События минкульта в следующие {BotConstants.SoonDays} дней"),
            new(BotConstants.UBotCommands.nearpoi,$"Ближайшие интересные точки (в радиусе {BotConstants.PoiSearchRadiusKm} км.)"),
            new(BotConstants.UBotCommands.help,"Справка")
        };

        public TelegramBotClient bot
        {
            get;
            private set;
        }

        public BotService(ILogger<BotService> logger
            , IConfiguration configuration
            , IServiceProvider serviceProvider
            , IMemoryCache memoryCache)
        {
            this.configuration = configuration;
            this.logger = logger;
            this.serviceProvider = serviceProvider;
            this.memoryCache = memoryCache;
        }

        public async Task Init(BotModeKinds botModeKinds)
        {
            if (bot == null)
            {
                string? botToken = configuration.GetSection("Telegram")["Token"];
                if (string.IsNullOrEmpty(botToken))
                    throw new Exception("No bot token in evn or config");

                bot = new TelegramBotClient(botToken, cancellationToken: cts.Token);
                //var me = await bot.GetMe();

                if (botModeKinds == BotModeKinds.polling)
                {
                    bot.OnError += OnErrorPolling;
                    //bot.OnMessage += OnMessage;
                    bot.OnUpdate += OnUpdate;
                }
                else if (botModeKinds == BotModeKinds.webhook)
                {
                    string? botWebhookUri = configuration.GetSection("Telegram")["WebhookUri"];
                    if (string.IsNullOrEmpty(botToken))
                        throw new Exception("No bot webhook in evn or config");
                    await bot.SetWebhook(botWebhookUri!, secretToken: webhookSecret);
                }

                //set commands
                List<BotCommand> botcommands = new List<BotCommand>();
                foreach (var cmd in commandsAry)
                {
                    botcommands.Add(new BotCommand() { Command = cmd.Item1.ToString(), Description = cmd.Item2.ToString() });
                }
                await bot.SetMyCommands(botcommands);
            }
        }

        public async Task OnUpdate(Update update)
        {
            try
            {
                if (bot == null)
                    return;

                using (var scope = serviceProvider.CreateScope())
                {
                    var commandHandler = scope.ServiceProvider.GetRequiredService<CommandHandler>();

                    //statistics
                    if (update is { Message: { From: { } From } })
                    {
                        await commandHandler.SaveStatisticUser(From);
                    }

                    if (update is { Message: { Text: { } } Message }) //вместо OnMessage
                    {
                        if (Message.Text.StartsWith("/"))
                        {
                            string pc = Message.Text.Substring(1).Trim().ToLower();

                            if (Enum.TryParse<BotConstants.UBotCommands>(pc, out var command))
                            {
                                await OnCommand(commandHandler, Message.Chat, command);
                                await commandHandler.SaveStatisticAction(command.ToString());
                            }
                            else
                                await commandHandler.OnUnrecognized(Message.Chat);
                        }
                        else
                            await OnDiagolgue(commandHandler, Message);
                    }

                    //обработка гео-ответа на запрос о вашем местоположении из команды /nearpoi
                    if (update is { Message: { Location: { }, ReplyToMessage: { Text: { } } } nearpoiAnswer })
                    {
                        if (nearpoiAnswer.ReplyToMessage.Text.Contains($"/{BotConstants.UBotCommands.nearpoi}"))
                            await commandHandler.OnNearPoiReceivePos(nearpoiAnswer.Chat, nearpoiAnswer.Location);
                    }

                    //обработка ответа на запрос о вашем местоположении из команды /mkrf
                    if (update is { Message: { ReplyToMessage: { Text: { } } } mkrfAnswer })
                    {
                        if (mkrfAnswer.ReplyToMessage.Text.Contains($"/{BotConstants.UBotCommands.mkrf_events}"))
                            await commandHandler.OnMkrfReceivePos(mkrfAnswer.Chat, mkrfAnswer.Location, mkrfAnswer.Text);
                    }

                    //обработка ответа на запрос о вашем местоположении из команды /mkrf
                    if (update is { CallbackQuery: { Data: { } } callbackCallback })
                    {
                        await OnCallback(commandHandler, callbackCallback.Message, callbackCallback.Data);
                    }

                    //inline бесполезен пока
                    //https://core.telegram.org/bots/features#inline-requests
                    //https://telegrambots.github.io/book/3/inline.html
                    //включение: /setinline /setinlinefeedback

                    //if (update is { InlineQuery: { } inlineqQuery }) //InlineQuery
                    //{
                    //    await OnInlineQueryReceived(inlineqQuery);
                    //}
                    //if (update is { ChosenInlineResult: { } chosenInlineResult })
                    //{

                    //    await OnChosenInlineResultReceived(chosenInlineResult);
                    //}
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"{ex.Message} {ex.InnerException}");
            }
        }

        private async Task OnCallback(CommandHandler commandHandler, Message callbackMessage, string data)
        {
            string[] parts = data.Split('_');
            BotConstants.CallbackDataMarks mark = Enum.Parse<CallbackDataMarks>( parts[0] );
            string payload = parts[1];
            switch(mark)
            {
                case CallbackDataMarks.mrkfev: 
                    await commandHandler.OnMkrfCallback(callbackMessage.Chat, payload); 
                    break;
            }   
        }

        
        public void Dispose()
        {
            cts.Cancel();
        }

        ///// <summary>
        ///// отправка списка команд
        ///// </summary>
        ///// <param name="inlineQuery"></param>
        ///// <returns></returns>
        //async Task OnInlineQueryReceived(InlineQuery inlineQuery)
        //{
        //    var results = new List<InlineQueryResult>();

        //    for (var i = 0; i < commandsAry.Length; i++)
        //    {
        //        results.Add(new InlineQueryResultArticle(
        //            i.ToString(), // id
        //            commandsAry[i].Item1.ToString(), // title
        //            new InputTextMessageContent(commandsAry[i].Item2)) // content
        //        );
        //    }

        //    await bot.AnswerInlineQuery(inlineQuery.Id, results); // answer by sending the inline query result list
        //}

        //Task OnChosenInlineResultReceived(ChosenInlineResult chosenInlineResult)
        //{
        //    if (uint.TryParse(chosenInlineResult.ResultId, out var i) // check if a result id is parsable and introduce variable
        //        && i < commandsAry.Length)
        //    {
        //    }

        //    return Task.CompletedTask;
        //}

        private async Task OnCommand(CommandHandler commandHandler, Chat chat, BotConstants.UBotCommands command)
        {
            //запомним текущую команду на 5 минут
            memoryCache.Set(chat.Id + BotConstants.LastCommandMark, command, TimeSpan.FromMinutes(5));
            switch (command)
            {
                case BotConstants.UBotCommands.help:
                    await commandHandler.OnHelp(chat);
                    break;
                case BotConstants.UBotCommands.uwl_choise:
                    await commandHandler.OnUwlEvents(chat);
                    break;
                case BotConstants.UBotCommands.mkrf_events:
                    await commandHandler.OnMkrfRequestPos(chat);
                    break;
                case BotConstants.UBotCommands.nearpoi:
                    await commandHandler.OnNearPoiRequestPos(chat);
                    break;
                case BotConstants.UBotCommands.start:
                    await commandHandler.OnStart(chat);
                    break;
            }
        }

        public async Task OnDiagolgue(CommandHandler commandHandler, Message message)
        {
            if (memoryCache.Get(message.Chat.Id + BotConstants.LastCommandMark) is BotConstants.UBotCommands lastCommand)
            {
                switch (lastCommand)
                {
                    case BotConstants.UBotCommands.mkrf_events:
                        await commandHandler.OnMkrfReceivePos(message.Chat, null, message.Text);
                        break;
                    default:
                        await commandHandler.OnJustTalk(message.Chat);
                        break;
                }
            }
            else 
            {
                await commandHandler.OnJustTalk(message.Chat);
            }
        }

        private async Task OnErrorPolling(Exception exception, HandleErrorSource source)
        {
            logger.LogError($"{exception} {exception.InnerException}, source: {source}");
        }

    }
}
