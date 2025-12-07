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

### First Run Setup

On first startup, an admin user is automatically created using environment variables:
- Default email: `admin@truedope.io`
- Default password: `ChangeMe123!`

**Important:** Change the admin password immediately after first login!

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

### Authentication Endpoints

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/api/auth/register` | POST | Register new user |
| `/api/auth/login` | POST | Login and get tokens |
| `/api/auth/refresh` | POST | Refresh access token |
| `/api/auth/logout` | POST | Logout (requires auth) |
| `/api/auth/forgot-password` | POST | Request password reset email |
| `/api/auth/reset-password` | POST | Reset password with token |

### User Profile Endpoints (Requires Auth)

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/api/users/profile` | GET | Get current user profile |
| `/api/users/profile` | PUT | Update profile |
| `/api/users/password` | PUT | Change password |

### Admin Endpoints (Requires Admin)

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/api/admin/users` | GET | List all users (paginated) |
| `/api/admin/users/{id}` | GET | Get user details |
| `/api/admin/users/{id}` | PUT | Update user (admin, disabled) |
| `/api/admin/users/{id}/reset-password` | POST | Reset user password |

## Authentication

TrueDope uses JWT-based authentication:

1. **Login** - POST to `/api/auth/login` with email and password
2. **Access Token** - Short-lived (15 min), included in `Authorization: Bearer <token>` header
3. **Refresh Token** - Long-lived (7 days), stored in Redis, used to get new access tokens

### Example Login Request

```bash
curl -X POST http://localhost:8080/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email": "admin@truedope.io", "password": "ChangeMe123!"}'
```

### Example Authenticated Request

```bash
curl http://localhost:8080/api/users/profile \
  -H "Authorization: Bearer <access_token>"
```

## Environment Variables

See `.env.example` for all available configuration options.

### Key Variables

| Variable | Description |
|----------|-------------|
| `JWT_SECRET_KEY` | Secret key for JWT signing (min 32 chars) |
| `ADMIN_EMAIL` | Initial admin email (first run only) |
| `ADMIN_PASSWORD` | Initial admin password (first run only) |
| `SMTP_*` | Email settings for password reset |
| `FRONTEND_URL` | Frontend URL for password reset links |

## Deployment

Deployment is automated via GitHub Actions when pushing to `main`. The workflow:
1. Builds and tests both backend and frontend
2. Deploys to production server via self-hosted runner
3. Performs health check
4. Rolls back on failure

## License

Private project - All rights reserved.
