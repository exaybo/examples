using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logics.Entities.Mkrf
{
    [BsonIgnoreExtraElements(true)]
    public class MkPlace
    {
        public long? id { get; set; }
        public string? name { get; set; }
        public MkAddress? address { get; set; }
        public List<long>? localeIds { get; set; }
        public MkCategory? category { get; set; }
    }
}
