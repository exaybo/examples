using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MigDomain.Entities
{
    public class Employee : BaseEntity
    {
        public int? DepartmentId { get; set; }
        public string? FullName { get; set; }
        public string? Login { get; set; }
        public string? Password { get; set; }
        public int? JobTitleId{ get; set; }

        public Department? Department { get; set; }
        public JobTitle? JobTitle { get; set; }
    }
}
