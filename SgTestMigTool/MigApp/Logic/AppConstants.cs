using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MigApp.Logic
{
    public static class AppConstants
    {
        public enum ArgCommands { IMPORT, PRINT, CLEAR };
        public enum ImportDestinations { Departments, Employees, JobTitles };
    }
}
