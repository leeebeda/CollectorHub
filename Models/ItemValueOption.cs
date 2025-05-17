using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CollectorHub.Models;

[Table("ItemValueOption")]
public partial class ItemValueOption
{
    [Key]
    public int id { get; set; }

    public int item_id { get; set; }

    public int field_id { get; set; }

    public int option_id { get; set; }

    [ForeignKey("field_id")]
    [InverseProperty("ItemValueOptions")]
    public virtual CollectionField field { get; set; } = null!;

    [ForeignKey("item_id")]
    [InverseProperty("ItemValueOptions")]
    public virtual Item item { get; set; } = null!;

    [ForeignKey("option_id")]
    [InverseProperty("ItemValueOptions")]
    public virtual FieldOption option { get; set; } = null!;
}
