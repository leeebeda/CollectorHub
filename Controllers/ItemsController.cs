using CollectorHub.Models;
using CollectorHub.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CollectorHub.Controllers
{
    [Authorize]
    public class ItemsController : Controller
    {
        private readonly DBContext _context;
        private readonly IWebHostEnvironment _environment; // !!! Добавили IWebHostEnvironment

        public ItemsController(DBContext context, IWebHostEnvironment environment) // !!! Обновили конструктор
        {
            _context = context;
            _environment = environment; // !!! Инициализировали _environment
        }

        // GET: Items/Index
        public async Task<IActionResult> Index(int? collectionId, ItemsFilterViewModel filter = null)
        {
            if (collectionId == null)
            {
                return NotFound();
            }

            var userId = int.Parse(User.FindFirstValue("UserId"));

            var collection = await _context.Collections
                .Include(c => c.visibility)
                .FirstOrDefaultAsync(c => c.collection_id == collectionId && c.user_id == userId);

            if (collection == null)
            {
                return NotFound();
            }

            if (filter == null)
            {
                filter = new ItemsFilterViewModel();
            }

            filter.CollectionId = collectionId.Value;
            filter.CollectionName = collection.name;

            filter.CollectionFields = await _context.CollectionFields
                .Include(f => f.field_type)
                .Where(f => f.collection_id == collectionId)
                .ToListAsync();

            filter.Statuses = await _context.ItemStatuses.ToListAsync();

            var query = _context.Items
                .Include(i => i.status)
                .Where(i => i.collection_id == collectionId);

            if (!string.IsNullOrEmpty(filter.SearchTerm))
            {
                query = query.Where(i => i.name.Contains(filter.SearchTerm));
            }

            if (filter.StatusId.HasValue)
            {
                query = query.Where(i => i.status_id == filter.StatusId.Value);
            }

            filter.TotalItems = await query.CountAsync();

            if (!string.IsNullOrEmpty(filter.SortBy))
            {
                switch (filter.SortBy)
                {
                    case "name":
                        query = filter.SortDescending
                            ? query.OrderByDescending(i => i.name)
                            : query.OrderBy(i => i.name);
                        break;

                    case "status":
                        query = filter.SortDescending
                            ? query.OrderByDescending(i => i.status.name)
                            : query.OrderBy(i => i.status.name);
                        break;

                    case "date":
                        query = filter.SortDescending
                            ? query.OrderByDescending(i => i.created_at)
                            : query.OrderBy(i => i.created_at);
                        break;

                    default:
                        query = query.OrderByDescending(i => i.created_at);
                        break;
                }
            }
            else
            {
                query = query.OrderByDescending(i => i.created_at);
            }

            query = query.Skip((filter.Page - 1) * filter.PageSize).Take(filter.PageSize);

            filter.Items = await query.ToListAsync();

            return View(filter);
        }

        [HttpPost]
        public IActionResult Filter(int collectionId, ItemsFilterViewModel filter)
        {
            return RedirectToAction(nameof(Index), new
            {
                collectionId = collectionId,
                searchTerm = filter.SearchTerm,
                statusId = filter.StatusId,
                sortBy = filter.SortBy,
                sortDescending = filter.SortDescending,
                page = 1
            });
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var userId = int.Parse(User.FindFirstValue("UserId"));

            var item = await _context.Items
                .Include(i => i.collection)
                .Include(i => i.status)
                .FirstOrDefaultAsync(m => m.item_id == id && m.collection.user_id == userId);

            if (item == null)
            {
                return NotFound();
            }

            var fields = await _context.CollectionFields
                .Include(f => f.field_type)
                .Where(f => f.collection_id == item.collection_id)
                .ToListAsync();

            var textValues = await _context.ItemValueTexts
                .Where(v => v.item_id == id)
                .ToDictionaryAsync(v => v.field_id, v => v.value);

            var numberValues = await _context.ItemValueNumbers
                .Where(v => v.item_id == id)
                .ToDictionaryAsync(v => v.field_id, v => v.value);

            var dateValues = await _context.ItemValueDates
                .Where(v => v.item_id == id)
                .ToDictionaryAsync(v => v.field_id, v => v.value.HasValue ?
                    (DateTime?)v.value.Value.ToDateTime(TimeOnly.MinValue) : null);

            var boolValues = await _context.ItemValueBools
                .Where(v => v.item_id == id)
                .ToDictionaryAsync(v => v.field_id, v => v.value);

            var optionValues = await _context.ItemValueOptions
                .Include(v => v.option)
                .Where(v => v.item_id == id)
                .ToDictionaryAsync(v => v.field_id, v => v.option);

            var imageValues = await _context.ItemValueImages
                .Where(v => v.item_id == id)
                .GroupBy(v => v.field_id)
                .ToDictionaryAsync(g => g.Key, g => g.OrderBy(v => v.sort_order).ToList());

            int? wishlistRank = null;
            if (item.status.code == "wishlist")
            {
                var wishlistItem = await _context.WishlistItems
                    .FirstOrDefaultAsync(w => w.item_id == id);

                if (wishlistItem != null)
                {
                    wishlistRank = wishlistItem.rank;
                }
            }

            var viewModel = new ItemDetailsViewModel
            {
                Item = item,
                CollectionFields = fields,
                TextValues = textValues,
                NumberValues = numberValues,
                DateValues = dateValues,
                BoolValues = boolValues,
                OptionValues = optionValues,
                ImageValues = imageValues,
                WishlistRank = wishlistRank,
                CollectionId = item.collection_id
            };

            return View(viewModel);
        }

        public async Task<IActionResult> Create(int? collectionId)
        {
            if (collectionId == null)
            {
                return NotFound();
            }

            var userId = int.Parse(User.FindFirstValue("UserId"));

            var collection = await _context.Collections
                .FirstOrDefaultAsync(c => c.collection_id == collectionId && c.user_id == userId);

            if (collection == null)
            {
                return NotFound();
            }

            var collectionFields = await _context.CollectionFields
                .Include(f => f.field_type)
                .Where(f => f.collection_id == collectionId)
                .ToListAsync();

            var optionFields = collectionFields
                .Where(f => f.field_type.name == "Варианты")
                .Select(f => f.field_id)
                .ToList();

            var fieldOptions = await _context.FieldOptions
                .Where(o => optionFields.Contains(o.field_id))
                .GroupBy(o => o.field_id)
                .ToDictionaryAsync(g => g.Key, g => g.ToList());

            ViewData["status_id"] = new SelectList(_context.ItemStatuses, "status_id", "name");

            var wishlistStatus = await _context.ItemStatuses.FirstOrDefaultAsync(s => s.code == "wishlist");
            ViewBag.WishlistStatusId = wishlistStatus?.status_id;

            var viewModel = new CreateItemViewModel
            {
                CollectionId = collectionId.Value,
                CollectionName = collection.name,
                CollectionFields = collectionFields,
                FieldOptions = fieldOptions
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateItemViewModel viewModel)
        {
            var userId = int.Parse(User.FindFirstValue("UserId"));

            var collection = await _context.Collections
                .FirstOrDefaultAsync(c => c.collection_id == viewModel.CollectionId && c.user_id == userId);

            if (collection == null)
            {
                return NotFound();
            }

            var status = await _context.ItemStatuses.FindAsync(viewModel.StatusId);
            if (viewModel.WishlistRank.HasValue && status?.code != "wishlist")
            {
                ModelState.AddModelError("WishlistRank", "Ранг в списке желаний можно указать только для статуса 'Вишлист'.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var item = new Item
                    {
                        collection_id = viewModel.CollectionId,
                        name = viewModel.Name,
                        status_id = viewModel.StatusId,
                        created_at = DateTime.Now
                    };

                    _context.Add(item);
                    await _context.SaveChangesAsync();

                    if (status?.code == "wishlist" && viewModel.WishlistRank.HasValue)
                    {
                        var wishlistItem = new WishlistItem
                        {
                            item_id = item.item_id,
                            rank = viewModel.WishlistRank.Value
                        };

                        _context.Add(wishlistItem);
                        await _context.SaveChangesAsync();
                    }

                    await SaveFieldValues(item.item_id, viewModel.FieldValues);

                    return RedirectToAction(nameof(Details), new { id = item.item_id });
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Произошла ошибка: {ex.Message}");
                }
            }

            var collectionFields = await _context.CollectionFields
                .Include(f => f.field_type)
                .Where(f => f.collection_id == viewModel.CollectionId)
                .ToListAsync();

            var optionFields = collectionFields
                .Where(f => f.field_type.name == "Варианты")
                .Select(f => f.field_id)
                .ToList();

            var fieldOptions = await _context.FieldOptions
                .Where(o => optionFields.Contains(o.field_id))
                .GroupBy(o => o.field_id)
                .ToDictionaryAsync(g => g.Key, g => g.ToList());

            ViewData["status_id"] = new SelectList(_context.ItemStatuses, "status_id", "name", viewModel.StatusId);

            var wishlistStatus = await _context.ItemStatuses.FirstOrDefaultAsync(s => s.code == "wishlist");
            ViewBag.WishlistStatusId = wishlistStatus?.status_id;

            viewModel.CollectionName = collection.name;
            viewModel.CollectionFields = collectionFields;
            viewModel.FieldOptions = fieldOptions;

            return View(viewModel);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var userId = int.Parse(User.FindFirstValue("UserId"));

            var item = await _context.Items
                .Include(i => i.collection)
                .Include(i => i.status)
                .FirstOrDefaultAsync(i => i.item_id == id && i.collection.user_id == userId);

            if (item == null)
            {
                return NotFound();
            }

            var collectionFields = await _context.CollectionFields
                .Include(f => f.field_type)
                .Where(f => f.collection_id == item.collection_id)
                .ToListAsync();

            var optionFields = collectionFields
                .Where(f => f.field_type.name == "Варианты")
                .Select(f => f.field_id)
                .ToList();

            var fieldOptions = await _context.FieldOptions
                .Where(o => optionFields.Contains(o.field_id))
                .GroupBy(o => o.field_id)
                .ToDictionaryAsync(g => g.Key, g => g.ToList());

            var textValues = await _context.ItemValueTexts
                .Where(v => v.item_id == id)
                .ToDictionaryAsync(v => v.field_id, v => v.value);

            var numberValues = await _context.ItemValueNumbers
                .Where(v => v.item_id == id)
                .ToDictionaryAsync(v => v.field_id, v => v.value);

            var dateValues = await _context.ItemValueDates
                .Where(v => v.item_id == id)
                .ToDictionaryAsync(v => v.field_id, v => v.value);

            var boolValues = await _context.ItemValueBools
                .Where(v => v.item_id == id)
                .ToDictionaryAsync(v => v.field_id, v => v.value);

            var optionValues = await _context.ItemValueOptions
                .Where(v => v.item_id == id)
                .ToDictionaryAsync(v => v.field_id, v => v.option_id);

            var imageValues = await _context.ItemValueImages
                .Where(v => v.item_id == id)
                .OrderBy(v => v.sort_order)
                .GroupBy(v => v.field_id)
                .ToDictionaryAsync(g => g.Key, g => g.ToList());

            ViewData["status_id"] = new SelectList(_context.ItemStatuses, "status_id", "name", item.status_id);

            var wishlistStatus = await _context.ItemStatuses.FirstOrDefaultAsync(s => s.code == "wishlist");
            ViewBag.WishlistStatusId = wishlistStatus?.status_id;

            int? wishlistRank = null;
            if (item.status_id == wishlistStatus?.status_id)
            {
                var wishlistItem = await _context.WishlistItems
                    .FirstOrDefaultAsync(w => w.item_id == id);

                wishlistRank = wishlistItem?.rank;
            }

            var viewModel = new EditItemViewModel
            {
                ItemId = item.item_id,
                CollectionId = item.collection_id,
                CollectionName = item.collection.name,
                Name = item.name,
                StatusId = item.status_id,
                WishlistRank = wishlistRank,
                CollectionFields = collectionFields,
                FieldOptions = fieldOptions,
                FieldValues = new Dictionary<int, string>()
            };

            foreach (var field in collectionFields)
            {
                switch (field.field_type.name)
                {
                    case "Текст":
                        if (textValues.TryGetValue(field.field_id, out var textValue))
                            viewModel.FieldValues[field.field_id] = textValue;
                        break;
                    case "Число":
                        if (numberValues.TryGetValue(field.field_id, out var numberValue))
                            viewModel.FieldValues[field.field_id] = numberValue?.ToString();
                        break;
                    case "Дата":
                        if (dateValues.TryGetValue(field.field_id, out var dateValue))
                            viewModel.FieldValues[field.field_id] = dateValue?.ToString("yyyy-MM-dd");
                        break;
                    case "Да-нет":
                        if (boolValues.TryGetValue(field.field_id, out var boolValue))
                            viewModel.FieldValues[field.field_id] = boolValue?.ToString();
                        break;
                    case "Варианты":
                        if (optionValues.TryGetValue(field.field_id, out var optionValue))
                            viewModel.FieldValues[field.field_id] = optionValue.ToString();
                        break;
                    case "Фото":
                        break;
                }
            }

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, EditItemViewModel viewModel)
        {
            if (id != viewModel.ItemId)
            {
                return NotFound();
            }

            var userId = int.Parse(User.FindFirstValue("UserId"));

            var item = await _context.Items
                .Include(i => i.collection)
                .FirstOrDefaultAsync(i => i.item_id == id && i.collection.user_id == userId);

            if (item == null)
            {
                return NotFound();
            }

            var status = await _context.ItemStatuses.FindAsync(viewModel.StatusId);
            if (viewModel.WishlistRank.HasValue && status?.code != "wishlist")
            {
                ModelState.AddModelError("WishlistRank", "Ранг в списке желаний можно указать только для статуса 'Вишлист'.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    item.name = viewModel.Name;
                    item.status_id = viewModel.StatusId;

                    _context.Update(item);

                    if (status?.code == "wishlist")
                    {
                        var wishlistItem = await _context.WishlistItems
                            .FirstOrDefaultAsync(w => w.item_id == id);

                        if (wishlistItem == null)
                        {
                            wishlistItem = new WishlistItem
                            {
                                item_id = id,
                                rank = viewModel.WishlistRank ?? 0
                            };

                            _context.Add(wishlistItem);
                        }
                        else
                        {
                            wishlistItem.rank = viewModel.WishlistRank ?? 0;
                            _context.Update(wishlistItem);
                        }
                    }
                    else
                    {
                        var wishlistItem = await _context.WishlistItems
                            .FirstOrDefaultAsync(w => w.item_id == id);

                        if (wishlistItem != null)
                        {
                            _context.Remove(wishlistItem);
                        }
                    }

                    await _context.SaveChangesAsync();

                    await UpdateFieldValues(id, viewModel.FieldValues);

                    return RedirectToAction(nameof(Details), new { id = id });
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ItemExists(id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            var collectionFields = await _context.CollectionFields
                .Include(f => f.field_type)
                .Where(f => f.collection_id == item.collection_id)
                .ToListAsync();

            var optionFields = collectionFields
                .Where(f => f.field_type.name == "Варианты")
                .Select(f => f.field_id)
                .ToList();

            var fieldOptions = await _context.FieldOptions
                .Where(o => optionFields.Contains(o.field_id))
                .GroupBy(o => o.field_id)
                .ToDictionaryAsync(g => g.Key, g => g.ToList());

            ViewData["status_id"] = new SelectList(_context.ItemStatuses, "status_id", "name", viewModel.StatusId);

            var wishlistStatus = await _context.ItemStatuses.FirstOrDefaultAsync(s => s.code == "wishlist");
            ViewBag.WishlistStatusId = wishlistStatus?.status_id;

            viewModel.CollectionName = item.collection.name;
            viewModel.CollectionFields = collectionFields;
            viewModel.FieldOptions = fieldOptions;

            return View(viewModel);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var userId = int.Parse(User.FindFirstValue("UserId"));

            var item = await _context.Items
                .Include(i => i.collection)
                .Include(i => i.status)
                .FirstOrDefaultAsync(i => i.item_id == id && i.collection.user_id == userId);

            if (item == null)
            {
                return NotFound();
            }

            return View(item);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var userId = int.Parse(User.FindFirstValue("UserId"));

            var item = await _context.Items
                .Include(i => i.collection)
                .FirstOrDefaultAsync(i => i.item_id == id && i.collection.user_id == userId);

            if (item == null)
            {
                return NotFound();
            }

            // Удаляем значения полей
            var textValues = await _context.ItemValueTexts
                .Where(v => v.item_id == id)
                .ToListAsync();
            _context.ItemValueTexts.RemoveRange(textValues);

            var numberValues = await _context.ItemValueNumbers
                .Where(v => v.item_id == id)
                .ToListAsync();
            _context.ItemValueNumbers.RemoveRange(numberValues);

            var dateValues = await _context.ItemValueDates
                .Where(v => v.item_id == id)
                .ToListAsync();
            _context.ItemValueDates.RemoveRange(dateValues);

            var boolValues = await _context.ItemValueBools
                .Where(v => v.item_id == id)
                .ToListAsync();
            _context.ItemValueBools.RemoveRange(boolValues);

            var optionValues = await _context.ItemValueOptions
                .Where(v => v.item_id == id)
                .ToListAsync();
            _context.ItemValueOptions.RemoveRange(optionValues);

            // Удаляем изображения и их файлы
            var imageValues = await _context.ItemValueImages
                .Where(v => v.item_id == id)
                .ToListAsync();
            foreach (var image in imageValues)
            {
                try
                {
                    var filePath = Path.Combine(_environment.WebRootPath, image.image_url.TrimStart('/'));
                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка при удалении файла {image.image_url}: {ex.Message}");
                }
            }
            _context.ItemValueImages.RemoveRange(imageValues); // !!! Удаляем записи из БД после удаления файлов

            // Удаляем запись из WishlistItems, если она есть
            var wishlistItem = await _context.WishlistItems
                .FirstOrDefaultAsync(w => w.item_id == id);
            if (wishlistItem != null)
            {
                _context.WishlistItems.Remove(wishlistItem);
            }

            // Удаляем сам предмет
            _context.Items.Remove(item);

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index), new { collectionId = item.collection_id });
        }

        private async Task SaveFieldValues(int itemId, Dictionary<int, string> fieldValues)
        {
            if (fieldValues == null)
                return;

            foreach (var fieldValue in fieldValues)
            {
                var fieldId = fieldValue.Key;
                var value = fieldValue.Value;

                var field = await _context.CollectionFields
                    .Include(f => f.field_type)
                    .FirstOrDefaultAsync(f => f.field_id == fieldId);

                if (field == null)
                    continue;

                switch (field.field_type.name)
                {
                    case "Текст":
                        if (!string.IsNullOrEmpty(value))
                        {
                            _context.ItemValueTexts.Add(new ItemValueText
                            {
                                item_id = itemId,
                                field_id = fieldId,
                                value = value
                            });
                        }
                        break;

                    case "Число":
                        if (int.TryParse(value, out var numberValue))
                        {
                            _context.ItemValueNumbers.Add(new ItemValueNumber
                            {
                                item_id = itemId,
                                field_id = fieldId,
                                value = numberValue
                            });
                        }
                        break;

                    case "Дата":
                        if (DateTime.TryParse(value, out var dateValue))
                        {
                            _context.ItemValueDates.Add(new ItemValueDate
                            {
                                item_id = itemId,
                                field_id = fieldId,
                                value = DateOnly.FromDateTime(dateValue)
                            });
                        }
                        break;

                    case "Да-нет":
                        if (bool.TryParse(value, out var boolValue))
                        {
                            _context.ItemValueBools.Add(new ItemValueBool
                            {
                                item_id = itemId,
                                field_id = fieldId,
                                value = boolValue
                            });
                        }
                        break;

                    case "Варианты":
                        if (int.TryParse(value, out var optionId))
                        {
                            _context.ItemValueOptions.Add(new ItemValueOption
                            {
                                item_id = itemId,
                                field_id = fieldId,
                                option_id = optionId
                            });
                        }
                        break;

                    case "Фото":
                        break;
                }
            }

            await _context.SaveChangesAsync();
        }

        private async Task UpdateFieldValues(int itemId, Dictionary<int, string> fieldValues)
        {
            if (fieldValues == null)
                return;

            var textValues = await _context.ItemValueTexts
                .Where(v => v.item_id == itemId)
                .ToListAsync();
            var numberValues = await _context.ItemValueNumbers
                .Where(v => v.item_id == itemId)
                .ToListAsync();
            var dateValues = await _context.ItemValueDates
                .Where(v => v.item_id == itemId)
                .ToListAsync();
            var boolValues = await _context.ItemValueBools
                .Where(v => v.item_id == itemId)
                .ToListAsync();
            var optionValues = await _context.ItemValueOptions
                .Where(v => v.item_id == itemId)
                .ToListAsync();

            _context.ItemValueTexts.RemoveRange(textValues);
            _context.ItemValueNumbers.RemoveRange(numberValues);
            _context.ItemValueDates.RemoveRange(dateValues);
            _context.ItemValueBools.RemoveRange(boolValues);
            _context.ItemValueOptions.RemoveRange(optionValues);

            await _context.SaveChangesAsync();

            await SaveFieldValues(itemId, fieldValues);
        }

        private bool ItemExists(int id)
        {
            return _context.Items.Any(e => e.item_id == id);
        }
    }
}