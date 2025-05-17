using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CollectorHub.Models;

[Table("ItemValueText")]
public partial class ItemValueText
{
    [Key]
    public int id { get; set; }

    public int item_id { get; set; }

    public int field_id { get; set; }

    [Column(TypeName = "text")]
    public string? value { get; set; }

    [ForeignKey("field_id")]
    [InverseProperty("ItemValueTexts")]
    public virtual CollectionField field { get; set; } = null!;

    [ForeignKey("item_id")]
    [InverseProperty("ItemValueTexts")]
    public virtual Item item { get; set; } = null!;
}
