using Bulky.DataAccess.Data;
using Bulky.DataAccess.Repository;
using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;
using Bulky.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BulkyWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class CategoryController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        public CategoryController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;

        }
        public IActionResult Index()
        {
            // Fetch categories from the database
            var categories =_unitOfWork.Category.GetAll();
            return View(categories);
        }
        public IActionResult Create()
        {
            return View();
        }
        [HttpPost]
        public IActionResult Create(Category category)
        {
            if (ModelState.IsValid)
            {
                // Add the new category to the database
                _unitOfWork.Category.Add(category);
                _unitOfWork.Category.Save();
                return RedirectToAction("Index");
            }
            
            return View(category);
        }
        public IActionResult Edit(int? id)
        {
            if(id is null || id==0)
            {
                return NotFound();
            }
            var category = _unitOfWork.Category.Get(c => c.Id == id);
            if(category is null)
            {
                return NotFound();
            }
            return View(category);
        }
        [HttpPost]
        public IActionResult Edit(Category category)
        {
            if (ModelState.IsValid)
            {
                // Update the existing category in the database
                _unitOfWork.Category.Update(category);
                _unitOfWork.Category.Save();

                return RedirectToAction("Index");
            }

            return View(category);
        }

        public IActionResult Delete(int? id)
        {
            if (id is null || id == 0)
            {
                return NotFound();
            }
            var category = _unitOfWork.Category.Get(c => c.Id == id);
            if (category is null)
            {
                return NotFound();
            }
            return View(category);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            var category = _unitOfWork.Category.Get(c => c.Id == id);
            if (category != null)
            {
                _unitOfWork.Category.Remove(category);
                _unitOfWork.Category.Save();
                TempData["success"] = "Category deleted successfully!";
            }
            return RedirectToAction("Index");
        }
    }
}
