using Microsoft.EntityFrameworkCore;
using MigDomain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MigApp.Logic
{
    public class PrinterService
    {
        AppDbContext dbContext;
        public PrinterService(AppDbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        public async Task Print(int depId = 0, int level = 0)
        {
            //если depId == 0 - находим корень
            //иначе - конкретный департмент
            Department? node = null;
            List<int>? uppers = null;
            if (depId == 0)
            {
                uppers = await dbContext.Departments.AsQueryable()
                    .Where(e => e.ParentId == null)
                    .OrderBy(e => e.Name)
                    .Select(e => e.Id)
                    .ToListAsync();
                if (uppers.Count == 1)
                    depId = uppers[0];
            }
            if (depId != 0)
            {
                node = await dbContext.Departments
                    .Include(e => e.Manager!)
                        .ThenInclude(m => m.JobTitle)
                    .Include(e => e.Employees!)
                        .ThenInclude(e => e.JobTitle)
                    .Include(e => e.Children)
                    .Where(e => e.Id == depId)
                    .FirstOrDefaultAsync();
            }
            //print info
            if (node == null)
            {
                //Console.WriteLine("=root");
                if (uppers != null)
                    foreach (var childId in uppers)
                    {
                        await Print(childId, level);
                    }
            }
            else
            {
                Console.WriteLine($"{Repeat(level+1, "=")} {node.Name} ID={node.Id} ");
                if (node.Manager != null
                    && node.Manager.DepartmentId != null)
                {
                    Console.WriteLine($"{Repeat(level)}* {node.Manager.FullName} ID={node.ManagerId} ({node.Manager.JobTitle?.Name} ID={node.Manager.JobTitleId})");
                }
                if (node.Employees != null)
                    foreach (var emp in node.Employees.OrderBy(e => e.FullName))
                        if (emp.JobTitle != null
                            && emp.DepartmentId != null
                            && emp.Id != node.ManagerId)
                        {
                            Console.WriteLine($"{Repeat(level)}- {emp.FullName} ID={emp.Id} ({emp.JobTitle?.Name} ID={emp.JobTitleId})");
                        }
                //print children
                if (node.Children != null)
                    foreach (var child in node.Children.OrderBy(e => e.Name))
                    {
                        await Print(child.Id, level + 1);
                    }
            }
        }

        string Repeat(int cnt, string symbol = " ")
        {
            string result = "";
            for (int i = 0; i < cnt; i++)
            {
                result += symbol;
            }
            return result;
        }
    }
}
