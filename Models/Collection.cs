using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CollectorHub.Models;

public partial class Collection
{
    [Key]
    public int collection_id { get; set; }

    [Required(ErrorMessage = "Поле пользователя обязательно")]
    public int user_id { get; set; }

    public int? template_id { get; set; }

    [Required(ErrorMessage = "Поле видимости обязательно")]
    public int visibility_id { get; set; }

    public int? parent_id { get; set; }

    [Required(ErrorMessage = "Название коллекции обязательно")]
    [StringLength(100, ErrorMessage = "Название не может быть длиннее 100 символов")]
    [Unicode(false)]
    public string name { get; set; } = null!;

    [Column(TypeName = "text")]
    public string? description { get; set; }

    [InverseProperty("collection")]
    public virtual ICollection<CollectionField> CollectionFields { get; set; } = new List<CollectionField>();

    [InverseProperty("collection")]
    public virtual ICollection<CollectionImage> CollectionImages { get; set; } = new List<CollectionImage>();

    [InverseProperty("parent")]
    public virtual ICollection<Collection> Inverseparent { get; set; } = new List<Collection>();

    [InverseProperty("collection")]
    public virtual ICollection<Item> Items { get; set; } = new List<Item>();

    [ForeignKey("parent_id")]
    [InverseProperty("Inverseparent")]
    public virtual Collection? parent { get; set; }

    [ForeignKey("template_id")]
    [InverseProperty("Collections")]
    public virtual Template? template { get; set; }

    [ForeignKey("user_id")]
    [InverseProperty("Collections")]
    public virtual User user { get; set; } = null!;

    [ForeignKey("visibility_id")]
    [InverseProperty("Collections")]
    public virtual VisibilityType visibility { get; set; } = null!;
}
