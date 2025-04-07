using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Logics.Entities.Mkrf
{
    [BsonIgnoreExtraElements(true)]
    public class MkEvent
    {
        public long? id { get; set; }
        public string? name { get; set; }
        public string? description { get; set; }
        public string? shortDescription { get; set; }
        public string? status { get; set; }
        public List<MkTag>? tags { get; set; }
        public MkOrganization? organization { get; set; }
        public List<MkPlace>? places { get; set; }
        public DateTime? start { get; set; }
        public DateTime? end { get; set; }
        public List<MkSeance>? seances { get; set; }
        public MkCategory? category { get; set; }

        [BsonIgnore]
        public string? HappeningDate
        {
            get
            {
                string outstr = null;
                if (start.HasValue)
                {
                    outstr = start.Value.ToLocalTime().ToString("dd.MM.yyyy");
                    if (end.HasValue)
                        outstr = $"{outstr} - {end.Value.ToLocalTime().ToString("dd.MM.yyyy")}";
                }
                return outstr;
            }
        }

        [BsonIgnore]
        public string? Name => Trimmer(name);

        [BsonIgnore]
        public string? ShortDescription
        {
            get
            {
                if(!string.IsNullOrWhiteSpace(shortDescription))
                    return shortDescription;
                if(!string.IsNullOrWhiteSpace(description))
                {
                    string outs = Trimmer(description);
                    outs = outs.Substring(0, Math.Min(200, outs.Length)) + "...";
                    return outs;
                }
                return null;
            }
        }

        [BsonIgnore]
        public string? Description => Trimmer(description);

        static string Trimmer(string outs)
        {
            if(string.IsNullOrEmpty (outs))
                return outs;
            outs = Regex.Replace(outs, @"<[^>]+>", ""); //удаление тегов
            outs = Regex.Replace(outs, @"(\s){2,}", "$1"); //удаление лишних пробелов и переводов строки внутри

            outs = Regex.Replace(outs, @"^\s*", "");//отрезаем от начала и от конца
            outs = Regex.Replace(outs, @"\s*$", "");
            return outs;
        }
    }
}
