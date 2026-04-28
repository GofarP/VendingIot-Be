using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using VendingIot.Data;
using VendingIot.Helpers;
using VendingIot.Models;
using VendingIot.Validators;
using VendingIoT.Helpers;

var builder = WebApplication.CreateBuilder(args);

// 1. SERVICES CONFIGURATION
builder.Services.AddValidatorsFromAssemblyContaining<DepartmentValidator>();

// Register this for refresh token
builder.Services.AddScoped<ITokenHelper, TokenHelper>();

// Database Configuration (MySQL)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString))
);

// Identity Configuration
builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// JWT Configuration
var jwtSettings = builder.Configuration.GetSection("Jwt");
var key = Encoding.ASCII.GetBytes(jwtSettings["Key"]!);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;

    // FIX: Menggabungkan OnMessageReceived dan OnChallenge dalam satu object Events
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            // Mengambil token dari cookie browser
            var accessToken = context.Request.Cookies["vending_token"];
            if (!string.IsNullOrEmpty(accessToken))
            {
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        },
        OnChallenge = context =>
        {
            // Custom response saat token tidak valid atau tidak ada
            context.HandleResponse();
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "application/json";
            var result = System.Text.Json.JsonSerializer.Serialize(new
            {
                message = "Unauthorized. Silakan login terlebih dahulu."
            });
            return context.Response.WriteAsync(result);
        }
    };

    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

// 2. CORS CONFIGURATION
builder.Services.AddCors(options =>
{
    options.AddPolicy("VendingIotFe", policy =>
    {
        policy.WithOrigins("http://localhost:3000") // Sesuaikan dengan URL Frontend
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials(); // WAJIB untuk mengirim cookie
    });
});

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

// 3. MIDDLEWARE PIPELINE
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Seed Database
await DbInitializer.Seed(app.Services);

// Menggunakan policy CORS yang sudah didefinisikan
app.UseCors("VendingIotFe");

// app.UseHttpsRedirection(); // Matikan jika testing via HTTP/LAN tanpa SSL

app.UseAuthentication();
app.UseAuthorization();

app.UseStaticFiles();

app.MapControllers();

app.Run();