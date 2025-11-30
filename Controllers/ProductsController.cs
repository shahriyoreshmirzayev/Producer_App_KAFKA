using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MVCandKAFKA3.Data;
using MVCandKAFKA3.Models;
using MVCandKAFKA3.Services;

namespace MVCandKAFKA3.Controllers
{
    public class ProductsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly KafkaProducerService _kafkaService;
        private readonly ExcelService _excelService;
        private readonly ILogger<ProductsController> _logger;

        public ProductsController(
            ApplicationDbContext context,
            KafkaProducerService kafkaService,
            ExcelService excelService,
            ILogger<ProductsController> logger)
        {
            _context = context;
            _kafkaService = kafkaService;
            _excelService = excelService;
            _logger = logger;
        }

        // GET: Products - Pagination, Search, Sort
        public async Task<IActionResult> Index(
            string searchString,
            string sortOrder,
            string currentFilter,
            int? pageNumber)
        {
            ViewData["CurrentSort"] = sortOrder;
            ViewData["NameSortParm"] = string.IsNullOrEmpty(sortOrder) ? "name_desc" : "";
            ViewData["CategorySortParm"] = sortOrder == "category" ? "category_desc" : "category";
            ViewData["PriceSortParm"] = sortOrder == "price" ? "price_desc" : "price";
            ViewData["DateSortParm"] = sortOrder == "date" ? "date_desc" : "date";

            if (searchString != null)
            {
                pageNumber = 1;
            }
            else
            {
                searchString = currentFilter;
            }

            ViewData["CurrentFilter"] = searchString;

            var products = from p in _context.Products select p;

            // Search
            if (!string.IsNullOrEmpty(searchString))
            {
                products = products.Where(p =>
                    p.Name.Contains(searchString) ||
                    p.Category.Contains(searchString) ||
                    p.Description.Contains(searchString) ||
                    p.Manufacturer.Contains(searchString));
            }

            // Sort
            products = sortOrder switch
            {
                "name_desc" => products.OrderByDescending(p => p.Name),
                "category" => products.OrderBy(p => p.Category),
                "category_desc" => products.OrderByDescending(p => p.Category),
                "price" => products.OrderBy(p => p.Price),
                "price_desc" => products.OrderByDescending(p => p.Price),
                "date" => products.OrderBy(p => p.CreatedDate),
                "date_desc" => products.OrderByDescending(p => p.CreatedDate),
                _ => products.OrderBy(p => p.Name),
            };

            int pageSize = 10;
            var count = await products.CountAsync();
            var items = await products
                .Skip(((pageNumber ?? 1) - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var paginatedList = new PaginatedList<Product>(
                items,
                count,
                pageNumber ?? 1,
                pageSize);

            return View(paginatedList);
        }

        // GET: Products/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Products/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product product)
        {
            if (ModelState.IsValid)
            {
                product.CreatedDate = DateTime.UtcNow;
                _context.Add(product);
                await _context.SaveChangesAsync();
                TempData["Success"] = " Mahsulot muvaffaqiyatli qo'shildi!";
                return RedirectToAction(nameof(Index));
            }
            return View(product);
        }

        // GET: Products/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();
            return View(product);
        }

        // POST: Products/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Product product)
        {
            if (id != product.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    product.UpdatedDate = DateTime.UtcNow;
                    _context.Update(product);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = " Mahsulot muvaffaqiyatli yangilandi!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProductExists(product.Id)) return NotFound();
                    throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(product);
        }

        // GET: Products/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();
            return View(product);
        }

        // POST: Products/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product != null)
            {
                _context.Products.Remove(product);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Mahsulot o'chirildi!";
            }
            return RedirectToAction(nameof(Index));
        }

        // GET: Excel Template yuklab olish
        public IActionResult DownloadTemplate()
        {
            var template = _excelService.CreateTemplate();
            return File(template, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"ProductsTemplate_{DateTime.Now:yyyyMMddHHmmss}.xlsx");
        }

        // GET: Export to Excel
        public async Task<IActionResult> ExportToExcel()
        {
            var products = await _context.Products.ToListAsync();
            var excelFile = _excelService.ExportToExcel(products);
            return File(excelFile, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"Products_{DateTime.Now:yyyyMMddHHmmss}.xlsx");
        }

        // GET: Import
        public IActionResult Import()
        {
            return View();
        }

        // POST: Import
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Import(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                TempData["Error"] = "Iltimos, fayl tanlang!";
                return View();
            }

            if (!file.FileName.EndsWith(".xlsx") && !file.FileName.EndsWith(".xls"))
            {
                TempData["Error"] = "Faqat Excel fayllari qabul qilinadi!";
                return View();
            }

            try
            {
                using var stream = file.OpenReadStream();
                var products = _excelService.ImportFromExcel(stream);

                if (products.Count == 0)
                {
                    TempData["Error"] = "Excel faylda ma'lumot topilmadi!";
                    return View();
                }

                foreach (var product in products)
                {
                    _context.Products.Add(product);
                }

                await _context.SaveChangesAsync();
                TempData["Success"] = $"{products.Count} ta mahsulot yuklandi!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Excel import xatolik");
                TempData["Error"] = $"Xatolik: {ex.Message}";
                return View();
            }
        }

        // POST: Send Multiple to Kafka
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendMultipleToKafka(List<int> selectedIds, string action)
        {
            List<Product> products;

            if (action == "all")
            {
                products = await _context.Products.Where(p => !p.IsSentToKafka).ToListAsync();
            }
            else if (action == "selected")
            {
                if (selectedIds == null || !selectedIds.Any())
                {
                    TempData["Error"] = "Hech qanday mahsulot tanlanmagan!";
                    return RedirectToAction(nameof(Index));
                }
                products = await _context.Products.Where(p => selectedIds.Contains(p.Id)).ToListAsync();
            }
            else
            {
                TempData["Error"] = "Noto'g'ri so'rov!";
                return RedirectToAction(nameof(Index));
            }

            if (!products.Any())
            {
                TempData["Error"] = "Yuborish uchun mahsulotlar yo'q!";
                return RedirectToAction(nameof(Index));
            }

            int successCount = 0;
            foreach (var product in products)
            {
                try
                {
                    var result = await _kafkaService.SendMessageAsync(product);
                    if (result)
                    {
                        product.IsSentToKafka = true;
                        product.SentToKafkaDate = DateTime.UtcNow;
                        product.KafkaStatus = "Pending";
                        successCount++;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Kafka xatolik: {ProductName}", product.Name);
                }
                await Task.Delay(50);
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = $"{successCount} ta mahsulot Kafka'ga yuborildi!";
            return RedirectToAction(nameof(Index));
        }

        private bool ProductExists(int id)
        {
            return _context.Products.Any(e => e.Id == id);
        }
    }
}