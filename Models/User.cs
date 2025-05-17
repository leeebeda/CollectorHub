using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CollectorHub.Models;

[Microsoft.EntityFrameworkCore.Index("email", Name = "UQ__Users__AB6E616400A9B683", IsUnique = true)]
public partial class User
{
    [Key]
    public int user_id { get; set; }

    [StringLength(100)]
    [Unicode(false)]
    public string email { get; set; } = null!;

    [StringLength(50)]
    [Unicode(false)]
    public string username { get; set; } = null!;

    [StringLength(255)]
    [Unicode(false)]
    public string password_hash { get; set; } = null!;

    public int role_id { get; set; }

    [InverseProperty("user")]
    public virtual ICollection<Collection> Collections { get; set; } = new List<Collection>();

    [InverseProperty("user")]
    public virtual ICollection<Tag> Tags { get; set; } = new List<Tag>();

    [ForeignKey("role_id")]
    [InverseProperty("Users")]
    public virtual Role role { get; set; } = null!;
}
