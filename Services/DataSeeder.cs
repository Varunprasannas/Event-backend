using EventBookingAPI.Data;
using EventBookingAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace EventBookingAPI.Services
{
    public static class DataSeeder
    {
        public static void Seed(ApplicationDbContext context)
        {
            try {
                // Ensure Database is created
                context.Database.EnsureCreated();
                
                // Open connection to run manual SQL schema updates
                var conn = context.Database.GetDbConnection();
                conn.Open();
                using (var cmd = conn.CreateCommand()) {
                    // --- Events Table Schema Updates ---
                    
                    // 1. Price
                    try { cmd.CommandText = "ALTER TABLE Events ADD COLUMN Price DECIMAL(18,2) DEFAULT 0"; cmd.ExecuteNonQuery(); } catch {}

                    // 2. Category - Allow NULL
                    try { cmd.CommandText = "ALTER TABLE Events ADD COLUMN Category LONGTEXT NULL"; cmd.ExecuteNonQuery(); } catch {}
                    try { cmd.CommandText = "ALTER TABLE Events MODIFY COLUMN Category LONGTEXT NULL"; cmd.ExecuteNonQuery(); } catch {}

                    // 3. ImageUrl - Allow NULL
                    try { cmd.CommandText = "ALTER TABLE Events ADD COLUMN ImageUrl LONGTEXT NULL"; cmd.ExecuteNonQuery(); } catch {}
                    try { cmd.CommandText = "ALTER TABLE Events MODIFY COLUMN ImageUrl LONGTEXT NULL"; cmd.ExecuteNonQuery(); } catch {}
                    
                    // 4. CreatedBy (Critical for 500 Fix)
                    try { cmd.CommandText = "ALTER TABLE Events ADD COLUMN CreatedBy INT DEFAULT 1"; cmd.ExecuteNonQuery(); } catch {}
                    
                    // --- Registrations Table Schema Updates ---
                    try { cmd.CommandText = "ALTER TABLE Registrations ADD COLUMN Quantity INT DEFAULT 1"; cmd.ExecuteNonQuery(); } catch {}
                    try { cmd.CommandText = "ALTER TABLE Registrations ADD COLUMN TotalPrice DECIMAL(18,2) DEFAULT 0"; cmd.ExecuteNonQuery(); } catch {}
                    try { cmd.CommandText = "ALTER TABLE Registrations ADD COLUMN IsScanned TINYINT(1) DEFAULT 0"; cmd.ExecuteNonQuery(); } catch {}
                }
                conn.Close();
            } catch (Exception ex) {
                Console.WriteLine("DB Sync failed: " + ex.Message);
            }

            // --- User Seeding ---
            var adminEmail = "admin@test.com";
            var adminUser = context.Users.FirstOrDefault(u => u.Email == adminEmail);

            if (adminUser == null)
            {
                adminUser = new User
                {
                    Name = "Admin User",
                    Email = adminEmail,
                    Password = "admin",
                    Role = "Admin"
                };
                context.Users.Add(adminUser);
            }
            else if (adminUser.Role != "Admin")
            {
                adminUser.Role = "Admin";
            }

            var userEmail = "user@test.com";
            if (!context.Users.Any(u => u.Email == userEmail))
            {
                 context.Users.Add(new User
                {
                    Name = "Normal User",
                    Email = userEmail,
                    Password = "user",
                    Role = "User"
                });
            }

            context.SaveChanges();
        }
    }
}