using System.ComponentModel.DataAnnotations;

namespace EventBookingAPI.Models
{
    public class User
    {
        [Key]
        public int UserId { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Password { get; set; } // Hashed
        public string Role { get; set; } = "User";
    }
}
