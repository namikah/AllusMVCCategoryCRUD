using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AllusMVC.Areas.AdminPanel.Data;
using AllusMVC.DataAccessLayer;
using AllusMVC.Models;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;

namespace AllusMVC.Areas.AdminPanel.Controllers
{
    [Area("AdminPanel")]
    public class CategoryController : Controller
    {
        private readonly AppDbContext _dbContext;

        public CategoryController(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<IActionResult> Index()
        {
            var categories = await _dbContext.Categories.Where(x => x.IsDeleted == false)
                .ToListAsync();

            return View(categories);
        }

        public async Task<IActionResult> Create()
        {
            var parentCategories = await _dbContext.Categories
                .Where(x => x.IsDeleted == false && x.IsMain).ToListAsync();
            ViewBag.ParentCategories = parentCategories;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Category category, int parentCategoryId)
        {
            var parentCategories = await _dbContext.Categories
                .Where(x => x.IsDeleted == false && x.IsMain).ToListAsync();
            ViewBag.ParentCategories = parentCategories;

            if (!ModelState.IsValid)
                return View();

            if (category.IsMain)
            {
                if (category.Photo == null)
                {
                    ModelState.AddModelError("", "Shekil sechin.");
                    return View();
                }

                if (!category.Photo.IsImage())
                {
                    ModelState.AddModelError("", "Duzgun shekil formati sechin.");
                    return View();
                }

                if (!category.Photo.IsAllowedSize(1))
                {
                    ModelState.AddModelError("", "1Mb-dan artiq ola bilmez.");
                    return View();
                }

                var fileName = await category.Photo.GenerateFile(Constants.ImageFolderPath);

                category.Image = fileName;
            }
            else
            {
                if (parentCategoryId == 0)
                {
                    ModelState.AddModelError("", "Parent kateqoriyasi sechin.");
                    return View();
                }

                var existParentCategory = await _dbContext.Categories
                    .Include(x => x.Children.Where(y => y.IsDeleted == false))
                    .FirstOrDefaultAsync(x => x.IsDeleted == false && x.Id == parentCategoryId);
                if (existParentCategory == null)
                    return NotFound();

                var existChildCategory = existParentCategory.Children
                    .Any(x => x.Name.ToLower() == category.Name.ToLower());
                if (existChildCategory)
                {
                    ModelState.AddModelError("", "Bu adda kateqoriya var.");
                    return View();
                }

                category.Parent = existParentCategory;
            }

            category.IsDeleted = false;
            await _dbContext.Categories.AddAsync(category);
            await _dbContext.SaveChangesAsync();

            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var category = await _dbContext.Categories
                .Where(x => x.Id == id && x.IsDeleted == false)
                .Include(x => x.Parent)
                .Include(x => x.Children.Where(y => y.IsDeleted == false))
                .FirstOrDefaultAsync();
            if (category == null)
                return NotFound();

            return View(category);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionName("Delete")]
        public async Task<IActionResult> DeleteCategory(int? id)
        {
            if (id == null)
                return NotFound();

            var category = await _dbContext.Categories
                .Where(x => x.Id == id && x.IsDeleted == false)
                .Include(x => x.Children.Where(y => y.IsDeleted == false))
                .FirstOrDefaultAsync();
            if (category == null)
                return NotFound();

            category.IsDeleted = true;
            if (category.IsMain)
            {
                foreach (var item in category.Children)
                {
                    item.IsDeleted = true;
                }
            }

            await _dbContext.SaveChangesAsync();

            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Update(int? id)
        {
            if (id == null)
                return NotFound();

            var category = await _dbContext.Categories
                .Where(x => x.Id == id && x.IsDeleted == false)
                .Include(x => x.Parent)
                .Include(x => x.Children.Where(y => y.IsDeleted == false))
                .FirstOrDefaultAsync();
            if (category == null)
                return NotFound();

            var parentCategories = await _dbContext.Categories
                .Where(x => x.IsDeleted == false && x.IsMain).ToListAsync();
            ViewBag.ParentCategories = parentCategories;

            return View(category);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(int? id, Category category, int parentId)
        {
            if (id == null || category == null)
                return NotFound();

            var parentCategories = await _dbContext.Categories
               .Where(x => !x.IsDeleted && x.IsMain).ToListAsync();
            ViewBag.ParentCategories = parentCategories;

            if (!ModelState.IsValid)
            {
                return View(category);
            }

            var existCategory = await _dbContext.Categories.Include(x=>x.Parent).FirstOrDefaultAsync(x => x.Id == id && x.IsDeleted == false);
            if (existCategory == null)
                return NotFound();

            if (existCategory.IsMain)
            {
                #region Upload Image, Validation
                if (category.Photo != null)
                {
                    var isImageType = category.Photo.ContentType.Contains("image");
                    if (!isImageType)
                    {
                        ModelState.AddModelError("Photo", "uploaded file must be an image");
                        return View();
                    }

                    var isImageSize = category.Photo.Length;
                    if (isImageSize > 1024 * 1000)
                    {
                        ModelState.AddModelError("Photo", "uploaded file must be max 1MB");
                        return View();
                    }

                    var fileName = await category.Photo.GenerateFile(Constants.ImageFolderPath);

                    existCategory.Image = fileName;
                }
                #endregion
                

                existCategory.Name = category.Name;
            }
            else
            {
                var isParentCategory = parentCategories.Any(x => x.Name.ToLower() == category.Name.ToLower());
                if (isParentCategory)
                {
                    ModelState.AddModelError("", "This name is already use for parent category");
                    return View(category);
                }

                var existParentCategory = await _dbContext.Categories
                   .Include(x => x.Children.Where(y => y.IsDeleted == false))
                   .FirstOrDefaultAsync(x => x.IsDeleted == false && x.Id == parentId);
                if (existParentCategory == null)
                    return NotFound();

                existCategory.Name = category.Name;
                existCategory.Parent = existParentCategory;
            }

            await _dbContext.SaveChangesAsync();

            return RedirectToAction("Index");
        }
    }
}
