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
    public class MkMapPosition
    {
        public string? type { get; set; }
        public List<double>? coordinates { get; set; }

        [BsonIgnore]
        public string? coordString { 
            get {
                CultureInfo cultureInfo = CultureInfo.InvariantCulture;
                string outs = null;
                if(coordinates != null && coordinates.Count >= 2)
                    outs = coordinates[0].ToString("F6", cultureInfo) +"," + coordinates[1].ToString("F6", cultureInfo);
                return outs;
            }
        }
    }
}
