using Microsoft.EntityFrameworkCore;
using iDataflow.Backend.Models;

namespace iDataflow.Backend.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Workflow> Workflows { get; set; }
        public DbSet<Execution> Executions { get; set; }
        public DbSet<WebSocketLog> WebSocketLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure User entity
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasIndex(u => u.Username).IsUnique();
                entity.HasIndex(u => u.Email).IsUnique();
                entity.Property(u => u.CreatedAt).HasDefaultValueSql("NOW()");
                entity.Property(u => u.UpdatedAt).HasDefaultValueSql("NOW()");
            });

            // Configure Workflow entity
            modelBuilder.Entity<Workflow>(entity =>
            {
                entity.HasIndex(w => w.N8nWorkflowId).IsUnique();
                entity.Property(w => w.CreatedAt).HasDefaultValueSql("NOW()");
                entity.Property(w => w.UpdatedAt).HasDefaultValueSql("NOW()");
                
                entity.HasOne(w => w.User)
                    .WithMany(u => u.Workflows)
                    .HasForeignKey(w => w.UserId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // Configure Execution entity
            modelBuilder.Entity<Execution>(entity =>
            {
                entity.HasOne(e => e.Workflow)
                    .WithMany(w => w.Executions)
                    .HasForeignKey(e => e.WorkflowId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");
            });

            // Configure WebSocketLog entity
            modelBuilder.Entity<WebSocketLog>(entity =>
            {
                entity.Property(w => w.CreatedAt).HasDefaultValueSql("NOW()");
            });
        }

        public override int SaveChanges()
        {
            UpdateTimestamps();
            return base.SaveChanges();
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            UpdateTimestamps();
            return base.SaveChangesAsync(cancellationToken);
        }

        private void UpdateTimestamps()
        {
            var entries = ChangeTracker.Entries()
                .Where(e => e.Entity is User && e.State == EntityState.Modified);

            foreach (var entry in entries)
            {
                if (entry.Entity is User user)
                {
                    user.UpdatedAt = DateTime.UtcNow;
                }
            }

            var workflowEntries = ChangeTracker.Entries()
                .Where(e => e.Entity is Workflow && e.State == EntityState.Modified);

            foreach (var entry in workflowEntries)
            {
                if (entry.Entity is Workflow workflow)
                {
                    workflow.UpdatedAt = DateTime.UtcNow;
                }
            }
        }
    }
}