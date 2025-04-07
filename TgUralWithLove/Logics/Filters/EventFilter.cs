using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Logics.Filters
{
    public class EventFilter: Filter
    {
        public DateTime? PeriodStart {  get; set; }
        public DateTime? PeriodEnd { get; set; }

    }
}
