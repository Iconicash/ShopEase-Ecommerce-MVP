using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ShopEase.Application.Interfaces;
using ShopEase.Application.Services;
using ShopEase.Domain.Entities;
using ShopEase.Infrastructure.Data;
using ShopEase.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Existing Category and Product modules.
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IProductService, ProductService>();

// Password hashing for local user accounts.
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();

var jwt = builder.Configuration.GetSection("Jwt");
var jwtKey = jwt["Key"] ?? throw new InvalidOperationException("Configure Jwt:Key using user-secrets or an environment variable.");
if (Encoding.UTF8.GetByteCount(jwtKey) < 32)
    throw new InvalidOperationException("Jwt:Key must be at least 32 bytes.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt["Issuer"],
            ValidAudience = jwt["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            NameClaimType = System.Security.Claims.ClaimTypes.Name,
            RoleClaimType = System.Security.Claims.ClaimTypes.Role,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddCors(options => options.AddPolicy("Frontend", policy =>
    policy.WithOrigins(builder.Configuration.GetSection("Frontend:AllowedOrigins").Get<string[]>()
                       ?? new[] { "http://localhost:5173" })
          .AllowAnyHeader()
          .AllowAnyMethod()));

var app = builder.Build();
// Seed initial Admin account securely from User Secrets.
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    var db = services.GetRequiredService<ApplicationDbContext>();
    var passwordHasher = services.GetRequiredService<IPasswordHasher<User>>();
    var configuration = services.GetRequiredService<IConfiguration>();

    var adminEmail = configuration["Admin:Email"]?.Trim().ToLowerInvariant();
    var adminName = configuration["Admin:FullName"]?.Trim();
    var adminPassword = configuration["Admin:Password"];

    if (string.IsNullOrWhiteSpace(adminEmail) ||
        string.IsNullOrWhiteSpace(adminName) ||
        string.IsNullOrWhiteSpace(adminPassword))
    {
        throw new InvalidOperationException(
            "Admin:Email, Admin:FullName and Admin:Password must be configured.");
    }

    if (adminPassword.Length < 14)
    {
        throw new InvalidOperationException(
            "The initial Admin password must contain at least 14 characters.");
    }

    var existingAdmin = await db.Users
        .SingleOrDefaultAsync(u => u.Email == adminEmail);

    if (existingAdmin is null)
    {
        var admin = new User
        {
            FullName = adminName,
            Email = adminEmail,
            Role = "Admin"
        };

        admin.PasswordHash = passwordHasher.HashPassword(admin, adminPassword);

        db.Users.Add(admin);
        await db.SaveChangesAsync();

        Console.WriteLine("Initial Admin account created successfully.");
    }
    else if (existingAdmin.Role != "Admin")
    {
        throw new InvalidOperationException(
            "The configured Admin email already belongs to a non-Admin account. No role changes were made.");
    }
    else
    {
        Console.WriteLine("Admin account already exists. No changes made.");
    }
}

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.UseHttpsRedirection();
app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
