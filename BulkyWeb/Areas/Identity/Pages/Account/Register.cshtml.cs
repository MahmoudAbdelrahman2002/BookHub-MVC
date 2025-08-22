// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading;
using System.Threading.Tasks;
using Bulky.Models.Models;
using Bulky.Utility;
using Bulky.DataAccess.Repository.IRepository;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;

namespace BulkyWeb.Areas.Identity.Pages.Account
{
    public class RegisterModel : PageModel
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IUserStore<IdentityUser> _userStore;
        private readonly IUserEmailStore<IdentityUser> _emailStore;
        private readonly ILogger<RegisterModel> _logger;
        private readonly IEmailSender _emailSender;
        private readonly IUnitOfWork _unitOfWork;

        public RegisterModel(
            UserManager<IdentityUser> userManager,
            IUserStore<IdentityUser> userStore,
            SignInManager<IdentityUser> signInManager,
            RoleManager<IdentityRole> roleManager,
            ILogger<RegisterModel> logger,
            IEmailSender emailSender,
            IUnitOfWork unitOfWork)
        {
            _userManager = userManager;
            _userStore = userStore;
            _emailStore = GetEmailStore();
            _signInManager = signInManager;
            _logger = logger;
            _emailSender = emailSender;
            _roleManager = roleManager;
            _unitOfWork = unitOfWork;
        }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [BindProperty]
        public InputModel Input { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public string ReturnUrl { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public class InputModel
        {
            /// <summary>
            ///     Email address for the user account
            /// </summary>
            [Required]
            [EmailAddress]
            [Display(Name = "Email")]
            public string Email { get; set; }

            /// <summary>
            ///     Password for the user account
            /// </summary>
            [Required]
            [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "Password")]
            public string Password { get; set; }

            /// <summary>
            ///     Password confirmation for validation
            /// </summary>
            [DataType(DataType.Password)]
            [Display(Name = "Confirm password")]
            [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
            public string ConfirmPassword { get; set; }

            /// <summary>
            ///     User role selection
            /// </summary>
            [Required]
            [Display(Name = "Role")]
            public string Role { get; set; }

            /// <summary>
            ///     Available roles for selection
            /// </summary>
            [ValidateNever]
            public IEnumerable<SelectListItem> RoleList { get; set; }

            /// <summary>
            ///     Full name of the user
            /// </summary>
            [Required]
            [Display(Name = "Full Name")]
            [StringLength(100, ErrorMessage = "The {0} must be at most {1} characters long.")]
            public string Name { get; set; }

            /// <summary>
            ///     Street address for shipping/billing (optional)
            /// </summary>
            [Display(Name = "Street Address")]
            [StringLength(200, ErrorMessage = "The {0} must be at most {1} characters long.")]
            public string? StreetAddress { get; set; }

            /// <summary>
            ///     City for address (optional)
            /// </summary>
            [Display(Name = "City")]
            [StringLength(50, ErrorMessage = "The {0} must be at most {1} characters long.")]
            public string? City { get; set; }

            /// <summary>
            ///     State/Province for address (optional)
            /// </summary>
            [Display(Name = "State")]
            [StringLength(50, ErrorMessage = "The {0} must be at most {1} characters long.")]
            public string? State { get; set; }

            /// <summary>
            ///     Postal/ZIP code for address (optional)
            /// </summary>
            [Display(Name = "Postal Code")]
            [StringLength(20, ErrorMessage = "The {0} must be at most {1} characters long.")]
            public string? PostalCode { get; set; }

            /// <summary>
            ///     Company ID for company users (required when role is Company)
            /// </summary>
            [Display(Name = "Company")]
            public int? CompanyId { get; set; }

            /// <summary>
            ///     Available companies for selection
            /// </summary>
            [ValidateNever]
            public IEnumerable<SelectListItem> CompanyList { get; set; }
        }

        public async Task OnGetAsync(string returnUrl = null)
        {
            try
            {
                // Ensure roles exist
                await EnsureRolesExist();

                // For public registration, only allow Customer role
                // Remove role selection - users will automatically be assigned Customer role
                Input = new InputModel
                {
                    Role = SD.Role_User_Cust, // Pre-set to Customer role
                    CompanyList = new List<SelectListItem>() // Empty company list since public users can't be company users
                };

                ReturnUrl = returnUrl;
                ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

                _logger.LogInformation("Registration page loaded successfully - Public registration limited to Customer role");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while loading registration page");
                throw;
            }
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            
            try
            {
                ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
                
                // Force Customer role for public registration
                Input.Role = SD.Role_User_Cust;
                Input.CompanyId = null; // Public users cannot be assigned to companies
                
                // Repopulate company list (empty for public registration)
                Input.CompanyList = new List<SelectListItem>();

                // Skip company validation since public users are always Customer role
                
                if (ModelState.IsValid)
                {
                    var user = CreateUser();

                    // Set basic Identity properties
                    await _userStore.SetUserNameAsync(user, Input.Email, CancellationToken.None);
                    await _emailStore.SetEmailAsync(user, Input.Email, CancellationToken.None);

                    // Set ApplicationUser specific properties for database storage
                    user.Name = Input.Name.Trim();
                    user.StreetAddress = string.IsNullOrWhiteSpace(Input.StreetAddress) ? null : Input.StreetAddress.Trim();
                    user.City = string.IsNullOrWhiteSpace(Input.City) ? null : Input.City.Trim();
                    user.State = string.IsNullOrWhiteSpace(Input.State) ? null : Input.State.Trim();
                    user.PostalCode = string.IsNullOrWhiteSpace(Input.PostalCode) ? null : Input.PostalCode.Trim();
                    user.CompanyId = null; // Public registration cannot assign company

                    var result = await _userManager.CreateAsync(user, Input.Password);

                    if (result.Succeeded)
                    {
                        _logger.LogInformation("User created a new account with password.");

                        // Always assign Customer role for public registration
                        await _userManager.AddToRoleAsync(user, SD.Role_User_Cust);

                        var userId = await _userManager.GetUserIdAsync(user);
                        var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                        code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                        var callbackUrl = Url.Page(
                            "/Account/ConfirmEmail",
                            pageHandler: null,
                            values: new { area = "Identity", userId = userId, code = code },
                            protocol: Request.Scheme);

                        await _emailSender.SendEmailAsync(Input.Email, "Confirm your email",
                            $"Please confirm your account by <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>clicking here</a>.");

                        if (_userManager.Options.SignIn.RequireConfirmedAccount)
                        {
                            return RedirectToPage("RegisterConfirmation", new { email = Input.Email, returnUrl = returnUrl });
                        }
                        else
                        {
                            await _signInManager.SignInAsync(user, isPersistent: false);
                            return LocalRedirect(returnUrl);
                        }
                    }
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                }

                _logger.LogWarning("Registration failed with validation errors: {Errors}", 
                    string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));

                // If we got this far, something failed, redisplay form
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred during user registration");
                ModelState.AddModelError(string.Empty, "An error occurred while creating your account. Please try again.");
                return Page();
            }
        }

        /// <summary>
        /// API endpoint to get companies list for AJAX calls
        /// </summary>
        public async Task<IActionResult> OnGetCompaniesAsync()
        {
            try
            {
                var companies = _unitOfWork.Company.GetAll().Select(c => new { 
                    value = c.Id.ToString(), 
                    text = c.Name,
                    details = new {
                        address = c.StreetAddress,
                        city = c.City,
                        state = c.State,
                        phone = c.PhoneNumber
                    }
                }).OrderBy(c => c.text);

                return new JsonResult(companies);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching companies list");
                return new JsonResult(new { error = "Failed to load companies" });
            }
        }

        /// <summary>
        /// Ensures all required roles exist in the database
        /// </summary>
        private async Task EnsureRolesExist()
        {
            var rolesToCreate = new[]
            {
                SD.Role_User_Cust,
                SD.Role_User_Comp,
                SD.Role_Admin,
                SD.Role_Employee
            };

            foreach (var roleName in rolesToCreate)
            {
                if (!await _roleManager.RoleExistsAsync(roleName))
                {
                    var role = new IdentityRole(roleName);
                    var result = await _roleManager.CreateAsync(role);
                    
                    if (result.Succeeded)
                    {
                        _logger.LogInformation($"Successfully created role: {roleName}");
                    }
                    else
                    {
                        _logger.LogError($"Failed to create role {roleName}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                    }
                }
            }
        }

        private ApplicationUser CreateUser()
        {
            try
            {
                return Activator.CreateInstance<ApplicationUser>();
            }
            catch
            {
                throw new InvalidOperationException($"Can't create an instance of '{nameof(ApplicationUser)}'. " +
                    $"Ensure that '{nameof(ApplicationUser)}' is not an abstract class and has a parameterless constructor, or alternatively " +
                    $"override the register page in /Areas/Identity/Pages/Account/Register.cshtml");
            }
        }

        private IUserEmailStore<IdentityUser> GetEmailStore()
        {
            if (!_userManager.SupportsUserEmail)
            {
                throw new NotSupportedException("The default UI requires a user store with email support.");
            }
            return (IUserEmailStore<IdentityUser>)_userStore;
        }
    }
}
