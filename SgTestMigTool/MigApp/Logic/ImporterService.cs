using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MigApp.Logic
{
    public class ImporterService
    {
        public async Task LoadFile(string filename, AppConstants.ImportDestinations dest)
        {
            switch (dest)
            {
                case AppConstants.ImportDestinations.Departments: LoadDepartments(filename); break;
                case AppConstants.ImportDestinations.Employees: LoadEmployees(filename); break;
                case AppConstants.ImportDestinations.JobTitles: LoadJobTitles(filename); break;
            }
        }

        private void LoadJobTitles(string filename)
        {
            //тут проблема, если в разных департментах есть одноименные чайлды, то исходные данные не смогут указать на правильный парент
            //манагера следовало бы указывать по уникальному логину, а не по имени, чтобы учесть тёсок
            //ингор - не может быть по условиям задачи

            //читаем строку со второй,
            //ищем существующий по имени + имя парента:
            //есть - обновляем,
            //нет - создаем:
            //ищем парента, если нет, создаем его с nulls 
            //ищем манагера, если нет создаем с nulls
            //создаем текущий елемент со ссылкой на парента и манагера
        }

        private void LoadEmployees(string filename)
        {
            //читаем со второй            
            //ищем департмент, если нет, создаем с nulls
            //ищем джоб, если нет, создаем
            //ищем существующий елемент по имени + департмент:
            //есть - дополняем данными
            //нет - создаем элемент со ссылкой на департмент и джоб
        }

        private void LoadDepartments(string filename)
        {
            //читаем со второй
            //ищем существующий, если нет - создаем
        }
    }
}
