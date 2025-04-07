using Logics.Statistics.Entities;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using static Logics.LogicConstants;

namespace Logics.Statistics
{
    public class UseStatLogic
    {
        IMongoClient mongoClient;
        IMongoDatabase mongoDatabase;
        IMongoCollection<TgUsesOnPeriod> tgUsesOnDays;
        IMongoCollection<TgCustomer> tgCustomers;

        public UseStatLogic(IMongoClient mongoClient, IMongoDatabase mongoDatabase)
        {
            this.mongoClient = mongoClient;
            this.mongoDatabase = mongoDatabase;
            tgUsesOnDays = mongoDatabase.GetCollection<TgUsesOnPeriod>(typeof(TgUsesOnPeriod).Name);
            tgCustomers = mongoDatabase.GetCollection<TgCustomer>(typeof(TgCustomer).Name);
        }

        public async Task SaveCustomer(TgCustomer tgCustomer)
        {
            //upsert юзера, не обновляем ид и первый вход
            var filter = Builders<TgCustomer>.Filter.Eq(nameof(TgCustomer.Id), tgCustomer.Id);
            var update = Builders<TgCustomer>.Update
                .SetOnInsert(nameof(TgCustomer.Id), tgCustomer.Id)
                .SetOnInsert(nameof(TgCustomer.FirstLoginDate), tgCustomer.FirstLoginDate)
                .Set(nameof(TgCustomer.FirstName), tgCustomer.FirstName)
                .Set(nameof(TgCustomer.LastName), tgCustomer.LastName)
                .Set(nameof(TgCustomer.Username), tgCustomer.Username)
                .Set(nameof(TgCustomer.PhotoUrl), tgCustomer.PhotoUrl)
                .Set(nameof(TgCustomer.LastLoginDate), tgCustomer.LastLoginDate)
                ;
            var updateOptions = new UpdateOptions { IsUpsert = true };

            await tgCustomers.UpdateOneAsync(filter, update, updateOptions);
        }

        public async Task SaveAction(string action, DateTime dateTimeUtc)
        {
            action = action.ToLower().Trim();
            //DAY
            DateTime day = dateTimeUtc.Date;
            // Фильтр для поиска документа
            var filter = Builders<TgUsesOnPeriod>.Filter.And(
                Builders<TgUsesOnPeriod>.Filter.Eq(nameof(TgUsesOnPeriod.Action), action),
                Builders<TgUsesOnPeriod>.Filter.Eq(nameof(TgUsesOnPeriod.PeriodBegin), day),
                Builders<TgUsesOnPeriod>.Filter.Eq(nameof(TgUsesOnPeriod.PeriodKind), PeriodKinds.Day)
                );

            // Обновление с инкрементом счетчика
            var update = Builders<TgUsesOnPeriod>.Update.Inc(nameof(TgUsesOnPeriod.CallCount), 1);

            // Опция для создания документа, если его нет
            var options = new UpdateOptions { IsUpsert = true };

            // Выполняем обновление или создание
            await tgUsesOnDays.UpdateOneAsync(filter, update, options);

            //MONTH
            DateTime month = new DateTime( dateTimeUtc.Year, dateTimeUtc.Month, 1);
            // Фильтр для поиска документа
            var filterM = Builders<TgUsesOnPeriod>.Filter.And(
                Builders<TgUsesOnPeriod>.Filter.Eq(nameof(TgUsesOnPeriod.Action), action),
                Builders<TgUsesOnPeriod>.Filter.Eq(nameof(TgUsesOnPeriod.PeriodBegin), month),
                Builders<TgUsesOnPeriod>.Filter.Eq(nameof(TgUsesOnPeriod.PeriodKind), PeriodKinds.Month)
                );

            // Обновление с инкрементом счетчика
            var updateM = Builders<TgUsesOnPeriod>.Update.Inc(nameof(TgUsesOnPeriod.CallCount), 1);

            // Опция для создания документа, если его нет
            var optionsM = new UpdateOptions { IsUpsert = true };

            // Выполняем обновление или создание
            await tgUsesOnDays.UpdateOneAsync(filterM, updateM, optionsM);
        }

        /// <summary>
        /// список всех сохранненных команд
        /// </summary>
        /// <returns></returns>
        public async Task<List<string>> GetAllActions()
        {
            var q = tgUsesOnDays.AsQueryable().Select(x => x.Action).Distinct();
            var commands = await q.ToListAsync();
            return commands.Order().ToList();
        }

        public async Task<List<Tuple<string, List<double>>>> GetActionsPerDayBack(DateTime dateEnd, int periodUtitsBack, PeriodKinds period)
        {
            DateTime dateStart;
            switch (period)
            {
                case PeriodKinds.Day:
                    dateEnd = dateEnd.Date;
                    dateStart = dateEnd.AddDays(-periodUtitsBack);
                    break;
                case PeriodKinds.Month:
                    dateEnd = new DateTime(dateEnd.Year, dateEnd.Month, 1);
                    dateStart = dateEnd.AddMonths(-periodUtitsBack);
                    break;
                default: dateStart = dateEnd; break;
            }
            //читаем все команды в датах с типом period
            var q = tgUsesOnDays.AsQueryable().Where(x =>
                dateStart <= x.PeriodBegin && x.PeriodBegin <= dateEnd
                && x.PeriodKind == period
                );

            var prdCommands = await q.ToListAsync();

            var allCommands = await GetAllActions();

            //даты для оси X
            List<DateTime> dateList = GetDatesAxis(dateEnd, periodUtitsBack, period);

            List<Tuple<string, List<double>>> result = new List<Tuple<string, List<double>>>();
            foreach (var ac in allCommands)
            {
                var acl = prdCommands
                    .Where(x => x.Action == ac)
                    .ToList();

                //если на дату нет данных, вставляем заглушку
                foreach (var d in dateList)
                    if (!acl.Any(x => x.PeriodBegin == d))
                        acl.Add(new TgUsesOnPeriod
                        {
                            PeriodBegin = d,
                            Action = ac,
                            CallCount = 0,
                            PeriodKind = period,
                        });

                var counts = acl
                    .OrderBy(x => x.PeriodBegin)
                    .Select(x => (double)x.CallCount)
                    .ToList();

                result.Add(new Tuple<string, List<double>>(ac, counts));
            }
            return result;
        }

        public List<DateTime> GetDatesAxis(DateTime dateEnd, int periodUtitsBack, PeriodKinds period)
        {
            dateEnd = dateEnd.Date;
            List<DateTime> dateList = new List<DateTime>();
            for (var i = 0; i < periodUtitsBack; i++)
            {
                DateTime x = dateEnd;
                switch (period)
                {
                    case PeriodKinds.Day: x = dateEnd.AddDays(-i); break;
                    case PeriodKinds.Month: x = dateEnd.AddMonths(-i); break;
                }
                dateList.Add(x);
            }

            return dateList.Order().ToList();
        }

        /// <summary>
        /// получает полное количество пользователей
        /// </summary>
        /// <returns></returns>
        public async Task<int> GetTotalUsers()
        {
            int cnt = await tgCustomers.AsQueryable().CountAsync();
            return cnt;
        }

        public async Task<List<double>> GetNewCustomers (DateTime dateEnd, int periodUtitsBack, PeriodKinds period)
        {
            DateTime dateStart;
            DateTime dateEndLast;
            switch (period)
            {
                case PeriodKinds.Day:
                    dateEnd = dateEnd.Date;
                    dateEndLast = dateEnd.AddDays(1);
                    dateStart = dateEnd.AddDays(-periodUtitsBack);
                    break;
                case PeriodKinds.Month:
                    dateEnd = new DateTime(dateEnd.Year, dateEnd.Month, 1);
                    dateEndLast = dateEnd.AddMonths(1);
                    dateStart = dateEnd.AddMonths(-periodUtitsBack);
                    break;
                default: dateEndLast = dateStart = dateEnd; break;
            }
            //читаем все команды в датах с типом period
            var q = tgCustomers.AsQueryable().Where(x =>
                dateStart <= x.FirstLoginDate && x.FirstLoginDate <= dateEndLast
                );

            //var debugCus = await tgCustomers.AsQueryable().ToListAsync();

            List<TgCustomer> allCustomers = await q.ToListAsync();

            //даты для оси X
            List<DateTime> dateList = GetDatesAxis(dateEnd, periodUtitsBack, period);

            List<double> result = new List<double>();
            foreach (var d in dateList)
            {
                double ucnt;
                DateTime frameB = d, frameE = d;
                switch (period) {
                    case PeriodKinds.Day:
                        frameB = d.Date;
                        frameE = d.Date.AddDays(1);
                        break;
                    case PeriodKinds.Month:
                        frameB = new DateTime(d.Year, d.Month, 1);
                        frameE = frameB.AddMonths(1);
                        break;
                }

                ucnt = allCustomers.Count(x => x.FirstLoginDate.HasValue &&
                    x.FirstLoginDate >= frameB &&
                    x.FirstLoginDate < frameE
                    );

                result.Add(ucnt);
            }        
            

            return result;
        }
    }
}
