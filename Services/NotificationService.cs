using System;

namespace EventBookingAPI.Services
{
    public interface INotificationService
    {
        void SendTicketConfirmation(string email, string ticketCode, string eventTitle);
    }

    public class NotificationService : INotificationService
    {
        public void SendTicketConfirmation(string email, string ticketCode, string eventTitle)
        {
            // In a real app, this would use SMTP or Twilio
            Console.WriteLine($"[EMAIL SENT] To: {email} | Subject: Ticket Confirmation | Body: You booked {eventTitle}. Ticket: {ticketCode}");
            Console.WriteLine($"[SMS SENT] To: {email} | Body: Your ticket for {eventTitle} is {ticketCode}");
        }
    }
}
