using CodeIQ.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CodeIQ.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<AdminUser> AdminUsers => Set<AdminUser>();
    public DbSet<Assessment> Assessments => Set<Assessment>();
    public DbSet<Question> Questions => Set<Question>();
    public DbSet<TestCase> TestCases => Set<TestCase>();
    public DbSet<CandidateAttempt> CandidateAttempts => Set<CandidateAttempt>();
    public DbSet<QuestionSubmission> QuestionSubmissions => Set<QuestionSubmission>();
    public DbSet<QuestionBankItem> QuestionBankItems => Set<QuestionBankItem>();
    public DbSet<QuestionBankTestCase> QuestionBankTestCases => Set<QuestionBankTestCase>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AdminUser>(entity =>
        {
            entity.HasIndex(x => x.Email).IsUnique();
            entity.Property(x => x.Email).HasMaxLength(256);
            entity.Property(x => x.DisplayName).HasMaxLength(128);
        });

        modelBuilder.Entity<Assessment>(entity =>
        {
            entity.HasIndex(x => x.Slug).IsUnique();
            entity.Property(x => x.Title).HasMaxLength(256);
            entity.Property(x => x.Slug).HasMaxLength(128);
        });

        modelBuilder.Entity<Question>(entity =>
        {
            entity.HasOne(x => x.Assessment)
                .WithMany(x => x.Questions)
                .HasForeignKey(x => x.AssessmentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TestCase>(entity =>
        {
            entity.HasOne(x => x.Question)
                .WithMany(x => x.TestCases)
                .HasForeignKey(x => x.QuestionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CandidateAttempt>(entity =>
        {
            entity.HasIndex(x => x.AccessToken).IsUnique();
            entity.HasIndex(x => new { x.AssessmentId, x.Email }).IsUnique();
            entity.Property(x => x.Email).HasMaxLength(256);
            entity.Property(x => x.FullName).HasMaxLength(128);
            entity.Property(x => x.AccessToken).HasMaxLength(64);
            entity.HasOne(x => x.Assessment)
                .WithMany(x => x.Attempts)
                .HasForeignKey(x => x.AssessmentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<QuestionSubmission>(entity =>
        {
            entity.HasIndex(x => new { x.AttemptId, x.QuestionId, x.IsFinal });
            entity.HasOne(x => x.Attempt)
                .WithMany(x => x.Submissions)
                .HasForeignKey(x => x.AttemptId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Question)
                .WithMany()
                .HasForeignKey(x => x.QuestionId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<QuestionBankItem>(entity =>
        {
            entity.HasIndex(x => x.Slug).IsUnique();
            entity.Property(x => x.Title).HasMaxLength(256);
            entity.Property(x => x.Slug).HasMaxLength(128);
            entity.Property(x => x.Category).HasMaxLength(64);
        });

        modelBuilder.Entity<QuestionBankTestCase>(entity =>
        {
            entity.HasOne(x => x.QuestionBankItem)
                .WithMany(x => x.TestCases)
                .HasForeignKey(x => x.QuestionBankItemId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
