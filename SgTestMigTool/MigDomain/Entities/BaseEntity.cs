using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MigDomain.Entities
{
    public class BaseEntity
    {
        public int Id { get; set; }
        public uint Version { get; set; }
    }
}
