using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logics.Entities.Ranking
{
    [BsonIgnoreExtraElements(true)]
    public class MkRankRule : RankRule
    {
        public long? OrganizationId { get; set; }
        public string? TagPattern { get; set; }
        public string? CategoryPattern { get; set; }
    }
}
