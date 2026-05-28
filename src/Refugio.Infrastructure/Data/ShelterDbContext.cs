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

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.Entity<Dog>().Property(d => d.WeightKg).HasColumnType("decimal(5,2)");
        mb.Entity<Donation>().Property(d => d.Amount).HasColumnType("decimal(10,2)");
        mb.Entity<Expense>().Property(e => e.Amount).HasColumnType("decimal(10,2)");
        mb.Entity<Dog>().Property(d => d.Status).HasConversion<string>();
        mb.Entity<Adoption>().Property(a => a.Type).HasConversion<string>();
        mb.Entity<Adoption>().Property(a => a.Status).HasConversion<string>();
        mb.Entity<Donation>().Property(d => d.Category).HasConversion<string>();
        mb.Entity<Volunteer>().Property(v => v.Status).HasConversion<string>();
        mb.Entity<ShelterTask>().HasOne(t => t.AssignedVolunteer).WithMany().HasForeignKey(t => t.AssignedVolunteerId).OnDelete(DeleteBehavior.SetNull);
    }
}
