using Logics.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logics.Entities
{
    [MongoDB.Bson.Serialization.Attributes.BsonIgnoreExtraElements(true)]
    public class Place : BizBase, IHtmlContainingEntity
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Address { get; set; }
        public double? GeoLongitude { get; set; }
        public double? GeoLatitude { get; set; }

        public string? LastTgSentError { get; set; }

        public Place()
        {
            IsPublic = true;
        }
    }
}
