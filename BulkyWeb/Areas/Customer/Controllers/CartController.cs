using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models.Models;
using System.Security.Claims;
using Bulky.Models.ViewModels;
using Microsoft.Extensions.Logging;
using Bulky.Utility;
using Stripe.BillingPortal;

namespace BulkyWeb.Areas.Customer.Controllers
{
    [Area("Customer")]
    [Authorize]
    public class CartController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<CartController> _logger;

        public CartController(IUnitOfWork unitOfWork, ILogger<CartController> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        /// <summary>
        /// Helper method to get current user ID with proper validation
        /// </summary>
        private string GetCurrentUserId()
        {
            var claimsIdentity = User.Identity as ClaimsIdentity;
            var userIdClaim = claimsIdentity?.FindFirst(ClaimTypes.NameIdentifier);
            
            if (userIdClaim == null || string.IsNullOrEmpty(userIdClaim.Value))
            {
                _logger.LogWarning("User ID not found in claims for user: {UserName}. Claims: {Claims}", 
                    User.Identity?.Name ?? "Anonymous", 
                    string.Join(", ", claimsIdentity?.Claims?.Select(c => $"{c.Type}:{c.Value}") ?? new string[0]));
                throw new UnauthorizedAccessException("User authentication failed. Please log in again.");
            }
            
            return userIdClaim.Value;
        }

        // GET: CartController
        public IActionResult Index()
        {
            try
            {
                var userId = GetCurrentUserId();
                
                var cartItems = _unitOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == userId, includeProperties: "Product");
                
                // Calculate order total with bulk pricing
                double orderTotal = 0;
                foreach (var item in cartItems)
                {
                    double itemPrice = 0;
                    
                    // Apply bulk pricing logic
                    if (item.Count >= 100)
                    {
                        itemPrice = item.Product.Price100;
                    }
                    else if (item.Count >= 50)
                    {
                        itemPrice = item.Product.Price50;
                    }
                    else
                    {
                        itemPrice = item.Product.Price;
                    }
                    
                    orderTotal += itemPrice * item.Count;
                }

                var shoppingCartViewModel = new ShoppingCartViewModel()
                {
                    ShoppingCartList = cartItems,
                    
                    OrderHeader = new OrderHeader() // Initialize OrderHeader if needed
                };

                _logger.LogInformation($"Cart loaded for user {userId} with {cartItems.Count()} items and total ${orderTotal:F2}");

                return View(shoppingCartViewModel);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access to cart");
                TempData["error"] = ex.Message;
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while loading shopping cart");
                TempData["error"] = "An error occurred while loading your cart. Please try again.";
                
                // Return empty cart view model
                var emptyCartViewModel = new ShoppingCartViewModel()
                {
                    ShoppingCartList = new List<ShoppingCart>(),
                    OrderHeader = new OrderHeader()
                };
                
                return View(emptyCartViewModel);
            }
        }

        public IActionResult Plus(int cartId)
        {
            try
            {
                var userId = GetCurrentUserId();
                var cartFromDb = _unitOfWork.ShoppingCart.Get(u => u.Id == cartId && u.ApplicationUserId == userId);
                
                if (cartFromDb != null)
                {
                    cartFromDb.Count += 1;
                    _unitOfWork.ShoppingCart.Update(cartFromDb);
                    _unitOfWork.Save();
                    
                    _logger.LogInformation($"Increased quantity for cart item {cartId} to {cartFromDb.Count} for user {userId}");
                    TempData["success"] = "Item quantity increased successfully!";
                }
                else
                {
                    TempData["error"] = "Cart item not found or access denied.";
                    _logger.LogWarning($"Attempted to increase quantity for non-existent or unauthorized cart item {cartId} by user {userId}");
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access to cart item {CartId}", cartId);
                TempData["error"] = ex.Message;
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error occurred while increasing quantity for cart item {cartId}");
                TempData["error"] = "An error occurred while updating the item quantity.";
            }
            
            return RedirectToAction(nameof(Index));
        }

        public IActionResult Minus(int cartId)
        {
            try
            {
                var userId = GetCurrentUserId();
                var cartFromDb = _unitOfWork.ShoppingCart.Get(u => u.Id == cartId && u.ApplicationUserId == userId);
                
                if (cartFromDb != null)
                {
                    if (cartFromDb.Count <= 1)
                    {
                        _unitOfWork.ShoppingCart.Remove(cartFromDb);
                        _logger.LogInformation($"Removed cart item {cartId} from cart for user {userId}");
                        TempData["success"] = "Item removed from cart successfully!";
                    }
                    else
                    {
                        cartFromDb.Count -= 1;
                        _unitOfWork.ShoppingCart.Update(cartFromDb);
                        _logger.LogInformation($"Decreased quantity for cart item {cartId} to {cartFromDb.Count} for user {userId}");
                        TempData["success"] = "Item quantity decreased successfully!";
                    }
                    _unitOfWork.Save();
                }
                else
                {
                    TempData["error"] = "Cart item not found or access denied.";
                    _logger.LogWarning($"Attempted to decrease quantity for non-existent or unauthorized cart item {cartId} by user {userId}");
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access to cart item {CartId}", cartId);
                TempData["error"] = ex.Message;
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error occurred while decreasing quantity for cart item {cartId}");
                TempData["error"] = "An error occurred while updating the item quantity.";
            }
            
            return RedirectToAction(nameof(Index));
        }

        public IActionResult Remove(int cartId)
        {
            try
            {
                var userId = GetCurrentUserId();
                var cartFromDb = _unitOfWork.ShoppingCart.Get(u => u.Id == cartId && u.ApplicationUserId == userId);
                
                if (cartFromDb != null)
                {
                    _unitOfWork.ShoppingCart.Remove(cartFromDb);
                    _unitOfWork.Save();
                    
                    _logger.LogInformation($"Removed cart item {cartId} from cart for user {userId}");
                    TempData["success"] = "Item removed from cart successfully!";
                }
                else
                {
                    TempData["error"] = "Cart item not found or access denied.";
                    _logger.LogWarning($"Attempted to remove non-existent or unauthorized cart item {cartId} by user {userId}");
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access to cart item {CartId}", cartId);
                TempData["error"] = ex.Message;
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error occurred while removing cart item {cartId}");
                TempData["error"] = "An error occurred while removing the item from cart.";
            }
            
            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Clear all items from the user's cart
        /// </summary>
        public IActionResult ClearCart()
        {
            try
            {
                var userId = GetCurrentUserId();
                
                var cartItems = _unitOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == userId);
                
                foreach (var item in cartItems)
                {
                    _unitOfWork.ShoppingCart.Remove(item);
                }
                
                _unitOfWork.Save();
                
                _logger.LogInformation($"Cleared cart for user {userId}");
                TempData["success"] = "Cart cleared successfully!";
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access to clear cart");
                TempData["error"] = ex.Message;
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while clearing cart");
                TempData["error"] = "An error occurred while clearing the cart.";
            }
            
            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Get cart item count for the current user (for navbar display)
        /// </summary>
        [HttpGet]
        public IActionResult GetCartCount()
        {
            try
            {
                var userId = GetCurrentUserId();
                
                var cartItems = _unitOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == userId);
                int totalCount = cartItems.Sum(x => x.Count);
                
                return Json(new { count = totalCount });
            }
            catch (UnauthorizedAccessException)
            {
                // Return 0 count for unauthorized users instead of error
                return Json(new { count = 0 });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting cart count");
                return Json(new { count = 0 });
            }
        }

        /// <summary>
        /// Get cart preview for navbar dropdown
        /// </summary>
        [HttpGet]
        public IActionResult GetCartPreview()
        {
            try
            {
                var userId = GetCurrentUserId();
                
                var cartItems = _unitOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == userId, includeProperties: "Product");
                
                // Calculate total for preview
                double orderTotal = 0;
                foreach (var item in cartItems)
                {
                    double itemPrice = item.Count >= 100 ? item.Product.Price100 :
                                      item.Count >= 50 ? item.Product.Price50 :
                                      item.Product.Price;
                    orderTotal += itemPrice * item.Count;
                }

                var viewModel = new ShoppingCartViewModel()
                {
                    ShoppingCartList = cartItems,
                    OrderHeader = new OrderHeader()
                };

                // Return partial view for cart preview
                return PartialView("_CartPreview", viewModel);
            }
            catch (UnauthorizedAccessException)
            {
                // Return empty cart for unauthorized users
                return PartialView("_CartPreview", new ShoppingCartViewModel 
                { 
                    ShoppingCartList = new List<ShoppingCart>(), 
                    OrderHeader = new OrderHeader()

                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting cart preview");
                return PartialView("_CartPreview", new ShoppingCartViewModel 
                { 
                    ShoppingCartList = new List<ShoppingCart>(), 
                    
                });
            }
        }
        public IActionResult Summary()
        {
            var userId = GetCurrentUserId();
            var cartItems = _unitOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == userId, includeProperties: "Product");
            // Calculate order total with bulk pricing


            var shoppingCartViewModel = new ShoppingCartViewModel()
            {
                ShoppingCartList = cartItems,
                
                OrderHeader = new OrderHeader() // Initialize OrderHeader if needed
            };
            double orderTotal = 0;
            foreach (var item in cartItems)
            {
                double itemPrice = 0;
                // Apply bulk pricing logic
                if (item.Count >= 100)
                {
                    itemPrice = item.Product.Price100;
                }
                else if (item.Count >= 50)
                {
                    itemPrice = item.Product.Price50;
                }
                else
                {
                    itemPrice = item.Product.Price;
                }
                orderTotal += itemPrice * item.Count;
            }
            shoppingCartViewModel.OrderHeader.OrderTotal = orderTotal;
            shoppingCartViewModel.OrderHeader.ApplicationUser = _unitOfWork.ApplicationUser.Get(u => u.Id == userId);
            shoppingCartViewModel.OrderHeader.Name = shoppingCartViewModel.OrderHeader.ApplicationUser.Name;
            shoppingCartViewModel.OrderHeader.StreetAddress = shoppingCartViewModel.OrderHeader.ApplicationUser.StreetAddress;
            shoppingCartViewModel.OrderHeader.City = shoppingCartViewModel.OrderHeader.ApplicationUser.City;
            shoppingCartViewModel.OrderHeader.State = shoppingCartViewModel.OrderHeader.ApplicationUser.State;
            shoppingCartViewModel.OrderHeader.PostalCode = shoppingCartViewModel.OrderHeader.ApplicationUser.PostalCode;
            shoppingCartViewModel.OrderHeader.PhoneNumber = shoppingCartViewModel.OrderHeader.ApplicationUser.PhoneNumber;



            return View(shoppingCartViewModel);
        }
        [HttpPost]
        public IActionResult Summary(ShoppingCartViewModel shoppingCart)
        {
            try
            {
                var userId = GetCurrentUserId();
                var cartItems = _unitOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == userId, includeProperties: "Product");
                
                // Get the current user
                var applicationUser = _unitOfWork.ApplicationUser.Get(u => u.Id == userId);
                if (applicationUser == null)
                {
                    _logger.LogError("ApplicationUser not found for user ID: {UserId}", userId);
                    TempData["error"] = "User information not found. Please try again.";
                    return RedirectToAction(nameof(Index));
                }

                shoppingCart.ShoppingCartList = cartItems.ToList();
                shoppingCart.OrderHeader.OrderDate = DateTime.Now;
                shoppingCart.OrderHeader.ApplicationUserId = userId;
                
                shoppingCart.OrderHeader.OrderTotal = cartItems.Sum(item =>
                {
                    double itemPrice = 0;
                    // Apply bulk pricing logic
                    if (item.Count >= 100)
                    {
                        itemPrice = item.Product.Price100;
                    }
                    else if (item.Count >= 50)
                    {
                        itemPrice = item.Product.Price50;
                    }
                    else
                    {
                        itemPrice = item.Product.Price;
                    }
                    return itemPrice * item.Count;
                });

                // Set payment and order status based on company membership
                if (applicationUser.CompanyId.GetValueOrDefault(0) == 0)
                {
                    // Individual customer - require immediate payment
                    shoppingCart.OrderHeader.PaymentStatus = SD.PaymentStatusPending;
                    shoppingCart.OrderHeader.OrderStatus = SD.StatusPending;
                }
                else
                {
                    // Company customer - allow delayed payment
                    shoppingCart.OrderHeader.PaymentStatus = SD.PaymentStatusDelayedForApproval;
                    shoppingCart.OrderHeader.OrderStatus = SD.StatusPending;
                }

                // Save the order header
                _unitOfWork.OrderHeader.Add(shoppingCart.OrderHeader);
                _unitOfWork.Save();

                // Create order details for each cart item
                foreach (var item in shoppingCart.ShoppingCartList)
                {
                    // Calculate the correct price based on quantity (bulk pricing)
                    double itemPrice = 0;
                    if (item.Count >= 100)
                    {
                        itemPrice = item.Product.Price100;
                    }
                    else if (item.Count >= 50)
                    {
                        itemPrice = item.Product.Price50;
                    }
                    else
                    {
                        itemPrice = item.Product.Price;
                    }

                    OrderDetail orderDetail = new OrderDetail()
                    {
                        ProductId = item.ProductId,
                        OrderHeaderId = shoppingCart.OrderHeader.Id,
                        Price = itemPrice, // Use the calculated price, not always regular price
                        Count = item.Count
                    };
                    _unitOfWork.OrderDetail.Add(orderDetail);
                }
                _unitOfWork.Save();

                

                _logger.LogInformation("Order placed successfully for user {UserId}. Order ID: {OrderId}, Total: {OrderTotal:C}", 
                    userId, shoppingCart.OrderHeader.Id, shoppingCart.OrderHeader.OrderTotal);

                if (applicationUser.CompanyId.GetValueOrDefault(0) == 0)
                {
                    // Individual customer - redirect to payment
                    TempData["success"] = "Order placed successfully! Please complete your payment.";
                    
                    var option = new Stripe.Checkout.SessionCreateOptions
                    {
                        PaymentMethodTypes = new List<string> { "card" },
                        LineItems = new List<Stripe.Checkout.SessionLineItemOptions>(),
                        Mode = "payment",
                        SuccessUrl = Url.Action("OrderConfirmation", "Cart", new { id = shoppingCart.OrderHeader.Id }, Request.Scheme),
                        CancelUrl = Url.Action("Index", "Cart", null, Request.Scheme),
                    };

                    foreach (var item in shoppingCart.ShoppingCartList)
                    {
                        // Null check to prevent exception
                        if (item?.Product == null)
                        {
                            _logger.LogWarning("Product is null for cart item with ProductId: {ProductId}", item?.ProductId);
                            continue;
                        }

                        // Calculate the correct price based on quantity (bulk pricing)
                        double itemPrice = 0;
                        if (item.Count >= 100)
                        {
                            itemPrice = item.Product.Price100;
                        }
                        else if (item.Count >= 50)
                        {
                            itemPrice = item.Product.Price50;
                        }
                        else
                        {
                            itemPrice = item.Product.Price;
                        }

                        var sessionLineItem = new Stripe.Checkout.SessionLineItemOptions
                        {
                            PriceData = new Stripe.Checkout.SessionLineItemPriceDataOptions
                            {
                                Currency = "usd",
                                ProductData = new Stripe.Checkout.SessionLineItemPriceDataProductDataOptions
                                {
                                    Name = item.Product.Title ?? "Book",
                                    Description = item.Product.Description ?? $"by {item.Product.Author ?? "Unknown Author"}",
                                },
                                UnitAmount = (long)(itemPrice * 100), // Use calculated bulk price, convert to cents
                            },
                            Quantity = item.Count,
                        };
                        option.LineItems.Add(sessionLineItem);
                    }

                    var service = new Stripe.Checkout.SessionService();
                    var session = service.Create(option);
                    _unitOfWork.OrderHeader.UpdateStripePaymentId(shoppingCart.OrderHeader.Id, session.Id, session.PaymentIntentId);
                    _unitOfWork.Save();

                    // Clear the shopping cart after successful order placement
                    foreach (var item in cartItems)
                    {
                        _unitOfWork.ShoppingCart.Remove(item);
                    }
                    _unitOfWork.Save();

                    // Redirect to Stripe payment page
                    Response.Headers.Add("Location", session.Url);
                    return new StatusCodeResult(303); // 303 See Other to redirect to Stripe
                }
                else
                {
                    // Company customer - order is approved, no immediate payment needed
                    
                    // Clear the shopping cart after successful order placement
                    foreach (var item in cartItems)
                    {
                        _unitOfWork.ShoppingCart.Remove(item);
                    }
                    _unitOfWork.Save();

                    TempData["success"] = "Order placed successfully! Payment will be processed according to your company terms.";
                    return RedirectToAction("OrderConfirmation", new { id = shoppingCart.OrderHeader.Id });
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access during order placement");
                TempData["error"] = ex.Message;
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while placing order for user {UserId}", GetCurrentUserId());
                TempData["error"] = "An error occurred while placing your order. Please try again.";
                
                // Reload the summary page with the original data
                return RedirectToAction(nameof(Summary));
            }
        }

        /// <summary>
        /// Order confirmation page
        /// </summary>
        public IActionResult OrderConfirmation(int id)
        {
            var orderHeader = _unitOfWork.OrderHeader.Get(u => u.Id == id, includeProperties: "ApplicationUser");
            if (orderHeader == null)
            {
                TempData["error"] = "Order not found.";
                return RedirectToAction("Index", "Home");
            }

            var orderDetails = _unitOfWork.OrderDetail.GetAll(u => u.OrderHeaderId == id, includeProperties: "Product");

            if(orderHeader.PaymentStatus != SD.PaymentStatusDelayedForApproval)
            {
                var service = new Stripe.Checkout.SessionService();
                var session = service.Get(orderHeader.SessionId);
                if (session.PaymentStatus.ToLower() == "paid")
                {
                    // Update order status to completed
                    _unitOfWork.OrderHeader.UpdateStripePaymentId(id, session.Id, session.PaymentIntentId);
                    _unitOfWork.OrderHeader.UpdateStatus(id, SD.StatusApproved, SD.PaymentStatusApproved);
                    _unitOfWork.Save();
                    
                    // Refresh order header after status update
                    orderHeader = _unitOfWork.OrderHeader.Get(u => u.Id == id, includeProperties: "ApplicationUser");
                    
                    TempData["success"] = "Order placed successfully! Thank you for your payment.";
                }
                else
                {
                    // If payment is not completed, update the order status to pending
                    _unitOfWork.OrderHeader.UpdateStatus(id, SD.StatusPending, SD.PaymentStatusPending);
                    _unitOfWork.Save();
                    
                    // Refresh order header after status update
                    orderHeader = _unitOfWork.OrderHeader.Get(u => u.Id == id, includeProperties: "ApplicationUser");
                    
                    TempData["warning"] = "Order placed but payment is still pending. You can complete your payment from the 'My Orders' section.";
                }
            }
            else
            {
                // If payment is delayed, just update the order status to pending
                _unitOfWork.OrderHeader.UpdateStatus(id, SD.StatusPending);
                _unitOfWork.Save();
                
                // Refresh order header after status update
                orderHeader = _unitOfWork.OrderHeader.Get(u => u.Id == id, includeProperties: "ApplicationUser");
                
                TempData["success"] = "Order placed successfully! You can complete payment later from the 'My Orders' section.";
            }

            // Create view model for order confirmation
            var orderViewModel = new OrderViewModel
            {
                OrderHeader = orderHeader,
                OrderDetails = orderDetails
            };

            return View(orderViewModel);
        }

        /// <summary>
        /// Retry payment for failed orders - redirect to Order area PayNow
        /// </summary>
        public IActionResult RetryPayment(int id)
        {
            try
            {
                var userId = GetCurrentUserId();
                
                // Verify order belongs to current user
                var orderHeader = _unitOfWork.OrderHeader.Get(
                    u => u.Id == id && u.ApplicationUserId == userId, 
                    includeProperties: "ApplicationUser"
                );

                if (orderHeader == null)
                {
                    TempData["error"] = "Order not found or access denied.";
                    return RedirectToAction("Index", "Home");
                }

                // Redirect to Order controller's PayNow action
                return RedirectToAction("PayNow", "Order", new { area = "Customer", id = id });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access during payment retry");
                TempData["error"] = ex.Message;
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrying payment for order {OrderId}", id);
                TempData["error"] = "An error occurred while processing payment retry.";
                return RedirectToAction("Index", "Home");
            }
        }
    }
}
