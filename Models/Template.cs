using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CollectorHub.Models;

public partial class Template
{
    [Key]
    public int template_id { get; set; }

    [StringLength(100)]
    [Unicode(false)]
    public string name { get; set; } = null!;

    [Column(TypeName = "text")]
    public string? description { get; set; }

    [InverseProperty("template")]
    public virtual ICollection<Collection> Collections { get; set; } = new List<Collection>();

    [InverseProperty("template")]
    public virtual ICollection<TemplateField> TemplateFields { get; set; } = new List<TemplateField>();
}
