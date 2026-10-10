#  Gift Raffle Platform

[![Angular](https://img.shields.io/badge/Angular-20.x-DD0031?logo=angular&logoColor=white)](#)
[![.NET](https://img.shields.io/badge/.NET-9.0-512BD4?logo=dotnet&logoColor=white)](#)
[![EF%20Core](https://img.shields.io/badge/EF%20Core-9.0-6DB33F?logo=microsoft&logoColor=white)](#)
[![SQL%20Server](https://img.shields.io/badge/SQL%20Server-Database-CC2927?logo=microsoftsqlserver&logoColor=white)](#)
[![Auth](https://img.shields.io/badge/Auth-JWT-000000?logo=jsonwebtokens&logoColor=white)](#)

A full‑stack raffle & gifts management system built with **Angular** (client) and **ASP.NET Core Web API** (server), backed by **SQL Server**.  
Includes authentication/authorization with **JWT**, a layered architecture (Controllers → Services → Repositories), and **EF Core Migrations**.

> This is an extended fork of [rivky9523/lottery-management-system](https://github.com/rivky9523/lottery-management-system) by Rivky Peretz. See [What's new in this fork](#-whats-new-in-this-fork) for the backend additions.

---

##  Table of Contents
- [Key Features](#-key-features)
- [Architecture](#-architecture)
- [Tech Stack](#-tech-stack)
- [Project Structure](#-project-structure)
- [Quick Start](#-quick-start)
  - [Server Configuration (Secrets)](#-server-configuration-secrets)
  - [Database & Migrations](#-database--migrations)
  - [Run the Server](#-run-the-server)
  - [Run the Client](#-run-the-client)
- [Swagger](#-swagger)
- [Troubleshooting](#-troubleshooting)
- [Screenshots](#-screenshots)
- [Roadmap](#-roadmap)
- [Author](#-author)

---

##  Key Features
-  Register / Login
-  JWT Authentication + Authorization
-  Role‑based access (Admin / User)
-  Raffle / Lottery management
-  Categories management
-  Prizes management
-  Donors management
-  Cart &  checkout flow
-  Clean REST API + layered architecture (Controllers / Services / Repositories)
-  EF Core + Migrations

---

##  Architecture
```
Angular SPA  →  ASP.NET Core Web API  →  SQL Server
         Auth: JWT         ORM: EF Core
```

---

## 🛠 Tech Stack

### Frontend
- Angular 20 (TypeScript, HTML, SCSS)
- PrimeNG / PrimeFlex

### Backend
- .NET 9 (ASP.NET Core Web API)
- Entity Framework Core (SQL Server provider)
- JWT Bearer Authentication
- Swagger (OpenAPI)
- Serilog (Console + rolling file logs)

### Database
- Microsoft SQL Server

---

##  Project Structure
```
gift-raffle-platform/
│
├── client/                 # Angular app
│   ├── src/
│   ├── public/
│   └── package.json
│
└── server/                 # .NET solution
    ├── ex1.sln
    └── ex1/
        ├── Controllers/
        ├── Services/
        ├── Repository/
        ├── Data/
        ├── Models/
        ├── Migrations/
        └── Program.cs
```

---

## 🚀 Quick Start

### 1) Clone
```bash
git clone https://github.com/rivky9523/Project-Angular-Api-Entity-Framework-.git
cd gift-raffle-platform
```

---

## 🔑 Server Configuration (Secrets)

This repository **does not commit** local settings files (e.g., `appsettings.json`) and **never** commits secrets.

The server expects:
- `ConnectionStrings:DefaultConnection`
- `RateLimiting:JwtSettings:Issuer`
- `RateLimiting:JwtSettings:Audience`
- `RateLimiting:JwtSettings:SecretKey`

> Note: The JWT section key is **RateLimiting:JwtSettings** in `Program.cs`.

### Option A — User Secrets (recommended for local dev)
**PowerShell (Windows):**
```powershell
cd server\ex1
dotnet user-secrets init

dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=.;Database=GiftRaffleDb;Trusted_Connection=True;TrustServerCertificate=True;"
dotnet user-secrets set "RateLimiting:JwtSettings:Issuer" "GiftRaffleApi"
dotnet user-secrets set "RateLimiting:JwtSettings:Audience" "GiftRaffleClient"
dotnet user-secrets set "RateLimiting:JwtSettings:SecretKey" "PUT_A_STRONG_SECRET_KEY_HERE"
```

### Option B — Environment Variables
**PowerShell:**
```powershell
$env:ConnectionStrings__DefaultConnection="Server=.;Database=GiftRaffleDb;Trusted_Connection=True;TrustServerCertificate=True;"
$env:RateLimiting__JwtSettings__Issuer="GiftRaffleApi"
$env:RateLimiting__JwtSettings__Audience="GiftRaffleClient"
$env:RateLimiting__JwtSettings__SecretKey="PUT_A_STRONG_SECRET_KEY_HERE"
```

---

## 🗄 Database & Migrations
From `server\ex1`:

> If `dotnet ef` is missing, install it once:
```bash
dotnet tool install --global dotnet-ef
```

Then run:
```bash
dotnet tool restore
dotnet ef database update
```

---

## 🖥 Run the Server
From the repository root:

```bash
cd server
dotnet restore
dotnet build
dotnet run --project ex1
```

Default URLs (from `launchSettings.json`):
- `http://localhost:5274`
- `https://localhost:7063`

---

## 🌐 Run the Client
From the repository root:

```bash
cd client
npm ci
ng serve
```

Client URL:
- `http://localhost:4200`

---

## 📘 Swagger
When the server is running:
- Swagger UI: `http://localhost:5274/swagger`

---

## 🧯 Troubleshooting

### Node / npm issues
Check installed versions:
```bash
node -v
npm -v
```
If `npm ci` fails, try:
```bash
npm install
```

### CORS blocked requests
The server includes a CORS policy for:
- `http://localhost:4200`

If you change the client URL/port, update the allowed origin in `Program.cs`.

### Database connection errors
- Make sure SQL Server is running
- Verify `DefaultConnection`
- Re-run migrations:
```bash
cd server/ex1
dotnet ef database update
```

---

## 📸 Screenshots

### Home
![Home](screenshots/home.png)

### Browse Prizes / Raffles
![Browse](screenshots/browse.png)

### Login
![Login](screenshots/login.png)

### Admin Dashboard
![Admin Dashboard](screenshots/admin-dashboard.png)

### Create Prize (Admin)
![Create Prize](screenshots/create-prize.png)

### Swagger (API Documentation)
![Swagger](screenshots/swagger.png)

---

## 🧭 Roadmap
- Move API base URL in Angular to `environment.ts` (avoid hardcoded localhost)
- ~~Add automated tests (unit/integration)~~ (done in this fork)
- ~~Add Docker (API + SQL Server)~~ (done in this fork; client not yet containerised)
- Add CI workflow (build + test on PR)

---

## ✨ What's new in this fork

Backend extensions by [Vivashwan Ghosh](https://github.com/Vivashwan). All changes are under `server/`.

### 🎯 Fair, concurrency-safe raffle
- **Ticket-weighted draw**: a user with 3 tickets is 3× as likely to win as a user with 1 (previously every buyer had equal odds regardless of tickets). Uses `RandomNumberGenerator` instead of `System.Random`, behind an `IRandomSource` so it can be tested deterministically.
- **No double draws**: the winner is recorded by the stored procedure `dbo.usp_RecordWinner`, whose `UPDATE … WHERE IsRaffleDone = 0` makes check-and-set a single atomic statement. An integration test fires 20 concurrent draws at real SQL Server and asserts exactly one wins.

### 🗄 SQL Server
- Migration `AddRaffleStoredProcedures`: `dbo.fn_PrizeTicketCount` (scalar function), `dbo.usp_RecordWinner`, and `dbo.usp_GetRaffleReport` (tickets sold, revenue and winner per prize, exposed at `GET /api/lottery/report`).
- Migration `SyncTicketModel`: the `Ticket` configuration had been commented out after the first migration, leaving the snapshot out of sync (a stray `PrizeId1` column). EF Core 9 refuses to migrate in that state; this migration realigns the schema.

### 📨 RabbitMQ + Worker Service
- Winner emails no longer block the HTTP request. The API publishes a `WinnerDrawnEvent`; a separate **.NET Worker Service** (`Lottery.NotificationWorker`, installable as a **Windows Service**) consumes it and sends the email.
- Manual acknowledgements (a crash mid-send redelivers), a **dead-letter queue** for messages that fail, prefetch limits, and parallel consumers (`ConsumerConcurrency`).
- If the broker is down, the draw still succeeds and the response reports `notificationQueued: false`. The existing `emailSent` field is kept for the Angular client.

### 🧩 Autofac & error handling
- **Autofac** replaces the built-in container. Repositories and services are registered by convention in modules (`DependencyInjection/AutofacModules.cs`) instead of one line each.
- **Typed exceptions + middleware** (`NotFoundException`, `ConflictException`, `BusinessRuleException`) produce RFC 7807 problem responses. This replaced matching on Hebrew exception messages in the controller, which had a typo that turned "prize not found" into a 500.

### 🧪 Tests (TDD)
64 xUnit tests (Moq, FluentAssertions), up from 26 with one failing:
- Weighted picker: roll-to-ticket mapping and a statistical check that win rate matches ticket share
- Lottery service, exception middleware, notification handler
- Container composition: every controller resolves from the real Autofac container, with correct lifetimes
- SQL Server integration (skipped unless `LOTTERY_TEST_SQL` is set): migrations, stored procedures, function, and the concurrency test

### 🐳 Build & run
- `docker compose up --build` (in `server/`) runs the API, the worker, SQL Server 2022 and RabbitMQ, applying migrations on startup. Copy `.env.example` to `.env` first.
- `build.ps1` (PowerShell): restore → Release build → tests with coverage → optional Docker images.
- `Directory.Build.props` / `Directory.Build.targets`: shared MSBuild settings and a custom target that stamps the git commit into every assembly's version (e.g. `1.1.0+650e0b9`).

---

## 👩‍💻 Author

Rivky Peretz
[GitHub](https://github.com/rivky9523)
[Email](mailto:r0548551732@gmail.com)
