using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Spice.Data;
using Spice.Models;
using Spice.Models.ViewModels;
using Spice.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Spice.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class OrderController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IEmailSender _emailSender;
        private int PageSize = 1;
        public OrderController(ApplicationDbContext db, IEmailSender emailSender)
        {
            _db = db;
            _emailSender = emailSender;
        }

        [Authorize]
        public async Task<IActionResult> Confirm(int id)
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claims = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

            OrderDetailsViewModel orderDetailsViewModel = new OrderDetailsViewModel()
            {
                OrderHeader = await _db.OrderHeader.Include(o => o.ApplicationUser).FirstOrDefaultAsync(o => o.Id == id && o.UserId == claims.Value),
                OrderDetails = await _db.OrderDetails.Where(o => o.OrderId == id).ToListAsync()
            };

            return View(orderDetailsViewModel);
        }
        public IActionResult Index()
        {
            return View();
        }

        [Authorize]
        public async Task<IActionResult> OrderHistory(int productPage = 1)
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

            OrderListViewModel orderListVM = new OrderListViewModel()
            {
                Orders = new List<OrderDetailsViewModel>()
            };


            List<OrderHeader> orderHeaderList = await _db.OrderHeader.Include(o => o.ApplicationUser).Where(u => u.UserId == claim.Value).ToListAsync();

            foreach (OrderHeader item in orderHeaderList)
            {
                OrderDetailsViewModel invidiual = new OrderDetailsViewModel()
                {
                    OrderHeader = item,
                    OrderDetails = await _db.OrderDetails.Where(o => o.OrderId == item.Id).ToListAsync()
                };

                orderListVM.Orders.Add(invidiual);
            }

            var count = orderListVM.Orders.Count;
            orderListVM.Orders = orderListVM.Orders.OrderByDescending(p => p.OrderHeader.Id)
                                 .Skip((productPage - 1) * PageSize)
                                 .Take(PageSize).ToList();

            orderListVM.PagingInfo = new PagingInfo
            {
                CurrentPage = productPage,
                ItemsPerPage = PageSize,
                TotalItem = count,
                urlParam = "/Customer/Order/OrderHistory?productPage=:"
            };

            return View(orderListVM);
        }

        public async Task<IActionResult> GetOrderDetail(int id)
        {
            OrderDetailsViewModel orderDetailsViewModel = new OrderDetailsViewModel()
            {
                OrderHeader = await _db.OrderHeader.Include(o => o.ApplicationUser).FirstOrDefaultAsync(o => o.Id == id),
                OrderDetails = await _db.OrderDetails.Where(o => o.OrderId == id).ToListAsync()
            };

            return PartialView("_IndividualOrderDetails", orderDetailsViewModel);
        }

        public async Task<IActionResult> GetOrderStatus(int id)
        {
            OrderHeader orderHeader = await _db.OrderHeader.FirstOrDefaultAsync(o => o.Id == id);
            return PartialView("_IndividualOrderStatus", orderHeader.Status);
        }

        [Authorize(Roles = SD.KitchenUser + "," + SD.ManagerUser)]
        public async Task<IActionResult> ManageOrder()
        {
            List<OrderDetailsViewModel> orderDetailsVM = new List<OrderDetailsViewModel>();

            List<OrderHeader> orderHeaderList = await _db.OrderHeader
                    .Where(o => o.Status == SD.StatusSubmitted || o.Status == SD.StatusInProcess)
                    .OrderByDescending(o => o.PickupTime).ToListAsync();

            foreach (OrderHeader item in orderHeaderList)
            {
                OrderDetailsViewModel invidiual = new OrderDetailsViewModel()
                {
                    OrderHeader = item,
                    OrderDetails = await _db.OrderDetails.Where(o => o.OrderId == item.Id).ToListAsync()
                };

                orderDetailsVM.Add(invidiual);
            }

            return View(orderDetailsVM.OrderByDescending(o => o.OrderHeader.PickupTime));
        }

        [Authorize(Roles = SD.KitchenUser + "," + SD.ManagerUser)]
        public async Task<IActionResult> OrderPrepare(int OrderId)
        {
            OrderHeader orderHeader = await _db.OrderHeader.FirstOrDefaultAsync(o => o.Id == OrderId);
            orderHeader.Status = SD.StatusInProcess;
            await _db.SaveChangesAsync();
            return RedirectToAction("ManageOrder", "Order");
        }


        [Authorize(Roles = SD.KitchenUser + "," + SD.ManagerUser)]
        public async Task<IActionResult> OrderReady(int OrderId)
        {
            OrderHeader orderHeader = await _db.OrderHeader.FirstOrDefaultAsync(o => o.Id == OrderId);
            orderHeader.Status = SD.StatusReady;
            await _db.SaveChangesAsync();

            // Email logic
            await _emailSender.SendEmailAsync(_db.Users.Where(u => u.Id == orderHeader.UserId)
                .FirstOrDefault().Email, "Spice - Order ready" + orderHeader.Id.ToString(), "Order is  ready for pickup");


            return RedirectToAction("ManageOrder", "Order");
        }


        [Authorize(Roles = SD.KitchenUser + "," + SD.ManagerUser)]
        public async Task<IActionResult> OrderCancel(int OrderId)
        {
            OrderHeader orderHeader = await _db.OrderHeader.FirstOrDefaultAsync(o => o.Id == OrderId);
            orderHeader.Status = SD.StatusCancelled;
            await _db.SaveChangesAsync();

            await _emailSender.SendEmailAsync(_db.Users.Where(u => u.Id == orderHeader.UserId)
                .FirstOrDefault().Email, "Spice - Order cancelled" + orderHeader.Id.ToString(), "Order has been cancelled successfully");

            return RedirectToAction("ManageOrder", "Order");
        }

        [Authorize]
        public async Task<IActionResult> OrderPickup(int productPage = 1, string searchName = null, string searchPhone = null, string searchEmail = null)
        {
            OrderListViewModel orderListVM = new OrderListViewModel()
            {
                Orders = new List<OrderDetailsViewModel>()
            };

            StringBuilder param = new StringBuilder();
            param.Append("/Customer/Order/OrderPickup?productPage=:");
            param.Append("&searchName=");
            if (searchName != null)
            {
                param.Append(searchName);
            }
            param.Append("&searchPhone=");
            if (searchPhone != null)
            {
                param.Append(searchPhone);
            }
            param.Append("&searchEmail=");
            if (searchEmail != null)
            {
                param.Append(searchEmail);
            }

            List<OrderHeader> orderHeaderList = new List<OrderHeader>();

            if (searchEmail != null || searchName != null || searchPhone != null)
            {
                var user = new ApplicationUser();
                if (searchName != null)
                {
                    orderHeaderList = await _db.OrderHeader.Include(o => o.ApplicationUser)
                        .Where(u => u.PickupName.ToLower().Contains(searchName.ToLower()))
                        .OrderByDescending(o => o.OrderDate).ToListAsync();
                }
                else
                {
                    if (searchEmail != null)
                    {
                        user = await _db.ApplicationUser.Where(u => u.Email.ToLower().Contains(searchEmail.ToLower())).FirstOrDefaultAsync();
                        orderHeaderList = await _db.OrderHeader.Include(o => o.ApplicationUser)
                            .Where(o => o.UserId == user.Id)
                            .OrderByDescending(o => o.OrderDate).ToListAsync();
                    }
                    else
                    {
                        if (searchPhone != null)
                        {
                            orderHeaderList = await _db.OrderHeader.Include(o => o.ApplicationUser)
                                .Where(u => u.PhoneNumber.Contains(searchPhone))
                                .OrderByDescending(o => o.OrderDate).ToListAsync();
                        }
                    }
                }
            }
            else
            {
                orderHeaderList = await _db.OrderHeader.Include(o => o.ApplicationUser).Where(u => u.Status == SD.StatusReady).ToListAsync();
            }
            foreach (OrderHeader item in orderHeaderList)
            {
                OrderDetailsViewModel invidiual = new OrderDetailsViewModel()
                {
                    OrderHeader = item,
                    OrderDetails = await _db.OrderDetails.Where(o => o.OrderId == item.Id).ToListAsync()
                };

                orderListVM.Orders.Add(invidiual);
            }

            var count = orderListVM.Orders.Count;
            orderListVM.Orders = orderListVM.Orders.OrderByDescending(p => p.OrderHeader.Id)
                                 .Skip((productPage - 1) * PageSize)
                                 .Take(PageSize).ToList();

            orderListVM.PagingInfo = new PagingInfo
            {
                CurrentPage = productPage,
                ItemsPerPage = PageSize,
                TotalItem = count,
                urlParam = param.ToString()
            };

            return View(orderListVM);
        }

        [Authorize(Roles = SD.FrontDeskUser + "," + SD.ManagerUser)]
        [HttpPost, ValidateAntiForgeryToken]
        [ActionName("OrderPickup")]
        public async Task<IActionResult> OrderPickupPOST(int orderId)
        {
            OrderHeader orderHeader = await _db.OrderHeader.FirstOrDefaultAsync(o => o.Id == orderId);
            orderHeader.Status = SD.StatusCompleted;
            await _db.SaveChangesAsync();

            await _emailSender.SendEmailAsync(_db.Users.Where(u => u.Id == orderHeader.UserId)
                .FirstOrDefault().Email, "Spice - Order completed" + orderHeader.Id.ToString(), "Order has been completed successfully");

            return RedirectToAction("OrderPickup", "Order");
        }
    }
}
