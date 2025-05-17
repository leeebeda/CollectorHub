using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CollectorHub.Models;

public partial class TemplateField
{
    [Key]
    public int field_id { get; set; }

    public int template_id { get; set; }

    [StringLength(100)]
    [Unicode(false)]
    public string name { get; set; } = null!;

    public int field_type_id { get; set; }

    public bool is_required { get; set; }

    [ForeignKey("field_type_id")]
    [InverseProperty("TemplateFields")]
    public virtual FieldType field_type { get; set; } = null!;

    [ForeignKey("template_id")]
    [InverseProperty("TemplateFields")]
    public virtual Template template { get; set; } = null!;
}
