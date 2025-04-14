using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AppInfrastructure
{
    public class DBContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        //useful for migration
        //  Add-Migration <name> -project appdbstuff -startup appdbstuff
        //  Update-Database -project appdbstuff -startup appdbstuff

        //clear Migrations dir
        //Drop-Database -project appdbstuff -startup appdbstuff

        public AppDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
            optionsBuilder.UseNpgsql("Server=localhost;Port=5432;Database=sgTestStruct;User Id=admin;Password=Wr()0m-wRo0M;Include Error Detail=true;")
                .EnableSensitiveDataLogging();

            return new AppDbContext(optionsBuilder.Options);
        }
    }
}
