using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuanLySanPhamApp.Data;
using QuanLySanPhamApp.Models;
using System.Linq;
using System.Threading.Tasks;

namespace QuanLySanPhamApp.Controllers
{
    [Authorize(Roles = "Admin,Manager")]
    public class CategoryController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CategoryController> _logger;

        public CategoryController(ApplicationDbContext context, ILogger<CategoryController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: Category
        public async Task<IActionResult> Index()
        {
            var categories = await _context.Categories
                .Include(c => c.ParentCategory)
                .OrderBy(c => c.DisplayOrder)
                .ToListAsync();
            
            return View(categories);
        }

        // GET: Category/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var category = await _context.Categories
                .Include(c => c.ParentCategory)
                .Include(c => c.ChildCategories)
                .Include(c => c.Products)
                .FirstOrDefaultAsync(m => m.CategoryId == id);
                
            if (category == null)
            {
                return NotFound();
            }

            return View(category);
        }

        // GET: Category/Create
        public async Task<IActionResult> Create()
        {
            await PopulateParentCategoriesDropDownList();
            return View();
        }

        // POST: Category/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Description,ParentCategoryId,DisplayOrder,IsActive")] Category category)
        {
            if (ModelState.IsValid)
            {
                _context.Add(category);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Category created: {Name}", category.Name);
                return RedirectToAction(nameof(Index));
            }
            
            await PopulateParentCategoriesDropDownList(category.ParentCategoryId);
            return View(category);
        }

        // GET: Category/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var category = await _context.Categories.FindAsync(id);
            if (category == null)
            {
                return NotFound();
            }
            
            await PopulateParentCategoriesDropDownList(category.ParentCategoryId, category.CategoryId);
            return View(category);
        }

        // POST: Category/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("CategoryId,Name,Description,ParentCategoryId,DisplayOrder,IsActive")] Category category)
        {
            if (id != category.CategoryId)
            {
                return NotFound();
            }

            // Prevent circular reference (category cannot be its own parent)
            if (category.CategoryId == category.ParentCategoryId)
            {
                ModelState.AddModelError("ParentCategoryId", "A category cannot be its own parent");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(category);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Category updated: {Name}", category.Name);
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CategoryExists(category.CategoryId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            
            await PopulateParentCategoriesDropDownList(category.ParentCategoryId, category.CategoryId);
            return View(category);
        }

        // GET: Category/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var category = await _context.Categories
                .Include(c => c.ParentCategory)
                .Include(c => c.ChildCategories)
                .Include(c => c.Products)
                .FirstOrDefaultAsync(m => m.CategoryId == id);
                
            if (category == null)
            {
                return NotFound();
            }

            return View(category);
        }

        // POST: Category/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var category = await _context.Categories
                .Include(c => c.ChildCategories)
                .Include(c => c.Products)
                .FirstOrDefaultAsync(m => m.CategoryId == id);
                
            if (category == null)
            {
                return NotFound();
            }

            // Check if category is in use
            if (category.ChildCategories != null && category.ChildCategories.Any())
            {
                ModelState.AddModelError(string.Empty, "Cannot delete category because it has child categories. Please reassign or delete child categories first.");
                return View(category);
            }

            if (category.Products != null && category.Products.Any())
            {
                ModelState.AddModelError(string.Empty, "Cannot delete category because it has associated products. Please reassign or delete products first.");
                return View(category);
            }

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Category deleted: {Name}", category.Name);
            
            return RedirectToAction(nameof(Index));
        }

        // POST: Category/ToggleStatus/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null)
            {
                return NotFound();
            }

            category.IsActive = !category.IsActive;
            _context.Update(category);
            await _context.SaveChangesAsync();
            
            string status = category.IsActive ? "activated" : "deactivated";
            _logger.LogInformation("Category {Name} was {Status}", category.Name, status);
            
            return RedirectToAction(nameof(Index));
        }

        private async Task PopulateParentCategoriesDropDownList(object selectedCategory = null, int? excludeId = null)
        {
            var categoriesQuery = from c in _context.Categories
                                 orderby c.Name
                                 select c;

            if (excludeId.HasValue)
            {
                // Exclude the current category and its children to prevent circular references
                categoriesQuery = categoriesQuery.Where(c => c.CategoryId != excludeId.Value).OrderBy(c => c.Name);
            }

            var categories = await categoriesQuery.ToListAsync();
            ViewBag.ParentCategoryId = new SelectList(categories, "CategoryId", "Name", selectedCategory);
        }

        private bool CategoryExists(int id)
        {
            return _context.Categories.Any(e => e.CategoryId == id);
        }
    }
}
