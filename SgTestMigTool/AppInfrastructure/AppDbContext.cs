using Microsoft.EntityFrameworkCore;
using MigDomain.Entities;

namespace AppInfrastructure
{
    public class AppDbContext  : DbContext
    {
        public AppDbContext(DbContextOptions options) : base(options)
        {
        }

        public DbSet<Department> Departments { get; set; }
        public DbSet<Employee> Employees { get; set; }
        public DbSet<JobTitle> JobTitles { get; set; }
        

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Department>()
                .HasMany(e => e.Children)
                .WithOne(e => e.Parent)
                .HasForeignKey(d => d.ParentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Department>()
                .HasOne(e => e.Manager)
                .WithMany()
                .IsRequired(false);

            modelBuilder.Entity<Department>()
                .HasMany(e => e.Employees)
                .WithOne(e => e.Department)
                .IsRequired(false);

            modelBuilder.Entity<Employee>()
                .HasOne(e => e.JobTitle)
                .WithMany()
                .IsRequired(false);

        }
    }
}
