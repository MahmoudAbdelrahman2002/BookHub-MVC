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
    public class OrderDetail
    {
        public int Id { get; set; }
        [Required]
        public int OrderHeaderId { get; set; } // Foreign key to OrderHeader
        [ValidateNever]
        [ForeignKey("OrderHeaderId")]
        public OrderHeader OrderHeader { get; set; } // Navigation property to OrderHeader
        [Required]
        public int ProductId { get; set; } // Foreign key to Product
        [ValidateNever]
        [ForeignKey("ProductId")]
        public Product Product { get; set; } // Navigation property to Product
        public int Count { get; set; } // Quantity of the product in the order
        public double Price { get; set; } // Price of the product at the time of order

    }
}
