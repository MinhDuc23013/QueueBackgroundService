using EmailWorker.Entity;
using Microsoft.EntityFrameworkCore;

namespace EmailWorker.Infrastructures
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(
            DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<OutboxMessage> OutboxMessages { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<OutboxMessage>(b =>
            {
                b.ToTable("outbox_messages");
                b.HasKey(x => x.Id);

                //b.Property(x => x.Id).UseIdentityAlwaysColumn();
                //b.Property(x => x.Payload).HasColumnType("jsonb");

                b.HasIndex(x => x.PublishedAt);
            });

        }
    }
}
