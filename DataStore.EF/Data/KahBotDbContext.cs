using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Core.Models;
using DataStore.EF.Configurations;

namespace DataStore.EF.Data
{
    public class KahBotDbContext : DbContext, IDesignTimeDbContextFactory<KahBotDbContext>
    {
        public KahBotDbContext()
        {

        }
        public KahBotDbContext(DbContextOptions<KahBotDbContext> options) : base(options)
        {

        }
        public DbSet<Counter> Counters { get; set; }
        public KahBotDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<KahBotDbContext>();
            optionsBuilder.UseSqlServer("Data Source=.;Initial Catalog=KahBotDb;Integrated Security=True;Connect Timeout=30;Encrypt=False;TrustServerCertificate=False;ApplicationIntent=ReadWrite;MultiSubnetFailover=False");
            return new KahBotDbContext(optionsBuilder.Options);
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfiguration(new CounterConfiguration());

            modelBuilder.Entity<Counter>()
            .HasKey(i => i.Guid);
        }
    }
}
