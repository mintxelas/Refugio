using Microsoft.EntityFrameworkCore;
using Refugio.Domain.Entities;

namespace Refugio.Infrastructure.Data;

public class ShelterDbContext(DbContextOptions<ShelterDbContext> options) : DbContext(options)
{
    public DbSet<Dog> Dogs => Set<Dog>();
    public DbSet<MedicalRecord> MedicalRecords => Set<MedicalRecord>();
    public DbSet<Medication> Medications => Set<Medication>();
    public DbSet<Adoption> Adoptions => Set<Adoption>();
    public DbSet<ShelterTask> Tasks => Set<ShelterTask>();
    public DbSet<Donation> Donations => Set<Donation>();
    public DbSet<Expense> Expenses => Set<Expense>();
    public DbSet<Volunteer> Volunteers => Set<Volunteer>();
    public DbSet<ShelterEvent> Events => Set<ShelterEvent>();
    public DbSet<Goal> Goals => Set<Goal>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.AddInterceptors(new SoftDeleteInterceptor());

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.Entity<Dog>().Property(d => d.WeightKg).HasColumnType("decimal(5,2)");
        mb.Entity<Donation>().Property(d => d.Amount).HasColumnType("decimal(10,2)");
        mb.Entity<Expense>().Property(e => e.Amount).HasColumnType("decimal(10,2)");
        mb.Entity<Goal>().Property(g => g.TargetAmount).HasColumnType("decimal(10,2)");
        mb.Entity<Goal>().Property(g => g.CurrentAmount).HasColumnType("decimal(10,2)");
        mb.Entity<Dog>().Property(d => d.Status).HasConversion<string>();
        mb.Entity<Adoption>().Property(a => a.Type).HasConversion<string>();
        mb.Entity<Adoption>().Property(a => a.Status).HasConversion<string>();
        mb.Entity<Donation>().Property(d => d.Category).HasConversion<string>();
        mb.Entity<Expense>().Property(e => e.Category).HasConversion<string>();
        mb.Entity<Volunteer>().Property(v => v.Status).HasConversion<string>();
        mb.Entity<ShelterTask>().HasOne(t => t.AssignedVolunteer).WithMany().HasForeignKey(t => t.AssignedVolunteerId).OnDelete(DeleteBehavior.SetNull);

        // Soft-delete global query filters
        mb.Entity<Dog>().HasQueryFilter(e => e.DeletedAt == null);
        mb.Entity<MedicalRecord>().HasQueryFilter(e => e.DeletedAt == null);
        mb.Entity<Medication>().HasQueryFilter(e => e.DeletedAt == null);
        mb.Entity<Adoption>().HasQueryFilter(e => e.DeletedAt == null);
        mb.Entity<Donation>().HasQueryFilter(e => e.DeletedAt == null);
        mb.Entity<Expense>().HasQueryFilter(e => e.DeletedAt == null);
        mb.Entity<ShelterTask>().HasQueryFilter(e => e.DeletedAt == null);
        mb.Entity<Volunteer>().HasQueryFilter(e => e.DeletedAt == null);
        mb.Entity<ShelterEvent>().HasQueryFilter(e => e.DeletedAt == null);
        mb.Entity<Goal>().HasQueryFilter(e => e.DeletedAt == null);
    }
}
