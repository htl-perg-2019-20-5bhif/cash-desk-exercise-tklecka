using CashDesk.Model;
using Microsoft.EntityFrameworkCore;

namespace CashDesk
{
    public class CashDesktDataContext : DbContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseInMemoryDatabase("CashDesk");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Member>()
                .HasKey(m => m.LastName);
            modelBuilder.Entity<Member>()
                .HasMany<Membership>()
                .WithOne()
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<Membership>()
                .HasMany<Deposit>()
                .WithOne()
                .OnDelete(DeleteBehavior.Cascade);
        }

        public DbSet<Member> Member { get; set; }

        public DbSet<Membership> Membership { get; set; }

        public DbSet<Deposit> Deposit { get; set; }
    }
}
