using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CollectorHub.Models;

public partial class CollectionField
{
    [Key]
    public int field_id { get; set; }

    public int collection_id { get; set; }

    [StringLength(100)]
    [Unicode(false)]
    public string name { get; set; } = null!;

    public int field_type_id { get; set; }

    public bool is_required { get; set; }

    [InverseProperty("field")]
    public virtual ICollection<FieldOption> FieldOptions { get; set; } = new List<FieldOption>();

    [InverseProperty("field")]
    public virtual ICollection<ItemValueBool> ItemValueBools { get; set; } = new List<ItemValueBool>();

    [InverseProperty("field")]
    public virtual ICollection<ItemValueDate> ItemValueDates { get; set; } = new List<ItemValueDate>();

    [InverseProperty("field")]
    public virtual ICollection<ItemValueImage> ItemValueImages { get; set; } = new List<ItemValueImage>();

    [InverseProperty("field")]
    public virtual ICollection<ItemValueNumber> ItemValueNumbers { get; set; } = new List<ItemValueNumber>();

    [InverseProperty("field")]
    public virtual ICollection<ItemValueOption> ItemValueOptions { get; set; } = new List<ItemValueOption>();

    [InverseProperty("field")]
    public virtual ICollection<ItemValueText> ItemValueTexts { get; set; } = new List<ItemValueText>();

    [ForeignKey("collection_id")]
    [InverseProperty("CollectionFields")]
    public virtual Collection collection { get; set; } = null!;

    [ForeignKey("field_type_id")]
    [InverseProperty("CollectionFields")]
    public virtual FieldType field_type { get; set; } = null!;
}
