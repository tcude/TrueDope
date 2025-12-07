# TrueDope v2 - Claude Instructions

## Project Overview

TrueDope v2 is a ballistics data logging platform with a session-first architecture. This is a complete rewrite of v1.

**Stack:**
- Backend: ASP.NET Core 8 Web API
- Frontend: React + Vite + TypeScript + Tailwind CSS v4
- Database: PostgreSQL 15
- Storage: MinIO (S3-compatible)
- Cache: Redis

## Project Structure

```
TrueDope/
├── .github/workflows/       # CI/CD pipelines
├── .specs/                  # Project specifications and phase docs
├── backend/
│   ├── src/TrueDope.Api/
│   │   ├── Configuration/   # Settings classes (JwtSettings, SmtpSettings)
│   │   ├── Controllers/     # API endpoints (all prefixed with /api/)
│   │   ├── Services/        # Business logic (JwtService, EmailService)
│   │   ├── Data/
│   │   │   ├── Entities/    # EF Core entities (User)
│   │   │   ├── ApplicationDbContext.cs
│   │   │   └── DbSeeder.cs  # Initial admin seeding
│   │   ├── DTOs/            # Request/response models
│   │   │   ├── Auth/        # Login, Register, Refresh, etc.
│   │   │   ├── Users/       # Profile DTOs
│   │   │   └── Admin/       # Admin user management DTOs
│   │   ├── Middleware/      # Custom middleware
│   │   └── Migrations/      # EF migrations
│   └── tests/TrueDope.Api.Tests/
│       ├── Services/        # JwtServiceTests
│       └── Controllers/     # AuthControllerTests
├── frontend/
│   └── src/
│       ├── components/
│       │   ├── layout/      # Header, Layout
│       │   └── ui/          # Button, Card, Input, etc.
│       ├── pages/
│       │   ├── auth/        # Login, Register, ForgotPassword, ResetPassword
│       │   └── settings/    # Settings, Profile, Password, AdminUsers
│       ├── hooks/           # useAuth custom hook
│       ├── services/        # API client, auth service
│       ├── stores/          # Zustand stores (auth.store)
│       ├── types/           # TypeScript types (auth.ts)
│       └── utils/           # Helpers
└── docker-compose.yml
```

## Development Commands

### Docker (Primary Method)
```bash
# Start all services
docker compose up -d

# Rebuild after changes
docker compose down && docker compose build && docker compose up -d

# View logs
docker compose logs -f backend
docker compose logs -f frontend

# Stop and remove volumes
docker compose down -v
```

### Backend (Local)
```bash
cd backend
dotnet run --project src/TrueDope.Api
dotnet test
dotnet ef migrations add <Name> --project src/TrueDope.Api
```

### Frontend (Local)
```bash
cd frontend
npm run dev
npm run build
npx tsc --noEmit
```

## Architecture Decisions

### API Design
- All endpoints prefixed with `/api/`
- Consistent response wrapper: `{ success, data, error, message }`
- Pagination: `{ items, pagination: { currentPage, pageSize, totalItems, totalPages } }`
- JWT Bearer authentication

### Backend Patterns
- Direct DbContext usage (no repository pattern)
- Serilog for structured logging
- Global exception handling middleware
- Controllers in `/api/[controller]` routes
- ASP.NET Core Identity for user management (extended User entity)

### Frontend Patterns
- Tailwind CSS v4 (uses `@import "tailwindcss"` syntax)
- React Router for routing
- Zustand for state management (auth.store persisted)
- Axios for API calls with automatic token refresh interceptor

## Key Endpoints

### Health
- `GET /api/health` - Health check with service status

### Authentication
- `POST /api/auth/register` - Register new user
- `POST /api/auth/login` - Login, returns access + refresh tokens
- `POST /api/auth/refresh` - Refresh access token using refresh token
- `POST /api/auth/logout` - Logout, revokes refresh token
- `POST /api/auth/forgot-password` - Request password reset email
- `POST /api/auth/reset-password` - Reset password with token

### User Profile (Requires Auth)
- `GET /api/users/profile` - Get current user profile
- `PUT /api/users/profile` - Update profile (firstName, lastName)
- `PUT /api/users/password` - Change password

### Admin (Requires Admin Role)
- `GET /api/admin/users` - List all users (paginated, searchable)
- `GET /api/admin/users/{id}` - Get user details
- `PUT /api/admin/users/{id}` - Update user (isAdmin, disabled)
- `POST /api/admin/users/{id}/reset-password` - Reset user password

## Authentication Flow

1. User logs in via `/api/auth/login`
2. Receives `accessToken` (15 min) and `refreshToken` (7 days)
3. Access token stored in localStorage, used in `Authorization: Bearer` header
4. Refresh token stored in localStorage and Redis
5. When access token expires, frontend auto-refreshes via `/api/auth/refresh`
6. On logout, refresh token is revoked from Redis

## Data Storage

Bind mounts in `.data/` directory:
- `.data/postgres/` - PostgreSQL data
- `.data/minio/` - Object storage
- `.data/redis/` - Redis persistence (refresh tokens)

## Environment Configuration

See `.env.example` for available variables. Key settings:
- `ASPNETCORE_ENVIRONMENT` - Development/Production
- `DB_*` - Database credentials
- `MINIO_*` - Object storage credentials
- `JWT_*` - Authentication settings
- `ADMIN_*` - Initial admin user (first run only)
- `SMTP_*` - Email settings for password reset
- `FRONTEND_URL` - For password reset links

## First Run

On first startup, if no users exist:
1. DbSeeder creates admin user from `ADMIN_*` env variables
2. Default: `admin@truedope.io` / `ChangeMe123!`
3. Change password immediately after first login!

## Test Coverage

Backend tests in `tests/TrueDope.Api.Tests/`:
- `JwtServiceTests` - Token generation, refresh token Redis operations
- `AuthControllerTests` - Register, Login, Refresh, Logout flows

Run tests: `dotnet test` in `backend/` directory

## Specifications

Phase documents in `.specs/`:
- `20251206.MANIFEST.md` - Overall project plan
- `20251207.Phase1.md` - Phase 1: Foundation setup (complete)
- `20251208.Phase2.md` - Phase 2: Authentication (complete)
