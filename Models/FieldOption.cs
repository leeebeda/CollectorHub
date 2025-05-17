using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CollectorHub.Models;

public partial class FieldOption
{
    [Key]
    public int option_id { get; set; }

    public int field_id { get; set; }

    [StringLength(100)]
    [Unicode(false)]
    public string? option_text { get; set; }

    [InverseProperty("option")]
    public virtual ICollection<ItemValueOption> ItemValueOptions { get; set; } = new List<ItemValueOption>();

    [ForeignKey("field_id")]
    [InverseProperty("FieldOptions")]
    public virtual CollectionField field { get; set; } = null!;
}
