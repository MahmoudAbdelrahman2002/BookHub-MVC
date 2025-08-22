using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bulky.Models.Models
{
    public class Product
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public string Title { get; set; }
        public string Description { get; set; }
        [Required]
        public string ISBN { get; set; }
        [Required]
        public string Author { get; set; }

        [Required]
        [Display(Name ="List Price")]
        [Range(1, 1000, ErrorMessage = "List Price must be between 1 and 1000")]
        public double ListPrice { get; set; }
        [Required]
        [Display(Name ="Price [1-50]")]
        [Range(1, 1000, ErrorMessage = "Price must be between 1 and 1000")]
        public double Price { get; set; }

        [Required]
        [Display(Name = "Price [50-100]")]
        [Range(1, 1000, ErrorMessage = "Price must be between 1 and 1000")]
        public double Price50 { get; set; }
        [Required]
        [Display(Name = "Price [100+]")]
        [Range(1, 1000, ErrorMessage = "Price must be between 1 and 1000")]
        public double Price100 { get; set; }

        [Display(Name = "Image URL")]
        [ValidateNever]
        public string? ImageUrl { get; set; }
        [ValidateNever]
        public Category Category { get; set; }
        [Required]
        public int CategoryId { get; set; }


    }
}
