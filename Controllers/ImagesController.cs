using CollectorHub.Models;
using CollectorHub.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CollectorHub.Controllers
{
    [Authorize]
    public class ImagesController : Controller
    {
        private readonly DBContext _context;
        private readonly IWebHostEnvironment _environment;

        public ImagesController(DBContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        // GET: Images/AddItemImage/5?fieldId=10 (5 - ID предмета, 10 - ID поля)
        public async Task<IActionResult> AddItemImage(int? id, int? fieldId)
        {
            if (id == null || fieldId == null)
            {
                return NotFound();
            }

            var userId = int.Parse(User.FindFirstValue("UserId"));

            // Получаем предмет и проверяем, принадлежит ли он коллекции текущего пользователя
            var item = await _context.Items
                .Include(i => i.collection)
                .FirstOrDefaultAsync(i => i.item_id == id && i.collection.user_id == userId);

            if (item == null)
            {
                return NotFound();
            }

            // Проверяем, существует ли поле и имеет ли оно тип "image"
            var field = await _context.CollectionFields
                .Include(f => f.field_type)
                .FirstOrDefaultAsync(f => f.field_id == fieldId && f.collection_id == item.collection_id && f.field_type.name == "image");

            if (field == null)
            {
                return NotFound();
            }

            // Получаем существующие изображения для этого поля
            var images = await _context.ItemValueImages
                .Where(i => i.item_id == id && i.field_id == fieldId)
                .OrderBy(i => i.sort_order)
                .ToListAsync();

            ViewData["ItemId"] = id;
            ViewData["FieldId"] = fieldId;
            ViewData["ItemName"] = item.name;
            ViewData["FieldName"] = field.name;
            ViewData["CollectionId"] = item.collection_id;

            return View(images);
        }

        // GET: Images/MoveItemImageUp/5
        public async Task<IActionResult> MoveItemImageUp(int id)
        {
            var userId = int.Parse(User.FindFirstValue("UserId"));

            // Получаем изображение
            var image = await _context.ItemValueImages
                .Include(i => i.item)
                    .ThenInclude(i => i.collection)
                .FirstOrDefaultAsync(i => i.id == id && i.item.collection.user_id == userId);

            if (image == null)
            {
                return NotFound();
            }

            // Получаем предыдущее изображение (с меньшим порядком сортировки)
            var previousImage = await _context.ItemValueImages
                .Where(i => i.item_id == image.item_id && i.field_id == image.field_id && i.sort_order < image.sort_order)
                .OrderByDescending(i => i.sort_order)
                .FirstOrDefaultAsync();

            if (previousImage != null)
            {
                // Меняем местами порядок сортировки
                var tempOrder = image.sort_order;
                image.sort_order = previousImage.sort_order;
                previousImage.sort_order = tempOrder;

                _context.Update(image);
                _context.Update(previousImage);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(AddItemImage), new { id = image.item_id, fieldId = image.field_id });
        }

        // GET: Images/MoveItemImageDown/5
        public async Task<IActionResult> MoveItemImageDown(int id)
        {
            var userId = int.Parse(User.FindFirstValue("UserId"));

            // Получаем изображение
            var image = await _context.ItemValueImages
                .Include(i => i.item)
                    .ThenInclude(i => i.collection)
                .FirstOrDefaultAsync(i => i.id == id && i.item.collection.user_id == userId);

            if (image == null)
            {
                return NotFound();
            }

            // Получаем следующее изображение (с большим порядком сортировки)
            var nextImage = await _context.ItemValueImages
                .Where(i => i.item_id == image.item_id && i.field_id == image.field_id && i.sort_order > image.sort_order)
                .OrderBy(i => i.sort_order)
                .FirstOrDefaultAsync();

            if (nextImage != null)
            {
                // Меняем местами порядок сортировки
                var tempOrder = image.sort_order;
                image.sort_order = nextImage.sort_order;
                nextImage.sort_order = tempOrder;

                _context.Update(image);
                _context.Update(nextImage);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(AddItemImage), new { id = image.item_id, fieldId = image.field_id });
        }

        // POST: Images/UploadItemImage
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadItemImage(int itemId, int fieldId, IFormFile image)
        {
            if (image == null || image.Length == 0)
            {
                TempData["Error"] = "Пожалуйста, выберите файл для загрузки.";
                return RedirectToAction(nameof(AddItemImage), new { id = itemId, fieldId = fieldId });
            }

            var userId = int.Parse(User.FindFirstValue("UserId"));

            // Получаем предмет и проверяем, принадлежит ли он коллекции текущего пользователя
            var item = await _context.Items
                .Include(i => i.collection)
                .FirstOrDefaultAsync(i => i.item_id == itemId && i.collection.user_id == userId);

            if (item == null)
            {
                return NotFound();
            }

            // Проверяем, существует ли поле и имеет ли оно тип "image"
            var field = await _context.CollectionFields
                .Include(f => f.field_type)
                .FirstOrDefaultAsync(f => f.field_id == fieldId && f.collection_id == item.collection_id && f.field_type.name == "image");

            if (field == null)
            {
                return NotFound();
            }

            // Проверяем тип файла
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
            var extension = Path.GetExtension(image.FileName).ToLowerInvariant();

            if (!allowedExtensions.Contains(extension))
            {
                TempData["Error"] = "Разрешены только файлы изображений (jpg, jpeg, png, gif).";
                return RedirectToAction(nameof(AddItemImage), new { id = itemId, fieldId = fieldId });
            }

            try
            {
                // Создаем директорию для хранения изображений, если она не существует
                var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "items", itemId.ToString(), fieldId.ToString());
                Directory.CreateDirectory(uploadsFolder);

                // Генерируем уникальное имя файла
                var uniqueFileName = Guid.NewGuid().ToString() + extension;
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                // Сохраняем файл
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await image.CopyToAsync(fileStream);
                }

                // Получаем максимальный порядок сортировки
                var maxSortOrder = await _context.ItemValueImages
                    .Where(i => i.item_id == itemId && i.field_id == fieldId)
                    .Select(i => (int?)i.sort_order)
                    .MaxAsync() ?? 0;

                // Создаем запись в базе данных
                var imageValue = new ItemValueImage
                {
                    item_id = itemId,
                    field_id = fieldId,
                    image_url = $"/uploads/items/{itemId}/{fieldId}/{uniqueFileName}",
                    sort_order = maxSortOrder + 1
                };

                _context.Add(imageValue);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Изображение успешно загружено.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Ошибка при загрузке изображения: {ex.Message}";
            }

            return RedirectToAction(nameof(AddItemImage), new { id = itemId, fieldId = fieldId });
        }

        // POST: Images/DeleteItemImage/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteItemImage(int id)
        {
            var userId = int.Parse(User.FindFirstValue("UserId"));

            // Получаем изображение
            var image = await _context.ItemValueImages
                .Include(i => i.item)
                    .ThenInclude(i => i.collection)
                .FirstOrDefaultAsync(i => i.id == id && i.item.collection.user_id == userId);

            if (image == null)
            {
                return NotFound();
            }

            try
            {
                // Удаляем файл
                var filePath = Path.Combine(_environment.WebRootPath, image.image_url.TrimStart('/'));
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }

                // Удаляем запись из базы данных
                _context.ItemValueImages.Remove(image);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Изображение успешно удалено.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Ошибка при удалении изображения: {ex.Message}";
            }

            return RedirectToAction(nameof(AddItemImage), new { id = image.item_id, fieldId = image.field_id });
        }

        // GET: Images/AddCollectionImage/5 (5 - ID коллекции)
        public async Task<IActionResult> AddCollectionImage(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var userId = int.Parse(User.FindFirstValue("UserId"));

            // Проверяем, принадлежит ли коллекция текущему пользователю
            var collection = await _context.Collections
                .FirstOrDefaultAsync(c => c.collection_id == id && c.user_id == userId);

            if (collection == null)
            {
                return NotFound();
            }

            // Получаем существующие изображения для коллекции
            var images = await _context.CollectionImages
                .Where(i => i.collection_id == id)
                .OrderBy(i => i.sort_order)
                .ToListAsync();

            ViewData["CollectionId"] = id;
            ViewData["CollectionName"] = collection.name;

            return View(images);
        }

        // POST: Images/UploadCollectionImage
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadCollectionImage(int collectionId, IFormFile image)
        {
            if (image == null || image.Length == 0)
            {
                TempData["Error"] = "Пожалуйста, выберите файл для загрузки.";
                return RedirectToAction(nameof(AddCollectionImage), new { id = collectionId });
            }

            var userId = int.Parse(User.FindFirstValue("UserId"));

            // Проверяем, принадлежит ли коллекция текущему пользователю
            var collection = await _context.Collections
                .FirstOrDefaultAsync(c => c.collection_id == collectionId && c.user_id == userId);

            if (collection == null)
            {
                return NotFound();
            }

            // Проверяем тип файла
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
            var extension = Path.GetExtension(image.FileName).ToLowerInvariant();

            if (!allowedExtensions.Contains(extension))
            {
                TempData["Error"] = "Разрешены только файлы изображений (jpg, jpeg, png, gif).";
                return RedirectToAction(nameof(AddCollectionImage), new { id = collectionId });
            }

            try
            {
                // Создаем директорию для хранения изображений, если она не существует
                var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "collections", collectionId.ToString());
                Directory.CreateDirectory(uploadsFolder);

                // Генерируем уникальное имя файла
                var uniqueFileName = Guid.NewGuid().ToString() + extension;
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                // Сохраняем файл
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await image.CopyToAsync(fileStream);
                }

                // Получаем максимальный порядок сортировки
                var maxSortOrder = await _context.CollectionImages
                    .Where(i => i.collection_id == collectionId)
                    .Select(i => (int?)i.sort_order)
                    .MaxAsync() ?? 0;

                // Создаем запись в базе данных
                var imageValue = new CollectionImage
                {
                    collection_id = collectionId,
                    image_url = $"/uploads/collections/{collectionId}/{uniqueFileName}",
                    sort_order = maxSortOrder + 1
                };

                _context.Add(imageValue);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Изображение успешно загружено.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Ошибка при загрузке изображения: {ex.Message}";
            }

            return RedirectToAction(nameof(AddCollectionImage), new { id = collectionId });
        }

        // POST: Images/DeleteCollectionImage/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCollectionImage(int id)
        {
            var userId = int.Parse(User.FindFirstValue("UserId"));

            // Получаем изображение
            var image = await _context.CollectionImages
                .Include(i => i.collection)
                .FirstOrDefaultAsync(i => i.id == id && i.collection.user_id == userId);

            if (image == null)
            {
                return NotFound();
            }

            try
            {
                // Удаляем файл
                var filePath = Path.Combine(_environment.WebRootPath, image.image_url.TrimStart('/'));
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }

                // Удаляем запись из базы данных
                _context.CollectionImages.Remove(image);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Изображение успешно удалено.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Ошибка при удалении изображения: {ex.Message}";
            }

            return RedirectToAction(nameof(AddCollectionImage), new { id = image.collection_id });
        }

        // GET: Images/MoveCollectionImageUp/5
        public async Task<IActionResult> MoveCollectionImageUp(int id)
        {
            var userId = int.Parse(User.FindFirstValue("UserId"));

            // Получаем изображение
            var image = await _context.CollectionImages
                .Include(i => i.collection)
                .FirstOrDefaultAsync(i => i.id == id && i.collection.user_id == userId);

            if (image == null)
            {
                return NotFound();
            }

            // Получаем предыдущее изображение (с меньшим порядком сортировки)
            var previousImage = await _context.CollectionImages
                .Where(i => i.collection_id == image.collection_id && i.sort_order < image.sort_order)
                .OrderByDescending(i => i.sort_order)
                .FirstOrDefaultAsync();

            if (previousImage != null)
            {
                // Меняем местами порядок сортировки
                var tempOrder = image.sort_order;
                image.sort_order = previousImage.sort_order;
                previousImage.sort_order = tempOrder;

                _context.Update(image);
                _context.Update(previousImage);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(AddCollectionImage), new { id = image.collection_id });
        }

        // GET: Images/MoveCollectionImageDown/5
        public async Task<IActionResult> MoveCollectionImageDown(int id)
        {
            var userId = int.Parse(User.FindFirstValue("UserId"));

            // Получаем изображение
            var image = await _context.CollectionImages
                .Include(i => i.collection)
                .FirstOrDefaultAsync(i => i.id == id && i.collection.user_id == userId);

            if (image == null)
            {
                return NotFound();
            }

            // Получаем следующее изображение (с большим порядком сортировки)
            var nextImage = await _context.CollectionImages
                .Where(i => i.collection_id == image.collection_id && i.sort_order > image.sort_order)
                .OrderBy(i => i.sort_order)
                .FirstOrDefaultAsync();

            if (nextImage != null)
            {
                // Меняем местами порядок сортировки
                var tempOrder = image.sort_order;
                image.sort_order = nextImage.sort_order;
                nextImage.sort_order = tempOrder;

                _context.Update(image);
                _context.Update(nextImage);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(AddCollectionImage), new { id = image.collection_id });
        }
    }
}