using System;
using System.Collections.Generic;

namespace Salama.Models;

public partial class Certificate
{
    public int Id { get; set; }

    public string CertificateName { get; set; } = null!;
}
