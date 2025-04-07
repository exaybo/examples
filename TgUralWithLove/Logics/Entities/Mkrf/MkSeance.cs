using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logics.Entities.Mkrf
{
    [BsonIgnoreExtraElements(true)]
    public class MkSeance
    {
        public DateTime? start { get; set; }
        public DateTime? end { get; set; }
        
        [BsonIgnore]
        public string? HappeningTime
        {
            get
            {
                string outstr = null;
                if (start.HasValue)
                {
                    CultureInfo cultureInfo = CultureInfo.GetCultureInfo("ru-RU");
                    outstr = start.Value.ToLocalTime().ToString("dd.MMM HH:mm", cultureInfo);
                    if (end.HasValue)
                    {
                        if(end.Value.Date != start.Value.Date)
                            outstr = $"{outstr} - {end.Value.ToLocalTime().ToString("dd.MMM HH:mm", cultureInfo)}";
                        else
                            outstr = $"{outstr} - {end.Value.ToLocalTime().ToString("HH:mm", cultureInfo)}";
                    }
                }
                return outstr;
            }
        }
    }
}
