using Bulky.Models.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BulkyWeb.Areas.Admin.Models
{
    public class RoleManagementViewModel
    {
        public ApplicationUser ApplicationUser { get; set; } = new ApplicationUser();
        public string Role { get; set; } = string.Empty; // Role property for the form
        public int? CompanyId { get; set; } // CompanyId property for the form
        public IEnumerable<SelectListItem> RoleList { get; set; } = new List<SelectListItem>();
        public IEnumerable<SelectListItem> CompanyList { get; set; } = new List<SelectListItem>();
    }
}