using System;
using System.Collections.Generic;

namespace Salama.Models;

public partial class DoctorCertificate
{
    public int? CertificateId { get; set; }

    public int? DoctorId { get; set; }

    public virtual Certificate? Certificate { get; set; }

    public virtual Doctor? Doctor { get; set; }
}
