using Logics.Entities;
using Logics.Filters;
using Logics.Interfaces;
using Logics.Statistics.Entities;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logics.LogicHelpers
{
    public static class HtmlContainingLogicExcension
    {
        public static async Task SetTgSendError<T,F>(this IHtmlContainingLogic<T,F> instance, T entity, string? message)
            where T : BizBase
            where F : Filter

        {
            if (!(instance is Logic<T, F>))
                return;
            var mongoCollection = ((Logic<T, F>)instance).mongoCollection;
            var filter = Builders<T>.Filter.Eq(x => x.Id, entity.Id);
            var update = Builders<T>.Update
                .Set(nameof(IHtmlContainingEntity.LastTgSentError), message)
                ;

            await mongoCollection.UpdateOneAsync(filter, update);
        }
    }
}
