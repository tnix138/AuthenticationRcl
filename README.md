# 🔐 AuthenticationRcl

> ماژول احراز هویت پیشرفته برای ASP.NET Core MVC | Authentication Module for ASP.NET Core MVC

[![.NET](https://img.shields.io/badge/.NET-9.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/license-MIT-green)](LICENSE)
[![Tests](https://img.shields.io/badge/tests-35%2B-brightgreen)](tests/)
[![PRs Welcome](https://img.shields.io/badge/PRs-welcome-brightgreen.svg)](http://makeapullrequest.com)

---

## 📖 معرفی | Introduction

**AuthenticationRcl** یک ماژول مستقل و کامل برای مدیریت احراز هویت در پروژه‌های ASP.NET Core MVC است. این ماژول با استفاده از بهترین روش‌های امنیتی، تمام نیازهای احراز هویت را پوشش می‌دهد.

**ویژگی‌های کلیدی:**

- ✅ **JWT و Refresh Token** | احراز هویت امن و مقیاس‌پذیر
- ✅ **BCrypt** | هش کردن رمز عبور با بالاترین سطح امنیت
- ✅ **Rate Limiting** | محدودیت درخواست برای جلوگیری از حملات Brute Force
- ✅ **قفل شدن خودکار** | بعد از ۵ تلاش ناموفق، کاربر به مدت ۱۵ دقیقه قفل می‌شود
- ✅ **بازنشانی رمز عبور** | با توکن و زمان انقضا
- ✅ **RESTful API** | طراحی کاملاً استاندارد
- ✅ **Seed Data** | کاربر ادمین و کاربر تست به صورت پیش‌فرض
- ✅ **تست واحد** | بیش از ۳۵ تست با xUnit و Moq
- ✅ **کامنت‌گذاری حرفه‌ای** | مستندسازی کامل کد

---

## 🚀 نصب و راه‌اندازی | Installation

### 1️⃣ ایجاد پروژه جدید

```bash
dotnet new mvc -n MyProject
cd MyProject
```

### 2️⃣ افزودن ارجاع به ماژول

```xml
<ProjectReference Include="..\Modules\AuthenticationRcl\AuthenticationRcl.csproj" />
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

app.MapControllers();
app.Run();
```

### 4️⃣ تنظیمات `appsettings.json`

```json
{
  "ConnectionStrings": {
    "AuthDb": "Server=localhost;Database=MyApp_Auth;Trusted_Connection=true;MultipleActiveResultSets=true;TrustServerCertificate=true"
  },
  "Jwt": {
    "SecretKey": "YourSuperSecretKeyWithMinimum32CharactersLong!",
    "Issuer": "https://localhost:7001",
    "Audience": "https://localhost:7001"
  },
  "RateLimiting": {
    "Global": { "PermitLimit": 100, "WindowInMinutes": 1 },
    "Login": { "PermitLimit": 5, "WindowInMinutes": 1 },
    "Register": { "PermitLimit": 3, "WindowInMinutes": 5 }
  }
}
```

### 5️⃣ اعمال Migration

```bash
dotnet ef migrations add InitialCreate
dotnet ef database update
```

---

## 📚 مستندات API | API Documentation

### 🔑 عملیات احراز هویت

| متد | مسیر | توضیح | محدودیت |
|-----|------|-------|---------|
| `POST` | `/api/v1/users` | ثبت‌نام کاربر جدید | ۳ درخواست در ۵ دقیقه |
| `POST` | `/api/v1/auth/login` | ورود کاربر | ۵ درخواست در ۱ دقیقه |
| `DELETE` | `/api/v1/auth/sessions/{sessionId}` | خروج کاربر | - |
| `POST` | `/api/v1/auth/refresh-token` | تجدید توکن | ۵ درخواست در ۱ دقیقه |

### 👤 مدیریت کاربر

| متد | مسیر | توضیح |
|-----|------|-------|
| `GET` | `/api/v1/users/{userId}` | دریافت اطلاعات کاربر |
| `PUT` | `/api/v1/users/{userId}` | بروزرسانی کاربر |
| `DELETE` | `/api/v1/users/{userId}` | حذف کاربر |
| `PUT` | `/api/v1/users/{userId}/password` | تغییر رمز عبور |

### 🔐 بازنشانی رمز عبور

| متد | مسیر | توضیح | محدودیت |
|-----|------|-------|---------|
| `POST` | `/api/v1/auth/password-reset/request` | درخواست بازنشانی رمز | ۳ درخواست در ۱۰ دقیقه |
| `POST` | `/api/v1/auth/password-reset` | بازنشانی رمز با توکن | ۵ درخواست در ۵ دقیقه |

---

## 🔒 امنیت | Security

| ویژگی | توضیح |
|-------|-------|
| **BCrypt** | هش کردن رمز عبور با Salt خودکار |
| **Rate Limiting** | جلوگیری از حملات Brute Force و DDoS |
| **Lockout** | قفل شدن کاربر بعد از ۵ تلاش ناموفق |
| **JWT** | احراز هویت بدون وضعیت (Stateless) |
| **Refresh Token** | افزایش امنیت با توکن‌های کوتاه‌مدت |
| **CORS** | پشتیبانی از درخواست‌های Cross-Origin |

---

## 🧪 تست‌ها | Tests

```bash
# اجرای تست‌ها
dotnet test

# اجرای تست‌ها با جزئیات
dotnet test --verbosity detailed
```

**تعداد تست‌ها:** ۳۵+ تست واحد

| کلاس تست | تعداد تست |
|----------|-----------|
| `AuthServiceTests` | ۲۰ تست |
| `UsersControllerTests` | ۱۵ تست |

---

## 📂 ساختار پروژه | Project Structure

```
AuthenticationRcl/
├── Controllers/Api/
│   └── UsersController.cs          # RESTful API
├── Data/
│   └── SeedData.cs                 # داده‌های اولیه
├── Models/
│   ├── User.cs                     # مدل کاربر
│   └── AppDbContext.cs             # DbContext
├── Services/
│   ├── AuthService.cs              # سرویس احراز هویت
│   └── TokenService.cs             # سرویس JWT
├── ViewModels/
│   ├── ResponseBase.cs             # پاسخ پایه
│   └── VMLogin.cs                  # ViewModel‌های احراز هویت
├── ModuleInitializer.cs            # ثبت ماژول
└── AuthenticationRcl.csproj
```

---

## 🛠️ تکنولوژی‌ها | Technologies

| تکنولوژی | نسخه |
|-----------|------|
| .NET | 9.0 |
| Entity Framework Core | 9.0 |
| BCrypt.Net-Next | 4.0.3 |
| JWT Bearer | 9.0 |
| xUnit | 2.9.2 |
| Moq | 4.20.72 |

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

- **ایجاد کننده:** [Your Name]
- **ایمیل:** your.email@example.com
- **گیت‌هاب:** [Your GitHub Profile](https://github.com/tnix138)

---

## ⭐ حمایت

اگر این پروژه برای شما مفید بود، لطفاً یک ⭐ به آن بدهید!

---

**ساخته شده با ❤️ برای جامعه دات‌نت ایران** | Made with ❤️ for the .NET Community
