# HR Lite - Kurumsal Standartlarda İK Yönetim Sistemi

.NET 9 kullanılarak geliştirilmiş, JWT tabanlı kimlik doğrulama, global hata yönetimi, loglama ve AI destekli rapor içgörüleri içeren kurumsal HR altyapısı.

## 🏗️ Mimari

```
hr-lite/src/
├── HrLite.Domain          # Entities, Enums, Common
├── HrLite.Application     # DTOs, Interfaces, Services (Business Logic)
├── HrLite.Infrastructure  # DbContext, Authentication, AI Client
└── HrLite.Api            # Controllers, Middlewares, Configuration
```

## ✨ Özellikler

### ✅ Tamamlanan Özellikler

- ✅ **JWT Authentication**: Token-based kimlik doğrulama (role, employeeId claims)
- ✅ **Role-based Authorization**: Admin, HR, Employee rolleri
- ✅ **Global Exception Middleware**: Tek noktadan hata yönetimi
- ✅ **Response Envelope**: Standart API response formatı (success, data, error, correlationId)
- ✅ **CorrelationId Middleware**: Request tracking ve debugging
- ✅ **Serilog**: Request/response logging, correlationId ile
- ✅ **Audit Interceptor**: Otomatik CreatedAt, CreatedBy, UpdatedAt, UpdatedBy
- ✅ **Reports API**: Headcount by department, monthly leave requests
- ✅ **AI Insights**: LLM ile HR data analizi (aggregated data, no PII)
- ✅ **Swagger**: Tam dokümantasyon ve JWT desteği

## 🚀 Hızlı Başlangıç

### Gereksinimler

- .NET 9 SDK
- SQL Server (LocalDB veya Full)
- OpenAI API Key (AI özelliği için opsiyonel)

### 1. Database Kurulumu

```bash
# Projeyi klonlayın ve src dizinine gidin
cd /Users/murathandede/hr-lite/src

# Database migration otomatik uygulanır (Program.cs'de)
# İlk çalıştırmada seed data otomatik yüklenir
```

### 2. Konfigürasyon

`appsettings.json` dosyasını düzenleyin:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=HrLiteDb;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true"
  },
  "Jwt": {
    "Secret": "YourSuperSecretKeyThatIsAtLeast32CharactersLongForHS256Algorithm",
    "Issuer": "HrLite.Api",
    "Audience": "HrLite.Client"
  },
  "Ai": {
    "EnableAiFeatures": true,
    "ApiKey": "your-openai-api-key",
    "TimeoutSeconds": 15,
    "MaxTokens": 800,
    "Temperature": 0.2
  }
}
```

### 3. Uygulamayı Çalıştırın

```bash
cd HrLite.Api
dotnet run
```

Swagger UI: `https://localhost:5001` veya `http://localhost:5000`

## 🔐 Demo Kullanıcıları

| Email | Password | Role | Açıklama |
|-------|----------|------|----------|
| admin@hrlite.com | password123 | Admin | Full access |
| sarah.johnson@hrlite.com | password123 | HR | Reports & employee management |
| john.doe@hrlite.com | password123 | Employee | Self-only access |

## 📋 API Endpoints

### Authentication
```
POST /api/auth/login
```

### Reports (HR/Admin Only)
```
GET  /api/reports/headcount-by-department
GET  /api/reports/leave-requests-monthly?year=2026
POST /api/reports/ai/insights?year=2026
```

## 🎯 Demo Senaryoları

### 1. Login - JWT Token Alma

```bash
curl -X POST https://localhost:5001/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "sarah.johnson@hrlite.com",
    "password": "password123"
  }'
```

**Response:**
```json
{
  "success": true,
  "data": {
    "token": "eyJhbGc...",
    "employeeId": 2,
    "email": "sarah.johnson@hrlite.com",
    "role": "HR",
    "expiresAt": "2026-01-17T16:00:00Z"
  },
  "error": null,
  "correlationId": "550e8400-e29b-41d4-a716-446655440000"
}
```

### 2. Employee ile Rapor Erişimi (403 Forbidden)

```bash
# Employee token ile
curl -X GET https://localhost:5001/api/reports/headcount-by-department \
  -H "Authorization: Bearer {employee-token}"
```

**Response:**
```json
{
  "success": false,
  "data": null,
  "error": {
    "code": "FORBIDDEN",
    "message": "You do not have permission to access this resource.",
    "details": []
  },
  "correlationId": "550e8400-e29b-41d4-a716-446655440001"
}
```

### 3. HR ile Headcount Raporu

```bash
curl -X GET https://localhost:5001/api/reports/headcount-by-department \
  -H "Authorization: Bearer {hr-token}"
```

**Response:**
```json
{
  "success": true,
  "data": [
    {
      "departmentName": "Engineering",
      "employeeCount": 3
    },
    {
      "departmentName": "Human Resources",
      "employeeCount": 3
    },
    {
      "departmentName": "Sales",
      "employeeCount": 2
    },
    {
      "departmentName": "Finance",
      "employeeCount": 2
    }
  ],
  "error": null,
  "correlationId": "550e8400-e29b-41d4-a716-446655440002"
}
```

### 4. Monthly Leave Requests

```bash
curl -X GET "https://localhost:5001/api/reports/leave-requests-monthly?year=2026" \
  -H "Authorization: Bearer {hr-token}"
```

**Response:**
```json
{
  "success": true,
  "data": [
    {
      "year": 2026,
      "month": 1,
      "totalRequests": 3,
      "approvedRequests": 2,
      "pendingRequests": 0,
      "rejectedRequests": 1
    },
    {
      "year": 2026,
      "month": 2,
      "totalRequests": 3,
      "approvedRequests": 1,
      "pendingRequests": 2,
      "rejectedRequests": 0
    }
  ],
  "error": null,
  "correlationId": "550e8400-e29b-41d4-a716-446655440003"
}
```

### 5. AI Insights (LLM ile Analiz)

```bash
curl -X POST "https://localhost:5001/api/reports/ai/insights?year=2026" \
  -H "Authorization: Bearer {hr-token}"
```

**Response:**
```json
{
  "success": true,
  "data": {
    "summary": "The organization has 10 employees distributed across 4 departments. Leave requests show seasonal patterns with February having the highest pending requests.",
    "insights": [
      "Engineering department has the highest headcount (30%)",
      "Leave approval rate is 50% with peak requests in February",
      "33% of leave requests are still pending review"
    ],
    "recommendedActions": [
      "Expedite pending leave request reviews to improve employee satisfaction",
      "Consider adding capacity to Engineering department",
      "Implement automated leave approval workflow for common scenarios"
    ]
  },
  "error": null,
  "correlationId": "550e8400-e29b-41d4-a716-446655440004"
}
```

### 6. Not Found Hatası (404)

```bash
# Geçersiz endpoint
curl -X GET https://localhost:5001/api/reports/invalid-endpoint \
  -H "Authorization: Bearer {hr-token}"
```

**Response:**
```json
{
  "success": false,
  "data": null,
  "error": {
    "code": "NOT_FOUND",
    "message": "The requested resource was not found.",
    "details": []
  },
  "correlationId": "550e8400-e29b-41d4-a716-446655440005"
}
```

## 🔍 Mimari Kararlar

### 1. Controller'da İş Kuralı Yok ❌
```csharp
// ❌ YANLIŞ
public async Task<IActionResult> GetReport()
{
    var data = await _context.Employees.ToListAsync();
    // Business logic here...
}

// ✅ DOĞRU
public async Task<IActionResult> GetReport()
{
    var result = await _reportsService.GetHeadcountByDepartmentAsync();
    return Ok(result);
}
```

### 2. Global Exception Handling
```csharp
// Controller'da try/catch YOK
// Tüm hatalar GlobalExceptionMiddleware'de yakalanır
// HTTP status code otomatik set edilir:
// - ValidationException → 400
// - NotFoundException → 404
// - BusinessException → 409
// - UnauthorizedAccess → 403
// - Exception → 500
```

### 3. Audit Alanları (Interceptor)
```csharp
// Manuel set etmek ❌
entity.CreatedAt = DateTime.UtcNow;
entity.CreatedBy = currentUserId;

// Otomatik (AuditInterceptor) ✅
// SaveChanges sırasında otomatik doldurulur
```

### 4. AI Özelliği - Privacy First
```csharp
// ❌ YANLIŞ: Ham çalışan verisi
var employees = await _context.Employees.ToListAsync();
await _llmClient.GenerateInsights(employees);

// ✅ DOĞRU: Sadece agregasyon
var aggregated = new {
    totalEmployees = 10,
    departmentCounts = [...]
};
await _llmClient.GenerateInsights(aggregated);
```

## 📊 Loglama

Tüm loglar `logs/` dizininde:
```
logs/hrlite-20260117.txt
```

Log örneği:
```
[16:30:45 INF] HTTP POST /api/auth/login responded 200 in 125ms
CorrelationId: 550e8400-e29b-41d4-a716-446655440000

[16:31:02 INF] HTTP GET /api/reports/headcount-by-department responded 200 in 45ms
CorrelationId: 550e8400-e29b-41d4-a716-446655440002
UserId: 2
Role: HR
```

## 🧪 Test Checklist

- [x] JWT authentication çalışıyor
- [x] Role-based access kontrolü
- [x] Global exception middleware
- [x] Response envelope her endpoint'te
- [x] CorrelationId response + log'da
- [x] Audit alanları otomatik
- [x] Reports endpoint'leri
- [x] AI Insights JSON üretiyor
- [x] Controller'da iş kuralı yok
- [x] 403/404/409 hata senaryoları

## 🔧 Geliştirme

```bash
# Build
dotnet build

# Run tests (opsiyonel)
dotnet test

# Migration oluştur
dotnet ef migrations add MigrationName --project HrLite.Infrastructure --startup-project HrLite.Api

# Database güncelle
dotnet ef database update --project HrLite.Infrastructure --startup-project HrLite.Api
```

## 📝 Notlar

- **Password**: Demo için plain text, production'da BCrypt kullanılmalı
- **AI Feature**: `EnableAiFeatures: false` yapılarak kapatılabilir
- **Database**: İlk çalıştırmada otomatik oluşturulur ve seed edilir
- **CORS**: Production'da konfigüre edilmeli

---

**Kurumsal Standart Teslim Kriterleri: ✅ TAMAMLANDI**
