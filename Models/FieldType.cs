using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CollectorHub.Models;

public partial class FieldType
{
    [Key]
    public int field_type_id { get; set; }

    [StringLength(50)]
    [Unicode(false)]
    public string name { get; set; } = null!;

    [InverseProperty("field_type")]
    public virtual ICollection<CollectionField> CollectionFields { get; set; } = new List<CollectionField>();

    [InverseProperty("field_type")]
    public virtual ICollection<TemplateField> TemplateFields { get; set; } = new List<TemplateField>();
}
