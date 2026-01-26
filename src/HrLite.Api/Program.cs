using DotNetEnv; // .env okumak için gerekli
using HrLite.Api.Middleware;
using HrLite.Api.Services;
using HrLite.Application.Common;
using HrLite.Application.Interfaces;
using HrLite.Application.Services;
using HrLite.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Text;
using System.Text.Json;

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
else if (apiKey.StartsWith("sk-") || apiKey.StartsWith("gsk_"))
{
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine("✅ [BAŞARILI] .env dosyası bulundu, AI anahtarı yüklendi.");
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
// Departman ve Çalışan Servisleri
builder.Services.AddScoped<IDepartmentService, DepartmentService>();
builder.Services.AddScoped<IEmployeeService, EmployeeService>();

// Leave Management Servisleri
builder.Services.AddScoped<ILeaveTypesService, LeaveTypesService>();
builder.Services.AddScoped<ILeaveRequestsService, LeaveRequestsService>();

// Infrastructure (DbContext, repositories, AI, auth)
builder.Services.AddInfrastructure(builder.Configuration);

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
builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var correlationId = context.HttpContext.Items["CorrelationId"]?.ToString();
            var errors = context.ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => string.IsNullOrWhiteSpace(e.ErrorMessage) ? "Invalid value." : e.ErrorMessage)
                .ToList();

            var response = ApiResponse<object>.ErrorResponse(
                "VALIDATION_ERROR",
                "One or more validation errors occurred.",
                correlationId,
                errors);

            return new BadRequestObjectResult(response);
        };
    });
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

// Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // "role" ve "email" claim'lerini aynen kullanmak için default mapping'i kapatıyoruz.
        // Aksi halde JWT handler bazı claim'leri (örn: "role") farklı tiplere map edebilir.
        options.MapInboundClaims = false;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            // JwtTokenGenerator "role" claim'i basıyor; Roles tabanlı authorization bunun üzerinden çalışsın.
            RoleClaimType = "role",
            NameClaimType = "email",
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Secret"] 
                    ?? throw new InvalidOperationException("JWT secret not configured")))
        };

        options.Events = new JwtBearerEvents
        {
            OnChallenge = async context =>
            {
                if (context.Response.HasStarted)
                {
                    return;
                }

                context.HandleResponse();
                var correlationId = context.HttpContext.Items["CorrelationId"]?.ToString();
                var response = ApiResponse<object>.ErrorResponse(
                    "UNAUTHORIZED",
                    "Authentication is required to access this resource.",
                    correlationId);

                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(JsonSerializer.Serialize(response, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                }));
            },
            OnForbidden = async context =>
            {
                if (context.Response.HasStarted)
                {
                    return;
                }

                var correlationId = context.HttpContext.Items["CorrelationId"]?.ToString();
                var response = ApiResponse<object>.ErrorResponse(
                    "FORBIDDEN",
                    "You do not have permission to access this resource.",
                    correlationId);

                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(JsonSerializer.Serialize(response, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                }));
            }
        };
    });

builder.Services.AddAuthorization();

// HTTP Context
builder.Services.AddHttpContextAccessor();

// Application Services (Diğer servisler)
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IReportsService, ReportsService>();

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
if (Environment.GetEnvironmentVariable("SKIP_DB_INIT") != "1")
{
    try
    {
        await app.Services.InitializeInfrastructureAsync();
        Log.Information("Database initialized and seeded successfully");
    }
    catch (Exception ex)
    {
        Log.Error(ex, "An error occurred while initializing the database");
    }
}

Log.Information("HR Lite API starting up...");

app.Run();
