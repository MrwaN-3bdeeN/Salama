using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace Salama.Models;

public partial class AppDbContext : DbContext
{
    public AppDbContext()
    {
    }

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Admin> Admins { get; set; }

    public virtual DbSet<Appointment> Appointments { get; set; }

    public virtual DbSet<Certificate> Certificates { get; set; }

    public virtual DbSet<Clinic> Clinics { get; set; }

    public virtual DbSet<Diagnosis> Diagnoses { get; set; }

    public virtual DbSet<Doctor> Doctors { get; set; }

    public virtual DbSet<DoctorCertificate> DoctorCertificates { get; set; }

    public virtual DbSet<DoctorClinic> DoctorClinics { get; set; }

    public virtual DbSet<Patient> Patients { get; set; }

    public virtual DbSet<Specialization> Specializations { get; set; }

    public virtual DbSet<User> Users { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseSqlServer("Server=.;Database=Salamaty;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Admin>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Admins__3214EC075EB32B94");

            entity.Property(e => e.Id).ValueGeneratedNever();

            entity.HasOne(d => d.IdNavigation).WithOne(p => p.Admin)
                .HasForeignKey<Admin>(d => d.Id)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Admins_Users");
        });

        modelBuilder.Entity<Appointment>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Appointm__3214EC07E44A1AB0");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.AppointmentStatus).HasMaxLength(25);

            entity.HasOne(d => d.Clinic).WithMany(p => p.Appointments)
                .HasForeignKey(d => d.ClinicId)
                .HasConstraintName("FK_Appointments_Clinics");

            entity.HasOne(d => d.Doctor).WithMany(p => p.Appointments)
                .HasForeignKey(d => d.DoctorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Appointments_Doctors");

            entity.HasOne(d => d.Patient).WithMany(p => p.Appointments)
                .HasForeignKey(d => d.PatientId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Appointments_Patients");
        });

        modelBuilder.Entity<Certificate>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Certific__3214EC07AF470AC7");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.CertificateName).HasMaxLength(200);
        });

        modelBuilder.Entity<Clinic>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Clinics__3214EC07A5C19CA6");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Address).HasMaxLength(50);
            entity.Property(e => e.ClinicName).HasMaxLength(50);
            entity.Property(e => e.Phone).HasMaxLength(20);

            entity.HasOne(d => d.Specialization).WithMany(p => p.Clinics)
                .HasForeignKey(d => d.SpecializationId)
                .HasConstraintName("FK_Clinics_Specialization");
        });

        modelBuilder.Entity<Diagnosis>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Diagnosi__3214EC07007A631A");

            entity.ToTable("Diagnosis");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Diagnosis1).HasColumnName("Diagnosis");

            entity.HasOne(d => d.Appointment).WithMany(p => p.Diagnoses)
                .HasForeignKey(d => d.AppointmentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Diagnosis_Appointments");

            entity.HasOne(d => d.Doctor).WithMany(p => p.Diagnoses)
                .HasForeignKey(d => d.DoctorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Diagnosis_Doctors");

            entity.HasOne(d => d.Patient).WithMany(p => p.Diagnoses)
                .HasForeignKey(d => d.PatientId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Diagnosis_Patients");
        });

        modelBuilder.Entity<Doctor>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Doctors__3214EC076F899DDD");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.About).HasMaxLength(1000);
            entity.Property(e => e.Experience).HasMaxLength(25);

            entity.HasOne(d => d.IdNavigation).WithOne(p => p.Doctor)
                .HasForeignKey<Doctor>(d => d.Id)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Doctors_Users");

            entity.HasOne(d => d.Specialization).WithMany(p => p.Doctors)
                .HasForeignKey(d => d.SpecializationId)
                .HasConstraintName("FK_Doctors_Specialization");
        });

        modelBuilder.Entity<DoctorCertificate>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("DoctorCertificate");

            entity.HasOne(d => d.Certificate).WithMany()
                .HasForeignKey(d => d.CertificateId)
                .HasConstraintName("FK_DoctorCertificate_Certificates");

            entity.HasOne(d => d.Doctor).WithMany()
                .HasForeignKey(d => d.DoctorId)
                .HasConstraintName("FK_DoctorCertificate_Doctors");
        });

        modelBuilder.Entity<DoctorClinic>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("DoctorClinic");

            entity.HasOne(d => d.Clinic).WithMany()
                .HasForeignKey(d => d.ClinicId)
                .HasConstraintName("FK_DoctorClinic_Clinics");

            entity.HasOne(d => d.Doctor).WithMany()
                .HasForeignKey(d => d.DoctorId)
                .HasConstraintName("FK_DoctorClinic_Doctors");
        });

        modelBuilder.Entity<Patient>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Patients__3214EC07FAD34CA3");

            entity.Property(e => e.Id).ValueGeneratedNever();

            entity.HasOne(d => d.Diagnosis).WithMany(p => p.Patients)
                .HasForeignKey(d => d.DiagnosisId)
                .HasConstraintName("FK_Patients_Diagnosis");

            entity.HasOne(d => d.IdNavigation).WithOne(p => p.Patient)
                .HasForeignKey<Patient>(d => d.Id)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Patients_Users");
        });

        modelBuilder.Entity<Specialization>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Speciali__3214EC07547D0BF5");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.SpecializationName).HasMaxLength(50);

            entity.HasOne(d => d.Clinic).WithMany(p => p.Specializations)
                .HasForeignKey(d => d.ClinicId)
                .HasConstraintName("FK_Specializations_Clinics");

            entity.HasOne(d => d.Doctor).WithMany(p => p.Specializations)
                .HasForeignKey(d => d.DoctorId)
                .HasConstraintName("FK_Specialization_Doctors");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Users__3214EC0732457A40");

            entity.HasIndex(e => e.Email, "UQ_Users_Email").IsUnique();

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Address).HasMaxLength(100);
            entity.Property(e => e.Email)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.Name).HasMaxLength(50);
            entity.Property(e => e.ProfilePicturePath).HasMaxLength(500);
            entity.Property(e => e.RefreshToken).HasMaxLength(500);
            entity.Property(e => e.Role).HasMaxLength(50);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
