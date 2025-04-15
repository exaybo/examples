using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AppInfrastructure
{
    public class DBContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        //useful for migration
        //  Add-Migration <name> -project AppInfrastructure -startup AppInfrastructure
        //  Update-Database -project AppInfrastructure -startup AppInfrastructure

        //clear Migrations dir
        //  Drop-Database -project AppInfrastructure -startup AppInfrastructure

        public AppDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
            optionsBuilder.UseNpgsql("Server=migdb;Port=5432;Database=migtest;User Id=migrator;Password=Wr()0m-wRo0M;Include Error Detail=true;")
                .EnableSensitiveDataLogging();

            return new AppDbContext(optionsBuilder.Options);
        }
    }
}
