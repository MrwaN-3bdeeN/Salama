using System;
using System.Collections.Generic;

namespace Salama.Models;

public partial class Doctor
{
    public int Id { get; set; }

    public string About { get; set; } = null!;

    public string Experience { get; set; } = null!;

    public int? SpecializationId { get; set; }

    public virtual ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();

    public virtual ICollection<Diagnosis> Diagnoses { get; set; } = new List<Diagnosis>();

    public virtual User IdNavigation { get; set; } = null!;

    public virtual Specialization? Specialization { get; set; }

    public virtual ICollection<Specialization> Specializations { get; set; } = new List<Specialization>();
}
