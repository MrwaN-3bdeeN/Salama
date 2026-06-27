using System;
using System.Collections.Generic;

namespace Salama.Models;

public partial class Specialization
{
    public int Id { get; set; }

    public string? SpecializationName { get; set; }

    public int? DoctorId { get; set; }

    public int? ClinicId { get; set; }

    public virtual Clinic? Clinic { get; set; }

    public virtual ICollection<Clinic> Clinics { get; set; } = new List<Clinic>();

    public virtual Doctor? Doctor { get; set; }

    public virtual ICollection<Doctor> Doctors { get; set; } = new List<Doctor>();
}
