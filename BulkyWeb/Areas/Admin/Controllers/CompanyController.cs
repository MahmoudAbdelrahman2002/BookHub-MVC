using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BulkyWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class CompanyController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        public CompanyController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        // GET: CompanyController
        public ActionResult Index()
        {

            var companies = _unitOfWork.Company.GetAll();
            return View(companies);
        }

        // GET: CompanyController/Details/5
        public ActionResult Details(int id)
        {
            return View();
        }

        // GET: CompanyController/Create
        public ActionResult Create()
        {
            // Initialize a new company object or view model if needed
            var company = new Company();
            return View(company);
        }

        // POST: CompanyController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(IFormCollection collection)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    // Create a new company object and populate it with data from the form
                    var company = new Company
                    {
                        Name = collection["Name"],
                        StreetAddress = collection["StreetAddress"],
                        City = collection["City"],
                        State = collection["State"],
                        PostalCode = collection["PostalCode"],
                        PhoneNumber = collection["PhoneNumber"]
                    };
                    // Add the new company to the database
                    _unitOfWork.Company.Add(company);
                    _unitOfWork.Save();
                    return RedirectToAction(nameof(Index));
                }
                return View(collection);


            }
            catch
            {
                // Handle any errors that occur during the creation process
                ModelState.AddModelError("", "An error occurred while creating the company.");
                return View(collection);

            }
        }

        // GET: CompanyController/Edit/5
        public ActionResult Edit(int id)
        {
            // Fetch the company by id from the database
            var company = _unitOfWork.Company.Get(c => c.Id == id);
            if(company is null)
            {
              return NotFound();
            }

            return View(company);
        }

        // POST: CompanyController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id, IFormCollection collection)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    // Fetch the existing company from the database
                    var company = _unitOfWork.Company.Get(c => c.Id == id);
                    if (company == null)
                    {
                        return NotFound();
                    }
                    // Update the company properties with data from the form
                    company.Name = collection["Name"];
                    company.StreetAddress = collection["StreetAddress"];
                    company.City = collection["City"];
                    company.State = collection["State"];
                    company.PostalCode = collection["PostalCode"];
                    company.PhoneNumber = collection["PhoneNumber"];
                    // Save changes to the database
                    _unitOfWork.Company.Update(company);
                    _unitOfWork.Save();
                    
                    return RedirectToAction(nameof(Index));
                }
                return View(collection);



            }
            catch
            {
                // Handle any errors that occur during the edit process
                ModelState.AddModelError("", "An error occurred while updating the company.");
                return View(collection);

            }
        }

        // GET: CompanyController/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: CompanyController/Delete/5
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
