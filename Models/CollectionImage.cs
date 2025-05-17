using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CollectorHub.Models;

public partial class CollectionImage
{
    [Key]
    public int id { get; set; }

    public int collection_id { get; set; }

    [StringLength(255)]
    [Unicode(false)]
    public string? image_url { get; set; }

    public int? sort_order { get; set; }

    [ForeignKey("collection_id")]
    [InverseProperty("CollectionImages")]
    public virtual Collection collection { get; set; } = null!;
}
