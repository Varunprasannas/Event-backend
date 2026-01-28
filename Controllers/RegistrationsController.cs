using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using EventBookingAPI.Data;
using EventBookingAPI.Models;
using System.Security.Claims;

namespace EventBookingAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class RegistrationsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly Services.INotificationService _notificationService;

        public RegistrationsController(ApplicationDbContext context, Services.INotificationService notificationService)
        {
            _context = context;
            _notificationService = notificationService;
        }

        [HttpPost]
        public async Task<ActionResult<Registration>> RegisterForEvent([FromBody] BookingRequest request)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            
            var eventItem = await _context.Events.FindAsync(request.EventId);
            if (eventItem == null) return NotFound("Event not found");

            if (await _context.Registrations.AnyAsync(r => r.UserId == userId && r.EventId == request.EventId))
            {
                return BadRequest("Already registered for this event");
            }

            if (eventItem.MaxSeats - request.Quantity < 0)
            {
                return BadRequest("Not enough seats available");
            }

            // Deduct seats? In a real app, yes. For now, we just check MaxSeats.
            // eventItem.MaxSeats -= request.Quantity;

            var ticketCode = "TKT-" + Guid.NewGuid().ToString().Substring(0, 8).ToUpper();
            var totalPrice = eventItem.Price * request.Quantity;

            var registration = new Registration
            {
                UserId = userId,
                EventId = request.EventId,
                TicketCode = ticketCode,
                Quantity = request.Quantity,
                TotalPrice = totalPrice
            };

            _context.Registrations.Add(registration);
            await _context.SaveChangesAsync();

            var userEmail = User.FindFirst(ClaimTypes.Email)?.Value;
            if(!string.IsNullOrEmpty(userEmail)) {
                 _notificationService.SendTicketConfirmation(userEmail, ticketCode, eventItem.Title);
            }

            return CreatedAtAction(nameof(GetRegistration), new { id = registration.RegistrationId }, registration);
        }

        public class BookingRequest
        {
            public int EventId { get; set; }
            public int Quantity { get; set; }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Registration>> GetRegistration(int id)
        {
            var registration = await _context.Registrations
                .Include(r => r.Event)
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.RegistrationId == id);

            if (registration == null) return NotFound();

            return registration;
        }

        [HttpGet("mybookings")]
        public async Task<ActionResult<IEnumerable<Registration>>> GetMyBookings()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            return await _context.Registrations
                .Include(r => r.Event)
                .Include(r => r.User) // Include User for QR Code
                .Where(r => r.UserId == userId)
                .ToListAsync();
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("scan")]
        public async Task<IActionResult> ScanTicket([FromBody] ScanRequest request)
        {
            var registration = await _context.Registrations
                .Include(r => r.User)
                .Include(r => r.Event)
                .FirstOrDefaultAsync(r => r.TicketCode == request.TicketCode);

            if (registration == null)
            {
                return NotFound(new { message = "Invalid Ticket Code" });
            }

            if (registration.IsScanned)
            {
                return BadRequest(new { 
                    message = "Ticket Already Used", 
                    registration.User.Name, 
                    Event = registration.Event.Title, 
                    registration.Quantity,
                    ScannedAt = registration.RegisteredDate // Placeholder, ideally add ScannedDate
                });
            }

            // Mark as Scanned
            registration.IsScanned = true;
            await _context.SaveChangesAsync();

            return Ok(new { 
                message = "Ticket Verified Successfully", 
                registration.User.Name, 
                Event = registration.Event.Title, 
                registration.Quantity,
                Status = "Active -> Used"
            });
        }

        public class ScanRequest
        {
            public string TicketCode { get; set; }
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("event/{eventId}")]
        public async Task<ActionResult<IEnumerable<Registration>>> GetEventRegistrations(int eventId)
        {
            return await _context.Registrations
                .Include(r => r.User)
                .Where(r => r.EventId == eventId)
                .ToListAsync();
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("all")]
        public async Task<ActionResult<IEnumerable<Registration>>> GetAllRegistrations()
        {
            return await _context.Registrations
                .Include(r => r.User)
                .Include(r => r.Event)
                .OrderByDescending(r => r.RegisteredDate)
                .ToListAsync();
        }
    }
}
