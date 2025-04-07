using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logics.Entities
{
    
    public class BizBase
    {
        public Guid Id { get; set; }
        public DateTime Created { get; set; }
        public bool IsPublic { get; set; }
    }
}
