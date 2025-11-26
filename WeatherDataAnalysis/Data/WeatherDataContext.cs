using Microsoft.EntityFrameworkCore;
using WeatherDataAnalysis.Models;

namespace WeatherDataAnalysis.Data
{
    public class WeatherDataContext : DbContext
    {
        public DbSet<Measurement> Measurements { get; set; } = null!;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite("Data Source=WeatherData.db");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Measurement>()
                .HasIndex(m => new { m.Date, m.Location });
        }
    }
}