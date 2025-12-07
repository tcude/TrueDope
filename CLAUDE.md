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
│   │   ├── Controllers/     # API endpoints (all prefixed with /api/)
│   │   ├── Services/        # Business logic
│   │   ├── Data/Entities/   # EF Core entities
│   │   ├── DTOs/            # Request/response models
│   │   ├── Middleware/      # Custom middleware
│   │   └── Migrations/      # EF migrations
│   └── tests/TrueDope.Api.Tests/
├── frontend/
│   └── src/
│       ├── components/      # Reusable UI components
│       ├── pages/           # Route pages
│       ├── hooks/           # Custom React hooks
│       ├── services/        # API client
│       ├── stores/          # Zustand state management
│       ├── types/           # TypeScript types
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

### Frontend Patterns
- Tailwind CSS v4 (uses `@import "tailwindcss"` syntax)
- React Router for routing
- Zustand for state management
- Axios for API calls

## Key Endpoints

- `GET /api/health` - Health check with service status

## Data Storage

Bind mounts in `.data/` directory:
- `.data/postgres/` - PostgreSQL data
- `.data/minio/` - Object storage
- `.data/redis/` - Redis persistence

## Environment Configuration

See `.env.example` for available variables. Key settings:
- `ASPNETCORE_ENVIRONMENT` - Development/Production
- `DB_*` - Database credentials
- `MINIO_*` - Object storage credentials
- `JWT_*` - Authentication settings

## Specifications

Phase documents in `.specs/`:
- `20251206.MANIFEST.md` - Overall project plan
- `20251207.Phase1.md` - Phase 1 setup details
