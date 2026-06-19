using System;
using System.Collections.Generic;

namespace Salama.Models;

public partial class Clinic
{
    public int Id { get; set; }

    public string? Phone { get; set; }

    public string? Address { get; set; }

    public string? ClinicName { get; set; }

    public int? SpecializationId { get; set; }

    public virtual ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();

    public virtual Specialization? Specialization { get; set; }

    public virtual ICollection<Specialization> Specializations { get; set; } = new List<Specialization>();
}
