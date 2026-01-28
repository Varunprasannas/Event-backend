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
    public class EventsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public EventsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Event>>> GetEvents()
        {
            return await _context.Events.ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Event>> GetEvent(int id)
        {
            var eventItem = await _context.Events.FindAsync(id);
            if (eventItem == null) return NotFound();
            return eventItem;
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<ActionResult<Event>> CreateEvent(Event eventItem)
        {
            // Logging incoming image
            Console.WriteLine($"[BACKEND] Creating Event: {eventItem.Title}");
            Console.WriteLine($"[BACKEND] Received ImageUrl: {(string.IsNullOrEmpty(eventItem.ImageUrl) ? "(EMPTY)" : eventItem.ImageUrl)}");

            // 1. Get User ID
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null) return Unauthorized("User ID not found in token");
            
            eventItem.CreatedBy = int.Parse(userIdClaim.Value);

            // 2. Clearer placeholder if empty
            if (string.IsNullOrEmpty(eventItem.ImageUrl))
            {
                eventItem.ImageUrl = "https://images.unsplash.com/photo-1540575861501-7cf05a4b125a?w=800";
            }

            // 3. Handle other potentially null fields
            if (string.IsNullOrEmpty(eventItem.Description)) eventItem.Description = "No description provided.";
            if (string.IsNullOrEmpty(eventItem.Category)) eventItem.Category = "General";

            try 
            {
                _context.Events.Add(eventItem);
                await _context.SaveChangesAsync();
                
                Console.WriteLine($"[BACKEND] Event Created with ID: {eventItem.EventId}");
                return CreatedAtAction(nameof(GetEvent), new { id = eventItem.EventId }, eventItem);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[BACKEND] ERROR during event creation: {ex.Message}");
                return StatusCode(500, $"Internal Server Error: {ex.Message} {ex.InnerException?.Message}");
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateEvent(int id, Event eventItem)
        {
            if (id != eventItem.EventId) return BadRequest();
            _context.Entry(eventItem).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteEvent(int id)
        {
            var eventItem = await _context.Events.FindAsync(id);
            if (eventItem == null) return NotFound();
            _context.Events.Remove(eventItem);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}