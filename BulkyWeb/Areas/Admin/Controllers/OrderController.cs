using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Bulky.Utility;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace BulkyWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,Employee")]
    public class OrderController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<IdentityUser> _userManager;

        public OrderController(IUnitOfWork unitOfWork, UserManager<IdentityUser> userManager)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
        }

        // GET: OrderController
        public ActionResult Index()
        {
            var orderHeaders = _unitOfWork.OrderHeader.GetAll(includeProperties: "ApplicationUser");
            var orderViewModels = new List<OrderViewModel>();

            foreach (var orderHeader in orderHeaders)
            {
                var orderDetails = _unitOfWork.OrderDetail.GetAll(u => u.OrderHeaderId == orderHeader.Id, includeProperties: "Product");
                orderViewModels.Add(new OrderViewModel
                {
                    OrderHeader = orderHeader,
                    OrderDetails = orderDetails
                });
            }

            return View(orderViewModels);
        }

        // GET: OrderController/Details/5
        public ActionResult Details(int id)
        {
            var orderHeader = _unitOfWork.OrderHeader.Get(u => u.Id == id, includeProperties: "ApplicationUser");
            if (orderHeader == null)
            {
                return NotFound();
            }

            var orderDetails = _unitOfWork.OrderDetail.GetAll(u => u.OrderHeaderId == id, includeProperties: "Product");
            var orderViewModel = new OrderViewModel
            {
                OrderHeader = orderHeader,
                OrderDetails = orderDetails
            };

            return View(orderViewModel);
        }

        // POST: Start Processing Order
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult StartProcessing(int orderId)
        {
            try
            {
                var orderHeader = _unitOfWork.OrderHeader.Get(u => u.Id == orderId);
                if (orderHeader == null)
                {
                    TempData["error"] = "Order not found.";
                    return RedirectToAction(nameof(Index));
                }

                if (orderHeader.OrderStatus != SD.StatusPending && orderHeader.OrderStatus != SD.StatusApproved)
                {
                    TempData["error"] = "Order cannot be processed from current status.";
                    return RedirectToAction(nameof(Details), new { id = orderId });
                }

                orderHeader.OrderStatus = SD.StatusInProcess;
                _unitOfWork.OrderHeader.Update(orderHeader);
                _unitOfWork.Save();

                TempData["success"] = "Order processing started successfully!";
            }
            catch (Exception ex)
            {
                TempData["error"] = "An error occurred while starting order processing.";
            }

            return RedirectToAction(nameof(Details), new { id = orderId });
        }

        // POST: Ship Order
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ShipOrder(int orderId, string carrier, string trackingNumber)
        {
            try
            {
                var orderHeader = _unitOfWork.OrderHeader.Get(u => u.Id == orderId);
                if (orderHeader == null)
                {
                    TempData["error"] = "Order not found.";
                    return RedirectToAction(nameof(Index));
                }

                if (orderHeader.OrderStatus != SD.StatusInProcess)
                {
                    TempData["error"] = "Order must be in processing status to ship.";
                    return RedirectToAction(nameof(Details), new { id = orderId });
                }

                if (string.IsNullOrWhiteSpace(carrier) || string.IsNullOrWhiteSpace(trackingNumber))
                {
                    TempData["error"] = "Carrier and tracking number are required.";
                    return RedirectToAction(nameof(Details), new { id = orderId });
                }

                orderHeader.OrderStatus = SD.StatusShipped;
                orderHeader.ShippingDate = DateTime.Now;
                orderHeader.Carrier = carrier;
                orderHeader.TrackingNumber = trackingNumber;

                // Update payment status if it was delayed for approval
                if (orderHeader.PaymentStatus == SD.PaymentStatusDelayedForApproval)
                {
                    orderHeader.PaymentStatus = SD.PaymentStatusApproved;
                }

                _unitOfWork.OrderHeader.Update(orderHeader);
                _unitOfWork.Save();

                TempData["success"] = "Order shipped successfully!";
            }
            catch (Exception ex)
            {
                TempData["error"] = "An error occurred while shipping the order.";
            }

            return RedirectToAction(nameof(Details), new { id = orderId });
        }

        // POST: Cancel Order
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CancelOrder(int orderId)
        {
            try
            {
                var orderHeader = _unitOfWork.OrderHeader.Get(u => u.Id == orderId);
                if (orderHeader == null)
                {
                    TempData["error"] = "Order not found.";
                    return RedirectToAction(nameof(Index));
                }

                if (orderHeader.OrderStatus == SD.StatusShipped)
                {
                    TempData["error"] = "Cannot cancel a shipped order.";
                    return RedirectToAction(nameof(Details), new { id = orderId });
                }

                orderHeader.OrderStatus = SD.StatusCancelled;

                // If payment was approved, mark it for refund
                if (orderHeader.PaymentStatus == SD.PaymentStatusApproved)
                {
                    orderHeader.PaymentStatus = SD.PaymentStatusRefunded;
                }

                _unitOfWork.OrderHeader.Update(orderHeader);
                _unitOfWork.Save();

                TempData["success"] = "Order cancelled successfully!";
            }
            catch (Exception ex)
            {
                TempData["error"] = "An error occurred while cancelling the order.";
            }

            return RedirectToAction(nameof(Details), new { id = orderId });
        }

        // POST: Update Order Details (Admin only)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public ActionResult UpdateOrderDetails(int orderId, string name, string phoneNumber, string streetAddress, string city, string state, string postalCode)
        {
            try
            {
                var orderHeader = _unitOfWork.OrderHeader.Get(u => u.Id == orderId);
                if (orderHeader == null)
                {
                    TempData["error"] = "Order not found.";
                    return RedirectToAction(nameof(Index));
                }

                orderHeader.Name = name;
                orderHeader.PhoneNumber = phoneNumber;
                orderHeader.StreetAddress = streetAddress;
                orderHeader.City = city;
                orderHeader.State = state;
                orderHeader.PostalCode = postalCode;

                _unitOfWork.OrderHeader.Update(orderHeader);
                _unitOfWork.Save();

                TempData["success"] = "Order details updated successfully!";
            }
            catch (Exception ex)
            {
                TempData["error"] = "An error occurred while updating order details.";
            }

            return RedirectToAction(nameof(Details), new { id = orderId });
        }

        // Helper method to check if user can edit orders
        private bool CanEditOrder()
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            return User.IsInRole(SD.Role_Admin) || User.IsInRole(SD.Role_Employee);
        }

        // GET: OrderController/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: OrderController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        // GET: OrderController/Edit/5
        public ActionResult Edit(int id)
        {
            return View();
        }

        // POST: OrderController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id, IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        // GET: OrderController/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: OrderController/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id, IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }
    }
}
