using Bulky.Models.Models;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;

namespace Bulky.Models
{
    public class Category
    {
        [Key]
        public int Id { get; set; }
        [Required]
        [MaxLength(30)]
        public string Name { get; set; }
        [Range(1,100)]
        public int DisplayOrder { get; set; }
        [ValidateNever]
        public List<Product> Products { get; set; }
    }
}
