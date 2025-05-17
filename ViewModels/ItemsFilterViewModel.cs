// ViewModels/ItemsFilterViewModel.cs
using System.ComponentModel.DataAnnotations;

namespace CollectorHub.ViewModels
{
    public class ItemsFilterViewModel
    {
        public int CollectionId { get; set; }
        public string? SearchTerm { get; set; }
        public int? StatusId { get; set; }
        public string? SortBy { get; set; }
        public bool SortDescending { get; set; }
        public Dictionary<int, string> FieldFilters { get; set; } = new Dictionary<int, string>();

        // Для пагинации
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 12;

        // Результаты
        public IEnumerable<Models.Item> Items { get; set; } = new List<Models.Item>();
        public int TotalItems { get; set; }
        public int TotalPages => (int)Math.Ceiling(TotalItems / (double)PageSize);

        // Дополнительные данные
        public string CollectionName { get; set; } = string.Empty;
        public IEnumerable<Models.CollectionField> CollectionFields { get; set; } = new List<Models.CollectionField>();
        public IEnumerable<Models.ItemStatus> Statuses { get; set; } = new List<Models.ItemStatus>();
    }
}