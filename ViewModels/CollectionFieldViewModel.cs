using System.ComponentModel.DataAnnotations;

namespace CollectorHub.ViewModels
{
    public class CollectionFieldViewModel
    {
        public int FieldId { get; set; }

        public int CollectionId { get; set; }

        [Required(ErrorMessage = "Название поля обязательно")]
        [StringLength(100, ErrorMessage = "Название не может быть длиннее 100 символов")]
        public string Name { get; set; } = null!;

        [Required(ErrorMessage = "Тип поля обязателен")]
        public int FieldTypeId { get; set; }

        public string? FieldTypeName { get; set; }

        public bool IsRequired { get; set; }

        public List<string> Options { get; set; } = new List<string>();
    }
}