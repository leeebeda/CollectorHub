using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CollectorHub.Models;

[PrimaryKey("tag_id", "target_type", "target_id")]
public partial class TagAssignment
{
    [Key]
    public int tag_id { get; set; }

    [Key]
    [StringLength(20)]
    [Unicode(false)]
    public string target_type { get; set; } = null!;

    [Key]
    public int target_id { get; set; }

    [ForeignKey("tag_id")]
    [InverseProperty("TagAssignments")]
    public virtual Tag tag { get; set; } = null!;
}
