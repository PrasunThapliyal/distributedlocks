using DistributedLocks.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;

namespace DistributedLocks.DBContext
{
    public class DistributedLockDBContext : DbContext
    {
        private readonly ILogger<DistributedLockDBContext> logger;
        private readonly IConfiguration configuration;
        private readonly IServiceProvider serviceProvider;

        public DistributedLockDBContext(
            ILogger<DistributedLockDBContext> logger,
            IConfiguration configuration,
            IServiceProvider serviceProvider)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            this.serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }
        public virtual DbSet<EntityIdToLockId>? EntityIdToLockId { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            var connectionString = this.configuration.GetConnectionString("DistributedLockDbConnection");
            optionsBuilder.UseNpgsql(connectionString);

            //optionsBuilder.LogTo(Console.WriteLine, LogLevel.Information).EnableSensitiveDataLogging();
            optionsBuilder.LogTo(
                Console.WriteLine,
                (eventId, logLevel) =>
                    logLevel >= LogLevel.Information
                    || eventId == RelationalEventId.ConnectionOpened
                    || eventId == RelationalEventId.ConnectionClosed
                ).EnableSensitiveDataLogging();

            base.OnConfiguring(optionsBuilder);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<EntityIdToLockId>().ToTable("entityid_to_lockid");
            modelBuilder.Entity<EntityIdToLockId>().Property(e => e.EntityId).HasColumnName("entity_id");
            modelBuilder.Entity<EntityIdToLockId>().Property(e => e.LockId).HasColumnName("lock_id");
            modelBuilder.Entity<EntityIdToLockId>().HasKey(e => e.EntityId);
        }


    }
}
