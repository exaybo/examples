using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MigApp.Logic
{
    public class App
    {
        public ImporterService importer;
        public PrinterService printer;
        public App(ImporterService importer, PrinterService printer)
        {
            this.importer = importer;
            this.printer = printer;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="args">
        /// режим загрузки данных: import "filename", tablename
        /// режим распечатки подразделений: print [depId]
        /// </param>
        /// <returns></returns>
        public async Task RunAsync(string[] args)
        {
            if (args.Length == 0)
            {
                ShowHelp();
                return;
            }

            if (Enum.TryParse<AppConstants.ArgCommands>(args[0], true, out var argCommand))
            {
                switch (argCommand)
                {
                    case AppConstants.ArgCommands.IMPORT:
                        if (args.Length != 3)
                        {
                            ShowHelp("wrong args count for import");
                            return;
                        }
                        if (Enum.TryParse<AppConstants.ImportDestinations>(args[2], true,  out var impDest))
                        {
                            await importer.LoadFile(args[1], impDest);
                        }
                        else
                        {
                            ShowHelp("unknown import destination");
                            return;
                        }
                        break;
                    case AppConstants.ArgCommands.PRINT:
                        int depId = 0;
                        if (args.Length == 2)
                        {
                            if(int.TryParse(args[1], out depId) == false)
                            {
                                ShowHelp("wrong print departmentId");
                                return;
                            }
                        }
                        else if(args.Length > 2)
                        {
                            ShowHelp("wrong print args count");
                            return;
                        }
                        
                        await printer.Print(depId);
                        break;
                    case AppConstants.ArgCommands.CLEAR:
                        await importer.Clear();
                        break;
                }
            }
            else
            {
                ShowHelp($"unknown command: {args[0]}");
                return;
            }


        }

        protected void ShowHelp(string comment  = "")
        {
            if (!string.IsNullOrEmpty(comment))
                Console.WriteLine(comment);
            Console.WriteLine(@$"migratin tool can be used in two modes: 
                import mode (loads data from .tsv to defined table): 
                    {AppConstants.ArgCommands.IMPORT} ""filename"" {AppConstants.ImportDestinations.Departments}|{AppConstants.ImportDestinations.Employees}|{AppConstants.ImportDestinations.JobTitles}
                print mode (shows the whole department structure in database or defined department if ID is set): 
                    {AppConstants.ArgCommands.PRINT} [department ID]
                clear tables:
                    {AppConstants.ArgCommands.CLEAR}
                "
            );
        }
    }
}
