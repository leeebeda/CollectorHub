using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using CollectorHub.Models;

namespace CollectorHub.ViewModels;

public class CollectionFieldViewModel
{
    public int FieldId { get; set; }
    public int CollectionId { get; set; }
    [Required]
    public string Name { get; set; } = null!;
    [Required]
    public int FieldTypeId { get; set; }
    public string? FieldTypeName { get; set; }
    public bool IsRequired { get; set; }
    public List<string> Options { get; set; } = new List<string>();
    public List<int> OptionIds { get; set; } = new List<int>();
    public List<string> DeleteOptions { get; set; } = new List<string>();

    public async Task<int> GetFieldTypeIdAsync(DBContext context, string fieldTypeName)
    {
        var fieldType = await context.FieldTypes
            .FirstOrDefaultAsync(ft => ft.name == fieldTypeName);
        return fieldType?.field_type_id ?? 0;
    }
}