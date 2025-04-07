using Logics.Entities.Mkrf;
using Logics.Entities.Ranking;
using Logics.Filters;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logics
{
    public class MkRankRuleLogic : Logic<MkRankRule, Filter>
    {
        List<MkRankRule> buffer = null;

        public MkRankRuleLogic(IMongoClient mongoClient, IMongoDatabase mongoDatabase) : base(mongoClient, mongoDatabase)
        {
        }

        public async Task HandleEventByRuelSet(MkEventWrapper mkEventWrapper, Func<MkEventWrapper, MkRankRule, bool> checkCallback)
        {
            if (buffer == null)
            {
                buffer = await mongoCollection.Find(FilterDefinition<MkRankRule>.Empty).ToListAsync();
            }
            foreach (MkRankRule rule in buffer)
            {
                if(checkCallback(mkEventWrapper, rule))
                {
                    mkEventWrapper.rank += rule.Rank??0;
                    mkEventWrapper.banCount += rule.Ban == true ? 1 : 0;
                }
            }
        }
    }
}
