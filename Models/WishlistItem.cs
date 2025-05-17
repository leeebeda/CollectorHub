using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CollectorHub.Models;

public partial class WishlistItem
{
    [Key]
    public int item_id { get; set; }

    public int? rank { get; set; }

    [ForeignKey("item_id")]
    [InverseProperty("WishlistItem")]
    public virtual Item item { get; set; } = null!;
}
