using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CollectorHub.Models;

public partial class ItemStatus
{
    [Key]
    public int status_id { get; set; }

    [StringLength(20)]
    [Unicode(false)]
    public string code { get; set; } = null!;

    [StringLength(50)]
    [Unicode(false)]
    public string name { get; set; } = null!;

    [InverseProperty("status")]
    public virtual ICollection<Item> Items { get; set; } = new List<Item>();
}
