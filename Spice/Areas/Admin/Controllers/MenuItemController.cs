using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Spice.Data;
using Spice.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Spice.Models;
using Spice.Utility;
using Microsoft.AspNetCore.Authorization;

namespace Spice.Areas.Admin.Controllers
{
    [Authorize(Roles = SD.ManagerUser)]
    [Area("Admin")]
    public class MenuItemController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IWebHostEnvironment _hostingEnvironment;
        [BindProperty]
        public MenuItemViewModel MenuItemVM { get; set; }
        public MenuItemController(ApplicationDbContext db, IWebHostEnvironment hostingEnvironment)
        {
            _db = db;
            _hostingEnvironment = hostingEnvironment;
            MenuItemVM = new MenuItemViewModel()
            {
                Category = _db.Category,
                MenuItem = new Models.MenuItem()
            };
        }
        public async Task<IActionResult> Index()
        {
            return View(await _db.MenuItem.Include(m => m.Category).Include(m => m.SubCategory).ToListAsync());
        }

        // GET - CREATE
        public IActionResult Create()
        {
            return View(MenuItemVM);
        }
        // POST - CREATE
        [HttpPost, ValidateAntiForgeryToken, ActionName("Create")]
        public async Task<IActionResult> CreatePOST()
        {
            MenuItemVM.MenuItem.SubCategoryId = Convert.ToInt32(Request.Form["SubCategoryId"].ToString());

            if (!ModelState.IsValid)
            {
                return View(MenuItemVM);
            }

            _db.MenuItem.Add(MenuItemVM.MenuItem);
            await _db.SaveChangesAsync();

            // Working on the image saving section
            string webRootPath = _hostingEnvironment.WebRootPath;
            var files = HttpContext.Request.Form.Files;

            var dbMenuItem = await _db.MenuItem.FindAsync(MenuItemVM.MenuItem.Id);

            if (files.Count > 0)
            {
                // file has been uploaded
                // wwwRoot/images
                var uploads = Path.Combine(webRootPath, "images");
                // Get file extension (.jpg, .png, ...)
                var extension = Path.GetExtension(files[0].FileName);
                string imagePath = uploads + '\\' + dbMenuItem.Id + extension;
                // Create image
                using (var fileStream = new FileStream(imagePath, FileMode.Create))
                {
                    files[0].CopyTo(fileStream);
                }
                dbMenuItem.Image = @"\images\" + dbMenuItem.Id + extension;
            }
            else
            {
                // no file was uploaded, so use default
                var uploads = webRootPath + @"\images\" + SD.DefaultFoodImage;
                System.IO.File.Copy(uploads, webRootPath + @"\images\" + dbMenuItem.Id + ".png");
                dbMenuItem.Image = @"\images\" + dbMenuItem.Id + ".png";
            }

            await _db.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // GET - EDIT
        public async Task<IActionResult> Edit(int? id)
        {
            if(id == null)
            {
                return NotFound();
            }
            MenuItemVM.MenuItem = await _db.MenuItem.Include(m => m.Category).Include(m => m.SubCategory).SingleOrDefaultAsync(m => m.Id == id);
            MenuItemVM.SubCategory = await _db.SubCategory.Where(s => s.CategoryId == MenuItemVM.MenuItem.CategoryId).ToListAsync();

            if(MenuItemVM.MenuItem == null)
            {
                return NotFound();
            }
            return View(MenuItemVM);
        }
        // POST - EDIT
        [HttpPost, ValidateAntiForgeryToken, ActionName("Edit")]
        public async Task<IActionResult> EditPOST()
        {
            if(MenuItemVM.MenuItem.Id == 0)
            {
                return NotFound();
            }
            MenuItemVM.MenuItem.SubCategoryId = Convert.ToInt32(Request.Form["SubCategoryId"].ToString());

            if (!ModelState.IsValid)
            {
                MenuItemVM.SubCategory = await _db.SubCategory.Where(s => s.CategoryId == MenuItemVM.MenuItem.CategoryId).ToListAsync();
                return View(MenuItemVM);
            }

            // Working on the image saving section
            string webRootPath = _hostingEnvironment.WebRootPath;
            var files = HttpContext.Request.Form.Files;

            var dbMenuItem = await _db.MenuItem.FindAsync(MenuItemVM.MenuItem.Id);

            if (files.Count > 0)
            {
                // new image has been uploaded
                // wwwRoot/images
                var uploads = Path.Combine(webRootPath, "images");
                // Get file extension (.jpg, .png, ...)
                var extension_new = Path.GetExtension(files[0].FileName);

                // Delete original image
                var imagePath = webRootPath + dbMenuItem.Image;

                if (System.IO.File.Exists(imagePath))
                {
                    System.IO.File.Delete(imagePath);
                }

                imagePath = uploads + '\\' + dbMenuItem.Id + extension_new;

                // Upload new image
                using (var fileStream = new FileStream(imagePath, FileMode.Create))
                {
                    files[0].CopyTo(fileStream);
                }
                dbMenuItem.Image = @"\images\" + dbMenuItem.Id + extension_new;
            }

            dbMenuItem.Name = MenuItemVM.MenuItem.Name;
            dbMenuItem.Description = MenuItemVM.MenuItem.Description;
            dbMenuItem.Price = MenuItemVM.MenuItem.Price;
            dbMenuItem.Spicyness = MenuItemVM.MenuItem.Spicyness;
            dbMenuItem.CategoryId = MenuItemVM.MenuItem.CategoryId;
            dbMenuItem.SubCategoryId = MenuItemVM.MenuItem.SubCategoryId;

            await _db.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // GET - DETAILS
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            MenuItemVM.MenuItem = await _db.MenuItem.Include(m => m.Category).Include(m => m.SubCategory).SingleOrDefaultAsync(m => m.Id == id);

            if (MenuItemVM.MenuItem == null)
            {
                return NotFound();
            }
            return View(MenuItemVM);
        }

        // GET - DELETE
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            MenuItemVM.MenuItem = await _db.MenuItem.Include(m => m.Category).Include(m => m.SubCategory).SingleOrDefaultAsync(m => m.Id == id);

            if (MenuItemVM.MenuItem == null)
            {
                return NotFound();
            }
            return View(MenuItemVM);
        }

        // POST - DELETE
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete()
        {
            string webRootPath = _hostingEnvironment.WebRootPath;
            MenuItem menuItem = await _db.MenuItem.FindAsync(MenuItemVM.MenuItem.Id);

            if(menuItem != null)
            {
                var imagePath = webRootPath + menuItem.Image;

                if (System.IO.File.Exists(imagePath))
                {
                    System.IO.File.Delete(imagePath);
                }

                _db.MenuItem.Remove(menuItem);
                await _db.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
