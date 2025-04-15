using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MigDomain.Entities
{
    public class Department : BaseEntity
    {
        
        public int? ParentId { get; set; }
        public int? ManagerId { get; set; }
        public string? Name { get; set; }
        public string? Phone { get; set; }

        public Employee? Manager { get; set; }
        public Department? Parent { get; set; }
        public List<Department>? Children { get; set; }
        public List<Employee>? Employees { get; set; }
    }
}
