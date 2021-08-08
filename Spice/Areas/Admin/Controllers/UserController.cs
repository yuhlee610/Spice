using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Spice.Data;
using Spice.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Spice.Areas.Admin.Controllers
{
    [Authorize(Roles = SD.ManagerUser)]
    [Area("Admin")]
    public class UserController : Controller
    {
        private readonly ApplicationDbContext _db;
        public UserController(ApplicationDbContext db)
        {
            _db = db;
        }
        public async Task<IActionResult> Index()
        {
            // Get current login user
            var claimsIdentity = (ClaimsIdentity)this.User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

            return View(await _db.ApplicationUser.Where(u => u.Id != claim.Value).ToListAsync());
        }
        public async Task<IActionResult> Lock(string id)
        {
            if(id == null)
            {
                return NotFound();
            }

            var dbUser = await _db.ApplicationUser.FirstOrDefaultAsync(u => u.Id == id);

            if(dbUser == null)
            {
                return NotFound();
            }
            dbUser.LockoutEnd = DateTime.Now.AddDays(3);
            await _db.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
        public async Task<IActionResult> UnLock(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var dbUser = await _db.ApplicationUser.FirstOrDefaultAsync(u => u.Id == id);

            if (dbUser == null)
            {
                return NotFound();
            }
            dbUser.LockoutEnd = DateTime.Now;
            await _db.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
    }
}
