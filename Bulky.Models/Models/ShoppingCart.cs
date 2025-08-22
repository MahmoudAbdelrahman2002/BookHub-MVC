using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bulky.Models.Models
{
    public class ShoppingCart
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        [Range(1, 1000,ErrorMessage ="must be between 1 and 1000")]
        public int Count { get; set; }
        [ForeignKey("ProductId")]
        public int ProductId { get; set; }
        [ValidateNever]
        public Product Product { get; set; }
        [ForeignKey("ApplicationUserId")]

        [ValidateNever]
        public string ApplicationUserId { get; set; }
        [ValidateNever]

        public ApplicationUser? ApplicationUser { get; set; }
    }
}
