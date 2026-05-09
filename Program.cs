using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authorization;
using System.Text;
using VendingIot.Data;
using VendingIot.Helpers;
using VendingIot.Models;
using VendingIot.Validators;
using VendingIoT.Helpers;
using VendingIot.Authorization;
using VendingIot.Hubs;
using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateBuilder(args);

// 1. SERVICES REGISTRATION
builder.Services.AddValidatorsFromAssemblyContaining<DepartmentValidator>();
builder.Services.AddScoped<ITokenHelper, TokenHelper>();
builder.Services.AddScoped<IFileService, FileService>();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString))
);

builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

var jwtSettings = builder.Configuration.GetSection("Jwt");
var key = Encoding.UTF8.GetBytes(jwtSettings["Key"]!);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;

    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;

            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hub"))
            {
                context.Token = accessToken;
            }
            
            return Task.CompletedTask;
        },
        OnChallenge = context =>
        {
            context.HandleResponse();
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "application/json";
            var result = System.Text.Json.JsonSerializer.Serialize(new { success = false, message = "Unauthorized. Silakan login terlebih dahulu." });
            return context.Response.WriteAsync(result);
        },
        OnForbidden = context =>
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            context.Response.ContentType = "application/json";
            var result = System.Text.Json.JsonSerializer.Serialize(new { success = false, message = "Forbidden. Anda tidak memiliki akses ke fitur ini." });
            return context.Response.WriteAsync(result);
        },
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
        ClockSkew = TimeSpan.FromSeconds(30)
    };
});

builder.Services.AddAuthorization();

builder.Services.AddCors(options =>
{
    options.AddPolicy("VendingIotFe", policy =>
    {
        policy.WithOrigins("http://localhost:3000") // Pastikan port 3000 sesuai dengan Next.js kamu
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials(); // WAJIB untuk SignalR
    });
});

builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Custom Permission Handlers
builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
builder.Services.AddSingleton<IAuthorizationHandler, PermissionHandler>();
builder.Services.AddSingleton<IUserIdProvider,CustomUserIdProvider>();

// Register SignalR
builder.Services.AddSignalR(options=>
{
    options.EnableDetailedErrors = true; 
});



var app = builder.Build();

// 2. MIDDLEWARE PIPELINE
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

using (var scope = app.Services.CreateScope())
{
    await DbInitializer.Seed(scope.ServiceProvider);
}

// URUTAN INI SANGAT KRUSIAL
app.UseStaticFiles();
app.UseRouting();
app.UseCors("VendingIotFe");

// app.UseHttpsRedirection(); // Matikan di lokal jika menggunakan http://

app.UseAuthentication();
app.UseAuthorization();

// MAPPING ENDPOINTS
app.MapHub<NotificationHub>("/hub/notification");
app.MapControllers();

app.Run();