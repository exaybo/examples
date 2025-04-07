using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Logics.LogicConstants;

namespace Logics.Statistics.Entities
{
    [MongoDB.Bson.Serialization.Attributes.BsonIgnoreExtraElements(true)]
    public class TgUsesOnPeriod
    {   
        public DateTime? PeriodBegin { get; set; }
        public PeriodKinds PeriodKind {  get; set; }
        public string Action { get; set; }
        public uint CallCount { get; set; }
    }
}
