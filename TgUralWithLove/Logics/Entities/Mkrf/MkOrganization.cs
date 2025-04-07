using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logics.Entities.Mkrf
{
    [BsonIgnoreExtraElements(true)]
    public class MkOrganization
    {
        public long? id { get; set; }
        public string? name { get; set; }
    }
}
