using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CollectorHub.Models;

[Table("ItemValueDate")]
public partial class ItemValueDate
{
    [Key]
    public int id { get; set; }

    public int item_id { get; set; }

    public int field_id { get; set; }

    public DateOnly? value { get; set; }

    [ForeignKey("field_id")]
    [InverseProperty("ItemValueDates")]
    public virtual CollectionField field { get; set; } = null!;

    [ForeignKey("item_id")]
    [InverseProperty("ItemValueDates")]
    public virtual Item item { get; set; } = null!;
}
