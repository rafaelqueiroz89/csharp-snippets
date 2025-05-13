using Microsoft.EntityFrameworkCore;
using OutboxDemo.Models;

namespace OutboxDemo.Data
{
    public class AppDbContext : DbContext
    {
        public DbSet<Order>         Orders  { get; set; } = default!;
        public DbSet<OutboxMessage> Outbox  { get; set; } = default!;

        public AppDbContext(DbContextOptions<AppDbContext> opts)
            : base(opts) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<OutboxMessage>()
                .HasIndex(o => new { o.Sent, o.CreatedAt });
        }
    }
}
