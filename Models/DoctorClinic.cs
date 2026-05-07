using System;
using System.Collections.Generic;

namespace Salama.Models;

public partial class DoctorClinic
{
    public int? DoctorId { get; set; }

    public int? ClinicId { get; set; }

    public virtual Clinic? Clinic { get; set; }

    public virtual Doctor? Doctor { get; set; }
}
