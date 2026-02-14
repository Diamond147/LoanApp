using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.DbContexts
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        { }
        
        public DbSet<UserProfile> UserProfiles { get; set; } = null!;
        public DbSet<Loan> Loans { get; set; } = null!;
        public DbSet<LoanHistory> LoanHistories { get; set; } = null!;
        public DbSet<PreQualifiedLoan> PreQualifiedLoans { get; set; } = null!;
        public DbSet<Payment> Payments { get; set; } = null!;
        public DbSet<EmailLog> EmailLogs { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder) 
        {
            modelBuilder.Entity<UserProfile>(entity =>
            {
                entity.ToContainer("UserProfiles");
                entity.HasPartitionKey(u => u.Id);
                entity.Property(u => u.Id).ToJsonProperty("id");
                entity.HasKey(u => u.Id);
                entity.HasNoDiscriminator();
            });

            modelBuilder.Entity<Loan>(entity =>
            {
                entity.ToContainer("Loans");
                entity.HasPartitionKey(l => l.UserProfileId);
                entity.Property(l => l.Id).ToJsonProperty("id");
                entity.HasKey(l => l.Id);
                entity.HasNoDiscriminator();
                // Store enums as strings
                entity.Property(l => l.Status)
                .HasConversion<string>();
                entity.Property(l => l.LoanType)
                .HasConversion<string>();
            });

            modelBuilder.Entity<LoanHistory>(entity =>
            {
                entity.ToContainer("LoanHistories");
                entity.HasPartitionKey(lh => lh.UserProfileId);
                entity.Property(lh => lh.Id).ToJsonProperty("id");
                entity.HasKey(lh => lh.Id);
                entity.HasNoDiscriminator();
                // Store enums as strings
                entity.Property(lh => lh.Status)
                .HasConversion<string>();
                entity.Property(lh => lh.LoanType)
                .HasConversion<string>();
            });

            modelBuilder.Entity<LoanHistory>()
                .HasOne(lh => lh.Loan)
                .WithMany(l => l.LoanHistories)
                .HasForeignKey(lh => lh.LoanId);

            modelBuilder.Entity<PreQualifiedLoan>(entity =>
            {
                entity.ToContainer("PreQualifiedLoans");
                entity.HasPartitionKey(p => p.Id);
                entity.Property(p => p.Id).ToJsonProperty("id");
                entity.HasKey(p => p.Id);
                entity.HasNoDiscriminator();
                // Store enums as strings
                entity.Property(p => p.LoanType)
                .HasConversion<string>();
            });

            modelBuilder.Entity<Payment>(entity =>
            {
                entity.ToContainer("Payments");
                entity.HasPartitionKey(py => py.UserProfileId);
                entity.Property(py => py.Id).ToJsonProperty("id");
                entity.HasKey(py => py.Id);
                entity.HasNoDiscriminator();
                // Store enums as strings
                entity.Property(py => py.Status)
                .HasConversion<string>();

                entity.HasIndex(py => py.PaystackReference);
                entity.HasIndex(py => py.LoanId);
            });

            modelBuilder.Entity<EmailLog>(entity =>
            {
                entity.ToContainer("EmailLogs");
                entity.HasPartitionKey(e => e.UserProfileId);
                entity.Property(e => e.Id).ToJsonProperty("id");
                entity.HasKey(e => e.Id);
                entity.HasNoDiscriminator();

                entity.HasIndex(e => e.EmailType);
            });
        }
    }

}
