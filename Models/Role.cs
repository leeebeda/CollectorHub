using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CollectorHub.Models;

public partial class Role
{
    [Key]
    public int role_id { get; set; }

    [StringLength(50)]
    [Unicode(false)]
    public string role_name { get; set; } = null!;

    [InverseProperty("role")]
    public virtual ICollection<User> Users { get; set; } = new List<User>();
}
