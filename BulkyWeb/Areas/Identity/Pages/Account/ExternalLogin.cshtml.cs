// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Bulky.Models.Models;
using Bulky.Utility;

namespace BulkyWeb.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class ExternalLoginModel : PageModel
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IUserStore<IdentityUser> _userStore;
        private readonly IUserEmailStore<IdentityUser> _emailStore;
        private readonly IEmailSender _emailSender;
        private readonly ILogger<ExternalLoginModel> _logger;

        public ExternalLoginModel(
            SignInManager<IdentityUser> signInManager,
            UserManager<IdentityUser> userManager,
            IUserStore<IdentityUser> userStore,
            ILogger<ExternalLoginModel> logger,
            IEmailSender emailSender)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _userStore = userStore;
            _emailStore = GetEmailStore();
            _logger = logger;
            _emailSender = emailSender;
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
        public string ProviderDisplayName { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public string ReturnUrl { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [TempData]
        public string ErrorMessage { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public class InputModel
        {
            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [Required]
            [EmailAddress]
            public string Email { get; set; }
            
            [Required]
            [Display(Name = "Full Name")]
            [StringLength(100, ErrorMessage = "The {0} must be at most {1} characters long.")]
            public string Name { get; set; }

            /// <summary>
            ///     Phone number (optional)
            /// </summary>
            [Display(Name = "Phone Number")]
            [Phone]
            [StringLength(20, ErrorMessage = "The {0} must be at most {1} characters long.")]
            public string? PhoneNumber { get; set; }

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
        }

        public IActionResult OnGet() 
        {
            // Check if we have external login data in TempData (user might have refreshed the page)
            var provider = TempData.Peek("ExternalLoginProvider") as string;
            var providerDisplayName = TempData.Peek("ExternalLoginProviderDisplayName") as string;
            
            if (!string.IsNullOrEmpty(provider))
            {
                // Restore the form data
                ProviderDisplayName = providerDisplayName ?? provider;
                ReturnUrl = Request.Query["returnUrl"].FirstOrDefault() ?? Url.Content("~/");
                
                if (Input == null)
                {
                    Input = new InputModel
                    {
                        Email = TempData.Peek("ExternalLoginEmail") as string ?? string.Empty,
                        Name = TempData.Peek("ExternalLoginName") as string ?? string.Empty,
                        PhoneNumber = TempData.Peek("ExternalLoginPhoneNumber") as string,
                        StreetAddress = TempData.Peek("ExternalLoginStreetAddress") as string,
                        City = TempData.Peek("ExternalLoginCity") as string,
                        State = TempData.Peek("ExternalLoginState") as string,
                        PostalCode = TempData.Peek("ExternalLoginPostalCode") as string
                    };
                }
                
                return Page();
            }
            
            // No external login data found, redirect to login
            return RedirectToPage("./Login");
        }

        public IActionResult OnPost(string provider, string returnUrl = null)
        {
            // Request a redirect to the external login provider.
            var redirectUrl = Url.Page("./ExternalLogin", pageHandler: "Callback", values: new { returnUrl });
            var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
            return new ChallengeResult(provider, properties);
        }

        public async Task<IActionResult> OnGetCallbackAsync(string returnUrl = null, string remoteError = null)
        {
            returnUrl = returnUrl ?? Url.Content("~/");
            if (remoteError != null)
            {
                ErrorMessage = $"Error from external provider: {remoteError}";
                _logger.LogWarning("External login error: {RemoteError}", remoteError);
                return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
            }
            
            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                ErrorMessage = "Error loading external login information.";
                _logger.LogWarning("External login info was null in callback.");
                return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
            }

            // Sign in the user with this external login provider if the user already has a login.
            var result = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: false, bypassTwoFactor: true);
            if (result.Succeeded)
            {
                _logger.LogInformation("{Name} logged in with {LoginProvider} provider.", info.Principal.Identity.Name, info.LoginProvider);
                return LocalRedirect(returnUrl);
            }
            if (result.IsLockedOut)
            {
                _logger.LogWarning("User account locked out during external login.");
                return RedirectToPage("./Lockout");
            }
            else
            {
                // If the user does not have an account, then ask the user to create an account.
                ReturnUrl = returnUrl;
                ProviderDisplayName = info.ProviderDisplayName;
                
                // Store external login info in TempData to persist across the redirect
                TempData["ExternalLoginProvider"] = info.LoginProvider;
                TempData["ExternalLoginProviderKey"] = info.ProviderKey;
                TempData["ExternalLoginProviderDisplayName"] = info.ProviderDisplayName;
                
                if (info.Principal.HasClaim(c => c.Type == ClaimTypes.Email))
                {
                    var email = info.Principal.FindFirstValue(ClaimTypes.Email);
                    var name = info.Principal.FindFirstValue(ClaimTypes.Name) ?? 
                               info.Principal.FindFirstValue(ClaimTypes.GivenName) ?? 
                               info.Principal.FindFirstValue(ClaimTypes.Surname) ??
                               string.Empty;
                    
                    // Handle composite names (first + last)
                    if (string.IsNullOrEmpty(name))
                    {
                        var firstName = info.Principal.FindFirstValue(ClaimTypes.GivenName);
                        var lastName = info.Principal.FindFirstValue(ClaimTypes.Surname);
                        if (!string.IsNullOrEmpty(firstName) || !string.IsNullOrEmpty(lastName))
                        {
                            name = $"{firstName} {lastName}".Trim();
                        }
                    }
                    
                    Input = new InputModel
                    {
                        Email = email,
                        Name = name,
                        PhoneNumber = info.Principal.FindFirstValue(ClaimTypes.MobilePhone) ?? 
                                     info.Principal.FindFirstValue(ClaimTypes.HomePhone) ?? 
                                     info.Principal.FindFirstValue(ClaimTypes.OtherPhone),
                        StreetAddress = info.Principal.FindFirstValue(ClaimTypes.StreetAddress),
                        City = info.Principal.FindFirstValue(ClaimTypes.Locality),
                        State = info.Principal.FindFirstValue(ClaimTypes.StateOrProvince),
                        PostalCode = info.Principal.FindFirstValue(ClaimTypes.PostalCode)
                    };
                    
                    // Store the input data in TempData as well for fallback
                    TempData["ExternalLoginEmail"] = email;
                    TempData["ExternalLoginName"] = name;
                    TempData["ExternalLoginPhoneNumber"] = Input.PhoneNumber;
                    TempData["ExternalLoginStreetAddress"] = Input.StreetAddress;
                    TempData["ExternalLoginCity"] = Input.City;
                    TempData["ExternalLoginState"] = Input.State;
                    TempData["ExternalLoginPostalCode"] = Input.PostalCode;
                    
                    _logger.LogInformation("External login callback successful for {Email} via {Provider}", email, info.LoginProvider);
                }
                else
                {
                    _logger.LogWarning("No email claim found in external login for provider {Provider}", info.LoginProvider);
                    ErrorMessage = "Unable to retrieve email information from external provider.";
                    return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
                }
                
                return Page();
            }
        }

        public async Task<IActionResult> OnPostConfirmationAsync(string returnUrl = null)
        {
            returnUrl = returnUrl ?? Url.Content("~/");
            
            // Try to get the information about the user from the external login provider
            var info = await _signInManager.GetExternalLoginInfoAsync();
            
            // If info is null, try to reconstruct it from TempData
            if (info == null)
            {
                _logger.LogWarning("External login info was null during confirmation, attempting to recover from TempData.");
                
                var provider = TempData["ExternalLoginProvider"] as string;
                var providerKey = TempData["ExternalLoginProviderKey"] as string;
                var providerDisplayName = TempData["ExternalLoginProviderDisplayName"] as string;
                
                if (string.IsNullOrEmpty(provider) || string.IsNullOrEmpty(providerKey))
                {
                    ErrorMessage = "External login session expired. Please try logging in again.";
                    _logger.LogError("Could not recover external login information from TempData during confirmation.");
                    return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
                }
                
                // Restore data from TempData if Input is not populated
                if (Input == null || string.IsNullOrEmpty(Input.Email))
                {
                    Input = new InputModel
                    {
                        Email = TempData["ExternalLoginEmail"] as string ?? string.Empty,
                        Name = TempData["ExternalLoginName"] as string ?? string.Empty,
                        PhoneNumber = TempData["ExternalLoginPhoneNumber"] as string,
                        StreetAddress = TempData["ExternalLoginStreetAddress"] as string,
                        City = TempData["ExternalLoginCity"] as string,
                        State = TempData["ExternalLoginState"] as string,
                        PostalCode = TempData["ExternalLoginPostalCode"] as string
                    };
                }
                
                // Create a minimal ExternalLoginInfo for the external login association
                info = new ExternalLoginInfo(
                    new ClaimsPrincipal(new ClaimsIdentity(new[]
                    {
                        new Claim(ClaimTypes.Email, Input.Email ?? string.Empty),
                        new Claim(ClaimTypes.Name, Input.Name ?? string.Empty)
                    }, provider)),
                    provider,
                    providerKey,
                    providerDisplayName ?? provider
                );
                
                _logger.LogInformation("Successfully recovered external login info from TempData for {Email}", Input.Email);
            }

            if (!ModelState.IsValid)
            {
                ProviderDisplayName = info.ProviderDisplayName;
                ReturnUrl = returnUrl;
                _logger.LogWarning("Model state is invalid for external login confirmation. Errors: {Errors}", 
                    string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
                return Page();
            }

            try
            {
                // Check if user already exists
                var existingUser = await _userManager.FindByEmailAsync(Input.Email);
                if (existingUser != null)
                {
                    // User exists, try to add the external login
                    var addLoginResult = await _userManager.AddLoginAsync(existingUser, info);
                    if (addLoginResult.Succeeded)
                    {
                        await _signInManager.SignInAsync(existingUser, isPersistent: false, info.LoginProvider);
                        _logger.LogInformation("External login {Provider} added to existing user {Email}", info.LoginProvider, Input.Email);
                        return LocalRedirect(returnUrl);
                    }
                    else
                    {
                        _logger.LogError("Failed to add external login {Provider} to existing user {Email}: {Errors}", 
                            info.LoginProvider, Input.Email, string.Join(", ", addLoginResult.Errors.Select(e => e.Description)));
                        ModelState.AddModelError(string.Empty, "This email is already associated with another account. Please use a different email or sign in with your existing account.");
                        ProviderDisplayName = info.ProviderDisplayName;
                        ReturnUrl = returnUrl;
                        return Page();
                    }
                }

                // Create new user
                var user = CreateUser();

                await _userStore.SetUserNameAsync(user, Input.Email, CancellationToken.None);
                await _emailStore.SetEmailAsync(user, Input.Email, CancellationToken.None);
                
                // Set ApplicationUser specific properties with null-safe handling
                user.Name = !string.IsNullOrWhiteSpace(Input.Name) ? Input.Name.Trim() : "User";
                user.StreetAddress = string.IsNullOrWhiteSpace(Input.StreetAddress) ? null : Input.StreetAddress.Trim();
                user.City = string.IsNullOrWhiteSpace(Input.City) ? null : Input.City.Trim();
                user.PostalCode = string.IsNullOrWhiteSpace(Input.PostalCode) ? null : Input.PostalCode.Trim();
                user.State = string.IsNullOrWhiteSpace(Input.State) ? null : Input.State.Trim();
                
                // Set phone number if provided
                if (!string.IsNullOrWhiteSpace(Input.PhoneNumber))
                {
                    user.PhoneNumber = Input.PhoneNumber.Trim();
                }

                var result = await _userManager.CreateAsync(user);
                if (result.Succeeded)
                {
                    result = await _userManager.AddLoginAsync(user, info);
                    if (result.Succeeded)
                    {
                        // Assign default Customer role
                        var roleResult = await _userManager.AddToRoleAsync(user, SD.Role_User_Cust);
                        if (!roleResult.Succeeded)
                        {
                            _logger.LogWarning("Failed to assign Customer role to user {Email}: {Errors}", 
                                Input.Email, string.Join(", ", roleResult.Errors.Select(e => e.Description)));
                        }

                        _logger.LogInformation("User {Email} created an account using {Provider} provider.", Input.Email, info.LoginProvider);

                        var userId = await _userManager.GetUserIdAsync(user);
                        var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                        code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                        var callbackUrl = Url.Page(
                            "/Account/ConfirmEmail",
                            pageHandler: null,
                            values: new { area = "Identity", userId = userId, code = code },
                            protocol: Request.Scheme);

                        try
                        {
                            await _emailSender.SendEmailAsync(Input.Email, "Confirm your email",
                                $"Please confirm your account by <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>clicking here</a>.");
                        }
                        catch (Exception emailEx)
                        {
                            _logger.LogError(emailEx, "Failed to send confirmation email to {Email}", Input.Email);
                            // Continue with the registration process even if email fails
                        }

                        // If account confirmation is required, we need to show the link if we don't have a real email sender
                        if (_userManager.Options.SignIn.RequireConfirmedAccount)
                        {
                            return RedirectToPage("./RegisterConfirmation", new { Email = Input.Email });
                        }

                        await _signInManager.SignInAsync(user, isPersistent: false, info.LoginProvider);
                        
                        // Clear TempData after successful registration
                        TempData.Remove("ExternalLoginProvider");
                        TempData.Remove("ExternalLoginProviderKey");
                        TempData.Remove("ExternalLoginProviderDisplayName");
                        TempData.Remove("ExternalLoginEmail");
                        TempData.Remove("ExternalLoginName");
                        TempData.Remove("ExternalLoginPhoneNumber");
                        TempData.Remove("ExternalLoginStreetAddress");
                        TempData.Remove("ExternalLoginCity");
                        TempData.Remove("ExternalLoginState");
                        TempData.Remove("ExternalLoginPostalCode");
                        
                        return LocalRedirect(returnUrl);
                    }
                    else
                    {
                        _logger.LogError("Failed to add external login for user {Email}: {Errors}", 
                            Input.Email, string.Join(", ", result.Errors.Select(e => e.Description)));
                    }
                }
                else
                {
                    _logger.LogError("Failed to create user {Email}: {Errors}", 
                        Input.Email, string.Join(", ", result.Errors.Select(e => e.Description)));
                }
                
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred during external login confirmation for {Email}", Input.Email);
                ModelState.AddModelError(string.Empty, "An error occurred while creating your account. Please try again.");
            }

            ProviderDisplayName = info.ProviderDisplayName;
            ReturnUrl = returnUrl;
            return Page();
        }

        private ApplicationUser CreateUser()
        {
            try
            {
                return Activator.CreateInstance<ApplicationUser>();
            }
            catch
            {
                throw new InvalidOperationException($"Can't create an instance of '{nameof(IdentityUser)}'. " +
                    $"Ensure that '{nameof(IdentityUser)}' is not an abstract class and has a parameterless constructor, or alternatively " +
                    $"override the external login page in /Areas/Identity/Pages/Account/ExternalLogin.cshtml");
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
