using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using EventBookingAPI.Data;

JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
var builder = WebApplication.CreateBuilder(args);

// =======================
// DATABASE CONFIGURATION
// =======================
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseMySql(
        connectionString,
        new MySqlServerVersion(new Version(8, 0, 36))
    );
});

// =======================
// CORS CONFIGURATION
// =======================
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.WithOrigins(
                "http://localhost:4200",
                "https://event-frontend-blush.vercel.app"
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()
            .SetPreflightMaxAge(TimeSpan.FromMinutes(10)); // Cache preflight
    });
});

// =======================
// JWT AUTHENTICATION
// =======================
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secret = jwtSettings["Secret"];
if (string.IsNullOrEmpty(secret))
{
    throw new Exception("JWT Secret is missing. Set JwtSettings__Secret in Render.");
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
// ⚠️ CRITICAL: CORS MUST BE FIRST (before authentication)
app.UseCors("AllowAll");

app.UseSwagger();
app.UseSwaggerUI();

// Only redirect in production if needed
if (app.Environment.IsProduction())
{
    app.UseHttpsRedirection();
}

app.UseRouting();

// Authentication/Authorization come AFTER CORS
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

app.Run();
