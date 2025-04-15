using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.Primitives;
using MigDomain.Entities;
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
        AppDbContext dbContext;
        public ImporterService(AppDbContext dbContext)
        {
            this.dbContext = dbContext;

        }
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
                    if (lineNum > 0)
                    {
                        Console.WriteLine($"handle line {lineNum}: \"{line}\"");
                        switch (dest)
                        {
                            case AppConstants.ImportDestinations.Departments: await HandleDepartment(line); break;
                            case AppConstants.ImportDestinations.Employees: await HandleEmployee(line); break;
                            case AppConstants.ImportDestinations.JobTitles: await HandleJobTitle(line); break;
                        }                        
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine($"line {lineNum} passed because of error");
                    await Console.Error.WriteLineAsync($"Loading error: {e.ToString()}, lineNum {lineNum}, line: \"{line}\"");
                }
                lineNum++;
            }
        }

        internal async Task Clear()
        {
            await dbContext.WrapInTransactionAsync(default, async () =>
            {
                await dbContext.Departments.ExecuteDeleteAsync();
                await dbContext.Employees.ExecuteDeleteAsync();
                await dbContext.JobTitles.ExecuteDeleteAsync();
                Console.WriteLine("cleared");
                return 0;
            });
        }

        private async Task HandleDepartment(string line)
        {
            //тут проблема, если в разных департментах есть одноименные чайлды, то исходные данные не смогут указать на правильный парент
            //манагера следовало бы указывать по уникальному логину, а не по имени, чтобы учесть тёсок
            //ингор - не может быть по условиям задачи

            var ary = ValidateLine(line, AppConstants.ImportDestinations.Departments);
            string depName = ary[0];
            string parentName = ary[1];
            string managerName = ary[2];
            string phone = ary[3];

            await dbContext.WrapInTransactionAsync(default, async () =>
            {
                //ищем существующий по имени + имя парента (если есть):
                Department? department = await dbContext.Departments.AsQueryable()
                    .Include(e => e.Parent)
                    .Where(e => EF.Functions.ILike(e.Name, depName)
                        && (e.Parent == null || EF.Functions.ILike(e.Parent.Name, parentName))
                    )
                    .FirstOrDefaultAsync();

                //есть - обновляем,
                //нет - создаем:
                if (department == null)
                {
                    department = new Department();
                    dbContext.Add<Department>(department);
                    department.Name = depName;
                    
                }

                //ищем парента, если нет, создаем его с nulls
                //если вообще нужен
                if (!string.IsNullOrWhiteSpace(parentName))
                {
                    Department? parent = await dbContext.Departments.AsQueryable()
                        .Where(e => EF.Functions.ILike(e.Name, parentName))
                        .FirstOrDefaultAsync();
                    if (parent == null)
                    {
                        parent = new Department();
                        dbContext.Add<Department>(parent);
                        parent.Name = parentName;
                    }
                    department.Parent = parent;
                }

                //ищем манагера, если нет создаем с nulls
                if (!string.IsNullOrWhiteSpace(managerName))
                {
                    Employee? manager = await dbContext.Employees.AsQueryable()
                        .Where(e => EF.Functions.ILike(e.FullName, managerName))
                        .FirstOrDefaultAsync();

                    if (manager == null)
                    {
                        manager = new Employee();
                        dbContext.Add(manager);
                        manager.FullName = managerName;
                    }
                    department.Manager = manager;
                }
                //создаем текущий елемент со ссылкой на парента и манагера
                department.Phone = phone;

                await dbContext.SaveChangesAsync();
                return 0;
            });
        }

        private async Task HandleEmployee(string line)
        {
            var ary = ValidateLine(line, AppConstants.ImportDestinations.Employees);
            string depName = ary[0];
            string name = ary[1];
            string login = ary[2];
            string password = ary[3];
            string job = ary[4];
            await dbContext.WrapInTransactionAsync(default, async () =>
            {
                //existing?
                Employee? employee = await dbContext.Employees.AsQueryable()
                    .Where(e => EF.Functions.ILike(e.FullName, name))
                    .FirstOrDefaultAsync();
                if (employee == null)
                {
                    employee = new Employee();
                    dbContext.Add(employee);
                    employee.FullName = name;
                }

                //ищем департмент, если нет, создаем с nulls
                if (!string.IsNullOrWhiteSpace(depName))
                {
                    Department? department = await dbContext.Departments.AsQueryable()
                        .Where(e => EF.Functions.ILike(e.Name, depName))
                        .FirstOrDefaultAsync();
                    if (department == null)
                    {
                        department = new Department();
                        dbContext.Add<Department>(department);
                        department.Name = depName;
                    }
                    employee.Department = department;
                }

                //ищем джоб, если нет, создаем
                JobTitle? jobTitle = await dbContext.JobTitles.AsQueryable()
                    .Where(e => EF.Functions.ILike(e.Name, job))
                    .FirstOrDefaultAsync();
                if (jobTitle == null)
                {
                    jobTitle = new JobTitle();
                    dbContext.Add(jobTitle);
                    jobTitle.Name = job;
                }
                employee.JobTitle = jobTitle;

                employee.Login = login;
                employee.Password = password;
                await dbContext.SaveChangesAsync();
                return 0;
            });
        }

        private async Task HandleJobTitle(string line)
        {
            var ary = ValidateLine(line, AppConstants.ImportDestinations.JobTitles);
            string name = ary[0];
            //ищем существующий, если нет - создаем
            await dbContext.WrapInTransactionAsync(default, async () =>
            {
                JobTitle? jobTitle = await dbContext.JobTitles.AsQueryable()
                    .Where(e => EF.Functions.ILike(e.Name, name))
                    .FirstOrDefaultAsync();
                if (jobTitle == null)
                {
                    jobTitle = new JobTitle();
                    dbContext.Add(jobTitle);
                    jobTitle.Name = name;
                }
                await dbContext.SaveChangesAsync();
                return 0;
            });
        }

        string[] ValidateLine(string line, AppConstants.ImportDestinations dest)
        {
            // серии пробелов заменяем единственным
            Regex regex = new Regex(@"[ ]{2,}");
            line = regex.Replace(line, " ");
            var ary = line.Split('\t', StringSplitOptions.TrimEntries);
            if (dest == AppConstants.ImportDestinations.Departments && ary.Length != 4
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
