#!/bin/bash

# HR Lite API Demo Script
# Bu script tüm demo senaryolarını çalıştırır

BASE_URL="http://localhost:5000"
CORRELATION_ID="demo-$(date +%s)"

echo "========================================="
echo "HR LITE API DEMO"
echo "========================================="
echo ""

# 1. Login - HR User
echo "1️⃣  LOGIN - HR User"
echo "-------------------"
HR_TOKEN=$(curl -s -X POST "$BASE_URL/api/auth/login" \
  -H "Content-Type: application/json" \
  -H "X-Correlation-Id: $CORRELATION_ID-login-hr" \
  -d '{
    "email": "sarah.johnson@hrlite.com",
    "password": "password123"
  }' | jq -r '.data.token')

echo "✅ HR Token alındı: ${HR_TOKEN:0:50}..."
echo ""
sleep 1

# 2. Login - Employee User
echo "2️⃣  LOGIN - Employee User"
echo "-------------------"
EMP_TOKEN=$(curl -s -X POST "$BASE_URL/api/auth/login" \
  -H "Content-Type: application/json" \
  -H "X-Correlation-Id: $CORRELATION_ID-login-emp" \
  -d '{
    "email": "john.doe@hrlite.com",
    "password": "password123"
  }' | jq -r '.data.token')

echo "✅ Employee Token alındı: ${EMP_TOKEN:0:50}..."
echo ""
sleep 1

# 3. Employee ile Reports Erişimi (403 bekliyoruz)
echo "3️⃣  FORBIDDEN TEST - Employee ile Reports Erişimi"
echo "---------------------------------------------------"
echo "❌ Employee token ile /api/reports/headcount-by-department"
curl -s -X GET "$BASE_URL/api/reports/headcount-by-department" \
  -H "Authorization: Bearer $EMP_TOKEN" \
  -H "X-Correlation-Id: $CORRELATION_ID-forbidden" \
  | jq '.'
echo ""
sleep 1

# 4. HR ile Headcount Report
echo "4️⃣  HEADCOUNT BY DEPARTMENT - HR Access"
echo "----------------------------------------"
curl -s -X GET "$BASE_URL/api/reports/headcount-by-department" \
  -H "Authorization: Bearer $HR_TOKEN" \
  -H "X-Correlation-Id: $CORRELATION_ID-headcount" \
  | jq '.'
echo ""
sleep 1

# 5. HR ile Monthly Leave Requests
echo "5️⃣  LEAVE REQUESTS MONTHLY - Year 2026"
echo "---------------------------------------"
curl -s -X GET "$BASE_URL/api/reports/leave-requests-monthly?year=2026" \
  -H "Authorization: Bearer $HR_TOKEN" \
  -H "X-Correlation-Id: $CORRELATION_ID-leave-monthly" \
  | jq '.'
echo ""
sleep 1

# 6. AI Insights (Feature kapalıysa graceful fail)
echo "6️⃣  AI INSIGHTS - Aggregated Data Analysis"
echo "------------------------------------------"
curl -s -X POST "$BASE_URL/api/reports/ai/insights?year=2026" \
  -H "Authorization: Bearer $HR_TOKEN" \
  -H "X-Correlation-Id: $CORRELATION_ID-ai-insights" \
  | jq '.'
echo ""
sleep 1

# 7. Not Found Error
echo "7️⃣  NOT FOUND ERROR - Invalid Endpoint"
echo "---------------------------------------"
curl -s -X GET "$BASE_URL/api/reports/invalid-endpoint" \
  -H "Authorization: Bearer $HR_TOKEN" \
  -H "X-Correlation-Id: $CORRELATION_ID-not-found" \
  | jq '.'
echo ""

echo "========================================="
echo "✅ DEMO TAMAMLANDI"
echo "========================================="
echo ""
echo "🔍 Log dosyasına bakın: logs/hrlite-*.txt"
echo "📊 Tüm isteklerde correlationId kullanıldı"
echo "🔐 Role-based access kontrolü çalışıyor"
echo "🎯 Response envelope standardı uygulandı"
