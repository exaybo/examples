using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logics
{
    public static class LogicConstants
    {
        public static double OneKmLat = 0.00899; //1 км ≈ 0.00899°.
        public static double OneKmLon = 0.01796; //1 км ≈ 0.01796°.

        public enum PeriodKinds { Day, Month }

        public static long[] MkfrValidLocales = 
        {
            212, //свердловская область
            //213, //челябинская
            //132, //тюменская
            //210, //курганская

            //209, //пермский край
            //208, //баскортостан
            //211, //оренбургская
        };

        public static byte MkfrDaysToLoad = 5;
    }
}
