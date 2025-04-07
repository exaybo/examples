using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Logics.Filters
{
    public class PlaceFilter: Filter
    {
        public double? GeoLongitude { get; set; }
        public double? GeoLatitude { get; set; }
        public double? GeoRadiusKm { get; set; }

    }
}
