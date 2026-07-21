# 🔐 AuthenticationRcl

> ماژول احراز هویت پیشرفته برای ASP.NET Core | Advanced Authentication Module for ASP.NET Core

[![.NET](https://img.shields.io/badge/.NET-9.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/license-MIT-green)](LICENSE)
[![Tests](https://img.shields.io/badge/tests-71%2B-brightgreen)](tests/)
[![PRs Welcome](https://img.shields.io/badge/PRs-welcome-brightgreen.svg)](http://makeapullrequest.com)

---

## 📖 معرفی | Introduction

**AuthenticationRcl** یک ماژول مستقل و کامل برای مدیریت احراز هویت در پروژه‌های ASP.NET Core است. این ماژول با استفاده از بهترین روش‌های امنیتی، تمام نیازهای احراز هویت را پوشش می‌دهد.

**ویژگی‌های کلیدی:**

| ویژگی | توضیح |
|-------|-------|
| ✅ **ثبت‌نام با Username** | ایمیل و شماره موبایل اختیاری |
| ✅ **ورود دو مرحله‌ای** | Username + Password → OTP |
| ✅ **۳ روش دریافت کد** | ایمیل، پیامک، Google Authenticator (TOTP) |
| ✅ **JWT و Refresh Token** | احراز هویت امن و مقیاس‌پذیر |
| ✅ **BCrypt** | هش کردن رمز عبور با بالاترین سطح امنیت |
| ✅ **Rate Limiting** | محدودیت درخواست برای جلوگیری از حملات |
| ✅ **قفل شدن خودکار** | بعد از ۵ تلاش ناموفق، ۱۵ دقیقه قفل |
| ✅ **مدیریت کاربران** | توسط ادمین |
| ✅ **بروزرسانی پروفایل** | ایمیل و شماره موبایل |
| ✅ **تست واحد** | بیش از ۵۰ تست با xUnit و Moq |

---

## 🚀 نصب و راه‌اندازی | Installation

### 1️⃣ ایجاد پروژه جدید

```bash
dotnet new webapi -n MyProject
cd MyProject
```

### 2️⃣ افزودن ارجاع به ماژول

```xml
<ProjectReference Include="..\AuthenticationRcl\AuthenticationRcl.csproj" />
```

### 3️⃣ ثبت ماژول در `Program.cs`

```csharp
using AuthenticationRcl;

var builder = WebApplication.CreateBuilder(args);

// ثبت ماژول احراز هویت
builder.Services.AddAuthenticationModule(builder.Configuration);

var app = builder.Build();

// استفاده از Middlewareها
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapControllers();
app.Run();
```

### 4️⃣ تنظیمات `appsettings.json`

```json
{
  "ConnectionStrings": {
    "AuthDb": "Server=localhost;Database=AuthDb;Trusted_Connection=true;TrustServerCertificate=true"
  },
  "Jwt": {
    "SecretKey": "YourSuperSecretKeyWithMinimum32CharactersLong!",
    "Issuer": "https://localhost:7001",
    "Audience": "https://localhost:7001"
  },
  "Auth": {
    "MinimumPasswordLength": 8,
    "MaxFailedAttempts": 5,
    "LockoutMinutes": 15,
    "RefreshTokenExpiryDays": 30,
    "ResetTokenExpiryHours": 1,
    "LoginTokenExpiryMinutes": 5,
    "OTPExpiryMinutes": 5,
    "BCryptWorkFactor": 12
  },
  "OTP": {
    "Length": 6,
    "ExpiryMinutes": 5,
    "MaxAttempts": 3
  }
}
```

### 5️⃣ اعمال Migration

```bash
dotnet ef migrations add InitialCreate --project AuthenticationRcl
dotnet ef database update --project AuthenticationRcl
```

---

## 📚 مستندات API | API Documentation

### 🔑 عملیات احراز هویت (Authentication)

| متد | مسیر | توضیح | محدودیت |
|-----|------|-------|---------|
| `POST` | `/api/v1/auth/register` | ثبت‌نام کاربر جدید | ۳ درخواست در ۵ دقیقه |
| `POST` | `/api/v1/auth/login/step1` | مرحله ۱: Username + Password | ۵ درخواست در ۱ دقیقه |
| `POST` | `/api/v1/auth/login/step2` | مرحله ۲: انتخاب روش دریافت کد | ۳ درخواست در ۵ دقیقه |
| `POST` | `/api/v1/auth/login/step3` | مرحله ۳: تایید کد | ۵ درخواست در ۵ دقیقه |
| `POST` | `/api/v1/auth/logout` | خروج کاربر | - |
| `POST` | `/api/v1/auth/refresh-token` | تجدید توکن | ۱۰ درخواست در ۱ دقیقه |
| `PUT` | `/api/v1/auth/change-password` | تغییر رمز عبور | ۳ درخواست در ۵ دقیقه |
| `POST` | `/api/v1/auth/forgot-password` | درخواست بازنشانی رمز | ۳ درخواست در ۱۰ دقیقه |
| `POST` | `/api/v1/auth/reset-password` | بازنشانی رمز با توکن | ۵ درخواست در ۵ دقیقه |

### 👤 مدیریت کاربر (User Management)

| متد | مسیر | توضیح | دسترسی |
|-----|------|-------|---------|
| `GET` | `/api/v1/user/me` | اطلاعات کاربر جاری | کاربر |
| `GET` | `/api/v1/user/by-username/{username}` | اطلاعات با نام کاربری | کاربر |
| `GET` | `/api/v1/user/{userId}` | اطلاعات کاربر | ادمین |
| `PUT` | `/api/v1/user/profile` | بروزرسانی پروفایل | کاربر |
| `PUT` | `/api/v1/user/{userId}` | بروزرسانی کاربر | ادمین |
| `DELETE` | `/api/v1/user/{userId}` | حذف کاربر | ادمین |

### 📱 OTP و 2FA

| متد | مسیر | توضیح | محدودیت |
|-----|------|-------|---------|
| `POST` | `/api/v1/otp/send` | ارسال کد تایید | ۳ درخواست در ۵ دقیقه |
| `POST` | `/api/v1/otp/verify` | تایید کد | ۵ درخواست در ۵ دقیقه |
| `GET` | `/api/v1/twofactor/status` | وضعیت 2FA | - |
| `POST` | `/api/v1/twofactor/setup-totp` | تنظیم TOTP | ۲ درخواست در ۱۰ دقیقه |
| `POST` | `/api/v1/twofactor/verify-totp` | تایید TOTP | ۵ درخواست در ۵ دقیقه |
| `POST` | `/api/v1/twofactor/disable` | غیرفعال‌سازی 2FA | - |

---

## 📝 مثال درخواست‌ها | Request Examples

### ثبت‌نام

```http
POST /api/v1/auth/register
Content-Type: application/json

{
  "username": "reza",
  "email": "reza@example.com",
  "phoneNumber": "09121234567",
  "password": "Test@123"
}
```

**پاسخ:**

```json
{
  "success": true,
  "message": "ثبت نام با موفقیت انجام شد",
  "data": {
    "userId": 1,
    "username": "reza",
    "accessToken": "eyJhbGciOiJIUzI1NiIs...",
    "refreshToken": "abc123...",
    "expiresIn": 900
  }
}
```

### ورود - مرحله ۱

```http
POST /api/v1/auth/login/step1
Content-Type: application/json

{
  "username": "reza",
  "password": "Test@123"
}
```

**پاسخ:**

```json
{
  "success": true,
  "message": "احراز هویت اولیه انجام شد",
  "data": {
    "loginToken": "abc123...",
    "expiresIn": 300,
    "requiresTwoFactor": true,
    "availableMethods": [
      {
        "type": "Email",
        "label": "ایمیل",
        "isAvailable": true,
        "maskedDestination": "re***@example.com"
      },
      {
        "type": "SMS",
        "label": "پیامک",
        "isAvailable": true,
        "maskedDestination": "091***567"
      }
    ],
    "userInfo": {
      "userId": 1,
      "username": "reza",
      "hasEmail": true,
      "hasPhone": true,
      "hasTOTP": false
    }
  }
}
```

### ورود - مرحله ۲

```http
POST /api/v1/auth/login/step2
Content-Type: application/json

{
  "loginToken": "abc123...",
  "method": "Email"
}
```

**پاسخ:**

```json
{
  "success": true,
  "message": "کد تایید با موفقیت ارسال شد",
  "data": {
    "method": "Email",
    "expiresIn": 300,
    "maskedDestination": "re***@example.com"
  }
}
```

### ورود - مرحله ۳

```http
POST /api/v1/auth/login/step3
Content-Type: application/json

{
  "loginToken": "abc123...",
  "code": "123456"
}
```

**پاسخ:**

```json
{
  "success": true,
  "message": "ورود با موفقیت انجام شد",
  "data": {
    "userId": 1,
    "username": "reza",
    "accessToken": "eyJhbGciOiJIUzI1NiIs...",
    "refreshToken": "xyz789...",
    "expiresIn": 900
  }
}
```

---

## 🔒 امنیت | Security

| ویژگی | توضیح |
|-------|-------|
| **BCrypt** | هش کردن رمز عبور با Salt خودکار و Cost Factor ۱۲ |
| **Rate Limiting** | جلوگیری از حملات Brute Force و DDoS |
| **Lockout** | قفل شدن کاربر بعد از ۵ تلاش ناموفق به مدت ۱۵ دقیقه |
| **JWT** | احراز هویت بدون وضعیت با امضای HMAC-SHA256 |
| **Refresh Token** | چرخش خودکار با انقضای ۳۰ روز |
| **TOTP** | احراز هویت دو مرحله‌ای با Google Authenticator |
| **کدهای پشتیبان** | ۱۰ کد یکبارمصرف برای بازیابی 2FA |
| **Login Token** | توکن موقت ۵ دقیقه‌ای برای ورود دو مرحله‌ای |

---

## 🧪 تست‌ها | Tests

```bash
# اجرای تست‌ها
dotnet test

# اجرای تست‌ها با پوشش کد
dotnet test --collect:"XPlat Code Coverage"

# اجرای تست‌های خاص
dotnet test --filter "Category=Registration"
```

**تعداد تست‌ها:** ۵۰+ تست واحد

| کلاس تست | تعداد تست |
|----------|-----------|
| `AuthServiceTests` | ۳۰+ تست |
| `ControllerTests` | ۲۰+ تست |

---

## 📂 ساختار پروژه | Project Structure

```
AuthenticationRcl/
├── Controllers/Api/
│   ├── AuthController.cs          # کنترلر احراز هویت
│   ├── UserController.cs          # کنترلر مدیریت کاربران
│   ├── OTPController.cs           # کنترلر OTP
│   └── TwoFactorController.cs     # کنترلر 2FA
├── Models/
│   ├── User.cs                    # مدل کاربر
│   ├── UserOTP.cs                 # مدل کدهای تایید
│   ├── UserBackupCode.cs          # مدل کدهای پشتیبان
│   ├── UserDevice.cs              # مدل دستگاه‌ها
│   └── AppDbContext.cs            # DbContext
├── Services/
│   ├── IAuthService.cs            # اینترفیس سرویس احراز هویت
│   ├── AuthService.cs             # پیاده‌سازی احراز هویت
│   ├── ITokenService.cs           # اینترفیس سرویس JWT
│   ├── TokenService.cs            # پیاده‌سازی JWT
│   ├── IOTPService.cs             # اینترفیس سرویس OTP
│   ├── OTPService.cs              # پیاده‌سازی OTP
│   ├── IEmailProvider.cs          # اینترفیس پروایدر ایمیل
│   └── ISMSProvider.cs            # اینترفیس پروایدر پیامک
├── ViewModels/
│   ├── Auth/                      # مدل‌های احراز هویت
│   │   ├── RegisterRequest.cs
│   │   ├── LoginStep1Request.cs
│   │   ├── LoginStep1Response.cs
│   │   ├── LoginStep2Request.cs
│   │   ├── LoginStep2Response.cs
│   │   ├── LoginStep3Request.cs
│   │   ├── LoginStep3Response.cs
│   │   └── ...
│   ├── User/                      # مدل‌های کاربر
│   ├── OTP/                       # مدل‌های OTP
│   ├── TwoFactor/                 # مدل‌های 2FA
│   └── Base/                      # کلاس‌های پایه
│       └── ResponseBase.cs
├── Options/
│   ├── AuthOptions.cs             # تنظیمات احراز هویت
│   ├── JwtOptions.cs              # تنظیمات JWT
│   └── OTPOptions.cs              # تنظیمات OTP
├── ModuleInitializer.cs           # ثبت ماژول در DI
├── AuthenticationRcl.csproj       # فایل پروژه
└── README.md
```

---

## 🛠️ تکنولوژی‌ها | Technologies

| تکنولوژی | نسخه |
|-----------|------|
| .NET | 9.0 |
| Entity Framework Core | 9.0 |
| BCrypt.Net-Next | 4.0.3 |
| Otp.NET | 1.4.0 |
| JWT Bearer | 9.0 |
| Rate Limiting | 9.0 |
| xUnit | 2.9.2 |
| Moq | 4.20.72 |

---

## 📋 پیش‌نیازها | Prerequisites

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [SQL Server](https://www.microsoft.com/en-us/sql-server/sql-server-downloads) (یا هر پایگاه داده پشتیبانی شده توسط EF Core)

---

## 🤝 مشارکت | Contributing

1. Fork کنید
2. Branch جدید بسازید: `git checkout -b feature/amazing-feature`
3. Commit کنید: `git commit -m 'Add some amazing feature'`
4. Push کنید: `git push origin feature/amazing-feature`
5. Pull Request باز کنید

---

## 📝 مجوز | License

این پروژه تحت مجوز **MIT** منتشر شده است.

---

## 📞 ارتباط | Contact

- **ایجاد کننده:** [Tohid Rajabali]
- **گیت‌هاب:** [Your GitHub Profile](https://github.com/tnix138)

---

## ⭐ حمایت

اگر این پروژه برای شما مفید بود، لطفاً یک ⭐ به آن بدهید!

---

**ساخته شده با ❤️ برای جامعه دات‌نت ایران** | Made with ❤️ for the .NET Community
