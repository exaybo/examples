using Logics.Entities;
using Logics.Entities.Mkrf;
using Logics.Filters;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using System.Linq.Expressions;
using System.Xml;
using System.Xml.Linq;
using static MongoDB.Driver.WriteConcern;

namespace Logics
{
    public class Logic<T, F>
        where T : BizBase
        where F : Filter
    {
        IMongoClient mongoClient;
        IMongoDatabase mongoDatabase;
        public IMongoCollection<T> mongoCollection;

        public Logic(IMongoClient mongoClient, IMongoDatabase mongoDatabase)
        {
            this.mongoClient = mongoClient;
            this.mongoDatabase = mongoDatabase;
            mongoCollection = mongoDatabase.GetCollection<T>(typeof(T).Name);
        }

        public virtual async Task<bool> Validate(T entity)
        {
            return true;
        }

        protected virtual void AddIndexes(List<CreateIndexModel<T>> indexModels)
        {
            indexModels.Add(new CreateIndexModel<T>(Builders<T>.IndexKeys
                .Ascending(e => e.IsPublic)));
        }

        public virtual async Task CreateIndexes()
        {
            List<CreateIndexModel<T>> indexModels = new List<CreateIndexModel<T>>();
            AddIndexes(indexModels);
            
            await mongoCollection.Indexes.CreateManyAsync(indexModels);
        }

        public virtual async Task<Guid> Create(T entity)
        {
            if (entity == null || entity.Id != Guid.Empty)
                throw new ArgumentException();

            entity.Id = Guid.NewGuid();
            entity.Created = DateTime.UtcNow;

            await mongoCollection.InsertOneAsync(entity);
            return entity.Id;
        }

        public virtual async Task<T> Read(Guid id)
        {
            T entity = await mongoCollection.AsQueryable().Where(x => x.Id == id).FirstOrDefaultAsync();
            return entity;
        }


        public virtual FilterDefinition<T> GetFilterDefinition(F? filter)
        {
            FilterDefinition<T> bzFilter = Builders<T>.Filter.Empty;

            return bzFilter;
        }

        public virtual SortDefinition<T> GetSortDefinition(string orderField, bool orderDesk)
        {
            return orderDesk ? Builders<T>.Sort.Descending(orderField) : Builders<T>.Sort.Ascending(orderField);
        }

        public virtual async Task<Tuple<List<T>, long>> Read(int? take = 5, int? skip = 0, F? filter = null, string orderField = "Created", bool orderDesk = true)
        {
            if (take == null)
                take = 5;
            if (skip == null)
                skip = 0;
            if (string.IsNullOrEmpty(orderField))
                orderField = "Created";

            FilterDefinition<T> filterDef = GetFilterDefinition(filter);
            SortDefinition<T> sortDef = GetSortDefinition(orderField, orderDesk);

            // Выполняем запрос
            var entities = await mongoCollection.Find(filterDef)
                .Sort(sortDef)
                .Skip(skip)
                .Limit(take)
                .ToListAsync();

            // Подсчет общего количества элементов
            long count = await mongoCollection.CountDocumentsAsync(filterDef);

            return new Tuple<List<T>, long>(entities, count);
        }



        public virtual async Task UpdateOrCreate(T entity)
        {
            if (entity == null)
                throw new ArgumentException();
            if (entity.Id != Guid.Empty)
            {
                // Создаем фильтр для поиска документа по ID
                var filter = Builders<T>.Filter.Eq(x => x.Id, entity.Id);

                // Заменяем документ на новый объект entity
                await mongoCollection.ReplaceOneAsync(filter, entity);
            }
            else
            {
                await Create(entity);
            }
        }

        public virtual async Task Delete(Guid id)
        {
            var filter = Builders<T>.Filter
                .Eq(x => x.Id, id);
            // Asynchronously deletes the first document that matches the filter
            await mongoCollection.DeleteOneAsync(filter);
        }

        #region linq request
        //public virtual async Task<Tuple<List<T>, long>> Read(int? take = 5, int? skip = 0, F? filter = null, string orderField = "Created", bool orderDesk = false)
        //{
        //    if (take == null)
        //        take = 5;
        //    if (skip == null)
        //        skip = 0;
        //    if (string.IsNullOrEmpty(orderField))
        //        orderField = "Created";

        //    var qf = GetQueryFilter(filter);
        //    var orderByExpression = GetOrderByExpression(orderField);
        //    var entitiesQ = mongoCollection.AsQueryable();
        //    entitiesQ = entitiesQ.Where(qf);
        //    if (orderDesk)
        //        entitiesQ = entitiesQ.OrderByDescending(orderByExpression);
        //    else
        //        entitiesQ = entitiesQ.OrderBy(orderByExpression);
        //    entitiesQ = entitiesQ
        //        .Take(take.Value)
        //        .Skip(skip.Value);

        //    var entities = await entitiesQ.ToListAsync();

        //    var count = await mongoCollection
        //        .AsQueryable()
        //        .Where(qf)
        //        .CountAsync();

        //    return new Tuple<List<T>, long> ( entities, count);
        //}


        //public virtual Expression<Func<T, bool>> GetQueryFilter(F? f)
        //{
        //    // Начальное выражение: true (чтобы можно было добавлять к нему условия)
        //    Expression<Func<T, bool>> filter = x => true;

        //    // Условие для Id
        //    if (f?.IsPublic != null)
        //    {
        //        filter = CombineExpressions(filter, x => x.IsPublic == f.IsPublic);
        //    }

        //    return filter;
        //}

        //protected Expression<Func<T, bool>> CombineExpressions<T>(
        //    Expression<Func<T, bool>> first,
        //    Expression<Func<T, bool>> second)
        //{
        //    var parameter = Expression.Parameter(typeof(T), "x");

        //    // Переписываем первое выражение с новым параметром
        //    var firstBody = new ParameterReplacer(parameter).Visit(first.Body);

        //    // Переписываем второе выражение с новым параметром
        //    var secondBody = new ParameterReplacer(parameter).Visit(second.Body);

        //    // Комбинируем их через логическое "И"
        //    var combinedBody = Expression.AndAlso(firstBody, secondBody);

        //    return Expression.Lambda<Func<T, bool>>(combinedBody, parameter);
        //}

        //// Класс для замены параметров в выражении
        //private class ParameterReplacer : ExpressionVisitor
        //{
        //    private readonly ParameterExpression _parameter;

        //    public ParameterReplacer(ParameterExpression parameter)
        //    {
        //        _parameter = parameter;
        //    }

        //    protected override Expression VisitParameter(ParameterExpression node)
        //    {
        //        // Заменяем параметр
        //        return _parameter;
        //    }
        //}

        //public Expression<Func<T, object>> GetOrderByExpression(string propertyName)
        //{
        //    // Получаем свойство по имени
        //    var propertyInfo = typeof(T).GetProperty(propertyName);

        //    // Проверяем, существует ли свойство
        //    if (propertyInfo == null)
        //    {
        //        throw new ArgumentException($"Свойство с именем {propertyName} не найдено.");
        //    }

        //    // Создаем параметр для выражения
        //    var parameter = Expression.Parameter(typeof(T), "x");

        //    // Создаем выражение для доступа к свойству
        //    var propertyAccess = Expression.MakeMemberAccess(parameter, propertyInfo);

        //    // Преобразуем тип свойства в объект, чтобы использовать с OrderBy (например, для чисел и строк)
        //    var convert = Expression.Convert(propertyAccess, typeof(object));

        //    // Создаем лямбда-выражение
        //    var lambda = Expression.Lambda<Func<T, object>>(convert, parameter);

        //    return lambda;
        //}
        #endregion
    }



}
