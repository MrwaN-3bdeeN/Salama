using System;
using System.Collections.Generic;

namespace Salama.Models;

public partial class Diagnosis
{
    public int Id { get; set; }

    public int PatientId { get; set; }

    public DateOnly DiagnosisDate { get; set; }

    public int AppointmentId { get; set; }

    public int DoctorId { get; set; }

    public string Diagnosis1 { get; set; } = null!;

    public virtual Appointment Appointment { get; set; } = null!;

    public virtual Doctor Doctor { get; set; } = null!;

    public virtual Patient Patient { get; set; } = null!;

    public virtual ICollection<Patient> Patients { get; set; } = new List<Patient>();
}
