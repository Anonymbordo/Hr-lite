using DotNetEnv; // .env okumak için gerekli
using HrLite.Api.Middleware;
using HrLite.Api.Services;
using HrLite.Application.Interfaces;
using HrLite.Application.Services;
using HrLite.Infrastructure.AI;
using HrLite.Infrastructure.Authentication;
using HrLite.Infrastructure.Persistence;
using HrLite.Infrastructure.Seed;
using HrLite.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Text;

// --- 1. ADIM: .env DOSYASINI YÜKLE VE KONTROL ET ---
// Bu blok, uygulama daha ayağa kalkmadan şifreleri kontrol eder.
Env.Load();

var apiKey = Environment.GetEnvironmentVariable("Ai__ApiKey");
var currentDirectory = Directory.GetCurrentDirectory();

Console.ForegroundColor = ConsoleColor.Cyan;
Console.WriteLine("==================================================");
Console.WriteLine($"📂 ÇALIŞMA KONUMU: {currentDirectory}");
Console.WriteLine("==================================================");

if (string.IsNullOrEmpty(apiKey))
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine("❌ [HATA] .env dosyası OKUNAMADI veya Ai__ApiKey boş!");
    Console.WriteLine("   -> Lütfen terminalde 'src/HrLite.Api' klasörüne girdiğinden emin ol.");
    Console.WriteLine("   -> Komut: cd src/HrLite.Api");
}
else if (apiKey.StartsWith("gsk_"))
{
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine("✅ [BAŞARILI] .env dosyası bulundu, Gerçek Groq Anahtarı yüklendi.");
}
else
{
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine($"⚠️ [UYARI] Bir anahtar bulundu ama 'gsk_' ile başlamıyor.");
    Console.WriteLine($"   -> Okunan Değer: {apiKey}");
    Console.WriteLine("   -> .env dosyasındaki şifreyi kontrol et.");
}
Console.ResetColor();
Console.WriteLine("==================================================");
// -------------------------------------------------------

var builder = WebApplication.CreateBuilder(args);

// --- SERVİS KAYITLARI (Dependency Injection) ---
// AI Servisi
builder.Services.AddScoped<IAiService, OpenAiService>();

// Departman ve Çalışan Servisleri
builder.Services.AddScoped<IDepartmentService, DepartmentService>();
builder.Services.AddScoped<IEmployeeService, EmployeeService>();

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "HrLite.Api")
    .WriteTo.Console()
    .WriteTo.File("logs/hrlite-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Swagger configuration with JWT support
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "HR Lite API",
        Version = "v1",
        Description = "Enterprise-grade HR Management System with AI-powered insights",
        Contact = new OpenApiContact
        {
            Name = "HR Lite Team",
            Email = "support@hrlite.com"
        }
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token in the text input below.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });

    // XML Comments (Varsa ekler, yoksa hata vermez)
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
});

// Database
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IApplicationDbContext>(provider => 
    provider.GetRequiredService<ApplicationDbContext>());

// Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Secret"] 
                    ?? throw new InvalidOperationException("JWT secret not configured")))
        };
    });

builder.Services.AddAuthorization();

// HTTP Context
builder.Services.AddHttpContextAccessor();

// Application Services (Diğer servisler)
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IReportsService, ReportsService>();

// AI/LLM Client
builder.Services.AddHttpClient<ILlmClient, OpenAiLlmClient>();

var app = builder.Build();

// Configure the HTTP request pipeline
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<GlobalExceptionMiddleware>();

// Serilog request logging
app.UseSerilogRequestLogging(options =>
{
    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        diagnosticContext.Set("CorrelationId", httpContext.Items["CorrelationId"]);
        diagnosticContext.Set("UserAgent", httpContext.Request.Headers["User-Agent"].ToString());
    };
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "HR Lite API v1");
        c.RoutePrefix = string.Empty; // Swagger at root
    });
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.UseMiddleware<ResponseEnvelopeMiddleware>();

app.MapControllers();

// Database initialization (optional - for demo purposes)
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    try
    {
        // Create database and apply migrations
        await context.Database.MigrateAsync();
        
        // Seed initial data
        await DatabaseSeeder.SeedAsync(context);
        
        Log.Information("Database initialized and seeded successfully");
    }
    catch (Exception ex)
    {
        Log.Error(ex, "An error occurred while initializing the database");
    }
}

Log.Information("HR Lite API starting up...");

app.Run();