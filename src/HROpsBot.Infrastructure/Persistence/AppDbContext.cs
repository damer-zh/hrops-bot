using HROpsBot.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HROpsBot.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<VacationRequest> VacationRequests => Set<VacationRequest>();
    public DbSet<CertificateRequest> CertificateRequests => Set<CertificateRequest>();
    public DbSet<EquipmentRequest> EquipmentRequests => Set<EquipmentRequest>();
    public DbSet<Regulation> Regulations => Set<Regulation>();
    public DbSet<TaskItem> TaskItems => Set<TaskItem>();
    public DbSet<HrAppointment> HrAppointments => Set<HrAppointment>();
    public DbSet<CsatScore> CsatScores => Set<CsatScore>();
    public DbSet<RequestLog> RequestLogs => Set<RequestLog>();
    public DbSet<ItRequest> ItRequests => Set<ItRequest>();
    public DbSet<OnboardingProgress> OnboardingProgresses => Set<OnboardingProgress>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Employee>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.TelegramId).IsUnique();
            e.Property(x => x.NameRu).HasMaxLength(200).IsRequired();
            e.Property(x => x.NameKk).HasMaxLength(200).IsRequired();
            e.Property(x => x.Department).HasMaxLength(200);
            e.Property(x => x.Position).HasMaxLength(200);
            e.Ignore(x => x.VacationDaysRemaining);
        });

        modelBuilder.Entity<Conversation>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasOne(x => x.Employee).WithMany(x => x.Conversations)
                .HasForeignKey(x => x.EmployeeId).IsRequired(false);
            e.Ignore(x => x.IsActive);
        });

        modelBuilder.Entity<Message>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasOne(x => x.Conversation).WithMany(x => x.Messages)
                .HasForeignKey(x => x.ConversationId);
            e.Property(x => x.ContentRu).HasMaxLength(4000);
            e.Property(x => x.ContentKk).HasMaxLength(4000);
            e.Property(x => x.Intent).HasMaxLength(100);
        });

        modelBuilder.Entity<CsatScore>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasOne(x => x.Conversation).WithOne(x => x.CsatScore)
                .HasForeignKey<CsatScore>(x => x.ConversationId);
        });

        modelBuilder.Entity<RequestLog>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasOne(x => x.Conversation).WithMany(x => x.RequestLogs)
                .HasForeignKey(x => x.ConversationId);
            e.Property(x => x.ScenarioType).HasMaxLength(100);
        });

        modelBuilder.Entity<VacationRequest>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasOne(x => x.Employee).WithMany(x => x.VacationRequests)
                .HasForeignKey(x => x.EmployeeId);
            e.Ignore(x => x.DaysCount);
        });

        modelBuilder.Entity<CertificateRequest>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasOne(x => x.Employee).WithMany(x => x.CertificateRequests)
                .HasForeignKey(x => x.EmployeeId);
            e.Ignore(x => x.EstimatedReadyAt);
            e.Property(x => x.RejectionReason).HasMaxLength(500);
        });

        modelBuilder.Entity<EquipmentRequest>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasOne(x => x.Employee).WithMany(x => x.EquipmentRequests)
                .HasForeignKey(x => x.EmployeeId);
            e.Property(x => x.RejectionReason).HasMaxLength(500);
        });

        modelBuilder.Entity<TaskItem>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasOne(x => x.Employee).WithMany(x => x.Tasks)
                .HasForeignKey(x => x.EmployeeId);
            e.Ignore(x => x.IsOverdue);
        });

        modelBuilder.Entity<Regulation>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.TitleRu).HasMaxLength(500);
            e.Property(x => x.TitleKk).HasMaxLength(500);
        });

        modelBuilder.Entity<HrAppointment>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasOne(x => x.Employee).WithMany()
                .HasForeignKey(x => x.EmployeeId);
        });

        modelBuilder.Entity<ItRequest>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasOne(x => x.Employee).WithMany(x => x.ItRequests)
                .HasForeignKey(x => x.EmployeeId);
            e.Property(x => x.SystemName).HasMaxLength(200);
            e.Property(x => x.Description).HasMaxLength(2000);
        });

        modelBuilder.Entity<OnboardingProgress>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasOne(x => x.Employee).WithOne(x => x.OnboardingProgress)
                .HasForeignKey<OnboardingProgress>(x => x.EmployeeId);
            e.Ignore(x => x.ProgressPercent);
        });
    }
}
