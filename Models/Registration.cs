using System;
using System.ComponentModel.DataAnnotations;

namespace EventBookingAPI.Models
{
    public class Registration
    {
        [Key]
        public int RegistrationId { get; set; }
        public int UserId { get; set; }
        public int EventId { get; set; }
        public string TicketCode { get; set; }
        public int Quantity { get; set; } = 1; // New
        public decimal TotalPrice { get; set; } // New
        public DateTime RegisteredDate { get; set; } = DateTime.Now;
        public bool IsScanned { get; set; } = false; // New: Track usage
        
        public User User { get; set; }
        public Event Event { get; set; }
    }
}
