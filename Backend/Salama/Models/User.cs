using System;
using System.Collections.Generic;

namespace Salama.Models;

public partial class User
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string Email { get; set; } = null!;

    public int Phone { get; set; }

    public string Role { get; set; } = null!;

    public string? Address { get; set; }

    public string? PasswordHash { get; set; }

    public string? ProfilePicturePath { get; set; }

    public string? RefreshToken { get; set; }

    public DateTime? RefreshTokenExpiry { get; set; }

    public virtual Admin? Admin { get; set; }

    public virtual Doctor? Doctor { get; set; }

    public virtual Patient? Patient { get; set; }
}
