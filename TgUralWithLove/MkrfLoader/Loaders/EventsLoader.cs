using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Amazon.Runtime.Internal.Endpoints.StandardLibrary;
using Logics;
using Logics.Entities.Mkrf;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json.Linq;
using ZstdSharp;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace MkrfLoader.Loaders
{
    public class EventsLoader
    {
        
        string eventsUri = "https://opendata.mkrf.ru/datatable/events_59a423129a5a55204f05ac00/";

        //and - разрушает все условия по дате. важно чтобы запрос начинася без /r/n, дальше не важно
        string eventsFormDataTemplate = @"start={{startElem}}
&length={{pageLength}}
&f={
""odSetVersions"":""5c3ce3f56cded97112201570"",""odSchema"":""59a423129a5a55204f05ac00"",
""data.general.start"":{""$gte"":""{{dtStart}}T19:00:00.000Z""},
""data.general.end"":{""$lte"":""{{dtEnd}}T19:00:00.000Z""},
""data.info.updateDate"":{""$lte"":""{{dtStart}}T19:00:00.000Z""}
}
&fo={""type"":""include""}
";

        ILogger logger = null;
        MkEventWrapperLogic mkEventWrapperLogic { get; set; }
        HttpClient httpClient;
        public EventsLoader(ILogger<EventsLoader> logger
            , MkEventWrapperLogic mkEventWrapperLogic
            , IHttpClientFactory httpClientFactory
            )
        {
            this.logger = logger;
            this.mkEventWrapperLogic = mkEventWrapperLogic;
            this.httpClient = httpClientFactory.CreateClient("riko");
        }

        public async Task StartLoadSession()
        {
            int startElem = 0;
            int pageLength = 1000;
            DateTime dtStart = DateTime.UtcNow;
            DateTime dtEnd = DateTime.UtcNow.AddDays(LogicConstants.MkfrDaysToLoad);
            Random rnd = new Random();
            Tuple<bool, string, string> isnext = new Tuple<bool, string, string>(true, "", "");

            //clear olds
            await ClearOlds(DateTime.UtcNow.AddDays(-1));
            //await ClearOlds(null); //for test - delete all

            Stopwatch stopwatch = new Stopwatch();
            while (isnext.Item1)
            {
                stopwatch.Start();
                isnext = await LoadPage(startElem, pageLength, dtStart, dtEnd);
                stopwatch.Stop();

                Console.WriteLine($"page: {isnext.Item2} of {isnext.Item3}. moar? - {isnext.Item1}. Secs {(long)stopwatch.Elapsed.TotalSeconds}");

                startElem += pageLength;
                await Task.Delay(rnd.Next(2000, 8000));
            }
        }

        //грузить страницу, возвращает true если контент есть, false - нет
        public async Task<Tuple<bool, string, string>> LoadPage(int startElem, int pageLength, DateTime dtStart, DateTime dtEnd)
        {
            //log dns
            Uri uri = new Uri(eventsUri);
            IPAddress[] addresses = Dns.GetHostAddresses(uri.Host);
            Console.WriteLine($"resolvng {uri.OriginalString}; host = {uri.Host}");
            foreach (var address in addresses)
            {
                Console.WriteLine($" - {address}");
            }

            string formData = eventsFormDataTemplate.Replace("{{startElem}}", startElem.ToString())
                .Replace("{{pageLength}}", pageLength.ToString())
                .Replace("{{dtStart}}", dtStart.ToString("yyyy-MM-dd"))
                .Replace("{{dtEnd}}", dtEnd.ToString("yyyy-MM-dd"));

            //по словам в имени или описании (data.general.name, data.general.shortDescription)
            //ловить на этапе запроса

            //по организации (data.general.organization.name)
            //на этапе запроса
            string? pageCurrent = "";
            string? pagesTotal = "";

            var content = new StringContent(formData, Encoding.UTF8, "application/x-www-form-urlencoded");

            // Отправка POST-запроса
            HttpResponseMessage response = await httpClient.PostAsync(eventsUri, content);

            // Проверка успешности выполнения запроса
            response.EnsureSuccessStatusCode();

            string json = await response.Content.ReadAsStringAsync();

            var jsonObject = JObject.Parse(json);

            pageCurrent = jsonObject["page"]?.ToString();
            pagesTotal = jsonObject["pages"]?.ToString();

            if (jsonObject["data"] is JArray dataArray)
            {
                if (dataArray.Count > 0)
                {
                    //элементы страницы
                    foreach (JToken jelem in dataArray)
                    {
                        await HandleEvent(jelem);
                    }
                }
                else
                    return new Tuple<bool, string, string>(false, pageCurrent, pagesTotal);
            }
            else
                return new Tuple<bool, string, string>(false, pageCurrent, pagesTotal);

            return new Tuple<bool, string, string>(true, pageCurrent, pagesTotal);
        }

        public async Task HandleEvent(JToken jelem)
        {
            try
            {
                var jev = jelem.SelectToken("data.general");
                if (jev == null)
                    return;
                //deserialize
                MkEvent? mkEvent = jev.ToObject<MkEvent>();
                if (mkEvent == null || mkEvent.id == default || string.IsNullOrEmpty(mkEvent.name))
                    throw new Exception($"jev is wrong MkEvent {jev}");
                //фильтры
                //локация - уральский регион
                if (!mkEvent.places.Any(x => x.localeIds.Intersect(LogicConstants.MkfrValidLocales).Any()))
                {
                    return;
                }

                //статус? возможно, в выборке присутствуют только accepted, и надо чистить бд от отстутствующих в выборке
                //но выборка может быть неуспешной - нельзя чистить в этом случае

                //тэги

                //upsert
                MkEventWrapper mkEventWrapper = new MkEventWrapper
                {
                    mkEvent = mkEvent
                };
                await mkEventWrapperLogic.UpdateOrCreate(mkEventWrapper);

            }
            catch (Exception ex)
            {
                logger.LogError($"{ex.Message}; {ex.InnerException?.Message}");
            }
        }

        /// <summary>
        /// стирает все что раньше now. Если now = null - clear collection
        /// </summary>
        /// <param name="now"></param>
        /// <returns></returns>
        public async Task ClearOlds(DateTime? now)
        {
            var filter = FilterDefinition<MkEventWrapper>.Empty;
            if(now != null)
                filter = Builders<MkEventWrapper>.Filter.Or(
                    Builders<MkEventWrapper>.Filter.Eq(x => x.mkEvent, null),
                    Builders<MkEventWrapper>.Filter.Eq(x => x.mkEvent.end, null),
                    Builders<MkEventWrapper>.Filter.Lt(x => x.mkEvent.end, now)
                    );

            await mkEventWrapperLogic.mongoCollection.DeleteManyAsync(filter);
        }
    }
}
