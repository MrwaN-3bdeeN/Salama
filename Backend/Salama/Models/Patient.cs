using System;
using System.Collections.Generic;

namespace Salama.Models;

public partial class Patient
{
    public int Id { get; set; }

    public int? DiagnosisId { get; set; }

    public virtual ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();

    public virtual ICollection<Diagnosis> Diagnoses { get; set; } = new List<Diagnosis>();

    public virtual Diagnosis? Diagnosis { get; set; }

    public virtual User IdNavigation { get; set; } = null!;
}
