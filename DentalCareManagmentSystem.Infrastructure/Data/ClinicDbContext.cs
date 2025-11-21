using DentalCareManagmentSystem.Domain.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace DentalCareManagmentSystem.Infrastructure.Data;

public class ClinicDbContext : IdentityDbContext<User>
{
    public ClinicDbContext(DbContextOptions<ClinicDbContext> options) : base(options)
    {
    }

    public DbSet<Patient> Patients { get; set; }
    public DbSet<Appointment> Appointments { get; set; }
    public DbSet<DiagnosisNote> DiagnosisNotes { get; set; }
    public DbSet<PatientImage> PatientImages { get; set; }
    public DbSet<PriceListItem> PriceListItems { get; set; }
    public DbSet<TreatmentPlan> TreatmentPlans { get; set; }
    public DbSet<TreatmentItem> TreatmentItems { get; set; }
    public DbSet<NotificationLog> NotificationLogs { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }
    public DbSet<PaymentTransaction> PaymentTransactions { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Configure relationships and constraints here
        builder.Entity<Patient>()
            .HasMany(p => p.Appointments)
            .WithOne(a => a.Patient)
            .HasForeignKey(a => a.PatientId);

        builder.Entity<Patient>()
            .HasMany(p => p.DiagnosisNotes)
            .WithOne(d => d.Patient)
            .HasForeignKey(d => d.PatientId);

        builder.Entity<Patient>()
            .HasMany(p => p.PatientImages)
            .WithOne(i => i.Patient)
            .HasForeignKey(i => i.PatientId);

        builder.Entity<Patient>()
            .HasMany(p => p.TreatmentPlans)
            .WithOne(tp => tp.Patient)
            .HasForeignKey(tp => tp.PatientId);
        
        builder.Entity<Patient>()
            .HasMany(p => p.PaymentTransactions)
            .WithOne(pt => pt.Patient)
            .HasForeignKey(pt => pt.PatientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<TreatmentPlan>()
            .HasMany(tp => tp.Items)
            .WithOne(ti => ti.TreatmentPlan)
            .HasForeignKey(ti => ti.TreatmentPlanId);

        builder.Entity<TreatmentItem>()
            .Property(ti => ti.PriceSnapshot)
            .HasColumnType("decimal(18,2)");

        builder.Entity<PriceListItem>()
            .Property(pli => pli.DefaultPrice)
            .HasColumnType("decimal(18,2)");
        
        builder.Entity<PaymentTransaction>()
            .Property(pt => pt.Amount)
            .HasColumnType("decimal(18,2)");
        
        builder.Entity<PaymentTransaction>()
            .HasOne(pt => pt.Appointment)
            .WithMany()
            .HasForeignKey(pt => pt.AppointmentId)
            .OnDelete(DeleteBehavior.SetNull);
        
        builder.Entity<PaymentTransaction>()
            .HasOne(pt => pt.CreatedByUser)
            .WithMany()
            .HasForeignKey(pt => pt.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
