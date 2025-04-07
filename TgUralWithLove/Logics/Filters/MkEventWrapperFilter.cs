using Logics.Entities.Ranking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logics.Filters
{
    public class MkEventWrapperFilter : Filter
    {
        public string? Address {  get; set; }
        public DateTime? PeriodStart { get; set; }
        public DateTime? PeriodEnd { get; set; }
        public double? GeoLongitude { get; set; }
        public double? GeoLatitude { get; set; }
        public double? GeoRadiusKm { get; set; }
        public MkRankRule? rankRule { get; set; }
    }
}
