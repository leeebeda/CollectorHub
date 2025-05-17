using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CollectorHub.Models;

[Table("ItemValueImage")]
public partial class ItemValueImage
{
    [Key]
    public int id { get; set; }

    public int item_id { get; set; }

    public int field_id { get; set; }

    [StringLength(255)]
    [Unicode(false)]
    public string? image_url { get; set; }

    public int? sort_order { get; set; }

    [ForeignKey("field_id")]
    [InverseProperty("ItemValueImages")]
    public virtual CollectionField field { get; set; } = null!;

    [ForeignKey("item_id")]
    [InverseProperty("ItemValueImages")]
    public virtual Item item { get; set; } = null!;
}
