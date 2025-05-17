using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CollectorHub.Models;

[Table("ItemValueNumber")]
public partial class ItemValueNumber
{
    [Key]
    public int id { get; set; }

    public int item_id { get; set; }

    public int field_id { get; set; }

    public int? value { get; set; }

    [ForeignKey("field_id")]
    [InverseProperty("ItemValueNumbers")]
    public virtual CollectionField field { get; set; } = null!;

    [ForeignKey("item_id")]
    [InverseProperty("ItemValueNumbers")]
    public virtual Item item { get; set; } = null!;
}
