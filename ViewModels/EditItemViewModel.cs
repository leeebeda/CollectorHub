using CollectorHub.Models;
using System.ComponentModel.DataAnnotations;

namespace CollectorHub.ViewModels
{
    public class EditItemViewModel
    {
        public int ItemId { get; set; }
        public int CollectionId { get; set; }
        public string CollectionName { get; set; } = null!;

        [Required(ErrorMessage = "Название предмета обязательно")]
        [StringLength(100, ErrorMessage = "Название не может быть длиннее 100 символов")]
        public string Name { get; set; } = null!;

        [Required(ErrorMessage = "Статус предмета обязателен")]
        public int StatusId { get; set; }

        public int? WishlistRank { get; set; }

        public IEnumerable<CollectionField> CollectionFields { get; set; } = new List<CollectionField>();
        public Dictionary<int, List<FieldOption>> FieldOptions { get; set; } = new Dictionary<int, List<FieldOption>>();
        public Dictionary<int, string> FieldValues { get; set; } = new Dictionary<int, string>();
    }
}