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
    public class PlaceLogic : Logic<Place, PlaceFilter> , IHtmlContainingLogic<Place, PlaceFilter>
    {
        public PlaceLogic(IMongoClient mongoClient, IMongoDatabase mongoDatabase) : base(mongoClient, mongoDatabase)
        {
         
        }

        protected override void AddIndexes(List<CreateIndexModel<Place>> indexModels)
        {
            base.AddIndexes(indexModels);
            indexModels.Add(new CreateIndexModel<Place>(Builders<Place>.IndexKeys
                .Ascending(e => e.Name)));
            indexModels.Add(new CreateIndexModel<Place>(Builders<Place>.IndexKeys
                .Ascending(e => e.Address)));
        }

        public override Task<bool> Validate(Place entity)
        {
            if (string.IsNullOrWhiteSpace(entity.Name))
            {
                throw new ArgumentException(nameof(entity.Name));
            }
            if (string.IsNullOrWhiteSpace(entity.Address))
            {
                throw new ArgumentException(nameof(entity.Address));
            }
            return base.Validate(entity);
        }

        public override FilterDefinition<Place> GetFilterDefinition(PlaceFilter? filter)
        {
            FilterDefinition<Place> bzFilter = Builders<Place>.Filter.Empty;

            // Фильтр для поиска по подстроке
            if (!string.IsNullOrWhiteSpace(filter.Substring))
            {
                var substringFilter = Builders<Place>.Filter.Regex(
                    nameof(Place.Name),
                    new BsonRegularExpression(filter.Substring, "i") // "i" для регистронезависимого поиска
                );
                bzFilter = Builders<Place>.Filter.And(bzFilter, substringFilter);
            }

            // Фильтр для проверки IsPublic
            if (filter.IsPublic.HasValue)
            {
                var isPublicFilter = Builders<Place>.Filter.Eq(nameof(Place.IsPublic), filter.IsPublic.Value);
                bzFilter = Builders<Place>.Filter.And(bzFilter, isPublicFilter);
            }

            return bzFilter;
        }


        //public override Expression<Func<Place, bool>> GetQueryFilter(PlaceFilter f)
        //{
        //    // Начальное выражение: true (чтобы можно было добавлять к нему условия)
        //    Expression<Func<Place, bool>> filter = x => true;

        //    // Условие для Id
        //    if (f?.IsPublic != null)
        //    {
        //        filter = CombineExpressions(filter, x => x.IsPublic == f.IsPublic);
        //    }

        //    if (f?.Substring != null)
        //    {
        //        f.Substring = f.Substring.ToUpperInvariant().Trim();
        //        filter = CombineExpressions(filter, x => x.Name.ToUpper().Contains(f.Substring));
        //    }

        //    if (f?.GeoLatitude != null && f?.GeoLongitude != null && f?.GeoRadiusKm != null)
        //    {
        //        double r, l, t, b;
        //        r = f.GeoLatitude.Value + f.GeoRadiusKm.Value * LogicConstants.OneKmLat;
        //        l = f.GeoLatitude.Value - f.GeoRadiusKm.Value * LogicConstants.OneKmLat;
        //        t = f.GeoLongitude.Value + f.GeoRadiusKm.Value * LogicConstants.OneKmLon;
        //        b = f.GeoLongitude.Value - f.GeoRadiusKm.Value * LogicConstants.OneKmLon;
        //        filter = CombineExpressions(filter, x => 
        //            x.GeoLatitude != null 
        //            && x.GeoLongitude != null
        //            && x.GeoLatitude > l
        //            && x.GeoLatitude < r
        //            && x.GeoLongitude > b
        //            && x.GeoLongitude < t
        //            );
        //    }

        //    return filter;
        //}
    }
}
