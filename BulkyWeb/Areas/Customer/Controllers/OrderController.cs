using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Bulky.Utility;
using Bulky.Models.Models;
using Stripe.Checkout;

namespace BulkyWeb.Areas.Customer.Controllers
{
    [Area("Customer")]
    [Authorize]
    public class OrderController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        public OrderController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        /// <summary>
        /// Get current user ID from claims
        /// </summary>
        private string GetCurrentUserId()
        {
            var claimsIdentity = User.Identity as ClaimsIdentity;
            var userIdClaim = claimsIdentity?.FindFirst(ClaimTypes.NameIdentifier);
            
            if (userIdClaim == null || string.IsNullOrEmpty(userIdClaim.Value))
            {
                throw new UnauthorizedAccessException("User authentication failed. Please log in again.");
            }
            
            return userIdClaim.Value;
        }

        // GET: Customer Order History
        public ActionResult Index()
        {
            try
            {
                var userId = GetCurrentUserId();
                
                // Get orders for current user only
                var orderHeaders = _unitOfWork.OrderHeader.GetAll(
                    u => u.ApplicationUserId == userId, 
                    includeProperties: "ApplicationUser"
                ).OrderByDescending(x => x.OrderDate);

                var orderViewModels = new List<OrderViewModel>();

                foreach (var orderHeader in orderHeaders)
                {
                    var orderDetails = _unitOfWork.OrderDetail.GetAll(
                        u => u.OrderHeaderId == orderHeader.Id, 
                        includeProperties: "Product"
                    );
                    
                    orderViewModels.Add(new OrderViewModel
                    {
                        OrderHeader = orderHeader,
                        OrderDetails = orderDetails
                    });
                }

                return View(orderViewModels);
            }
            catch (UnauthorizedAccessException)
            {
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }
            catch (Exception)
            {
                TempData["error"] = "An error occurred while loading your orders.";
                return RedirectToAction("Index", "Home");
            }
        }

        // GET: Customer Order Details (Read-only)
        public ActionResult Details(int id)
        {
            try
            {
                var userId = GetCurrentUserId();
                
                // Get order header and verify it belongs to current user
                var orderHeader = _unitOfWork.OrderHeader.Get(
                    u => u.Id == id && u.ApplicationUserId == userId, 
                    includeProperties: "ApplicationUser"
                );

                if (orderHeader == null)
                {
                    TempData["error"] = "Order not found or access denied.";
                    return RedirectToAction(nameof(Index));
                }

                var orderDetails = _unitOfWork.OrderDetail.GetAll(
                    u => u.OrderHeaderId == id, 
                    includeProperties: "Product"
                );

                var orderViewModel = new OrderViewModel
                {
                    OrderHeader = orderHeader,
                    OrderDetails = orderDetails
                };

                return View(orderViewModel);
            }
            catch (UnauthorizedAccessException)
            {
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }
            catch (Exception)
            {
                TempData["error"] = "An error occurred while loading the order details.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Pay Now for Orders with Pending Payment
        public ActionResult PayNow(int id)
        {
            try
            {
                var userId = GetCurrentUserId();
                
                // Get order header and verify it belongs to current user
                var orderHeader = _unitOfWork.OrderHeader.Get(
                    u => u.Id == id && u.ApplicationUserId == userId, 
                    includeProperties: "ApplicationUser"
                );

                if (orderHeader == null)
                {
                    TempData["error"] = "Order not found or access denied.";
                    return RedirectToAction(nameof(Index));
                }

                // Verify this order requires payment (either pending or delayed for approval)
                if (orderHeader.PaymentStatus != SD.PaymentStatusDelayedForApproval && 
                    orderHeader.PaymentStatus != SD.PaymentStatusPending)
                {
                    TempData["error"] = "This order does not require payment or payment has already been processed.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                // Check if order is not cancelled
                if (orderHeader.OrderStatus == SD.StatusCancelled)
                {
                    TempData["error"] = "Cannot process payment for a cancelled order.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                // Get order details for Stripe payment
                var orderDetails = _unitOfWork.OrderDetail.GetAll(
                    u => u.OrderHeaderId == id, 
                    includeProperties: "Product"
                );

                if (!orderDetails.Any())
                {
                    TempData["error"] = "Order details not found.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                // Create Stripe payment session
                var options = new SessionCreateOptions
                {
                    PaymentMethodTypes = new List<string> { "card" },
                    LineItems = new List<SessionLineItemOptions>(),
                    Mode = "payment",
                    SuccessUrl = Url.Action("PaymentConfirmation", "Order", new { id = orderHeader.Id }, Request.Scheme),
                    CancelUrl = Url.Action("Details", "Order", new { id = orderHeader.Id }, Request.Scheme),
                    CustomerEmail = orderHeader.ApplicationUser?.Email,
                    Metadata = new Dictionary<string, string>
                    {
                        { "order_id", orderHeader.Id.ToString() },
                        { "payment_type", orderHeader.PaymentStatus == SD.PaymentStatusPending ? "pending_payment" : "delayed_payment" }
                    }
                };

                // Add line items to Stripe session
                foreach (var item in orderDetails)
                {
                    if (item?.Product == null) continue;

                    var sessionLineItem = new SessionLineItemOptions
                    {
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            Currency = "usd",
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = item.Product.Title ?? "Book",
                                Description = $"by {item.Product.Author ?? "Unknown Author"}",
                            },
                            UnitAmount = (long)(item.Price * 100), // Convert to cents
                        },
                        Quantity = item.Count,
                    };
                    options.LineItems.Add(sessionLineItem);
                }

                var service = new SessionService();
                var session = service.Create(options);

                // Update order with new session information
                _unitOfWork.OrderHeader.UpdateStripePaymentId(orderHeader.Id, session.Id, session.PaymentIntentId);
                _unitOfWork.Save();

                TempData["success"] = "Redirecting to payment processing...";

                // Redirect to Stripe payment page
                Response.Headers.Add("Location", session.Url);
                return new StatusCodeResult(303); // 303 See Other to redirect to Stripe
            }
            catch (UnauthorizedAccessException)
            {
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }
            catch (Exception ex)
            {
                TempData["error"] = "An error occurred while processing payment. Please try again.";
                return RedirectToAction(nameof(Details), new { id });
            }
        }

        // GET: Payment Confirmation after Stripe payment
        public ActionResult PaymentConfirmation(int id)
        {
            try
            {
                var userId = GetCurrentUserId();
                
                // Get order header and verify it belongs to current user
                var orderHeader = _unitOfWork.OrderHeader.Get(
                    u => u.Id == id && u.ApplicationUserId == userId, 
                    includeProperties: "ApplicationUser"
                );

                if (orderHeader == null)
                {
                    TempData["error"] = "Order not found or access denied.";
                    return RedirectToAction(nameof(Index));
                }

                // Verify payment with Stripe
                if (!string.IsNullOrEmpty(orderHeader.SessionId))
                {
                    var service = new SessionService();
                    var session = service.Get(orderHeader.SessionId);

                    if (session.PaymentStatus.ToLower() == "paid")
                    {
                        // Update order status to approved and payment status to approved
                        _unitOfWork.OrderHeader.UpdateStripePaymentId(id, session.Id, session.PaymentIntentId);
                        _unitOfWork.OrderHeader.UpdateStatus(id, SD.StatusApproved, SD.PaymentStatusApproved);
                        _unitOfWork.Save();

                        TempData["success"] = "Payment completed successfully! Your order is now being processed.";
                    }
                    else
                    {
                        TempData["warning"] = "Payment verification is still pending. Please check back in a few minutes.";
                    }
                }
                else
                {
                    TempData["error"] = "Payment session not found. Please contact support if payment was processed.";
                }

                return RedirectToAction(nameof(Details), new { id });
            }
            catch (UnauthorizedAccessException)
            {
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }
            catch (Exception)
            {
                TempData["error"] = "An error occurred while confirming payment.";
                return RedirectToAction(nameof(Details), new { id });
            }
        }
    }
}