using System.ComponentModel.DataAnnotations;

namespace Salama.Models.DTOs
{
    // ─── ADMIN DTOs ────────────────────────────────────────────────
    public class CreateDoctorRequest
    {
        [Required] [MaxLength(50)] public string Name { get; set; } = null!;
        [Required] [EmailAddress] public string Email { get; set; } = null!;
        [Required] [Range(100000000, 2147483647)] public int Phone { get; set; }
        [Required] [MinLength(6)] public string Password { get; set; } = null!;
        public string? Address { get; set; }
        public string? About { get; set; }
        public string? Experience { get; set; }
        public int? SpecializationId { get; set; }
    }

    public class CreatePatientRequest
    {
        [Required] [MaxLength(50)] public string Name { get; set; } = null!;
        [Required] [EmailAddress] public string Email { get; set; } = null!;
        [Required] [Range(100000000, 2147483647)] public int Phone { get; set; }
        [Required] [MinLength(6)] public string Password { get; set; } = null!;
        public string? Address { get; set; }
    }

    public class UpdateDoctorRequest
    {
        [MaxLength(50)] public string? Name { get; set; }
        [EmailAddress] public string? Email { get; set; }
        [Range(100000000, 2147483647)] public int? Phone { get; set; }
        public string? Address { get; set; }
        public string? About { get; set; }
        public string? Experience { get; set; }
        public int? SpecializationId { get; set; }
    }

    public class UpdatePatientRequest
    {
        [MaxLength(50)] public string? Name { get; set; }
        [EmailAddress] public string? Email { get; set; }
        [Range(100000000, 2147483647)] public int? Phone { get; set; }
        public string? Address { get; set; }
    }

    public class CreateClinicRequest
    {
        [Required] [MaxLength(50)] public string ClinicName { get; set; } = null!;
        public string? Address { get; set; }
        public string? Phone { get; set; }
        public int? SpecializationId { get; set; }
    }

    public class UpdateClinicRequest
    {
        [MaxLength(50)] public string? ClinicName { get; set; }
        public string? Address { get; set; }
        public string? Phone { get; set; }
        public int? SpecializationId { get; set; }
    }

    public class CreateSpecializationRequest
    {
        [Required] [MaxLength(50)] public string SpecializationName { get; set; } = null!;
    }

    public class UpdateSpecializationRequest
    {
        [MaxLength(50)] public string? SpecializationName { get; set; }
    }

    public class CreateCertificateRequest
    {
        [Required] [MaxLength(200)] public string CertificateName { get; set; } = null!;
    }

    public class AssignDoctorClinicRequest
    {
        [Required] public int ClinicId { get; set; }
    }

    public class AssignDoctorCertificateRequest
    {
        [Required] public int CertificateId { get; set; }
    }

    // ─── DOCTOR DTOs ───────────────────────────────────────────────
    public class UpdateDoctorProfileRequest
    {
        [MaxLength(50)] public string? Name { get; set; }
        [Range(100000000, 2147483647)] public int? Phone { get; set; }
        public string? Address { get; set; }
        public string? About { get; set; }
        public string? Experience { get; set; }
    }

    public class CreateDiagnosisRequest
    {
        [Required] public int AppointmentId { get; set; }
        [Required] public int PatientId { get; set; }
        [Required] public string Diagnosis { get; set; } = null!;
    }

    public class UpdateDiagnosisRequest
    {
        [Required] public string Diagnosis { get; set; } = null!;
    }

    // ─── PATIENT DTOs ──────────────────────────────────────────────
    public class UpdatePatientProfileRequest
    {
        [MaxLength(50)] public string? Name { get; set; }
        [Range(100000000, 2147483647)] public int? Phone { get; set; }
        public string? Address { get; set; }
    }
}
