using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BulkyWeb.Areas.Admin.Models
{
    public class CreateUserViewModel
    {
        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Role")]
        public string Role { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Full Name")]
        [StringLength(100, ErrorMessage = "The {0} must be at most {1} characters long.")]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "Phone Number")]
        [Phone]
        [StringLength(20, ErrorMessage = "The {0} must be at most {1} characters long.")]
        public string? PhoneNumber { get; set; }

        [Display(Name = "Street Address")]
        [StringLength(200, ErrorMessage = "The {0} must be at most {1} characters long.")]
        public string? StreetAddress { get; set; }

        [Display(Name = "City")]
        [StringLength(50, ErrorMessage = "The {0} must be at most {1} characters long.")]
        public string? City { get; set; }

        [Display(Name = "State")]
        [StringLength(50, ErrorMessage = "The {0} must be at most {1} characters long.")]
        public string? State { get; set; }

        [Display(Name = "Postal Code")]
        [StringLength(20, ErrorMessage = "The {0} must be at most {1} characters long.")]
        public string? PostalCode { get; set; }

        [Display(Name = "Company")]
        public int? CompanyId { get; set; }

        public IEnumerable<SelectListItem>? RoleList { get; set; }
        public IEnumerable<SelectListItem>? CompanyList { get; set; }
    }
}