using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models.Models;
using Bulky.Models.ViewModels;
using Bulky.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.IO;

namespace BulkyWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class ProductController : Controller
    {       
        private readonly IUnitOfWork _unitOfWork;
        private readonly IWebHostEnvironment _webHostEnvironment;
        
        // Constructor
        public ProductController(IUnitOfWork unitOfWork, IWebHostEnvironment webHostEnvironment)
        {
            _unitOfWork = unitOfWork;
            _webHostEnvironment = webHostEnvironment;
        }
        
        // GET: ProductController
        public ActionResult Index()
        {
            var products = _unitOfWork.Product.GetAll(includeProperties:"Category");
            return View(products);
        }

        // GET: ProductController/Details/5
        public ActionResult Details(int id)
        {
            var product = _unitOfWork.Product.Get(p => p.Id == id, "Category");
            if (product == null)
            {
                return NotFound();
            }
            return View(product);
        }

        // GET: ProductController/Create
        public ActionResult Create()
        {
            ProductVM productVM = new ProductVM()
            {
                Product = new Product(),
                CategoryList = _unitOfWork.Category.GetAll().Select(c => new SelectListItem
                {
                    Text = c.Name,
                    Value = c.Id.ToString()
                })
            };

            return View(productVM);
        }

        // POST: ProductController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(ProductVM productVM, IFormFile? imageFile)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    // Handle image upload
                    if (imageFile != null && imageFile.Length > 0)
                    {
                        string wwwRootPath = _webHostEnvironment.WebRootPath;
                        string fileName = Guid.NewGuid().ToString() + Path.GetExtension(imageFile.FileName);
                        string productPath = Path.Combine(wwwRootPath, @"Images\Products");

                        // Create directory if it doesn't exist
                        if (!Directory.Exists(productPath))
                        {
                            Directory.CreateDirectory(productPath);
                        }

                        using (var fileStream = new FileStream(Path.Combine(productPath, fileName), FileMode.Create))
                        {
                            imageFile.CopyTo(fileStream);
                        }

                        productVM.Product.ImageUrl = @"\Images\Products\" + fileName;
                    }

                    _unitOfWork.Product.Add(productVM.Product);
                    _unitOfWork.Save();
                    TempData["success"] = "Product created successfully!";
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    // If validation fails, repopulate the CategoryList
                    productVM.CategoryList = _unitOfWork.Category.GetAll().Select(c => new SelectListItem
                    {
                        Text = c.Name,
                        Value = c.Id.ToString()
                    });
                    
                    return View(productVM);
                }
            }
            catch
            {
                // If an error occurs, repopulate the CategoryList
                productVM.CategoryList = _unitOfWork.Category.GetAll().Select(c => new SelectListItem
                {
                    Text = c.Name,
                    Value = c.Id.ToString()
                });
                
                return View(productVM);
            }
        }

        // GET: ProductController/Edit/5
        public ActionResult Edit(int id)
        {
            var product = _unitOfWork.Product.Get(p => p.Id == id);
            if (product == null)
            {
                return NotFound();
            }

            ProductVM productVM = new ProductVM()
            {
                Product = product,
                CategoryList = _unitOfWork.Category.GetAll().Select(c => new SelectListItem
                {
                    Text = c.Name,
                    Value = c.Id.ToString()
                })
            };

            return View(productVM);
        }

        // POST: ProductController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(ProductVM productVM, IFormFile? imageFile)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    // Handle image upload
                    if (imageFile != null && imageFile.Length > 0)
                    {
                        string wwwRootPath = _webHostEnvironment.WebRootPath;
                        string fileName = Guid.NewGuid().ToString() + Path.GetExtension(imageFile.FileName);
                        string productPath = Path.Combine(wwwRootPath, @"Images\Products");

                        // Create directory if it doesn't exist
                        if (!Directory.Exists(productPath))
                        {
                            Directory.CreateDirectory(productPath);
                        }

                        // Delete old image if it exists
                        if (!string.IsNullOrEmpty(productVM.Product.ImageUrl))
                        {
                            var oldImagePath = Path.Combine(wwwRootPath, productVM.Product.ImageUrl.TrimStart('\\'));
                            if (System.IO.File.Exists(oldImagePath))
                            {
                                System.IO.File.Delete(oldImagePath);
                            }
                        }

                        using (var fileStream = new FileStream(Path.Combine(productPath, fileName), FileMode.Create))
                        {
                            imageFile.CopyTo(fileStream);
                        }

                        productVM.Product.ImageUrl = @"\Images\Products\" + fileName;
                    }

                    _unitOfWork.Product.Update(productVM.Product);
                    _unitOfWork.Save();
                    TempData["success"] = "Product updated successfully!";
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    // If model state is invalid, repopulate CategoryList and return the view
                    productVM.CategoryList = _unitOfWork.Category.GetAll().Select(c => new SelectListItem
                    {
                        Text = c.Name,
                        Value = c.Id.ToString()
                    });
                    
                    return View(productVM);
                }
            }
            catch
            {
                // If an error occurs, repopulate CategoryList and return the view
                productVM.CategoryList = _unitOfWork.Category.GetAll().Select(c => new SelectListItem
                {
                    Text = c.Name,
                    Value = c.Id.ToString()
                });
                
                return View(productVM);
            }
        }

        // GET: ProductController/Delete/5
        public ActionResult Delete(int id)
        {
            var product = _unitOfWork.Product.Get(p => p.Id == id, "Category");
            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        // POST: ProductController/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            try
            {
                var product = _unitOfWork.Product.Get(p => p.Id == id);
                if (product != null)
                {
                    // Delete image file if it exists
                    if (!string.IsNullOrEmpty(product.ImageUrl))
                    {
                        string wwwRootPath = _webHostEnvironment.WebRootPath;
                        var imagePath = Path.Combine(wwwRootPath, product.ImageUrl.TrimStart('\\'));
                        if (System.IO.File.Exists(imagePath))
                        {
                            System.IO.File.Delete(imagePath);
                        }
                    }

                    _unitOfWork.Product.Remove(product);
                    _unitOfWork.Save();
                    TempData["success"] = "Product deleted successfully!";
                }
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                TempData["error"] = "Error occurred while deleting the product!";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}
