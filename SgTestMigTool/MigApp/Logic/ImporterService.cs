using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MigApp.Logic
{
    public class ImporterService
    {
        public async Task LoadFile(string filename, AppConstants.ImportDestinations dest)
        {
            //читаем файл, обрабатываем каждую строку в зависимости от dest
            var lineNum = 0;
            await foreach (var line in File.ReadLinesAsync(filename))
            {
                try
                {
                    if (line.Length > 1000)
                    {
                        throw new InvalidDataException("Line too long");
                    }
                    switch (dest)
                    {
                        case AppConstants.ImportDestinations.Departments: await HandleDepartment(line); break;
                        case AppConstants.ImportDestinations.Employees: await HandleEmployee(line); break;
                        case AppConstants.ImportDestinations.JobTitles: await HandleJobTitle(line); break;
                    }
                }
                catch(Exception e)
                {
                    await Console.Error.WriteLineAsync($"Loading error: {e.ToString()}, lineNum {lineNum}, line: \"{line}\"");
                }
                lineNum++;
            }            
        }

        private async Task  HandleDepartment(string line)
        {
            //тут проблема, если в разных департментах есть одноименные чайлды, то исходные данные не смогут указать на правильный парент
            //манагера следовало бы указывать по уникальному логину, а не по имени, чтобы учесть тёсок
            //ингор - не может быть по условиям задачи

            var ary = ValidateLine(line, AppConstants.ImportDestinations.Departments);
            string depName = ary[0];
            string parentName  =ary[1];
            string managerName = ary[2];
            string phone  = ary[3];
            //ищем существующий по имени + имя парента:
            //есть - обновляем,
            //нет - создаем:
            //ищем парента, если нет, создаем его с nulls 
            //ищем манагера, если нет создаем с nulls
            //создаем текущий елемент со ссылкой на парента и манагера
        }

        private async Task HandleEmployee(string line)
        {
                     
            //ищем департмент, если нет, создаем с nulls
            //ищем джоб, если нет, создаем
            //ищем существующий елемент по имени + департмент:
            //есть - дополняем данными
            //нет - создаем элемент со ссылкой на департмент и джоб
        }

        private async Task HandleJobTitle(string line)
        {
            
            //ищем существующий, если нет - создаем
        }

        string[] ValidateLine(string line, AppConstants.ImportDestinations dest)
        {
            // серии пробелов заменяем единственным
            Regex regex = new Regex(@"\s{2,}");
            line = regex.Replace(line, " ");
            var ary = line.Split('\t', StringSplitOptions.TrimEntries);
            if( dest == AppConstants.ImportDestinations.Departments && ary.Length != 4
                || dest == AppConstants.ImportDestinations.Employees && ary.Length != 5
                || dest == AppConstants.ImportDestinations.JobTitles && ary.Length != 1
                )
            {
                throw new InvalidDataException($"wrong parameters count for {dest}");
            }
            return ary;
        }
    }
}
