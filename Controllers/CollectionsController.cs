using CollectorHub.Models;
using CollectorHub.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CollectorHub.Controllers
{
    [Authorize] // Требует аутентификации для всех действий
    public class CollectionsController : Controller
    {
        private readonly DBContext _context;

        public CollectionsController(DBContext context)
        {
            _context = context;
        }

        // GET: Collections
        public async Task<IActionResult> Index()
        {
            // Получаем ID текущего пользователя
            var userId = int.Parse(User.FindFirstValue("UserId"));

            // Получаем коллекции текущего пользователя
            var collections = await _context.Collections
                .Include(c => c.visibility)
                .Include(c => c.template)
                .Include(c => c.parent)
                .Where(c => c.user_id == userId)
                .ToListAsync();

            return View(collections);
        }

        // GET: Collections/Details
        public async Task<IActionResult> Details(int? id)
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
                .Include(c => c.Items) // Загружаем предметы
                    .ThenInclude(i => i.status) // Загружаем статусы предметов
                .Include(c => c.CollectionImages) // Загружаем изображения коллекции
                .FirstOrDefaultAsync(m => m.collection_id == id && m.user_id == userId);

            if (collection == null)
            {
                return NotFound();
            }

            // Загружаем дочерние коллекции
            var childCollections = await _context.Collections
                .Where(c => c.parent_id == id && c.user_id == userId)
                .ToListAsync();

            ViewData["ChildCollections"] = childCollections;

            // Загружаем поля коллекции
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

            // Заполняем выпадающие списки
            ViewData["template_id"] = new SelectList(_context.Templates, "template_id", "name");
            ViewData["visibility_id"] = new SelectList(_context.VisibilityTypes, "visibility_id", "name");

            // Получаем список коллекций пользователя для выбора родительской коллекции
            ViewData["parent_id"] = new SelectList(
                await _context.Collections.Where(c => c.user_id == userId).ToListAsync(),
                "collection_id",
                "name"
            );

            // Создаем модель представления
            var viewModel = new CreateCollectionViewModel();

            // Если передан parentId, устанавливаем его в модели
            if (parentId.HasValue)
            {
                // Проверяем, существует ли родительская коллекция и принадлежит ли она текущему пользователю
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
                    // Создаем новый объект Collection из данных ViewModel
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

                    // Если выбран шаблон, копируем поля из шаблона
                    if (collection.template_id.HasValue)
                    {
                        await CopyTemplateFieldsToCollection(collection.template_id.Value, collection.collection_id);
                    }

                    return RedirectToAction(nameof(Details), new { id = collection.collection_id });
                }
                else
                {
                    // Выводим ошибки валидации
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
                // Логируем исключение
                Console.WriteLine($"Exception in Create: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");

                // Добавляем ошибку в ModelState
                ModelState.AddModelError("", $"Произошла ошибка: {ex.Message}");
            }

            // Если модель невалидна или произошла ошибка, заново заполняем выпадающие списки
            ViewData["template_id"] = new SelectList(_context.Templates, "template_id", "name", viewModel.TemplateId);
            ViewData["visibility_id"] = new SelectList(_context.VisibilityTypes, "visibility_id", "name", viewModel.VisibilityId);

            var userId = int.Parse(User.FindFirstValue("UserId"));
            ViewData["parent_id"] = new SelectList(
                await _context.Collections.Where(c => c.user_id == userId).ToListAsync(),
                "collection_id",
                "name",
                viewModel.ParentId
            );

            return View(viewModel);
        }

        // Метод для копирования полей из шаблона в коллекцию
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

            // Создаем модель представления из модели коллекции
            var viewModel = new EditCollectionViewModel
            {
                Name = collection.name,
                Description = collection.description,
                VisibilityId = collection.visibility_id,
                TemplateId = collection.template_id,
                ParentId = collection.parent_id
            };

            ViewData["template_id"] = new SelectList(_context.Templates, "template_id", "name", collection.template_id);
            ViewData["visibility_id"] = new SelectList(_context.VisibilityTypes, "visibility_id", "name", collection.visibility_id);
            ViewData["parent_id"] = new SelectList(
                await _context.Collections.Where(c => c.user_id == userId && c.collection_id != id).ToListAsync(),
                "collection_id",
                "name",
                collection.parent_id
            );

            return View(viewModel);
        }

        // POST: Collections/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, EditCollectionViewModel viewModel)
        {
            // Проверяем, принадлежит ли коллекция текущему пользователю
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
                    // Обновляем только те поля, которые можно изменять
                    collection.name = viewModel.Name;
                    collection.description = viewModel.Description;
                    collection.visibility_id = viewModel.VisibilityId;
                    collection.parent_id = viewModel.ParentId;

                    // Не изменяем template_id, так как это может привести к потере данных
                    // collection.template_id = viewModel.TemplateId;

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

            // Если модель невалидна, заново заполняем выпадающие списки
            ViewData["template_id"] = new SelectList(_context.Templates, "template_id", "name", viewModel.TemplateId);
            ViewData["visibility_id"] = new SelectList(_context.VisibilityTypes, "visibility_id", "name", viewModel.VisibilityId);
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

            // Удаляем связанные поля коллекции
            var collectionFields = await _context.CollectionFields
                .Where(f => f.collection_id == id)
                .ToListAsync();

            _context.CollectionFields.RemoveRange(collectionFields);

            // Удаляем связанные предметы
            var items = await _context.Items
                .Where(i => i.collection_id == id)
                .ToListAsync();

            _context.Items.RemoveRange(items);

            // Удаляем саму коллекцию
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

            // Проверяем, принадлежит ли коллекция текущему пользователю
            var collection = await _context.Collections
                .FirstOrDefaultAsync(c => c.collection_id == collectionId && c.user_id == userId);

            if (collection == null)
            {
                return NotFound();
            }

            // Получаем типы полей
            var fieldTypes = await _context.FieldTypes.ToListAsync();
            ViewData["field_type_id"] = new SelectList(fieldTypes, "field_type_id", "name");

            var model = new CollectionFieldViewModel
            {
                CollectionId = collectionId
            };

            return View(model);
        }

        // POST: Collections/AddField
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddField(CollectionFieldViewModel model)
        {
            var userId = int.Parse(User.FindFirstValue("UserId"));

            // Проверяем, принадлежит ли коллекция текущему пользователю
            var collection = await _context.Collections
                .FirstOrDefaultAsync(c => c.collection_id == model.CollectionId && c.user_id == userId);

            if (collection == null)
            {
                return NotFound();
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

                // Если тип поля - "option", создаем опции
                var fieldType = await _context.FieldTypes.FirstOrDefaultAsync(ft => ft.field_type_id == model.FieldTypeId);
                if (fieldType != null && fieldType.name == "Варианты" && model.Options != null)
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

            // Если модель невалидна, заново заполняем выпадающий список типов полей
            var fieldTypes = await _context.FieldTypes.ToListAsync();
            ViewData["field_type_id"] = new SelectList(fieldTypes, "field_type_id", "name", model.FieldTypeId);

            return View(model);
        }

        // GET: Collections/EditField
        public async Task<IActionResult> EditField(int collectionId, int fieldId)
        {
            var userId = int.Parse(User.FindFirstValue("UserId"));

            // Проверяем, принадлежит ли коллекция текущему пользователю
            var collection = await _context.Collections
                .FirstOrDefaultAsync(c => c.collection_id == collectionId && c.user_id == userId);

            if (collection == null)
            {
                return NotFound();
            }

            // Получаем поле
            var field = await _context.CollectionFields
                .Include(f => f.field_type)
                .FirstOrDefaultAsync(f => f.field_id == fieldId && f.collection_id == collectionId);

            if (field == null)
            {
                return NotFound();
            }

            // Получаем типы полей
            var fieldTypes = await _context.FieldTypes.ToListAsync();
            ViewData["field_type_id"] = new SelectList(fieldTypes, "field_type_id", "name", field.field_type_id);

            // Получаем опции, если тип поля - "option"
            var options = new List<string>();
            if (field.field_type.name == "Варианты")
            {
                options = await _context.FieldOptions
                    .Where(o => o.field_id == fieldId)
                    .Select(o => o.option_text)
                    .ToListAsync();
            }

            var model = new CollectionFieldViewModel
            {
                FieldId = field.field_id,
                CollectionId = field.collection_id,
                Name = field.name,
                FieldTypeId = field.field_type_id,
                FieldTypeName = field.field_type.name,
                IsRequired = field.is_required,
                Options = options
            };

            return View(model);
        }

        // POST: Collections/EditField
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditField(CollectionFieldViewModel model)
        {
            var userId = int.Parse(User.FindFirstValue("UserId"));

            // Проверяем, принадлежит ли коллекция текущему пользователю
            var collection = await _context.Collections
                .FirstOrDefaultAsync(c => c.collection_id == model.CollectionId && c.user_id == userId);

            if (collection == null)
            {
                return NotFound();
            }

            // Получаем поле
            var field = await _context.CollectionFields
                .Include(f => f.field_type)
                .FirstOrDefaultAsync(f => f.field_id == model.FieldId && f.collection_id == model.CollectionId);

            if (field == null)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                // Обновляем поле
                field.name = model.Name;
                field.is_required = model.IsRequired;

                // Если тип поля изменился, удаляем все значения этого поля в предметах
                if (field.field_type_id != model.FieldTypeId)
                {
                    // Удаляем значения в зависимости от текущего типа поля
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

                            // Удаляем опции поля
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

                    // Обновляем тип поля
                    field.field_type_id = model.FieldTypeId;
                }

                _context.Update(field);
                await _context.SaveChangesAsync();

                // Если тип поля - "option", обновляем опции
                var fieldType = await _context.FieldTypes.FirstOrDefaultAsync(ft => ft.field_type_id == model.FieldTypeId);
                if (fieldType != null && fieldType.name == "Варианты" && model.Options != null)
                {
                    // Удаляем существующие опции
                    var existingOptions = await _context.FieldOptions
                        .Where(o => o.field_id == field.field_id)
                        .ToListAsync();
                    _context.FieldOptions.RemoveRange(existingOptions);

                    // Добавляем новые опции
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

            // Если модель невалидна, заново заполняем выпадающий список типов полей
            var fieldTypes = await _context.FieldTypes.ToListAsync();
            ViewData["field_type_id"] = new SelectList(fieldTypes, "field_type_id", "name", model.FieldTypeId);

            return View(model);
        }

        // GET: Collections/DeleteField
        public async Task<IActionResult> DeleteField(int collectionId, int fieldId)
        {
            var userId = int.Parse(User.FindFirstValue("UserId"));

            // Проверяем, принадлежит ли коллекция текущему пользователю
            var collection = await _context.Collections
                .FirstOrDefaultAsync(c => c.collection_id == collectionId && c.user_id == userId);

            if (collection == null)
            {
                return NotFound();
            }

            // Получаем поле
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

            // Проверяем, принадлежит ли коллекция текущему пользователю
            var collection = await _context.Collections
                .FirstOrDefaultAsync(c => c.collection_id == collectionId && c.user_id == userId);

            if (collection == null)
            {
                return NotFound();
            }

            // Получаем поле
            var field = await _context.CollectionFields
                .Include(f => f.field_type)
                .FirstOrDefaultAsync(f => f.field_id == fieldId && f.collection_id == collectionId);

            if (field == null)
            {
                return NotFound();
            }

            // Удаляем значения в зависимости от типа поля
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

                    // Удаляем опции поля
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

            // Удаляем поле
            _context.CollectionFields.Remove(field);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id = collectionId });
        }

        // GET: Collections/AddOption
        public async Task<IActionResult> AddOption(int fieldId, int collectionId)
        {
            var userId = int.Parse(User.FindFirstValue("UserId"));

            // Проверяем, принадлежит ли коллекция текущему пользователю
            var collection = await _context.Collections
                .FirstOrDefaultAsync(c => c.collection_id == collectionId && c.user_id == userId);

            if (collection == null)
            {
                return NotFound();
            }

            // Получаем поле
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

            // Проверяем, принадлежит ли коллекция текущему пользователю
            var collection = await _context.Collections
                .FirstOrDefaultAsync(c => c.collection_id == model.CollectionId && c.user_id == userId);

            if (collection == null)
            {
                return NotFound();
            }

            // Получаем поле
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