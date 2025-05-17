using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CollectorHub.Models;

public partial class Item
{
    [Key]
    public int item_id { get; set; }

    public int collection_id { get; set; }

    [StringLength(100)]
    [Unicode(false)]
    public string name { get; set; } = null!;

    public int status_id { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? created_at { get; set; }

    [InverseProperty("item")]
    public virtual ICollection<ItemValueBool> ItemValueBools { get; set; } = new List<ItemValueBool>();

    [InverseProperty("item")]
    public virtual ICollection<ItemValueDate> ItemValueDates { get; set; } = new List<ItemValueDate>();

    [InverseProperty("item")]
    public virtual ICollection<ItemValueImage> ItemValueImages { get; set; } = new List<ItemValueImage>();

    [InverseProperty("item")]
    public virtual ICollection<ItemValueNumber> ItemValueNumbers { get; set; } = new List<ItemValueNumber>();

    [InverseProperty("item")]
    public virtual ICollection<ItemValueOption> ItemValueOptions { get; set; } = new List<ItemValueOption>();

    [InverseProperty("item")]
    public virtual ICollection<ItemValueText> ItemValueTexts { get; set; } = new List<ItemValueText>();

    [InverseProperty("item")]
    public virtual WishlistItem? WishlistItem { get; set; }

    [ForeignKey("collection_id")]
    [InverseProperty("Items")]
    public virtual Collection collection { get; set; } = null!;

    [ForeignKey("status_id")]
    [InverseProperty("Items")]
    public virtual ItemStatus status { get; set; } = null!;
}
