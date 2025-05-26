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
    public class CollectionsController : Controller
    {
        private readonly DBContext _context;
        private readonly IWebHostEnvironment _environment;

        public CollectionsController(DBContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment; // !!! Добавили инициализацию _environment
        }

        // GET: Collections
        public async Task<IActionResult> Index(string? accessToken)
        {
            var userId = int.Parse(User.FindFirstValue("UserId"));
            var collections = HttpContext.Items["Collections"] as List<Collection>;

            List<Collection> accessibleCollections = new List<Collection>();
            if (!string.IsNullOrEmpty(accessToken))
            {
                try
                {
                    var collectionId = int.Parse(accessToken);
                    var collection = await _context.Collections
                        .Include(c => c.visibility)
                        .Include(c => c.template)
                        .Include(c => c.parent)
                        .Include(c => c.Items)
                        .FirstOrDefaultAsync(c => c.collection_id == collectionId && c.visibility.code == "link");

                    if (collection != null && collection.user_id != userId)
                    {
                        accessibleCollections.Add(collection);
                    }
                }
                catch (FormatException)
                {
                    // Неверный формат accessToken, игнорируем
                }
            }

            var combinedCollections = collections.Union(accessibleCollections).ToList();
            return View(combinedCollections);
        }

        // GET: Collections/Details
        public async Task<IActionResult> Details(int? id, string? accessToken)
        {
            if (id == null)
            {
                return NotFound();
            }

            var userId = int.Parse(User.FindFirstValue("UserId"));

            var collection = await _context.Collections
                .Include(c => c.visibility)
                .Include(c => c.template)
                .Include(c => c.parent)
                .Include(c => c.Items)
                    .ThenInclude(i => i.status)
                .Include(c => c.CollectionImages)
                .FirstOrDefaultAsync(m => m.collection_id == id);

            if (collection == null)
            {
                return NotFound();
            }

            bool hasAccess = false;
            if (collection.user_id == userId)
            {
                hasAccess = true;
            }
            else if (collection.visibility.code == "link")
            {
                if (accessToken != null && accessToken == collection.collection_id.ToString())
                {
                    hasAccess = true;
                }
            }

            if (!hasAccess)
            {
                return Forbid();
            }

            var childCollections = await _context.Collections
                .Where(c => c.parent_id == id)
                .Include(c => c.Items)
                .ToListAsync();

            ViewData["ChildCollections"] = childCollections;

            var collectionFields = await _context.CollectionFields
                .Include(f => f.field_type)
                .Where(f => f.collection_id == id)
                .ToListAsync();

            ViewData["CollectionFields"] = collectionFields;

            return View(collection);
        }

        // GET: Collections/Create
        public async Task<IActionResult> Create(int? parentId)
        {
            var userId = int.Parse(User.FindFirstValue("UserId"));

            ViewData["template_id"] = new SelectList(_context.Templates, "template_id", "name");
            ViewData["visibility_id"] = new SelectList(
                await _context.VisibilityTypes
                    .Where(v => v.visibility_id == 1 || v.visibility_id == 3)
                    .ToListAsync(),
                "visibility_id",
                "name",
                1
            );

            ViewData["parent_id"] = new SelectList(
                await _context.Collections.Where(c => c.user_id == userId).ToListAsync(),
                "collection_id",
                "name"
            );

            var viewModel = new CreateCollectionViewModel();

            if (parentId.HasValue)
            {
                var parentCollection = await _context.Collections
                    .FirstOrDefaultAsync(c => c.collection_id == parentId && c.user_id == userId);

                if (parentCollection != null)
                {
                    viewModel.ParentId = parentId;
                }
            }

            return View(viewModel);
        }

        // POST: Collections/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateCollectionViewModel viewModel)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var collection = new Collection
                    {
                        name = viewModel.Name,
                        description = viewModel.Description,
                        visibility_id = viewModel.VisibilityId,
                        template_id = viewModel.TemplateId,
                        parent_id = viewModel.ParentId,
                        user_id = int.Parse(User.FindFirstValue("UserId"))
                    };

                    _context.Add(collection);
                    await _context.SaveChangesAsync();

                    if (collection.template_id.HasValue)
                    {
                        await CopyTemplateFieldsToCollection(collection.template_id.Value,
                            collection.collection_id);
                    }

                    return RedirectToAction(nameof(Details), new { id = collection.collection_id });
                }
                else
                {
                    foreach (var key in ModelState.Keys)
                    {
                        var modelStateEntry = ModelState[key];
                        foreach (var error in modelStateEntry.Errors)
                        {
                            Console.WriteLine($"Key: {key}, Error: {error.ErrorMessage}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in Create: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                ModelState.AddModelError("", $"Произошла ошибка: {ex.Message}");
            }

            var userId = int.Parse(User.FindFirstValue("UserId"));
            ViewData["template_id"] = new SelectList(_context.Templates, "template_id", "name", viewModel.TemplateId);
            ViewData["visibility_id"] = new SelectList(
                await _context.VisibilityTypes
                    .Where(v => v.visibility_id == 1 || v.visibility_id == 3)
                    .ToListAsync(),
                "visibility_id",
                "name",
                viewModel.VisibilityId
            );
            ViewData["parent_id"] = new SelectList(
                await _context.Collections.Where(c => c.user_id == userId).ToListAsync(),
                "collection_id",
                "name",
                viewModel.ParentId
            );

            return View(viewModel);
        }

        private async Task CopyTemplateFieldsToCollection(int templateId, int collectionId)
        {
            var templateFields = await _context.TemplateFields
                .Where(tf => tf.template_id == templateId)
                .ToListAsync();

            foreach (var templateField in templateFields)
            {
                var collectionField = new CollectionField
                {
                    collection_id = collectionId,
                    name = templateField.name,
                    field_type_id = templateField.field_type_id,
                    is_required = templateField.is_required
                };

                _context.CollectionFields.Add(collectionField);
            }

            await _context.SaveChangesAsync();
        }

        // GET: Collections/Edit
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var userId = int.Parse(User.FindFirstValue("UserId"));

            var collection = await _context.Collections
                .FirstOrDefaultAsync(m => m.collection_id == id && m.user_id == userId);

            if (collection == null)
            {
                return NotFound();
            }

            var viewModel = new EditCollectionViewModel
            {
                Name = collection.name,
                Description = collection.description,
                VisibilityId = collection.visibility_id,
                TemplateId = collection.template_id,
                ParentId = collection.parent_id
            };

            ViewData["template_id"] = new SelectList(_context.Templates, "template_id", "name", collection.template_id);
            ViewData["visibility_id"] = new SelectList(
                await _context.VisibilityTypes
                    .Where(v => v.visibility_id == 1 || v.visibility_id == 3)
                    .ToListAsync(),
                "visibility_id",
                "name",
                collection.visibility_id
            );
            ViewData["parent_id"] = new SelectList(
                await _context.Collections.Where(c => c.user_id == userId && c.collection_id != id).ToListAsync(),
                "collection_id",
                "name",
                collection.parent_id
            );

            ViewData["CollectionId"] = id;

            return View(viewModel);
        }

        // POST: Collections/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, EditCollectionViewModel viewModel)
        {
            var userId = int.Parse(User.FindFirstValue("UserId"));
            var collection = await _context.Collections
                .FirstOrDefaultAsync(c => c.collection_id == id && c.user_id == userId);

            if (collection == null)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    collection.name = viewModel.Name;
                    collection.description = viewModel.Description;
                    collection.visibility_id = viewModel.VisibilityId;
                    collection.parent_id = viewModel.ParentId;

                    _context.Update(collection);
                    await _context.SaveChangesAsync();

                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CollectionExists(id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            ViewData["template_id"] = new SelectList(_context.Templates, "template_id", "name", viewModel.TemplateId);
            ViewData["visibility_id"] = new SelectList(
                await _context.VisibilityTypes
                    .Where(v => v.visibility_id == 1 || v.visibility_id == 3)
                    .ToListAsync(),
                "visibility_id",
                "name",
                viewModel.VisibilityId
            );
            ViewData["parent_id"] = new SelectList(
                await _context.Collections.Where(c => c.user_id == userId && c.collection_id != id).ToListAsync(),
                "collection_id",
                "name",
                viewModel.ParentId
            );

            return View(viewModel);
        }

        // GET: Collections/Delete
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var userId = int.Parse(User.FindFirstValue("UserId"));

            var collection = await _context.Collections
                .Include(c => c.visibility)
                .Include(c => c.template)
                .Include(c => c.parent)
                .FirstOrDefaultAsync(c => c.collection_id == id && c.user_id == userId);

            if (collection == null)
            {
                return NotFound();
            }

            return View(collection);
        }

        // POST: Collections/Delete
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var userId = int.Parse(User.FindFirstValue("UserId"));

            var collection = await _context.Collections
                .FirstOrDefaultAsync(c => c.collection_id == id && c.user_id == userId);

            if (collection == null)
            {
                return NotFound();
            }

            // Проверяем, есть ли дочерние коллекции
            var hasChildCollections = await _context.Collections
                .AnyAsync(c => c.parent_id == id);

            if (hasChildCollections)
            {
                ModelState.AddModelError("", "Невозможно удалить коллекцию, так как у неё есть дочерние коллекции.");

                collection = await _context.Collections
                    .Include(c => c.visibility)
                    .Include(c => c.template)
                    .Include(c => c.parent)
                    .FirstOrDefaultAsync(c => c.collection_id == id && c.user_id == userId);

                return View(collection);
            }

            // 1. Находим все предметы, связанные с коллекцией
            var items = await _context.Items
                .Where(i => i.collection_id == id)
                .ToListAsync();

            // 2. Находим все item_id для предметов
            var itemIds = items.Select(i => i.item_id).ToList();

            // 3. Удаляем все значения полей для этих предметов
            var textValues = await _context.ItemValueTexts
                .Where(v => itemIds.Contains(v.item_id))
                .ToListAsync();
            _context.ItemValueTexts.RemoveRange(textValues);

            var numberValues = await _context.ItemValueNumbers
                .Where(v => itemIds.Contains(v.item_id))
                .ToListAsync();
            _context.ItemValueNumbers.RemoveRange(numberValues);

            var dateValues = await _context.ItemValueDates
                .Where(v => itemIds.Contains(v.item_id))
                .ToListAsync();
            _context.ItemValueDates.RemoveRange(dateValues);

            var boolValues = await _context.ItemValueBools
                .Where(v => itemIds.Contains(v.item_id))
                .ToListAsync();
            _context.ItemValueBools.RemoveRange(boolValues);

            var optionValues = await _context.ItemValueOptions
                .Where(v => itemIds.Contains(v.item_id))
                .ToListAsync();
            _context.ItemValueOptions.RemoveRange(optionValues);

            var imageValues = await _context.ItemValueImages
                .Where(v => itemIds.Contains(v.item_id))
                .ToListAsync();
            _context.ItemValueImages.RemoveRange(imageValues);

            // 4. Удаляем записи из WishlistItems, связанные с этими предметами
            var wishlistItems = await _context.WishlistItems
                .Where(w => itemIds.Contains(w.item_id))
                .ToListAsync();
            _context.WishlistItems.RemoveRange(wishlistItems);

            // 5. Удаляем сами предметы
            _context.Items.RemoveRange(items);

            // 6. Удаляем связанные поля коллекции и их опции
            var collectionFields = await _context.CollectionFields
                .Where(f => f.collection_id == id)
                .ToListAsync();

            var fieldIds = collectionFields.Select(f => f.field_id).ToList();
            var fieldOptions = await _context.FieldOptions
                .Where(fo => fieldIds.Contains(fo.field_id))
                .ToListAsync();

            _context.FieldOptions.RemoveRange(fieldOptions);
            _context.CollectionFields.RemoveRange(collectionFields);

            // 7. Удаляем изображения коллекции и связанные файлы
            var collectionImages = await _context.CollectionImages
                .Where(ci => ci.collection_id == id)
                .ToListAsync();

            foreach (var image in collectionImages)
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
            _context.CollectionImages.RemoveRange(collectionImages);

            // 8. Удаляем саму коллекцию
            _context.Collections.Remove(collection);

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        private bool CollectionExists(int id)
        {
            return _context.Collections.Any(e => e.collection_id == id);
        }

        // GET: Collections/AddField
        public async Task<IActionResult> AddField(int collectionId)
        {
            var userId = int.Parse(User.FindFirstValue("UserId"));

            var collection = await _context.Collections
                .FirstOrDefaultAsync(c => c.collection_id == collectionId && c.user_id == userId);

            if (collection == null)
            {
                return NotFound();
            }

            var fieldTypes = await _context.FieldTypes.ToListAsync();
            ViewData["field_type_id"] = new SelectList(fieldTypes, "field_type_id", "name");

            var model = new CollectionFieldViewModel
            {
                CollectionId = collectionId
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddField(CollectionFieldViewModel model)
        {
            var userId = int.Parse(User.FindFirstValue("UserId"));

            var collection = await _context.Collections
                .FirstOrDefaultAsync(c => c.collection_id == model.CollectionId && c.user_id == userId);

            if (collection == null)
            {
                return NotFound();
            }

            var fieldType = await _context.FieldTypes.FirstOrDefaultAsync(ft => ft.field_type_id == model.FieldTypeId);
            if (fieldType?.name == "Варианты" && (model.Options == null || !model.Options.Any(x => !string.IsNullOrEmpty(x))))
            {
                ModelState.AddModelError("Options", "Необходимо добавить хотя бы один вариант.");
            }

            if (ModelState.IsValid)
            {
                var field = new CollectionField
                {
                    collection_id = model.CollectionId,
                    name = model.Name,
                    field_type_id = model.FieldTypeId,
                    is_required = model.IsRequired
                };

                _context.Add(field);
                await _context.SaveChangesAsync();

                if (fieldType?.name == "Варианты" && model.Options != null)
                {
                    foreach (var option in model.Options.Where(o => !string.IsNullOrEmpty(o)))
                    {
                        var fieldOption = new FieldOption
                        {
                            field_id = field.field_id,
                            option_text = option
                        };
                        _context.Add(fieldOption);
                    }
                    await _context.SaveChangesAsync();
                }

                return RedirectToAction(nameof(Details), new { id = model.CollectionId });
            }

            var fieldTypes = await _context.FieldTypes.ToListAsync();
            ViewData["field_type_id"] = new SelectList(fieldTypes, "field_type_id", "name", model.FieldTypeId);

            return View(model);
        }

        // GET: Collections/EditField
        public async Task<IActionResult> EditField(int collectionId, int fieldId)
        {
            var userId = int.Parse(User.FindFirstValue("UserId"));

            var collection = await _context.Collections
                .FirstOrDefaultAsync(c => c.collection_id == collectionId && c.user_id == userId);

            if (collection == null)
            {
                return NotFound();
            }

            var field = await _context.CollectionFields
                .Include(f => f.field_type)
                .FirstOrDefaultAsync(f => f.field_id == fieldId && f.collection_id == collectionId);

            if (field == null)
            {
                return NotFound();
            }

            var fieldTypes = await _context.FieldTypes.ToListAsync();
            ViewData["field_type_id"] = new SelectList(fieldTypes, "field_type_id", "name", field.field_type_id);

            var options = new List<string>();
            var optionIds = new List<int>();
            if (field.field_type.name == "Варианты")
            {
                var fieldOptions = await _context.FieldOptions
                    .Where(o => o.field_id == fieldId)
                    .Select(o => new { o.option_id, o.option_text })
                    .ToListAsync();
                options = fieldOptions.Select(o => o.option_text).ToList();
                optionIds = fieldOptions.Select(o => o.option_id).ToList();
            }

            var model = new CollectionFieldViewModel
            {
                FieldId = field.field_id,
                CollectionId = field.collection_id,
                Name = field.name,
                FieldTypeId = field.field_type_id,
                FieldTypeName = field.field_type.name,
                IsRequired = field.is_required,
                Options = options,
                OptionIds = optionIds
            };

            return View(model);
        }

        // POST: Collections/EditField
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditField(CollectionFieldViewModel model)
        {
            var userId = int.Parse(User.FindFirstValue("UserId"));

            var collection = await _context.Collections
                .FirstOrDefaultAsync(c => c.collection_id == model.CollectionId && c.user_id == userId);

            if (collection == null)
            {
                return NotFound();
            }

            var field = await _context.CollectionFields
                .Include(f => f.field_type)
                .FirstOrDefaultAsync(f => f.field_id == model.FieldId && f.collection_id == model.CollectionId);

            if (field == null)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                field.name = model.Name;
                field.is_required = model.IsRequired;

                if (field.field_type_id != model.FieldTypeId)
                {
                    switch (field.field_type.name)
                    {
                        case "Текст":
                            var textValues = await _context.ItemValueTexts
                                .Where(v => v.field_id == field.field_id)
                                .ToListAsync();
                            _context.ItemValueTexts.RemoveRange(textValues);
                            break;

                        case "Число":
                            var numberValues = await _context.ItemValueNumbers
                                .Where(v => v.field_id == field.field_id)
                                .ToListAsync();
                            _context.ItemValueNumbers.RemoveRange(numberValues);
                            break;

                        case "Дата":
                            var dateValues = await _context.ItemValueDates
                                .Where(v => v.field_id == field.field_id)
                                .ToListAsync();
                            _context.ItemValueDates.RemoveRange(dateValues);
                            break;

                        case "Да-нет":
                            var boolValues = await _context.ItemValueBools
                                .Where(v => v.field_id == field.field_id)
                                .ToListAsync();
                            _context.ItemValueBools.RemoveRange(boolValues);
                            break;

                        case "Варианты":
                            var optionValues = await _context.ItemValueOptions
                                .Where(v => v.field_id == field.field_id)
                                .ToListAsync();
                            _context.ItemValueOptions.RemoveRange(optionValues);

                            var options = await _context.FieldOptions
                                .Where(o => o.field_id == field.field_id)
                                .ToListAsync();
                            _context.FieldOptions.RemoveRange(options);
                            break;

                        case "Фото":
                            var imageValues = await _context.ItemValueImages
                                .Where(v => v.field_id == field.field_id)
                                .ToListAsync();
                            _context.ItemValueImages.RemoveRange(imageValues);
                            break;
                    }

                    field.field_type_id = model.FieldTypeId;
                }

                _context.Update(field);
                await _context.SaveChangesAsync();

                var newFieldType = await _context.FieldTypes.FirstOrDefaultAsync(ft => ft.field_type_id == model.FieldTypeId);
                if (newFieldType != null && newFieldType.name == "Варианты" && model.Options != null)
                {
                    var existingOptions = await _context.FieldOptions
                        .Where(o => o.field_id == field.field_id)
                        .ToDictionaryAsync(o => o.option_id, o => o.option_text);

                    var optionsToDelete = model.DeleteOptions?.Where(d => !string.IsNullOrEmpty(d)).ToList() ?? new List<string>();
                    var optionsToKeep = new List<int>();

                    for (int i = 0; i < model.OptionIds.Count && i < model.Options.Count; i++)
                    {
                        int optionId = model.OptionIds[i];
                        string optionText = model.Options[i] ?? string.Empty;

                        if (!string.IsNullOrEmpty(optionText))
                        {
                            if (optionsToDelete.Contains(optionText) && existingOptions.ContainsKey(optionId))
                            {
                                var optionToDelete = await _context.FieldOptions.FindAsync(optionId);
                                if (optionToDelete != null)
                                {
                                    _context.FieldOptions.Remove(optionToDelete);
                                }
                            }
                            else if (existingOptions.ContainsKey(optionId))
                            {
                                var option = await _context.FieldOptions.FindAsync(optionId);
                                if (option != null && option.option_text != optionText)
                                {
                                    option.option_text = optionText;
                                    _context.Update(option);
                                }
                                optionsToKeep.Add(optionId);
                            }
                            else if (optionId == 0)
                            {
                                var newOption = new FieldOption
                                {
                                    field_id = field.field_id,
                                    option_text = optionText
                                };
                                _context.Add(newOption);
                            }
                        }
                    }

                    var optionsToRemove = await _context.FieldOptions
                        .Where(o => o.field_id == field.field_id && !optionsToKeep.Contains(o.option_id))
                        .ToListAsync();
                    _context.FieldOptions.RemoveRange(optionsToRemove);

                    await _context.SaveChangesAsync();
                }

                return RedirectToAction(nameof(Details), new { id = model.CollectionId });
            }

            var fieldTypes = await _context.FieldTypes.ToListAsync();
            ViewData["field_type_id"] = new SelectList(fieldTypes, "field_type_id", "name", model.FieldTypeId);

            return View(model);
        }

        // GET: Collections/DeleteField
        public async Task<IActionResult> DeleteField(int collectionId, int fieldId)
        {
            var userId = int.Parse(User.FindFirstValue("UserId"));

            var collection = await _context.Collections
                .FirstOrDefaultAsync(c => c.collection_id == collectionId && c.user_id == userId);

            if (collection == null)
            {
                return NotFound();
            }

            var field = await _context.CollectionFields
                .Include(f => f.field_type)
                .FirstOrDefaultAsync(f => f.field_id == fieldId && f.collection_id == collectionId);

            if (field == null)
            {
                return NotFound();
            }

            var model = new CollectionFieldViewModel
            {
                FieldId = field.field_id,
                CollectionId = field.collection_id,
                Name = field.name,
                FieldTypeId = field.field_type_id,
                FieldTypeName = field.field_type.name
            };

            return View(model);
        }

        // POST: Collections/DeleteFieldConfirmed
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteFieldConfirmed(int fieldId, int collectionId)
        {
            var userId = int.Parse(User.FindFirstValue("UserId"));

            var collection = await _context.Collections
                .FirstOrDefaultAsync(c => c.collection_id == collectionId && c.user_id == userId);

            if (collection == null)
            {
                return NotFound();
            }

            var field = await _context.CollectionFields
                .Include(f => f.field_type)
                .FirstOrDefaultAsync(f => f.field_id == fieldId && f.collection_id == collectionId);

            if (field == null)
            {
                return NotFound();
            }

            if (field.field_type.name == "Текст")
            {
                var textValues = await _context.ItemValueTexts
                    .Where(ivt => ivt.field_id == fieldId)
                    .ToListAsync();
                _context.ItemValueTexts.RemoveRange(textValues);
            }
            else if (field.field_type.name == "Число")
            {
                var numberValues = await _context.ItemValueNumbers
                    .Where(ivn => ivn.field_id == fieldId)
                    .ToListAsync();
                _context.ItemValueNumbers.RemoveRange(numberValues);
            }
            else if (field.field_type.name == "Дата")
            {
                var dateValues = await _context.ItemValueDates
                    .Where(ivd => ivd.field_id == fieldId)
                    .ToListAsync();
                _context.ItemValueDates.RemoveRange(dateValues);
            }
            else if (field.field_type.name == "Да-нет")
            {
                var boolValues = await _context.ItemValueBools
                    .Where(ivb => ivb.field_id == fieldId)
                    .ToListAsync();
                _context.ItemValueBools.RemoveRange(boolValues);
            }
            else if (field.field_type.name == "Варианты")
            {
                var optionValues = await _context.ItemValueOptions
                    .Where(ivo => ivo.field_id == fieldId)
                    .ToListAsync();
                _context.ItemValueOptions.RemoveRange(optionValues);

                var fieldOptions = await _context.FieldOptions
                    .Where(fo => fo.field_id == fieldId)
                    .ToListAsync();
                _context.FieldOptions.RemoveRange(fieldOptions);
            }
            else if (field.field_type.name == "Фото")
            {
                var imageValues = await _context.ItemValueImages
                    .Where(ivi => ivi.field_id == fieldId)
                    .ToListAsync();
                _context.ItemValueImages.RemoveRange(imageValues);
            }

            _context.CollectionFields.Remove(field);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id = collectionId });
        }

        // GET: Collections/AddOption
        public async Task<IActionResult> AddOption(int fieldId, int collectionId)
        {
            var userId = int.Parse(User.FindFirstValue("UserId"));

            var collection = await _context.Collections
                .FirstOrDefaultAsync(c => c.collection_id == collectionId && c.user_id == userId);

            if (collection == null)
            {
                return NotFound();
            }

            var field = await _context.CollectionFields
                .Include(f => f.field_type)
                .FirstOrDefaultAsync(f => f.field_id == fieldId && f.collection_id == collectionId);

            if (field == null || field.field_type.name != "Варианты")
            {
                return NotFound();
            }

            var model = new FieldOptionViewModel
            {
                FieldId = fieldId,
                CollectionId = collectionId,
                FieldName = field.name
            };

            return View(model);
        }

        // POST: Collections/AddOption
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddOption(FieldOptionViewModel model)
        {
            var userId = int.Parse(User.FindFirstValue("UserId"));

            var collection = await _context.Collections
                .FirstOrDefaultAsync(c => c.collection_id == model.CollectionId && c.user_id == userId);

            if (collection == null)
            {
                return NotFound();
            }

            var field = await _context.CollectionFields
                .Include(f => f.field_type)
                .FirstOrDefaultAsync(f => f.field_id == model.FieldId && f.collection_id == model.CollectionId);

            if (field == null || field.field_type.name != "Варианты")
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                var option = new FieldOption
                {
                    field_id = model.FieldId,
                    option_text = model.OptionText
                };

                _context.Add(option);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(EditField), new { collectionId = model.CollectionId, fieldId = model.FieldId });
            }

            return View(model);
        }
    }
}