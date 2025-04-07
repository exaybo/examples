using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logics.Entities.Mkrf
{
    [BsonIgnoreExtraElements(true)]
    public class MkEventWrapper : BizBase
    {
        public MkEvent? mkEvent {  get; set; }

        public int rank { get; set; } = 0;
        public int banCount { get; set; } = 0;

        [BsonIgnore]
        public bool? rankingMark { get; set; }

        public MkEventWrapper()
        {
            IsPublic = true;
        }
    }
}
