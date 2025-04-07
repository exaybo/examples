using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logics.Entities.Mkrf
{
    [BsonIgnoreExtraElements(true)]
    public class MkAddress
    {
        public string? fullAddress {  get; set; }
        public MkMapPosition? mapPosition { get; set; }
    }
}
