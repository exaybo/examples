﻿using Microsoft.EntityFrameworkCore;
using MigDomain.Entities;
using Npgsql;

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

            modelBuilder.Entity<Department>()
                .Property(b => b.Version)
                .IsRowVersion();
            modelBuilder.Entity<Employee>()
                .Property(b => b.Version)
                .IsRowVersion();
            modelBuilder.Entity<JobTitle>()
                .Property(b => b.Version)
                .IsRowVersion();
        }

        public async Task<object> WrapInTransactionAsync(
            CancellationToken cancellationToken,
            Func<Task<object>> funcAsync)
        {
            int maxRetries = 3;
            int retryCount = 0;
            int pauseBetwinRetriesMs = 500;
            while (true)
            {
                if (cancellationToken.IsCancellationRequested)
                    throw new OperationCanceledException(cancellationToken);

                try
                {
                    await this.Database.BeginTransactionAsync(cancellationToken);
                    object result = await funcAsync();
                    await this.Database.CommitTransactionAsync(cancellationToken);
                    return result;
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    await this.Database.RollbackTransactionAsync(cancellationToken);
                    //do nothing - stay in cycle
                }
                
                catch (Exception)
                {
                    await this.Database.RollbackTransactionAsync(cancellationToken);
                    throw;
                }

                if (retryCount++ >= maxRetries)
                {
                    throw new Exception($"WrapInTransactionAsync. Достигнуто максимальное количество повторных попыток ({maxRetries}). Транзакция не выполнена.");
                }
            }
        }
    }
}
