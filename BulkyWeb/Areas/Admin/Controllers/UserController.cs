using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Authorization;
using Bulky.Models.Models;
using Bulky.DataAccess.Repository.IRepository;
using Bulky.Utility;
using System.Security.Claims;
using System.ComponentModel.DataAnnotations;
using BulkyWeb.Areas.Admin.Models;

namespace BulkyWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class UserController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<UserController> _logger;

        public UserController(
            UserManager<IdentityUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IUnitOfWork unitOfWork,
            ILogger<UserController> logger)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                List<ApplicationUser> objUserList = _unitOfWork.ApplicationUser.GetAll(includeProperties: "Company").ToList();
                
                var userRoles = new List<object>();
                
                foreach (ApplicationUser user in objUserList)
                {
                    var role = (await _userManager.GetRolesAsync(user)).FirstOrDefault();
                    
                    userRoles.Add(new
                    {
                        id = user.Id,
                        name = user.Name,
                        email = user.Email,
                        phoneNumber = user.PhoneNumber ?? "N/A",
                        company = user.Company?.Name ?? "N/A",
                        role = role ?? "No Role",
                        lockoutEnd = user.LockoutEnd
                    });
                }

                return Json(new { data = userRoles });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching users");
                return Json(new { error = "Failed to load users" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            try
            {
                await EnsureRolesExist();

                var viewModel = new CreateUserViewModel
                {
                    RoleList = _roleManager.Roles.Select(i => new SelectListItem
                    {
                        Text = i.Name,
                        Value = i.Name
                    }).OrderBy(x => x.Text),
                    CompanyList = _unitOfWork.Company.GetAll().Select(i => new SelectListItem
                    {
                        Text = i.Name,
                        Value = i.Id.ToString()
                    }).OrderBy(x => x.Text)
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while loading create user page");
                TempData["error"] = "An error occurred while loading the page.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateUserViewModel model)
        {
            try
            {
                // Repopulate lists if validation fails
                model.RoleList = _roleManager.Roles.Select(i => new SelectListItem
                {
                    Text = i.Name,
                    Value = i.Name
                }).OrderBy(x => x.Text);

                model.CompanyList = _unitOfWork.Company.GetAll().Select(i => new SelectListItem
                {
                    Text = i.Name,
                    Value = i.Id.ToString()
                }).OrderBy(x => x.Text);

                // Validate company selection for Company role
                if (model.Role == SD.Role_User_Comp && (!model.CompanyId.HasValue || model.CompanyId.Value <= 0))
                {
                    ModelState.AddModelError(nameof(model.CompanyId), "Please select a company when creating a Company user.");
                }

                if (!ModelState.IsValid)
                {
                    return View(model);
                }

                // Validate role exists
                if (!await _roleManager.RoleExistsAsync(model.Role))
                {
                    ModelState.AddModelError(string.Empty, "Selected role is invalid.");
                    _logger.LogWarning($"Invalid role selected during user creation: {model.Role}");
                    return View(model);
                }

                // Validate company exists if CompanyId is provided
                if (model.CompanyId.HasValue && model.CompanyId.Value > 0)
                {
                    var company = _unitOfWork.Company.Get(c => c.Id == model.CompanyId.Value);
                    if (company == null)
                    {
                        ModelState.AddModelError(nameof(model.CompanyId), "Selected company does not exist.");
                        _logger.LogWarning($"Invalid company selected during user creation: {model.CompanyId}");
                        return View(model);
                    }
                }

                // Check if email already exists
                var existingUser = await _userManager.FindByEmailAsync(model.Email);
                if (existingUser != null)
                {
                    ModelState.AddModelError(nameof(model.Email), "A user with this email already exists.");
                    return View(model);
                }

                // Create new user
                var user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    Name = model.Name.Trim(),
                    StreetAddress = string.IsNullOrWhiteSpace(model.StreetAddress) ? null : model.StreetAddress.Trim(),
                    City = string.IsNullOrWhiteSpace(model.City) ? null : model.City.Trim(),
                    State = string.IsNullOrWhiteSpace(model.State) ? null : model.State.Trim(),
                    PostalCode = string.IsNullOrWhiteSpace(model.PostalCode) ? null : model.PostalCode.Trim(),
                    PhoneNumber = string.IsNullOrWhiteSpace(model.PhoneNumber) ? null : model.PhoneNumber.Trim(),
                    CompanyId = model.Role == SD.Role_User_Comp ? model.CompanyId : null,
                    EmailConfirmed = true // Admin-created users are automatically confirmed
                };

                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    // Assign role
                    var roleResult = await _userManager.AddToRoleAsync(user, model.Role);
                    if (!roleResult.Succeeded)
                    {
                        _logger.LogError("Failed to assign role {Role} to user {Email}: {Errors}", 
                            model.Role, model.Email, string.Join(", ", roleResult.Errors.Select(e => e.Description)));
                        
                        // Delete the user if role assignment failed
                        await _userManager.DeleteAsync(user);
                        ModelState.AddModelError(string.Empty, "Failed to assign role to user.");
                        return View(model);
                    }

                    _logger.LogInformation("Admin {AdminEmail} created user {UserEmail} with role {Role}", 
                        User.FindFirstValue(ClaimTypes.Email), model.Email, model.Role);

                    TempData["success"] = $"User '{model.Name}' created successfully with role '{model.Role}'.";
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                    return View(model);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred during user creation");
                ModelState.AddModelError(string.Empty, "An error occurred while creating the user. Please try again.");
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> RoleManagement(string userId)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    TempData["error"] = "User not found.";
                    return RedirectToAction(nameof(Index));
                }

                var appUser = _unitOfWork.ApplicationUser.Get(u => u.Id == userId, includeProperties: "Company");
                var currentRole = (await _userManager.GetRolesAsync(user)).FirstOrDefault();

                var viewModel = new RoleManagementViewModel
                {
                    ApplicationUser = appUser,
                    Role = currentRole ?? string.Empty,
                    CompanyId = appUser.CompanyId,
                    RoleList = _roleManager.Roles.Select(i => new SelectListItem
                    {
                        Text = i.Name,
                        Value = i.Name,
                        Selected = i.Name == currentRole
                    }).OrderBy(x => x.Text),
                    CompanyList = _unitOfWork.Company.GetAll().Select(i => new SelectListItem
                    {
                        Text = i.Name,
                        Value = i.Id.ToString(),
                        Selected = i.Id == appUser.CompanyId
                    }).OrderBy(x => x.Text)
                };

                ViewBag.CurrentRole = currentRole;
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while loading role management for user {UserId}", userId);
                TempData["error"] = "An error occurred while loading user information.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RoleManagement(RoleManagementViewModel viewModel)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(viewModel.ApplicationUser.Id);
                if (user == null)
                {
                    TempData["error"] = "User not found.";
                    return RedirectToAction(nameof(Index));
                }

                var appUser = _unitOfWork.ApplicationUser.Get(u => u.Id == viewModel.ApplicationUser.Id);
                var oldRole = (await _userManager.GetRolesAsync(user)).FirstOrDefault();

                // Validate company selection for Company role
                if (viewModel.Role == SD.Role_User_Comp && 
                    (!viewModel.CompanyId.HasValue || viewModel.CompanyId.Value <= 0))
                {
                    TempData["error"] = "Please select a company when assigning Company role.";
                    return RedirectToAction(nameof(RoleManagement), new { userId = viewModel.ApplicationUser.Id });
                }

                // Update role if changed
                if (oldRole != viewModel.Role)
                {
                    // Remove old role
                    if (!string.IsNullOrEmpty(oldRole))
                    {
                        await _userManager.RemoveFromRoleAsync(user, oldRole);
                    }

                    // Add new role
                    await _userManager.AddToRoleAsync(user, viewModel.Role);
                }

                // Update company assignment
                if (viewModel.Role == SD.Role_User_Comp)
                {
                    appUser.CompanyId = viewModel.CompanyId;
                }
                else
                {
                    appUser.CompanyId = null;
                }

                // Since IRepository doesn't have Update, we need to save through unit of work
                _unitOfWork.Save();

                _logger.LogInformation("Admin {AdminEmail} updated user {UserEmail} role from {OldRole} to {NewRole}", 
                    User.FindFirstValue(ClaimTypes.Email), user.Email, oldRole, viewModel.Role);

                TempData["success"] = "User role updated successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating user role");
                TempData["error"] = "An error occurred while updating the user role.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        public async Task<IActionResult> LockUnlock([FromBody] string id)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(id);
                if (user == null)
                {
                    return Json(new { success = false, message = "User not found." });
                }

                if (user.LockoutEnd != null && user.LockoutEnd > DateTime.Now)
                {
                    // User is locked, unlock them
                    user.LockoutEnd = DateTime.Now;
                    await _userManager.UpdateAsync(user);
                    _logger.LogInformation("Admin {AdminEmail} unlocked user {UserEmail}", 
                        User.FindFirstValue(ClaimTypes.Email), user.Email);
                    return Json(new { success = true, message = "User unlocked successfully." });
                }
                else
                {
                    // User is unlocked, lock them for 1000 years
                    user.LockoutEnd = DateTime.Now.AddYears(1000);
                    await _userManager.UpdateAsync(user);
                    _logger.LogInformation("Admin {AdminEmail} locked user {UserEmail}", 
                        User.FindFirstValue(ClaimTypes.Email), user.Email);
                    return Json(new { success = true, message = "User locked successfully." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while toggling user lock status");
                return Json(new { success = false, message = "An error occurred while updating user status." });
            }
        }

        private async Task EnsureRolesExist()
        {
            if (!await _roleManager.RoleExistsAsync(SD.Role_User_Cust))
                await _roleManager.CreateAsync(new IdentityRole(SD.Role_User_Cust));
            if (!await _roleManager.RoleExistsAsync(SD.Role_Employee))
                await _roleManager.CreateAsync(new IdentityRole(SD.Role_Employee));
            if (!await _roleManager.RoleExistsAsync(SD.Role_Admin))
                await _roleManager.CreateAsync(new IdentityRole(SD.Role_Admin));
            if (!await _roleManager.RoleExistsAsync(SD.Role_User_Comp))
                await _roleManager.CreateAsync(new IdentityRole(SD.Role_User_Comp));
        }
    }
}