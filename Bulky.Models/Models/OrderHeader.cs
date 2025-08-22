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
    public class OrderHeader
    {
        public int Id { get; set; }
        public string ApplicationUserId { get; set; }
        [ForeignKey("ApplicationUserId")]
        [ValidateNever]
        public ApplicationUser ApplicationUser { get; set; }
        public DateTime OrderDate { get; set; }
        public DateTime ShippingDate { get; set; }
        public double OrderTotal { get; set; }
        public string? OrderStatus { get; set; } // Pending, Approved, Shipped, Cancelled
        public string? PaymentStatus { get; set; } // Pending, Approved, Failed
        public string? TrackingNumber { get; set; }
        public string? Carrier { get; set; } // FedEx, UPS, USPS
        public DateTime PaymentDate { get; set; }
        public DateOnly PaymentDueDate { get; set; }
        public string?  SessionId { get; set; } // For Stripe payment integration
        public string? PaymentIntentId { get; set; } // For Stripe payment integration
        [Required]
        public string Name { get; set; } // Name of the person placing the order
      
        public string? StreetAddress { get; set; } // Shipping address
        
        public string? City { get; set; } // Shipping city
        
        public string? State { get; set; } // Shipping state
       
        public string? PostalCode { get; set; } // Shipping postal code
        
        public string? PhoneNumber { get; set; } // Contact phone number

    }
}
