using Logics.Entities;
using Logics.Entities.Mkrf;
using Logics.Entities.Ranking;
using Logics.Filters;
using Logics.LogicHelpers;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.GeoJsonObjectModel;
using MongoDB.Driver.Linq;
using SharpCompress.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Logics
{
    public class MkEventWrapperLogic : Logic<MkEventWrapper, MkEventWrapperFilter>
    {
        MkRankRuleLogic mkRankRuleLogic { get; set; }
        public MkEventWrapperLogic(IMongoClient mongoClient
            , IMongoDatabase mongoDatabase
            , MkRankRuleLogic mkRankRuleLogic
            )
            : base(mongoClient, mongoDatabase)
        {
            this.mkRankRuleLogic = mkRankRuleLogic;
        }

        protected override void AddIndexes(List<CreateIndexModel<MkEventWrapper>> indexModels)
        {
            base.AddIndexes(indexModels);

            indexModels.Add(new CreateIndexModel<MkEventWrapper>(Builders<MkEventWrapper>.IndexKeys
                .Ascending(e => e.mkEvent.name)
                .Ascending(e => e.mkEvent.shortDescription)
                .Ascending(e => e.mkEvent.description)));

            indexModels.Add(new CreateIndexModel<MkEventWrapper>(Builders<MkEventWrapper>.IndexKeys
                .Ascending(e => e.mkEvent.start)));
            indexModels.Add(new CreateIndexModel<MkEventWrapper>(Builders<MkEventWrapper>.IndexKeys
                .Ascending(e => e.mkEvent.end)));

            indexModels.Add(new CreateIndexModel<MkEventWrapper>(Builders<MkEventWrapper>.IndexKeys
                .Ascending("mkEvent.places.name")));//e => e.mkEvent.places[-1].name
            indexModels.Add(new CreateIndexModel<MkEventWrapper>(
                Builders<MkEventWrapper>.IndexKeys.Ascending("mkEvent.places.address.fullAddress")
            ));
            //пример запроса по мультиключевому индексу
            //var filter = Builders<MkEventWrapper>.Filter.Eq(e => e.mkEvent.places[-1].name, "PlaceName");

            indexModels.Add(new CreateIndexModel<MkEventWrapper>(
                    Builders<MkEventWrapper>.IndexKeys.Geo2DSphere("mkEvent.places.address.mapPosition")
                ));

            //var filter = Builders<BsonDocument>.Filter.NearSphere(
            //    "places",
            //    new GeoJsonPoint<GeoJson2DGeographicCoordinates>(
            //        new GeoJson2DGeographicCoordinates(30.0, 50.0) // долгота, широта
            //    ),
            //    maxDistance: 5000 // в метрах
            //);
        }

        //все загружаемое должно заново фильтроваться
        public override async Task UpdateOrCreate(MkEventWrapper entity)
        {
            if (entity == null)
                throw new ArgumentException();
            //попытка найти враппер по внутреннему евенту
            long? newId = entity.mkEvent?.id;
            if (entity.Id == Guid.Empty && newId.HasValue)
            {

                var fe = await mongoCollection.AsQueryable().FirstOrDefaultAsync(x => x.mkEvent.id == newId);
                if (fe != null)
                {
                    entity.Id = fe.Id;
                    entity.IsPublic = fe.IsPublic;
                    entity.Created = fe.Created;
                }
            }
            //фильтры
            //в этом месте:
            //rank = 0
            //bancount = 0,
            //ipPublic - наследован с предыдущей загрузки, если была

            //по сути, каждая загрузка ресетит влияние правил
            await mkRankRuleLogic.HandleEventByRuelSet(entity, IsApplicable);


            await base.UpdateOrCreate(entity);
        }

        public override FilterDefinition<MkEventWrapper> GetFilterDefinition(MkEventWrapperFilter? filter)
        {
            FilterDefinition<MkEventWrapper> bzFilter = Builders<MkEventWrapper>.Filter.Empty;

            // Фильтр для поиска по подстроке
            if (!string.IsNullOrWhiteSpace(filter.Address))
            {
                var substringFilter = Builders<MkEventWrapper>.Filter.Regex(
                    "mkEvent.places.address.fullAddress",

                    new BsonRegularExpression(filter.Address, "i") // "i" для регистронезависимого поиска
                );
                bzFilter = Builders<MkEventWrapper>.Filter.And(bzFilter, substringFilter);
            }

            // Фильтр для поиска по подстроке
            if (!string.IsNullOrWhiteSpace(filter.Substring))
            {
                var substringFilter = Builders<MkEventWrapper>.Filter.Or(
                    Builders<MkEventWrapper>.Filter.Regex(
                        e => e.mkEvent.name,
                        new BsonRegularExpression(filter.Substring, "i")
                    ),
                    Builders<MkEventWrapper>.Filter.Regex(
                        e => e.mkEvent.shortDescription,
                        new BsonRegularExpression(filter.Substring, "i")
                    ),
                    Builders<MkEventWrapper>.Filter.Regex(
                        e => e.mkEvent.description,
                        new BsonRegularExpression(filter.Substring, "i")
                    )
                );
                bzFilter = Builders<MkEventWrapper>.Filter.And(bzFilter, substringFilter);
            }

            if (filter?.PeriodStart != null)
            {
                var dateFilter = Builders<MkEventWrapper>.Filter.And(
                    Builders<MkEventWrapper>.Filter.Gt("mkEvent.start", filter.PeriodStart)
                    , Builders<MkEventWrapper>.Filter.Gt("mkEvent.end", filter.PeriodStart));
                bzFilter = Builders<MkEventWrapper>.Filter.And(bzFilter, dateFilter);
            }

            if (filter?.PeriodEnd != null)
            {
                var dateFilter =
                    Builders<MkEventWrapper>.Filter.Lt("mkEvent.start", filter.PeriodEnd);
                bzFilter = Builders<MkEventWrapper>.Filter.And(bzFilter, dateFilter);
            }

            // Фильтр для проверки IsPublic
            if (filter.IsPublic.HasValue)
            {
                var isPublicFilter = Builders<MkEventWrapper>.Filter.Eq(nameof(MkEventWrapper.IsPublic), filter.IsPublic.Value);
                bzFilter = Builders<MkEventWrapper>.Filter.And(bzFilter, isPublicFilter);
            }

            if (filter.rankRule != null)
            {
                var curFilter = this.GetFilterByRule(filter.rankRule);
                bzFilter = Builders<MkEventWrapper>.Filter.And(bzFilter, curFilter);
            }

            return bzFilter;
        }

        /// <summary>
        /// пометка в коллекции евенты, соответствующие правилу
        /// </summary>
        /// <param name="mkEventWrappers"></param>
        /// <param name="testRule"></param>
        public void MarkByTestRank(List<MkEventWrapper> mkEventWrappers, MkRankRule testRule)
        {
            foreach (var mkEventWrapper in mkEventWrappers)
            {
                mkEventWrapper.rankingMark = IsApplicable(mkEventWrapper, testRule);
            }
        }

        //для загрузки - тест перед помещением в коллекцию
        protected bool IsApplicable(MkEventWrapper mkEventWrapper, MkRankRule testRule)
        {
            bool result = false;

            Regex? regexNameDescr = null;
            if (!string.IsNullOrWhiteSpace(testRule.NameDescrPattern))
                regexNameDescr = new Regex(testRule.NameDescrPattern, RegexOptions.IgnoreCase);
            Regex? regexTag = null;
            if (!string.IsNullOrWhiteSpace(testRule.TagPattern))
                regexTag = new Regex(testRule.TagPattern, RegexOptions.IgnoreCase);
            Regex? regexCat = null;
            if (!string.IsNullOrWhiteSpace(testRule.CategoryPattern))
                regexCat = new Regex(testRule.CategoryPattern, RegexOptions.IgnoreCase);
            if (regexNameDescr != null)
            {
                if (regexNameDescr.IsMatch(mkEventWrapper?.mkEvent?.name ?? "")
                    || regexNameDescr.IsMatch(mkEventWrapper?.mkEvent?.description ?? "")
                    || regexNameDescr.IsMatch(mkEventWrapper?.mkEvent?.shortDescription ?? "")
                    )
                    result = true;
            }
            if (regexTag != null)
            {
                if (mkEventWrapper?.mkEvent?.tags?.Any(x => regexTag.IsMatch(x.name ?? "")) == true)
                    result = true;
            }
            if (regexCat != null)
            {
                if (mkEventWrapper?.mkEvent?.places?.Any(x => regexCat.IsMatch(x.category?.name ?? "")) == true)
                    result = true;
            }
            if (testRule.OrganizationId.HasValue)
            {
                if (mkEventWrapper?.mkEvent?.organization?.id == testRule.OrganizationId.Value)
                    result = true;
            }
            return result;
        }

        /// <summary>
        /// применяем правило к уже загруженным евентам
        /// </summary>
        /// <param name="newRule">правило</param>
        /// <param name="deleting">удаление правила - откат его влияния</param>
        /// <returns></returns>
        public async Task ApplyRankRule(MkRankRule newRule, bool deleting = false)
        {
            //добавляем правило
            if (!deleting)
                await mkRankRuleLogic.Create(newRule);

            var bzFilter = GetFilterByRule(newRule);
            //применяем правило ко все элементам - событиям
            //получаем список на фильтрах и применяем ранк-бан
            var updateBuilder = new UpdateDefinitionBuilder<MkEventWrapper>();
            var updates = new List<UpdateDefinition<MkEventWrapper>>();

            int sign = deleting ? -1 : 1;

            if (newRule.Rank.HasValue && newRule.Rank != 0)
                updates.Add(updateBuilder.Inc(x => x.rank, sign * newRule.Rank.Value));

            if (newRule.Ban == true)
            {
                updates.Add(updateBuilder.Inc(x => x.banCount, sign * 1));

            }


            if (updates.Count > 0)
            {
                var update = updateBuilder.Combine(updates);
                var result = await mongoCollection.UpdateManyAsync(bzFilter, update);
            }

            if (deleting)
                await mkRankRuleLogic.Delete(newRule.Id);
        }

        private FilterDefinition<MkEventWrapper> GetFilterByRule(MkRankRule newRule)
        {
            FilterDefinition<MkEventWrapper> bzFilter = Builders<MkEventWrapper>.Filter.Empty;

            // Фильтр для поиска по подстроке
            if (!string.IsNullOrWhiteSpace(newRule.NameDescrPattern))
            {
                var curFilter = Builders<MkEventWrapper>.Filter.Or(
                    Builders<MkEventWrapper>.Filter
                        .Regex("mkEvent.name",
                        new BsonRegularExpression(newRule.NameDescrPattern, "i")),
                    Builders<MkEventWrapper>.Filter
                        .Regex("mkEvent.shortDescription",
                        new BsonRegularExpression(newRule.NameDescrPattern, "i")),
                    Builders<MkEventWrapper>.Filter
                        .Regex("mkEvent.description",
                        new BsonRegularExpression(newRule.NameDescrPattern, "i"))
                );

                bzFilter = Builders<MkEventWrapper>.Filter.And(bzFilter, curFilter);
            }

            if (!string.IsNullOrWhiteSpace(newRule.CategoryPattern))
            {
                var curFilter = Builders<MkEventWrapper>.Filter.Or(
                    Builders<MkEventWrapper>.Filter
                        .Regex("mkEvent.places.category.name",
                        new BsonRegularExpression(newRule.CategoryPattern, "i")),
                    Builders<MkEventWrapper>.Filter
                        .Regex("mkEvent.category.name",
                        new BsonRegularExpression(newRule.CategoryPattern, "i"))
                );

                bzFilter = Builders<MkEventWrapper>.Filter.And(bzFilter, curFilter);
            }

            if (!string.IsNullOrWhiteSpace(newRule.TagPattern))
            {
                var curFilter =
                    Builders<MkEventWrapper>.Filter
                        .Regex("mkEvent.tags.name",
                        new BsonRegularExpression(newRule.TagPattern, "i"));

                bzFilter = Builders<MkEventWrapper>.Filter.And(bzFilter, curFilter);
            }

            // Фильтр для проверки id
            if (newRule.OrganizationId != null)
            {
                var curFilter = Builders<MkEventWrapper>
                    .Filter
                    .Eq("mkEvent.organization.id", newRule.OrganizationId);

                bzFilter = Builders<MkEventWrapper>.Filter.And(bzFilter, curFilter);
            }
            return bzFilter;
        }

        /// <summary>
        /// чтение с заданными настройками сортировки
        /// ранг, дата, близость, бан...
        /// </summary>
        /// <param name="take"></param>
        /// <param name="skip"></param>
        /// <param name="filter"></param>
        /// <returns></returns>
        public virtual async Task<Tuple<List<MkEventWrapper>, long>> Read4Bot(int? take = 5
            , int? skip = 0
            , MkEventWrapperFilter? filter = null
            )
        {
            FilterDefinition<MkEventWrapper> bzFilter = Builders<MkEventWrapper>.Filter.Empty;

            // Фильтр для поиска по подстроке
            if (!string.IsNullOrWhiteSpace(filter.Address))
            {
                var substringFilter = Builders<MkEventWrapper>.Filter.Regex(
                    "mkEvent.places.address.fullAddress",
                    new BsonRegularExpression($@"{filter.Address}", "i") // "i" для регистронезависимого поиска
                );
                bzFilter = Builders<MkEventWrapper>.Filter.And(bzFilter, substringFilter);
            }

            if (filter?.PeriodStart != null)
            {
                var dateFilter = Builders<MkEventWrapper>.Filter.Gt("mkEvent.end", filter.PeriodStart);
                bzFilter = Builders<MkEventWrapper>.Filter.And(bzFilter, dateFilter);
            }

            if (filter?.PeriodEnd != null)
            {
                var dateFilter = Builders<MkEventWrapper>.Filter.Lt("mkEvent.start", filter.PeriodEnd);
                bzFilter = Builders<MkEventWrapper>.Filter.And(bzFilter, dateFilter);
            }

            if (filter.GeoLatitude.HasValue
                && filter.GeoLongitude.HasValue
                && filter.GeoRadiusKm.HasValue)
            {
                var curFilter = Builders<MkEventWrapper>.Filter.NearSphere(
                    field: "mkEvent.places.address.mapPosition",
                    x: filter.GeoLongitude.Value,
                    y: filter.GeoLatitude.Value,
                    maxDistance: filter.GeoRadiusKm * 1000
                );
                bzFilter = Builders<MkEventWrapper>.Filter.And(bzFilter, curFilter);
            }

            //

            // Фильтр для проверки IsPublic
            var isPublicFilter = Builders<MkEventWrapper>.Filter.Eq(nameof(MkEventWrapper.IsPublic), true);
            bzFilter = Builders<MkEventWrapper>.Filter.And(bzFilter, isPublicFilter);

            // Фильтр для проверки Ban
            var banFilter = Builders<MkEventWrapper>.Filter.Lte(nameof(MkEventWrapper.banCount), 0);
            bzFilter = Builders<MkEventWrapper>.Filter.And(bzFilter, banFilter);

            //сортировка
            var sortDef = Builders<MkEventWrapper>.Sort
                .Descending(nameof(MkEventWrapper.rank));
                //.Ascending("mkEvent.start");


            var entities = await mongoCollection.Find(bzFilter)
                .Sort(sortDef)
                .Skip(skip)
                .Limit(take)
                .ToListAsync();

            entities = entities.OrderBy(x => x.mkEvent.start).ToList();

            // Подсчет общего количества элементов
            long count = entities.Count;
            

            return new Tuple<List<MkEventWrapper>, long>(entities, count);
        }
    }
}
