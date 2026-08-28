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
            // UserProfile - one user has many loans
            modelBuilder.Entity<UserProfile>(entity =>
            {
                entity.HasKey(u => u.Id);
                entity.Property(u => u.FirstName).IsRequired().HasMaxLength(100);
                entity.Property(u => u.LastName).IsRequired().HasMaxLength(100);
                entity.Property(u => u.Email).IsRequired().HasMaxLength(255);
                entity.Property(u => u.Gender).HasMaxLength(50);
                entity.Property(u => u.MobileNumber).HasMaxLength(20);
                entity.Property(u => u.Nationality).HasMaxLength(100);

                // Index on Email for quick lookups
                entity.HasIndex(u => u.Email).IsUnique();
            });

            // Loan - one user has many loans
            modelBuilder.Entity<Loan>(entity =>
            {
                entity.HasKey(l => l.Id);
                entity.Property(l => l.Status).HasConversion<string>().IsRequired();
                entity.Property(l => l.LoanType).HasConversion<string>().IsRequired();
                entity.Property(l => l.RequestedAmount).HasPrecision(18, 2);
                entity.Property(l => l.PrincipalBalance).HasPrecision(18, 2);
                entity.Property(l => l.InterestRate).HasPrecision(5, 4);
                entity.Property(l => l.AccruedInterest).HasPrecision(18, 2);

                // Foreign key to UserProfile
                entity.HasOne<UserProfile>()
                    .WithMany(u => u.Loans)
                    .HasForeignKey(l => l.UserProfileId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(l => l.UserProfileId);
                entity.HasIndex(l => l.Status);
            });

            // LoanHistory - tracks changes to a loan
            modelBuilder.Entity<LoanHistory>(entity =>
            {
                entity.HasKey(lh => lh.Id);
                entity.Property(lh => lh.Status).HasConversion<string>().IsRequired();
                entity.Property(lh => lh.LoanType).HasConversion<string>().IsRequired();
                entity.Property(lh => lh.PrincipalBalance).HasPrecision(18, 2);
                entity.Property(lh => lh.InterestRate).HasPrecision(5, 4);
                entity.Property(lh => lh.AccruedInterest).HasPrecision(18, 2);

                // Foreign key to Loan (explicitly specify dependent navigation to avoid shadow FK)
                entity.HasOne(lh => lh.Loan)
                    .WithMany(l => l.LoanHistories)
                    .HasForeignKey(lh => lh.LoanId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(lh => lh.LoanId);
                entity.HasIndex(lh => lh.UserProfileId);
            });

            // PreQualifiedLoan - available loan products
            modelBuilder.Entity<PreQualifiedLoan>(entity =>
            {
                entity.HasKey(p => p.Id);
                entity.Property(p => p.LoanType).HasConversion<string>().IsRequired();
                entity.Property(p => p.MinAmount).HasPrecision(18, 2);
                entity.Property(p => p.MaxAmount).HasPrecision(18, 2);
                entity.Property(p => p.InterestRate).HasPrecision(5, 4);

                entity.HasIndex(p => p.LoanType);
            });

            // Payment - payment for a loan
            modelBuilder.Entity<Payment>(entity =>
            {
                entity.HasKey(py => py.Id);
                entity.Property(py => py.Status).HasConversion<string>().IsRequired();
                entity.Property(py => py.Amount).HasPrecision(18, 2);
                entity.Property(py => py.PaystackReference).HasMaxLength(255);

                entity.HasIndex(py => py.PaystackReference).IsUnique();
                entity.HasIndex(py => py.LoanId);
                entity.HasIndex(py => py.UserProfileId);
                entity.HasIndex(py => py.Status);
            });

            // EmailLog - audit trail for emails sent
            modelBuilder.Entity<EmailLog>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.EmailAddress).IsRequired().HasMaxLength(255);
                entity.Property(e => e.Subject).IsRequired();
                entity.Property(e => e.Body).IsRequired();
                entity.Property(e => e.EmailType).HasMaxLength(100);

                entity.HasIndex(e => e.UserProfileId);
                entity.HasIndex(e => e.EmailType);
                entity.HasIndex(e => e.SentDate);
            });
        }
    }

}
