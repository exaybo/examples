using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logics.Statistics.Entities
{
    public class TgCustomer
    {
        public long? Id { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Username { get; set; }
        public string? PhotoUrl { get; set; }
        public DateTime? FirstLoginDate { get; set; }
        public DateTime? LastLoginDate { get; set; }
    }
}
