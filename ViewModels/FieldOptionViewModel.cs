using System.ComponentModel.DataAnnotations;

namespace CollectorHub.ViewModels
{
    public class FieldOptionViewModel
    {
        public int FieldId { get; set; }

        public int CollectionId { get; set; }

        public string FieldName { get; set; } = null!;

        [Required(ErrorMessage = "Текст варианта обязателен")]
        [StringLength(100, ErrorMessage = "Текст варианта не может быть длиннее 100 символов")]
        public string OptionText { get; set; } = null!;
    }
}