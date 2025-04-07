using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logics.Entities.Ranking
{
    public class RankRule : BizBase
    {
        public int? Rank { get; set; } // + or -
        public string? NameDescrPattern { get; set; } = null;
        public bool? Ban { get; set; } = null;
    }
}
