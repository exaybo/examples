using Logics.Entities;
using Logics.Entities.Mkrf;
using Logics.Filters;
using Logics.Interfaces;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Logics
{
    public class EventLogic : Logic<Event, EventFilter>, IHtmlContainingLogic<Event, EventFilter>
    {
        PlaceLogic placeLogic;

        public EventLogic(
            IMongoClient mongoClient,
            IMongoDatabase mongoDatabase,
            PlaceLogic placeLogic)
            : base(mongoClient, mongoDatabase)
        {
            this.placeLogic = placeLogic;
        }

        protected override void AddIndexes(List<CreateIndexModel<Event>> indexModels)
        {
            base.AddIndexes(indexModels);
            indexModels.Add(new CreateIndexModel<Event>(Builders<Event>.IndexKeys
                .Ascending(e => e.Title)));
            indexModels.Add(new CreateIndexModel<Event>(Builders<Event>.IndexKeys
                .Ascending(e => e.Body)));
            indexModels.Add(new CreateIndexModel<Event>(Builders<Event>.IndexKeys
                .Ascending(e => e.HappeningDateStart)));
            indexModels.Add(new CreateIndexModel<Event>(Builders<Event>.IndexKeys
                .Ascending(e => e.HappeningDateEnd)));
        }

        public override Task<bool> Validate(Event entity)
        {
            if (string.IsNullOrWhiteSpace(entity.Title))
            {
                throw new ArgumentException(nameof(entity.Title));
            }
            if (string.IsNullOrWhiteSpace(entity.Body))
            {
                throw new ArgumentException(nameof(entity.Body));
            }
            if (entity.HappeningDateStart == null)
            {
                throw new ArgumentException(nameof(entity.HappeningDateStart));
            }
            if (entity.HappeningDateEnd.HasValue && entity.HappeningDateEnd < entity.HappeningDateStart)
            {
                throw new ArgumentException(nameof(entity.HappeningDateEnd));
            }
            return base.Validate(entity);
        }

        public override FilterDefinition<Event> GetFilterDefinition(EventFilter? filter)
        {
            FilterDefinition<Event> bzFilter = Builders<Event>.Filter.Empty;

            // Фильтр для поиска по подстроке
            if (!string.IsNullOrWhiteSpace(filter?.Substring))
            {
                var substringFilter = Builders<Event>.Filter.Regex(
                    nameof(Event.Title),
                    new BsonRegularExpression(filter.Substring, "i") // "i" для регистронезависимого поиска
                );
                bzFilter = Builders<Event>.Filter.And(bzFilter, substringFilter);
            }

            if (filter?.PeriodStart != null)
            {
                var dateFilter = Builders<Event>.Filter.And(
                    Builders<Event>.Filter.Gt(nameof(Event.HappeningDateStart), filter.PeriodStart)
                    , Builders<Event>.Filter.Gt(nameof(Event.HappeningDateEnd), filter.PeriodStart));
                bzFilter = Builders<Event>.Filter.And(bzFilter, dateFilter);
            }

            if (filter?.PeriodEnd != null)
            {
                var dateFilter = 
                    Builders<Event>.Filter.Lt(nameof(Event.HappeningDateStart), filter.PeriodEnd);
                bzFilter = Builders<Event>.Filter.And(bzFilter, dateFilter);
            }

            // Фильтр для проверки IsPublic
            if (filter?.IsPublic != null)
            {
                var isPublicFilter = Builders<Event>.Filter.Eq(nameof(Event.IsPublic), filter.IsPublic.Value);
                bzFilter = Builders<Event>.Filter.And(bzFilter, isPublicFilter);
            }

            return bzFilter;
        }


        //public override Expression<Func<Event, bool>> GetQueryFilter(EventFilter f)
        //{
        //    // Начальное выражение: true (чтобы можно было добавлять к нему условия)
        //    Expression<Func<Event, bool>> filter = x => true;

        //    // Условие
        //    if (f?.IsPublic != null)
        //    {
        //        filter = CombineExpressions(filter, x => x.IsPublic == f.IsPublic);
        //    }

        //    if (f?.Substring != null)
        //    {
        //        f.Substring = f.Substring.ToUpperInvariant().Trim();
        //        filter = CombineExpressions(filter, x => x.Title.ToUpper().Contains(f.Substring));
        //    }

        //    if (f?.PeriodStart != null)
        //    {
        //        filter = CombineExpressions(filter, 
        //            x => x.HappeningDateStart >= f.PeriodStart || x.HappeningDateEnd >= f.PeriodStart);
        //    }

        //    if (f?.PeriodEnd != null)
        //    {
        //        filter = CombineExpressions(filter,
        //            x => x.HappeningDateStart <= f.PeriodEnd);
        //    }

        //    return filter;
        //}


        //дополнение place
        public override async Task<Event> Read(Guid id)
        {

            var anevent = await base.Read(id);
            if (anevent.PlaceId != null && anevent.PlaceId != Guid.Empty)
            {
                anevent.Place = await placeLogic.Read(anevent.PlaceId.Value);
            }
            return anevent;
        }

        //дополнение place
        public override async Task<Tuple<List<Event>, long>> Read(int? take = 5, int? skip = 0, EventFilter? filter = null, string orderField = "Created", bool orderDesk = false)
        {
            var evs = await base.Read(take, skip, filter, orderField, orderDesk);
            foreach (var e in evs.Item1)
            {
                if (e.PlaceId != null && e.PlaceId != Guid.Empty)
                {
                    e.Place = await placeLogic.Read(e.PlaceId.Value);
                }
            }
            return new Tuple<List<Event>, long>(evs.Item1, evs.Item2);   
        }

        public override Task UpdateOrCreate(Event entity)
        {
            if(entity != null && entity.Place != null && entity.Place.Id != Guid.Empty)
                entity.PlaceId = entity.Place.Id;
            return base.UpdateOrCreate(entity);
        }
    }
}
