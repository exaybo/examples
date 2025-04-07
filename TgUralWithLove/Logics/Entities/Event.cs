using Logics.Interfaces;
using Logics.mongoHelpers;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logics.Entities
{
    [BsonIgnoreExtraElements(true)]
    public class Event : BizBase, IHtmlContainingEntity
    {
        public DateTime? HappeningDateStart { get; set; }
        public DateTime? HappeningDateEnd { get; set; }
        
        public string Title { get; set; }
        public string Body { get; set; }
        public Guid? PlaceId { get; set; }

        public string? LastTgSentError { get; set; }

        [BsonIgnore]
        public Place? Place { get; set; }

        public Event()
        {
            IsPublic = true;
        }

        //[BsonSerializer(typeof(CustomTimeSpanSerializer))]
        [BsonIgnoreIfDefault]

        public TimeSpan? HappeningTime { get; set; }

        //[BsonIgnore]
        //public TimeSpan? BeginTime
        //{
        //    get => BeginTimeAsLong.HasValue ? TimeSpan.FromMilliseconds(BeginTimeAsLong.Value) : null;
        //    set => BeginTimeAsLong = value.HasValue ? (long)value.Value.TotalMilliseconds : null;
        //}
    }
}
