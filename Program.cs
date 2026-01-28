using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using EventBookingAPI.Data;

JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

// ✅ CRITICAL: Disable file watching for Render/Railway to prevent restarts
var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    ContentRootPath = Directory.GetCurrentDirectory(),
    WebRootPath = "wwwroot"
});

// Clear default configuration and add without reloading
builder.Configuration.Sources.Clear();
builder.Configuration
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: false)
    .AddEnvironmentVariables();

// =======================
// DATABASE CONFIGURATION
// =======================

// IMPORTANT:
// Connection string must come from Render/Railway Environment Variable:
// ConnectionStrings__DefaultConnection
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    // Switch to PostgreSQL for Railway
    options.UseNpgsql(connectionString);
});

// =======================
// CORS CONFIGURATION
// =======================

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(
                "http://localhost:4200",
                "http://localhost:3000",
                "https://event-frontend-blush.vercel.app"
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()
            .SetPreflightMaxAge(TimeSpan.FromMinutes(10));
    });
});

// =======================
// JWT AUTHENTICATION
// =======================

var jwtSettings = builder.Configuration.GetSection("JwtSettings");

var secret = jwtSettings["Secret"];
if (string.IsNullOrEmpty(secret))
{
    throw new Exception("JWT Secret is missing. Set JwtSettings__Secret in Railway.");
}

var secretKey = Encoding.UTF8.GetBytes(secret);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(secretKey),
        RoleClaimType = ClaimTypes.Role,
        ClockSkew = TimeSpan.Zero
    };
});

// =======================
// SERVICES
// =======================

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped
    EventBookingAPI.Services.INotificationService,
    EventBookingAPI.Services.NotificationService
>();

var app = builder.Build();

// =======================
// PIPELINE CONFIGURATION
// =======================
// ✅ CRITICAL ORDER: Swagger → HTTPS → Routing → CORS → Auth → Authorization

app.UseSwagger();
app.UseSwaggerUI();

if (app.Environment.IsProduction())
{
    app.UseHttpsRedirection();
}

app.UseRouting();
app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// =======================
// SAFE DATABASE SEEDING
// =======================

using (var scope = app.Services.CreateScope())
{
    try
    {
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        EventBookingAPI.Services.DataSeeder.Seed(context);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[WARN] Seeder skipped: {ex.Message}");
    }
}

// =======================
// DEBUG ENDPOINTS
// =======================

app.MapGet("/api/health", () => Results.Ok(new { Status = "Healthy", Time = DateTime.UtcNow }));

app.MapGet("/api/test-db", async (ApplicationDbContext db) =>
{
    try
    {
        var canConnect = await db.Database.CanConnectAsync();
        return Results.Ok(new { DatabaseConnection = canConnect ? "Success" : "Failed" });
    }
    catch (Exception ex)
    {
        return Results.Problem(detail: ex.ToString(), statusCode: 500);
    }
});

app.Run();
