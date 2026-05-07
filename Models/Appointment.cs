using System;
using System.Collections.Generic;

namespace Salama.Models;

public partial class Appointment
{
    public int Id { get; set; }

    public string AppointmentStatus { get; set; } = null!;

    public int AppointmentOrder { get; set; }

    public DateOnly AppintmentDate { get; set; }

    public int PatientId { get; set; }

    public int DoctorId { get; set; }

    public virtual ICollection<Diagnosis> Diagnoses { get; set; } = new List<Diagnosis>();

    public virtual Doctor Doctor { get; set; } = null!;

    public virtual Patient Patient { get; set; } = null!;
}
