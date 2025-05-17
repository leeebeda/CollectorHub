using System.ComponentModel.DataAnnotations;

namespace CollectorHub.ViewModels
{
    public class CreateCollectionViewModel
    {
        [Required(ErrorMessage = "Название коллекции обязательно")]
        [StringLength(100, ErrorMessage = "Название не может быть длиннее 100 символов")]
        public string Name { get; set; } = null!;

        public string? Description { get; set; }

        [Required(ErrorMessage = "Поле видимости обязательно")]
        public int VisibilityId { get; set; }

        public int? TemplateId { get; set; }

        public int? ParentId { get; set; }
    }
}