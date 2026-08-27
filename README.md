# TodoApp - ASP.NET Core MVC

A task management web application built with ASP.NET Core 10 MVC, featuring user authentication, task CRUD operations, and email notifications.

## Tech Stack

| Layer | Technology |
|-------|------------|
| Framework | ASP.NET Core 10 MVC |
| Database | SQLite (dev) / SQL Server (prod) |
| ORM | Entity Framework Core 10 |
| Authentication | ASP.NET Core Identity (cookie-based) |
| Frontend | Razor Views, Bootstrap 5, Bootstrap Icons |
| Email | System.Net.Mail (SMTP) |
| Testing | xUnit, Moq, EF Core InMemory |

## Architecture

```
TodoApp/
├── TodoApp.sln                    # Solution file
├── Makefile                       # Development commands
│
├── TodoApp.Web/                   # Main web application
│   ├── Program.cs                 # Entry point, DI configuration
│   ├── appsettings.json           # Configuration (connection string, SMTP)
│   │
│   ├── Controllers/
│   │   ├── HomeController.cs      # Landing page (redirects if authenticated)
│   │   ├── AccountController.cs   # Auth: login, register, password reset
│   │   └── TaskController.cs      # Task CRUD with filtering
│   │
│   ├── Models/
│   │   ├── Entities/
│   │   │   ├── ApplicationUser.cs # User entity (extends IdentityUser)
│   │   │   └── TodoTask.cs        # Task entity
│   │   └── ViewModels/
│   │       ├── Account/           # Login, Register, ForgotPassword, ResetPassword
│   │       └── Task/              # CreateTask, TaskItem, TaskList
│   │
│   ├── Data/
│   │   └── ApplicationDbContext.cs # EF Core DbContext
│   │
│   ├── Services/
│   │   ├── IEmailService.cs       # Email interface
│   │   └── EmailService.cs        # SMTP implementation
│   │
│   ├── Views/
│   │   ├── Shared/                # _Layout.cshtml, _LoginPartial.cshtml
│   │   ├── Home/                  # Landing page
│   │   ├── Account/               # Auth views
│   │   └── Task/                  # Task dashboard, partials
│   │
│   └── wwwroot/
│       ├── css/site.css           # Custom styles
│       └── js/
│           ├── site.js            # General JS
│           └── task.js            # AJAX toggle/delete
│
└── TodoApp.Web.Tests/             # Unit tests
    ├── Controllers/               # Controller tests
    ├── Services/                  # Service tests
    └── Models/                    # ViewModel tests
```

## Quick Start

```bash
cd TodoApp
make restore      # Install dependencies
make migrate      # Create database
make run          # Start at http://localhost:5000
```

## Development Commands

Run `make` to see all available commands:

| Command | Description |
|---------|-------------|
| `make run` | Start the app at http://localhost:5000 |
| `make watch` | Start with hot reload |
| `make test` | Run all 60 unit tests |
| `make build` | Build the solution |
| `make clean` | Clean build artifacts |
| `make migrate` | Apply pending migrations |
| `make reset-db` | Delete and recreate database |
| `make new-migration NAME=X` | Create a new migration |
| `make publish` | Build for production |

## Core Features

### Authentication
- **Register**: Email, name, password (8+ chars, upper, lower, digit, symbol)
- **Login**: Email/password with "remember me" (3-day cookie)
- **Password Reset**: Email-based reset flow
- **Security**: Account lockout after 5 failed attempts (5 min)

### Tasks
- **Create**: Title (required) + optional description
- **Toggle**: Mark complete/incomplete (AJAX, no page reload)
- **Delete**: Remove task (AJAX with confirmation)
- **Filter**: All / Active / Completed tabs
- **Isolation**: Users only see their own tasks

### Email Notifications
- Password reset emails (HTML formatted)
- Task creation confirmation (optional, requires SMTP config)

## Key Routes

| Route | Auth | Description |
|-------|------|-------------|
| `GET /` | No | Landing page (redirects to /Task if logged in) |
| `GET /Account/Login` | No | Login form |
| `GET /Account/Register` | No | Registration form |
| `POST /Account/Logout` | Yes | Sign out |
| `GET /Account/ForgotPassword` | No | Request password reset |
| `GET /Task` | Yes | Task dashboard |
| `GET /Task?filter=active` | Yes | Filter by status |
| `POST /Task/Create` | Yes | Create new task |
| `POST /Task/Toggle/{id}` | Yes | Toggle completion |
| `POST /Task/Delete/{id}` | Yes | Delete task |

## Configuration

### Database

**Development (SQLite)** - Default, no setup required:
```json
"ConnectionStrings": {
  "DefaultConnection": "Data Source=TodoApp.db"
}
```

**Production (SQL Server)** - Requires changes:
1. Update `TodoApp.Web.csproj`: Change `Sqlite` package to `SqlServer`
2. Update `Program.cs`: Change `UseSqlite` to `UseSqlServer`
3. Update connection string in `appsettings.json`
4. Run `make reset-db`

### Email (Optional)

Configure SMTP in `appsettings.json` for password reset and notifications:

```json
"SmtpSettings": {
  "Host": "smtp.gmail.com",
  "Port": 587,
  "EnableSsl": true,
  "Username": "your-email@gmail.com",
  "Password": "your-app-password",
  "FromEmail": "noreply@todoapp.com",
  "FromName": "Todo App"
}
```

> **Gmail**: Use an [App Password](https://support.google.com/accounts/answer/185833), not your regular password.

## Testing

60 unit tests covering:

| Area | Tests | Coverage |
|------|-------|----------|
| HomeController | 3 | Redirect logic, error handling |
| AccountController | 24 | All auth flows |
| TaskController | 14 | CRUD, filtering, user isolation |
| EmailService | 4 | SMTP config, error resilience |
| TaskItemViewModel | 15 | TimeAgo helper |

```bash
make test              # Run all tests
make test-verbose      # Detailed output
```

## Key Design Decisions

1. **Cookie Authentication**: Server-rendered app uses cookies (not JWT) for simplicity
2. **User Isolation**: Tasks filtered by `UserId` in all queries - users never see others' data
3. **AJAX for Toggle/Delete**: Smooth UX without page reloads, falls back to redirect if JS disabled
4. **Email Failure Resilience**: Email errors are logged but don't break app flow
5. **TimeAgo Helper**: Computed property on ViewModel for human-readable timestamps
6. **Anti-Enumeration**: Password reset always shows success (prevents email discovery)

## Database Schema

```
AspNetUsers (Identity)
├── Id (string, PK)
├── Name (string)
├── Email (string)
├── PasswordHash (string)
├── ...Identity fields...
└── CreatedAt, UpdatedAt

Tasks
├── Id (int, PK, auto-increment)
├── Title (string, required, max 200)
├── Description (string, nullable, max 1000)
├── Completed (bool, default false)
├── UserId (string, FK → AspNetUsers)
└── CreatedAt, UpdatedAt
```

## Common Tasks

### Add a new field to Task

1. Update `Models/Entities/TodoTask.cs`
2. Update ViewModels if needed
3. `make new-migration NAME=AddFieldName`
4. `make migrate`
5. Update Views to display/edit the field

### Add a new controller

1. Create `Controllers/NewController.cs`
2. Add `[Authorize]` attribute if auth required
3. Create `Views/New/` folder with views
4. Add navigation link in `Views/Shared/_Layout.cshtml`

### Change password requirements

Edit `Program.cs`:
```csharp
options.Password.RequiredLength = 8;
options.Password.RequireUppercase = true;
// etc.
```

## Troubleshooting

| Issue | Solution |
|-------|----------|
| `dotnet: command not found` | Install .NET SDK or add to PATH |
| `dotnet-ef: command not found` | Run `dotnet tool install --global dotnet-ef` and add `~/.dotnet/tools` to PATH |
| LocalDB not supported | You're on macOS - use SQLite (default) or Docker for SQL Server |
| SMTP errors | Check credentials; Gmail needs App Password; errors are logged but non-blocking |
