using BotLib;
using Logics;
using Logics.Entities;
using Logics.Entities.Mkrf;
using Logics.Filters;
using Logics.Statistics;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Globalization;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InlineQueryResults;
using Telegram.Bot.Types.ReplyMarkups;

using static Logics.LogicHelpers.HtmlContainingLogicExcension;

namespace BotLibrary
{
    /// <summary>
    /// ответ на команды пользователя
    /// Получает данные из БД и формирует ответ подходящий для Tg
    /// Возможно, дробит одну сущность на несколько сообщений в чате
    /// </summary>
    public class CommandHandler
    {
        BotService botService;
        EventLogic eventLogic;
        PlaceLogic placeLogic;
        MkEventWrapperLogic mkEventWrapperLogic;
        UseStatLogic useStatLogic;
        ILogger logger;
        IMemoryCache memoryCache;
        IConfiguration configuration;

        string botName;

        public CommandHandler(ILogger<CommandHandler> logger
            , BotService botService
            , EventLogic eventLogic
            , PlaceLogic placeLogic
            , UseStatLogic useStatLogic
            , MkEventWrapperLogic mkEventWrapperLogic
            , IMemoryCache memoryCache
            , IConfiguration configuration)
        {
            this.eventLogic = eventLogic;
            this.placeLogic = placeLogic;
            this.botService = botService;
            this.useStatLogic = useStatLogic;
            this.logger = logger;
            this.memoryCache = memoryCache;
            this.mkEventWrapperLogic = mkEventWrapperLogic;
            this.configuration = configuration;

            botName = configuration.GetSection("Telegram")["BotName"];
        }

        public async Task OnStart(Chat chat)
        {
            try
            {
                //string moar = $"https://t.me/{botName}?start={BotConstants.DirectLinkMark_Mkrf}{ev.Id}";
                //мессага-текстовая с описанием
                var txtM = await botService.bot.SendMessage(chat.Id
                    , @"Привет. Подскажу по предстоящим и ближайшим событиям."
                    , ParseMode.Html
                    , protectContent: false);
            }
            catch (Exception ex)
            {
                logger.LogError($"{ex.Message} {ex.InnerException}");
            }
        }

        public async Task OnHelp(Chat chat)
        {
            try
            {
                //мессага-текстовая с описанием
                var txtM = await botService.bot.SendMessage(chat.Id
                    , @$"/mkrf - события, анонсированные минкульт-ы. Прогноз на {BotConstants.SoonDays} дней. Отсортированы по дате (раннее - в конце). Вывод не более {BotConstants.SoonTake} штук. {Environment.NewLine}" +

                        @$"/help - эта подсказка {Environment.NewLine}"
                    , ParseMode.Html
                    , protectContent: false);
            }
            catch (Exception ex)
            {
                logger.LogError($"{ex.Message} {ex.InnerException}");
            }
        }

        public async Task OnUnrecognized(Chat chat)
        {
            try
            {
                var txtM = await botService.bot.SendMessage(chat.Id
                    , @$"неизвестная команда"
                    , ParseMode.None
                    , protectContent: false);
            }
            catch (Exception ex)
            {
                logger.LogError($"{ex.Message} {ex.InnerException}");
            }
        }

        public async Task OnJustTalk(Chat chat)
        {
            try
            {
                var txtM = await botService.bot.SendMessage(chat.Id
                    , @$"не понимаю вас, используйте команды"
                    , ParseMode.None
                    , protectContent: false);
            }
            catch (Exception ex)
            {
                logger.LogError($"{ex.Message} {ex.InnerException}");
            }
        }

        /// <summary>
        /// отсылает в чат ближайшие по времени события. отсортированные от текущего момента
        /// </summary>
        /// <returns></returns>
        public async Task OnUwlEvents(Chat chat)
        {
            EventFilter filter = new EventFilter
            {
                IsPublic = true,
                PeriodStart = DateTime.UtcNow,
                PeriodEnd = DateTime.UtcNow.AddDays(BotConstants.SoonDays), //ближайшее будущее
            };
            //отсортированы так чтобы раньше в чат попали более поздние
            var events = await eventLogic.Read(BotConstants.SoonTake, 0, filter, nameof(Event.HappeningDateStart), true);

            if (events.Item2 == 0)
            {
                try
                {
                    //мессага-текстовая с описанием
                    var txtM = await botService.bot.SendMessage(chat.Id
                        , @"ничего ¯\_(ツ)_/¯"
                        , ParseMode.Html
                        , protectContent: false);
                }
                catch (Exception ex)
                {
                    logger.LogError($"{ex.Message} {ex.InnerException}");
                }
            }

            long counter = events.Item2;
            foreach (var ev in events.Item1)
            {
                try
                {
                    string counterString = $"№{counter} из {events.Item2}";
                    counter--;
                    //отсылаем события в виде пары
                    //гео
                    //if (ev.Place != null
                    //    && ev.Place.GeoLatitude.HasValue
                    //    && ev.Place.GeoLongitude.HasValue
                    //    && !string.IsNullOrWhiteSpace(ev.Place.Name)
                    //    && !string.IsNullOrWhiteSpace(ev.Place.Address)
                    //    )
                    //{
                    //    //мессага-гео с адресом и координатами
                    //    var venueM = await botService.bot.SendVenue(chat.Id
                    //        , latitude: ev.Place.GeoLatitude.Value
                    //        , longitude: ev.Place.GeoLongitude.Value
                    //        , title: ev.Place.Name
                    //        , address: ev.Place.Address);
                    //    //log success?
                    //}

                    InlineKeyboardMarkup inlineMarkup = GetMapsMarkup( (ev?.Place?.GeoLatitude, ev?.Place?.GeoLongitude) );

                    //мессага-текстовая с описанием
                    var text = new StringBuilder();

                    text.AppendLine(counterString);

                    if (ev.HappeningDateStart.HasValue)
                    {
                        text.Append(ev.HappeningDateStart.Value.ToString("d MMMM yyyy (ddd)", new CultureInfo("ru-RU")));

                        if (ev.HappeningDateEnd.HasValue)
                        {
                            text.Append(" - ")
                                .Append(ev.HappeningDateEnd.Value.ToString("d MMMM yyyy", new CultureInfo("ru-RU")));
                        }

                        text.AppendLine(); // Добавляем перенос строки
                    }

                    if (ev.HappeningTime.HasValue)
                    {
                        text.AppendLine($"начало в {ev.HappeningTime.Value.ToString(@"hh\:mm")}"); // Добавляем перенос строки после заголовка
                    }

                    if (!string.IsNullOrWhiteSpace(ev.Title))
                    {
                        text.AppendLine(ev.Title); // Добавляем перенос строки после заголовка
                    }

                    if (!string.IsNullOrWhiteSpace(ev.Body))
                    {
                        text.AppendLine(ev.Body); // Добавляем перенос строки после текста
                    }

                    if (ev.Place != null && !string.IsNullOrWhiteSpace(ev.Place.Name))
                    {
                        text.AppendLine(ev.Place.Name); // Добавляем место с переносом строки
                    }

                    if (ev.Place != null && !string.IsNullOrWhiteSpace(ev.Place.Address))
                    {
                        text.AppendLine(ev.Place.Address); // Добавляем место с переносом строки
                    }

                    // Получаем итоговую строку
                    var strText = text.ToString();
                    if (text.Length > 0 && text[^1] == '\n')
                    {
                        text.Remove(text.Length - Environment.NewLine.Length, Environment.NewLine.Length);
                    }

                    var txtM = await botService.bot.SendMessage(chat.Id
                        , strText
                        , ParseMode.Html
                        , protectContent: false
                        , replyMarkup: inlineMarkup);

                    //сброс ошибки, если все ок
                    if (!string.IsNullOrWhiteSpace(ev.LastTgSentError))
                        await eventLogic.SetTgSendError(ev, null);
                }
                catch (Exception ex)
                {

                    //сохранить ошибку в сущности для просмотра в консоли
                    await eventLogic.SetTgSendError(ev, $"{ex.Message} {ex.InnerException?.Message}");

                    //log
                    logger.LogError($"{ex.Message} {ex.InnerException}");
                }
            }
        }

        private InlineKeyboardMarkup GetMapsMarkup((double? GeoLatitude, double? GeoLongitude) pl)
        {
            InlineKeyboardMarkup inlineMarkup = null;
            if (pl.GeoLatitude == null || pl.GeoLongitude == null)
                return null;
            string lat = pl.GeoLatitude.Value.ToString(CultureInfo.InvariantCulture);
            string lon = pl.GeoLongitude.Value.ToString(CultureInfo.InvariantCulture);
            inlineMarkup = new InlineKeyboardMarkup(
            [
                [
                                InlineKeyboardButton.WithUrl(
                                    text: "карта google",
                                    url: @$"https://www.google.com/maps?q={lat},{lon}"
                                ),
                                InlineKeyboardButton.WithUrl(
                                    text: "карта yandex",
                                    url: @$"https://yandex.com/maps/?ll={lon}%2C{lat}&pt={lon}%2C{lat}&z=16"
                                ),
                                InlineKeyboardButton.WithUrl(
                                    text: "карта 2gis",
                                    url: @$"https://2gis.ru/geo/{lon},{lat}?m={lon},{lat}%2F17"
                                )
                            ]
            ]);

            return inlineMarkup;
        }

        /// <summary>
        /// отсылает в чат сегодняшние события в радиусе N км
        /// </summary>
        /// <returns></returns>
        public async Task OnNearEvents(Chat chat)
        {

        }

        public async Task OnNearPoiRequestPos(Chat chat)
        {
            //запрос геопозиции юзера
            var replyMarkup = new ReplyKeyboardMarkup(
                [
                    [
                        KeyboardButton.WithRequestLocation($"мои координаты")
                    ]
                ]);
            replyMarkup.OneTimeKeyboard = true;
            replyMarkup.ResizeKeyboard = true;

            var sent = await botService.bot.SendMessage(chat.Id, $"Поделитесь своим местоположением для /{BotConstants.UBotCommands.nearpoi}", replyMarkup: replyMarkup);
        }

        public async Task OnNearPoiReceivePos(Chat chat, Location location)
        {

            PlaceFilter pf = new PlaceFilter
            {
                GeoLatitude = location.Latitude,
                GeoLongitude = location.Longitude,
                GeoRadiusKm = BotConstants.PoiSearchRadiusKm,
                IsPublic = true,
            };
            var places = await placeLogic.Read(BotConstants.NearPoiTake, 0, pf, nameof(Place.Name));
            string nothing = "";
            if (places.Item2 == 0)
            {
                nothing = @"ничего ¯\_(ツ)_/¯";
            }

            //удаление клавиатуры, созданной в OnNearPoiRequestPos
            await botService.bot.SendMessage(chat.Id
                        , $"найдено в радиусе {BotConstants.PoiSearchRadiusKm}км.: {nothing}"
                        , ParseMode.None
                        , protectContent: false
                        , replyMarkup: new ReplyKeyboardRemove()
                        );

            long counter = places.Item2;
            foreach (var pl in places.Item1)
            {
                try
                {
                    string counterString = $"№{counter} из {places.Item2}";
                    counter--;

                    InlineKeyboardMarkup inlineMarkup = GetMapsMarkup((pl?.GeoLatitude, pl?.GeoLongitude));

                    //мессага-текстовая с описанием
                    var text = new StringBuilder();

                    text.AppendLine(counterString);

                    if (!string.IsNullOrWhiteSpace(pl.Name))
                    {
                        text.AppendLine(pl.Name);
                    }

                    if (!string.IsNullOrWhiteSpace(pl.Description))
                    {
                        text.AppendLine(pl.Description);
                    }

                    if (!string.IsNullOrWhiteSpace(pl.Address))
                    {
                        text.AppendLine(pl.Address);
                    }

                    // Получаем итоговую строку
                    var strText = text.ToString();
                    if (text.Length > 0 && text[^1] == '\n')
                    {
                        text.Remove(text.Length - Environment.NewLine.Length, Environment.NewLine.Length);
                    }

                    var txtM = await botService.bot.SendMessage(chat.Id
                        , strText
                        , ParseMode.Html
                        , protectContent: false
                        , replyMarkup: inlineMarkup
                        );

                    //сброс ошибки, если все ок
                    if (!string.IsNullOrWhiteSpace(pl.LastTgSentError))
                        await placeLogic.SetTgSendError(pl, null);
                }
                catch (Exception ex)
                {
                    //ошибку в бд
                    await placeLogic.SetTgSendError(pl, $"{ex.Message} {ex.InnerException?.Message}");
                    //log
                    logger.LogError($"{ex.Message} {ex.InnerException}");
                }
            }


        }

        public async Task SaveStatisticAction(string action)
        {
            await useStatLogic.SaveAction(action, DateTime.UtcNow);
        }

        public async Task SaveStatisticUser(User from)
        {
            await useStatLogic.SaveCustomer(new Logics.Statistics.Entities.TgCustomer
            {
                FirstLoginDate = DateTime.UtcNow,
                LastName = from.LastName,
                FirstName = from.FirstName,
                Id = from.Id,
                LastLoginDate = DateTime.UtcNow,
                Username = from.Username
            });
        }

        public async Task OnMkrfRequestPos(Chat chat)
        {
            //запрос геопозиции юзера
            var replyMarkup = new ReplyKeyboardMarkup(
                [
                    [
                        KeyboardButton.WithRequestLocation($"мои координаты")
                    ]
                ]);
            replyMarkup.OneTimeKeyboard = true;
            replyMarkup.ResizeKeyboard = true;

            var sent = await botService.bot.SendMessage(chat.Id, $"Поделитесь местоположением или напишите название города для /{BotConstants.UBotCommands.mkrf_events}", replyMarkup: replyMarkup);
        }

        public async Task OnMkrfReceivePos(Chat chat, Location location, string city)
        {
            //выбор фильтра в зависимости от того, передан Location или City
            //если по city возвращаются несколько городов, возможно стоит попросить уточнения, или нет
            MkEventWrapperFilter filter = new MkEventWrapperFilter();
            string dropKeyboardMsg = "";
            if (location != null)
            {
                filter.GeoLatitude = location.Latitude;
                filter.GeoLongitude = location.Longitude;
                filter.GeoRadiusKm = BotConstants.PoiSearchRadiusKm;
                dropKeyboardMsg = $"найдено в радиусе {BotConstants.PoiSearchRadiusKm}км.";
            }
            else if (!string.IsNullOrWhiteSpace(city) && city.Length >= 3)
            {
                filter.Address = city;
                dropKeyboardMsg = $"найдено по адресному запросу \"{city}\"";
            }
            else
            {
                return;
            }

            filter.PeriodStart = DateTime.UtcNow.Date;
            filter.PeriodEnd = filter.PeriodStart.Value.AddDays(BotConstants.SoonDays);

            var mkEvents = await mkEventWrapperLogic.Read4Bot(BotConstants.NearPoiTake, 0, filter);
            string nothing = "";
            if (mkEvents.Item2 == 0)
            {
                nothing = @"ничего ¯\_(ツ)_/¯";
            }

            //удаление клавиатуры, созданной в OnNearPoiRequestPos
            await botService.bot.SendMessage(chat.Id
                        , $"{dropKeyboardMsg}: {nothing}"
                        , ParseMode.None
                        , protectContent: false
                        , replyMarkup: new ReplyKeyboardRemove()
                        );

            long counter = mkEvents.Item2;
            foreach (var ev in mkEvents.Item1.OrderByDescending(x => x.mkEvent.start))
            {
                try
                {
                    string counterString = $"№{counter} из {mkEvents.Item2}";
                    counter--;

                    //мессага-текстовая с описанием
                    var text = new StringBuilder();

                    text.AppendLine(counterString);

                    if (!string.IsNullOrWhiteSpace(ev?.mkEvent?.name))
                    {
                        text.AppendLine(ev.mkEvent.Name);
                    }

                    if (!string.IsNullOrWhiteSpace(ev?.mkEvent?.ShortDescription))
                    {
                        text.AppendLine(ev.mkEvent.ShortDescription);
                    }

                    if (ev?.mkEvent?.start != null)
                    {
                        string ds = ev.mkEvent.start.Value.ToLocalTime().ToString("dd.MM");
                        if (ev?.mkEvent?.end != null)
                            ds = $"{ds} - {ev.mkEvent.end.Value.ToLocalTime().ToString("dd.MM")}";

                        string times = "";
                        if (ev?.mkEvent?.seances != null)
                        {
                            DateTime nowDate = DateTime.Now.Date;
                            MkSeance? seance = ev.mkEvent.seances.OrderBy(x => x.start).FirstOrDefault(x => x.start >= nowDate);
                            if (seance != null)
                                times = $"{seance.start.Value.ToLocalTime().ToString("HH:mm")} - {seance.end.Value.ToLocalTime().ToString("HH:mm")}";
                        }

                        if (!string.IsNullOrEmpty(times))
                            ds = $"{ds}, {times}";

                        text.AppendLine(ds);
                    }

                    if (ev?.mkEvent?.places != null)
                    {
                        foreach (var place in ev.mkEvent.places)
                            if (place.address?.fullAddress is string adr)
                            {
                                adr = Regex.Replace(adr, @"^[^,]+,\s*", ""); //отрезаем верхний уровень, область
                                text.AppendLine(adr);
                            }
                    }

                    //if (!string.IsNullOrWhiteSpace(ev.Address))
                    //{
                    //    text.AppendLine(ev.Address);
                    //}

                    // Получаем итоговую строку
                    var strText = text.ToString();
                    if (text.Length > 0 && text[^1] == '\n')
                    {
                        text.Remove(text.Length - Environment.NewLine.Length, Environment.NewLine.Length);
                    }

                    //клавиатура

                    var markup = new InlineKeyboardMarkup([
                        [ // Массив кнопок в одной строке
                            //InlineKeyboardButton.WithUrl(
                            //        text: "карта google",
                            //        url: @$"https://www.google.com/maps?q={lat},{lon}"
                            //    ),
                            InlineKeyboardButton.WithCallbackData("Подробнее...", $"{BotConstants.CallbackDataMarks.mrkfev}_{ev.Id}")
                        ]
                    ]); 



                    var txtM = await botService.bot.SendMessage(chat.Id
                        , strText
                        , ParseMode.Html
                        , protectContent: false
                        , replyMarkup: markup
                        );

                }
                catch (Exception ex)
                {
                    //log
                    logger.LogError($"{ex.Message} {ex.InnerException}");
                }
            }


        }

        public async Task OnMkrfCallback(Chat chat, string payload)
        {
            Guid Id;
            if (!Guid.TryParse(payload, out Id))
                return;
            var ev = await mkEventWrapperLogic.Read(Id);
            if (ev == null)
                return;

            var text = new StringBuilder();

            if (!string.IsNullOrWhiteSpace(ev?.mkEvent?.name))
            {   
                text.AppendLine(ev.mkEvent.Name);
            }

            if (!string.IsNullOrWhiteSpace(ev?.mkEvent?.description))
            {
                text.AppendLine(ev.mkEvent.Description);
            }

            if (ev?.mkEvent?.start != null)
            {
                string ds = ev.mkEvent.start.Value.ToLocalTime().ToString("dd.MM");
                if (ev?.mkEvent?.end != null)
                    ds = $"{ds} - {ev.mkEvent.end.Value.ToLocalTime().ToString("dd.MM")}";

                string times = "";
                if (ev?.mkEvent?.seances != null)
                {
                    DateTime nowDate = DateTime.Now.Date;
                    MkSeance? seance = ev.mkEvent.seances.OrderBy(x => x.start).FirstOrDefault(x => x.start >= nowDate);
                    if (seance != null)
                        times = $"{seance.start.Value.ToLocalTime().ToString("HH:mm")} - {seance.end.Value.ToLocalTime().ToString("HH:mm")}";
                }

                if (!string.IsNullOrEmpty(times))
                    ds = $"{ds}, {times}";

                text.AppendLine(ds);
            }

            if (ev?.mkEvent?.places != null)
            {
                foreach (var place in ev.mkEvent.places)
                    if (place.address?.fullAddress is string adr)
                    {
                        adr = Regex.Replace(adr, @"^[^,]+,\s*", ""); //отрезаем верхний уровень, область
                        text.AppendLine(adr);
                    }
            }

            //if (!string.IsNullOrWhiteSpace(ev.Address))
            //{
            //    text.AppendLine(ev.Address);
            //}

            // Получаем итоговую строку
            var strText = text.ToString();
            if (text.Length > 0 && text[^1] == '\n')
            {
                text.Remove(text.Length - Environment.NewLine.Length, Environment.NewLine.Length);
            }

            //клавиатура
            InlineKeyboardMarkup markup = null;
            if (ev?.mkEvent?.places?.FirstOrDefault()?.address?.mapPosition?.coordinates is { } coord) {
                markup = GetMapsMarkup((coord[1], coord[0]));
            }

            var txtM = await botService.bot.SendMessage(chat.Id
                , strText
                , ParseMode.Html
                , protectContent: false
                , replyMarkup: markup
                );
        }
    }
}
