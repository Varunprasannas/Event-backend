using System;
using System.ComponentModel.DataAnnotations;

namespace EventBookingAPI.Models
{
    public class Event
    {
        [Key]
        public int EventId { get; set; }
        
        [Required]
        public string Title { get; set; }
        
        public string? Description { get; set; } // Allow null
        public DateTime Date { get; set; }
        public string Venue { get; set; }
        public int MaxSeats { get; set; }
        public decimal Price { get; set; }
        public string? Category { get; set; } // Allow null
        
        // FIX: Make nullable (?) so C# doesn't complain, 
        // we will handle the DB value in the Controller.
        public string? ImageUrl { get; set; } 
        
        public int CreatedBy { get; set; }
    }
}