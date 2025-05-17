// ViewModels/ItemDetailsViewModel.cs
using CollectorHub.Models;

namespace CollectorHub.ViewModels
{
    public class ItemDetailsViewModel
    {
        public Item Item { get; set; } = null!;
        public IEnumerable<CollectionField> CollectionFields { get; set; } = new List<CollectionField>();
        public Dictionary<int, string?> TextValues { get; set; } = new Dictionary<int, string?>();
        public Dictionary<int, int?> NumberValues { get; set; } = new Dictionary<int, int?>();
        public Dictionary<int, DateTime?> DateValues { get; set; } = new Dictionary<int, DateTime?>();
        public Dictionary<int, bool?> BoolValues { get; set; } = new Dictionary<int, bool?>();
        public Dictionary<int, FieldOption> OptionValues { get; set; } = new Dictionary<int, FieldOption>();
        public Dictionary<int, List<ItemValueImage>> ImageValues { get; set; } = new Dictionary<int, List<ItemValueImage>>();
        public int? WishlistRank { get; set; }
        public int CollectionId { get; set; }
    }
}