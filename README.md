# TrueDope v2

Ballistics data logging and analytics platform.

## Quick Start

### Prerequisites
- Docker and Docker Compose
- .NET 8 SDK (for local backend development)
- Node.js 20+ (for local frontend development)

### Running with Docker (Recommended)

```bash
# Copy environment file and configure
cp .env.example .env

# Start all services
docker compose up -d

# View logs
docker compose logs -f

# Stop services
docker compose down
```

**Services:**
- Frontend: http://localhost:3000
- Backend API: http://localhost:8080
- Swagger UI: http://localhost:8080/swagger (Development only)
- MinIO Console: http://localhost:9001

### Rebuilding After Changes

```bash
docker compose down && docker compose build && docker compose up -d
```

## Project Structure

```
TrueDope/
├── .github/workflows/    # CI/CD pipelines
├── .specs/               # Project specifications
├── backend/              # ASP.NET Core 8 API
│   ├── src/TrueDope.Api/ # Main API project
│   └── tests/            # Unit tests
├── frontend/             # React + Vite + TypeScript
│   └── src/              # Frontend source
├── .data/                # Bind-mounted data (gitignored)
│   ├── postgres/         # PostgreSQL data
│   ├── minio/            # MinIO object storage
│   └── redis/            # Redis data
└── docker-compose.yml    # Local development setup
```

## Development

### Backend Only

```bash
cd backend
dotnet run --project src/TrueDope.Api
```

### Frontend Only

```bash
cd frontend
npm install
npm run dev
```

### Running Tests

```bash
# Backend tests
cd backend
dotnet test

# Frontend type check
cd frontend
npx tsc --noEmit
```

## API

All API endpoints are prefixed with `/api/`.

### Health Check

```
GET /api/health
```

Returns API status, version, and environment information.

## Environment Variables

See `.env.example` for all available configuration options.

## Deployment

Deployment is automated via GitHub Actions when pushing to `main`. The workflow:
1. Builds and tests both backend and frontend
2. Deploys to production server via self-hosted runner
3. Performs health check
4. Rolls back on failure

## License

Private project - All rights reserved.
