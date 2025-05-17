using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CollectorHub.Models;

public partial class Tag
{
    [Key]
    public int tag_id { get; set; }

    public int user_id { get; set; }

    [StringLength(100)]
    [Unicode(false)]
    public string? name { get; set; }

    [InverseProperty("tag")]
    public virtual ICollection<TagAssignment> TagAssignments { get; set; } = new List<TagAssignment>();

    [ForeignKey("user_id")]
    [InverseProperty("Tags")]
    public virtual User user { get; set; } = null!;
}
