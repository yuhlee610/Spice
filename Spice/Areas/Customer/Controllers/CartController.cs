using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Spice.Data;
using Spice.Models;
using Spice.Models.ViewModels;
using Spice.Utility;
using Stripe;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Spice.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IEmailSender _emailSender;
        [BindProperty]
        public OrderDetailsCart detailsCart { get; set; }
        public CartController(ApplicationDbContext db, IEmailSender emailSender)
        {
            _db = db;
            _emailSender = emailSender;
        }
        public async Task<IActionResult> Index()
        {
            detailsCart = new OrderDetailsCart()
            {
                OrderHeader = new Models.OrderHeader()
            };

            detailsCart.OrderHeader.OrderTotal = 0;

            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

            var cart = _db.ShoppingCart.Where(s => s.ApplicationUserId == claim.Value);

            if (cart != null)
            {
                detailsCart.listCart = cart.ToList();
            }

            foreach (var list in detailsCart.listCart)
            {
                list.MenuItem = await _db.MenuItem.FirstOrDefaultAsync(m => m.Id == list.MenuItemId);
                detailsCart.OrderHeader.OrderTotal = detailsCart.OrderHeader.OrderTotal + (list.MenuItem.Price * list.Count);
                list.MenuItem.Description = SD.ConvertToRawHtml(list.MenuItem.Description);
                if (list.MenuItem.Description.Length > 100)
                {
                    list.MenuItem.Description = list.MenuItem.Description.Substring(0, 99) + "...";
                }
            }
            detailsCart.OrderHeader.OrderTotalOriginal = detailsCart.OrderHeader.OrderTotal;
            if (HttpContext.Session.GetString(SD.ssCouponCode) != null)
            {
                detailsCart.OrderHeader.CouponCode = HttpContext.Session.GetString(SD.ssCouponCode);
                var dbCoupon = await _db.Coupon.Where(c => c.Name.ToLower() == detailsCart.OrderHeader.CouponCode.ToLower()).FirstOrDefaultAsync();
                detailsCart.OrderHeader.OrderTotal = SD.Discounted(dbCoupon, detailsCart.OrderHeader.OrderTotalOriginal);
            }


            return View(detailsCart);
        }

        public IActionResult AddCoupon()
        {
            if (detailsCart.OrderHeader.CouponCode == null)
            {
                detailsCart.OrderHeader.CouponCode = "";
            }
            HttpContext.Session.SetString(SD.ssCouponCode, detailsCart.OrderHeader.CouponCode);

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Plus(int cartId)
        {
            var cart = await _db.ShoppingCart.Where(s => s.Id == cartId).FirstOrDefaultAsync();
            cart.Count += 1;
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Minus(int cartId)
        {
            var cart = await _db.ShoppingCart.Where(s => s.Id == cartId).FirstOrDefaultAsync();
            if(cart.Count == 1)
            {
                _db.ShoppingCart.Remove(cart);
                await _db.SaveChangesAsync();

                var count = _db.ShoppingCart.Where(s => s.ApplicationUserId == cart.ApplicationUserId).ToList().Count;
                HttpContext.Session.SetInt32(SD.ssCartCount, count);
            }
            else
            {
                cart.Count -= 1;
                await _db.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Remove(int cartId)
        {
            var cart = await _db.ShoppingCart.Where(s => s.Id == cartId).FirstOrDefaultAsync();

            _db.ShoppingCart.Remove(cart);
            await _db.SaveChangesAsync();

            var count = _db.ShoppingCart.Where(s => s.ApplicationUserId == cart.ApplicationUserId).ToList().Count;
            HttpContext.Session.SetInt32(SD.ssCartCount, count);

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Summary()
        {
            detailsCart = new OrderDetailsCart()
            {
                OrderHeader = new Models.OrderHeader()
            };

            detailsCart.OrderHeader.OrderTotal = 0;

            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

            ApplicationUser applicationUser = await _db.ApplicationUser.Where(a => a.Id == claim.Value).FirstOrDefaultAsync();

            var cart = _db.ShoppingCart.Where(s => s.ApplicationUserId == claim.Value);

            if (cart != null)
            {
                detailsCart.listCart = cart.ToList();
            }

            foreach (var list in detailsCart.listCart)
            {
                list.MenuItem = await _db.MenuItem.FirstOrDefaultAsync(m => m.Id == list.MenuItemId);
                detailsCart.OrderHeader.OrderTotal = detailsCart.OrderHeader.OrderTotal + (list.MenuItem.Price * list.Count);
                
            }

            detailsCart.OrderHeader.OrderTotalOriginal = detailsCart.OrderHeader.OrderTotal;
            detailsCart.OrderHeader.PhoneNumber = applicationUser.PhoneNumber;
            detailsCart.OrderHeader.PickupName = applicationUser.Name;
            detailsCart.OrderHeader.PickupTime = DateTime.Now;


            if (HttpContext.Session.GetString(SD.ssCouponCode) != null)
            {
                detailsCart.OrderHeader.CouponCode = HttpContext.Session.GetString(SD.ssCouponCode);
                var dbCoupon = await _db.Coupon.Where(c => c.Name.ToLower() == detailsCart.OrderHeader.CouponCode.ToLower()).FirstOrDefaultAsync();
                detailsCart.OrderHeader.OrderTotal = SD.Discounted(dbCoupon, detailsCart.OrderHeader.OrderTotalOriginal);
            }

            return View(detailsCart);
        }

        [HttpPost, ValidateAntiForgeryToken, ActionName("Summary")]
        public async Task<IActionResult> SummaryPOST(string stripeToken)
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

            var cart = await _db.ShoppingCart.Where(s => s.ApplicationUserId == claim.Value).ToListAsync();

            detailsCart.OrderHeader.PaymentStatus = SD.PaymentStatusPending;
            detailsCart.OrderHeader.OrderDate = DateTime.Now;
            detailsCart.OrderHeader.UserId = claim.Value;
            detailsCart.OrderHeader.Status = SD.PaymentStatusPending;
            detailsCart.OrderHeader.PickupTime = Convert.ToDateTime(detailsCart.OrderHeader.PickupDate.ToShortDateString() + " " + detailsCart.OrderHeader.PickupTime.ToShortTimeString());
            detailsCart.listCart = cart;

            _db.OrderHeader.Add(detailsCart.OrderHeader);
            await _db.SaveChangesAsync();

            List<OrderDetails> orderDetailsList = new List<OrderDetails>();

            detailsCart.OrderHeader.OrderTotalOriginal = 0;

            foreach (var item in detailsCart.listCart)
            {
                item.MenuItem = await _db.MenuItem.FirstOrDefaultAsync(m => m.Id == item.MenuItemId);
                OrderDetails orderDetails = new OrderDetails
                {
                    MenuItemId = item.MenuItemId,
                    OrderId = detailsCart.OrderHeader.Id,
                    Description = item.MenuItem.Description,
                    Name = item.MenuItem.Name,
                    Price = item.MenuItem.Price,
                    Count = item.Count
                };
                detailsCart.OrderHeader.OrderTotalOriginal += orderDetails.Count * orderDetails.Price;
                _db.OrderDetails.Add(orderDetails);
            }


            if (HttpContext.Session.GetString(SD.ssCouponCode) != null)
            {
                detailsCart.OrderHeader.CouponCode = HttpContext.Session.GetString(SD.ssCouponCode);
                var dbCoupon = await _db.Coupon.Where(c => c.Name.ToLower() == detailsCart.OrderHeader.CouponCode.ToLower()).FirstOrDefaultAsync();
                detailsCart.OrderHeader.OrderTotal = SD.Discounted(dbCoupon, detailsCart.OrderHeader.OrderTotalOriginal);
            }
            else
            {
                detailsCart.OrderHeader.OrderTotal = detailsCart.OrderHeader.OrderTotalOriginal;
            }

            detailsCart.OrderHeader.CouponCodeDiscount = detailsCart.OrderHeader.OrderTotalOriginal - detailsCart.OrderHeader.OrderTotal;

            _db.ShoppingCart.RemoveRange(detailsCart.listCart);
            HttpContext.Session.SetInt32(SD.ssCartCount, 0);

            await _db.SaveChangesAsync();

            var options = new ChargeCreateOptions()
            {
                Amount = Convert.ToInt32(detailsCart.OrderHeader.OrderTotal * 100),
                Currency = "usd",
                Description = "Order ID: " + detailsCart.OrderHeader.Id,
                Source = stripeToken
            };
            var service = new ChargeService();
            Charge charge = service.Create(options);

            if(charge.BalanceTransactionId == null)
            {
                detailsCart.OrderHeader.PaymentStatus = SD.PaymentStatusRejected;
            }
            else
            {
                detailsCart.OrderHeader.TransactionId = charge.BalanceTransactionId;
            }

            if(charge.Status.ToLower() == "succeeded")
            {
                //email
                await _emailSender.SendEmailAsync(_db.Users.Where(u => u.Id == claim.Value)
                    .FirstOrDefault().Email, "Spice - Order created" + detailsCart.OrderHeader.Id.ToString(), "Order has been submitted successfully");

                detailsCart.OrderHeader.PaymentStatus = SD.PaymentStatusApproved;
                detailsCart.OrderHeader.Status = SD.StatusSubmitted;
            }
            else
            {
                detailsCart.OrderHeader.PaymentStatus = SD.PaymentStatusRejected;
            }

            await _db.SaveChangesAsync();

            //return RedirectToAction("Index", "Home");
            return RedirectToAction("Confirm", "Order", new { id = detailsCart.OrderHeader.Id });
        }

        
    }
}
