using System.Diagnostics;
using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;
using Bulky.Models.Models;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace BulkyWeb.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IUnitOfWork _unitOfWork;

        public HomeController(ILogger<HomeController> logger, IUnitOfWork unitOfWork)
        {
            _logger = logger;
            _unitOfWork = unitOfWork;
        }

        public IActionResult Index()
        {
            var products = _unitOfWork.Product.GetAll(includeProperties: "Category");
            return View(products);
        }

        public IActionResult Details(int id)
        {
            var cart = new ShoppingCart()
            {
                Product = _unitOfWork.Product.Get(u => u.Id == id, includeProperties: "Category"),
                Count = 1,
                ProductId = id
            };

            if (cart.Product == null)
            {
                return NotFound();
            }

            return View(cart);
        }

        [HttpPost]
        [Authorize] // ← CRITICAL FIX: Added missing [Authorize] attribute
        public IActionResult Details(ShoppingCart shoppingCart)
        {
            try
            {
                // Get the current user's ID with null checks
                var claimsIdentity = (ClaimsIdentity)User.Identity;
                var userIdClaim = claimsIdentity?.FindFirst(ClaimTypes.NameIdentifier);
                
                if (userIdClaim == null || string.IsNullOrEmpty(userIdClaim.Value))
                {
                    _logger.LogWarning("User ID not found in claims. User may not be properly authenticated. IsAuthenticated: {IsAuthenticated}, Identity: {Identity}", 
                        User.Identity?.IsAuthenticated, User.Identity?.Name);
                    
                    // Log all available claims for debugging
                    if (claimsIdentity?.Claims != null)
                    {
                        var claimsInfo = string.Join(", ", claimsIdentity.Claims.Select(c => $"{c.Type}:{c.Value}"));
                        _logger.LogWarning("Available claims: {Claims}", claimsInfo);
                    }
                    
                    TempData["error"] = "Authentication error. Please log in again.";
                    return RedirectToAction("Login", "Account", new { area = "Identity" });
                }
                
                var userId = userIdClaim.Value;
                shoppingCart.ApplicationUserId = userId;

                // Validate the shopping cart
                if (!ModelState.IsValid)
                {
                    // Reload the product if validation fails
                    shoppingCart.Product = _unitOfWork.Product.Get(u => u.Id == shoppingCart.ProductId, includeProperties: "Category");
                    return View(shoppingCart);
                }

                // Check if the product exists
                var product = _unitOfWork.Product.Get(u => u.Id == shoppingCart.ProductId);
                if (product == null)
                {
                    _logger.LogWarning($"Product with ID {shoppingCart.ProductId} not found");
                    TempData["error"] = "Product not found.";
                    return RedirectToAction(nameof(Index));
                }

                // Check if item already exists in cart for this user
                ShoppingCart cartFromDb = _unitOfWork.ShoppingCart.Get(u => u.ApplicationUserId == userId && 
                                                                          u.ProductId == shoppingCart.ProductId);

                if (cartFromDb != null)
                {
                    // Update existing cart item
                    cartFromDb.Count += shoppingCart.Count;
                    _unitOfWork.ShoppingCart.Update(cartFromDb);
                    TempData["success"] = $"Cart updated successfully! Added {shoppingCart.Count} more items.";
                    _logger.LogInformation($"Updated cart item for user {userId}: Product {shoppingCart.ProductId}, new total count: {cartFromDb.Count}");
                }
                else
                {
                    // Add new cart item - ensure Id is 0 for new records
                    shoppingCart.Id = 0;
                    _unitOfWork.ShoppingCart.Add(shoppingCart);
                    TempData["success"] = $"Item added to cart successfully! ({shoppingCart.Count} items)";
                    _logger.LogInformation($"Added new cart item for user {userId}: Product {shoppingCart.ProductId}, count: {shoppingCart.Count}");
                }

                _unitOfWork.Save();

                // Add a flag to trigger cart animation on redirect
                TempData["CartUpdated"] = true;
                TempData["AddedCount"] = shoppingCart.Count;

                // Redirect to cart or back to products
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while adding item to cart for product {ProductId}", shoppingCart.ProductId);
                TempData["error"] = "An error occurred while adding the item to cart. Please try again.";
                
                // Reload the product for the view
                shoppingCart.Product = _unitOfWork.Product.Get(u => u.Id == shoppingCart.ProductId, includeProperties: "Category");
                return View(shoppingCart);
            }
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
