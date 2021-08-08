using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Spice.Data;
using Spice.Models;
using Spice.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Spice.Areas.Admin.Controllers
{
    [Authorize(Roles = SD.ManagerUser)]
    [Area("Admin")]
    public class CouponController : Controller
    {
        private readonly ApplicationDbContext _db;
        public CouponController(ApplicationDbContext db)
        {
            _db = db;
        }
        public async Task<IActionResult> Index()
        {
            return View(await _db.Coupon.ToListAsync());
        }
        // GET - CREATE
        public IActionResult Create()
        {
            return View();
        }
        // POST - CREATE
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Coupon coupon)
        {
            if (ModelState.IsValid)
            {
                var files = HttpContext.Request.Form.Files;
                if (files.Count > 0)
                {
                    byte[] p1 = null;
                    using (var fs1 = files[0].OpenReadStream())
                    {
                        using (var ms1 = new MemoryStream())
                        {
                            // Convert image to byte array and store in p1
                            fs1.CopyTo(ms1);
                            p1 = ms1.ToArray();
                        }
                    }

                    coupon.Picture = p1;
                }

                _db.Coupon.Add(coupon);
                await _db.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }
            return View(coupon);
        }
        // GET - EDIT
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var dbCoupon = await _db.Coupon.FindAsync(id);
            if (dbCoupon == null)
            {
                return NotFound();
            }

            return View(dbCoupon);
        }

        // POST - EDIT
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Coupon coupon)
        {
            if (coupon.Id == 0)
            {
                return NotFound();
            }

            var dbCoupon = await _db.Coupon.FindAsync(coupon.Id);
            if (ModelState.IsValid)
            {
                var files = HttpContext.Request.Form.Files;
                if (files.Count > 0)
                {
                    byte[] p1 = null;
                    using (var fs1 = files[0].OpenReadStream())
                    {
                        using (var ms1 = new MemoryStream())
                        {
                            // Convert image to byte array and store in p1
                            fs1.CopyTo(ms1);
                            p1 = ms1.ToArray();
                        }
                    }

                    dbCoupon.Picture = p1;
                }

                dbCoupon.MinimumAmount = coupon.MinimumAmount;
                dbCoupon.Name = coupon.Name;
                dbCoupon.Discount = coupon.Discount;
                dbCoupon.CouponType = coupon.CouponType;
                dbCoupon.IsActive = coupon.IsActive;

                await _db.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }
            return View(coupon);
        }

        // GET - DETAILS
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var dbCoupon = await _db.Coupon.FindAsync(id);
            if (dbCoupon == null)
            {
                return NotFound();
            }

            return View(dbCoupon);
        }
        // GET - DELETE
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var dbCoupon = await _db.Coupon.FindAsync(id);
            if (dbCoupon == null)
            {
                return NotFound();
            }

            return View(dbCoupon);
        }
        // POST - DELETE
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Coupon coupon)
        {
            if (coupon.Id == 0)
            {
                return NotFound();
            }

            var dbCoupon = await _db.Coupon.FindAsync(coupon.Id);
            if(dbCoupon == null)
            {
                return NotFound();
            }

            _db.Coupon.Remove(dbCoupon);
            await _db.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
    }
}
