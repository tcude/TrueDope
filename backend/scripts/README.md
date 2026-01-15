# TrueDope Database Scripts

## db-sync.sh

Export and import PostgreSQL databases between environments.

### Quick Start

```bash
# Export local database
./db-sync.sh export

# List available exports
./db-sync.sh list

# Import to local Docker
./db-sync.sh import exports/TrueDope_v2_Export_local_20251226.sql

# Import to remote database
./db-sync.sh import exports/TrueDope_v2_Export_local_20251226.sql --target remote
```

### Remote Database Setup

For remote imports/exports, create `.env.remote`:

```bash
REMOTE_DB_HOST=your-db-host.com
REMOTE_DB_PORT=5432
REMOTE_DB_NAME=truedope
REMOTE_DB_USER=postgres
REMOTE_DB_PASSWORD=your_password
```

### Full Usage

```bash
./db-sync.sh --help
```

---

## Clone User Data API

Admin API endpoint to clone all data from one user to another. Useful for setting up test accounts with realistic data.

### Overview

This feature allows administrators to:
- Copy all data (rifles, ammo, sessions, images, etc.) from a source user to a target user
- Target user's existing data is **completely deleted** before cloning
- Images are duplicated in MinIO with new file paths (full data independence)
- Admin flag is never copied (target user stays non-admin)

### Prerequisites

- Must be logged in as an admin user
- Both source and target users must exist
- Access to the API (via Swagger UI or curl)

### Usage

#### 1. Preview (Dry Run)

See what would be deleted and copied without making changes:

```bash
curl -X POST "http://localhost:8080/api/admin/clone-user-data/preview" \
  -H "Authorization: Bearer YOUR_ADMIN_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "sourceUserId": "source-user-guid",
    "targetUserId": "target-user-guid"
  }'
```

Response shows counts of data to be deleted from target and copied from source.

#### 2. Execute Clone

Actually perform the clone operation:

```bash
curl -X POST "http://localhost:8080/api/admin/clone-user-data" \
  -H "Authorization: Bearer YOUR_ADMIN_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "sourceUserId": "source-user-guid",
    "targetUserId": "target-user-guid",
    "confirmOverwrite": true
  }'
```

**Important:** The `confirmOverwrite: true` flag is required to proceed.

### Example Response

```json
{
  "success": true,
  "data": {
    "success": true,
    "sourceUserId": "abc123...",
    "targetUserId": "def456...",
    "statistics": {
      "rifleSetupsCopied": 5,
      "ammunitionCopied": 10,
      "ammoLotsCopied": 3,
      "savedLocationsCopied": 2,
      "rangeSessionsCopied": 15,
      "dopeEntriesCopied": 45,
      "chronoSessionsCopied": 8,
      "velocityReadingsCopied": 80,
      "groupEntriesCopied": 12,
      "groupMeasurementsCopied": 6,
      "imagesCopied": 25,
      "imageBytesCopied": 52428800,
      "userPreferencesCopied": true,
      "rifleSetupsDeleted": 0,
      "ammunitionDeleted": 0,
      "rangeSessionsDeleted": 0,
      "imagesDeleted": 0
    },
    "completedAt": "2026-01-14T12:00:00Z",
    "durationMs": 3500
  },
  "message": "User data cloned successfully"
}
```

### Using Swagger UI

1. Navigate to `http://localhost:8080/swagger`
2. Authenticate with an admin JWT token (click "Authorize")
3. Find `POST /api/admin/clone-user-data/preview` or `POST /api/admin/clone-user-data`
4. Click "Try it out" and fill in the request body

### Finding User IDs

To get user IDs, use the admin users list endpoint:

```bash
curl -X GET "http://localhost:8080/api/admin/users" \
  -H "Authorization: Bearer YOUR_ADMIN_JWT_TOKEN"
```

Or search for a specific user:

```bash
curl -X GET "http://localhost:8080/api/admin/users?search=test@example.com" \
  -H "Authorization: Bearer YOUR_ADMIN_JWT_TOKEN"
```

### Typical Workflow

1. Create a test user account (register via app or API)
2. Get both user IDs from `/api/admin/users`
3. Run preview to verify source has data and target will be cleared
4. Execute clone with `confirmOverwrite: true`
5. Log in as test user to verify data was copied

### Notes

- The operation is atomic - if anything fails, all changes are rolled back
- MinIO images copied during a failed operation are automatically cleaned up
- All clone operations are logged in the admin audit log
- Large datasets with many images may take several seconds to complete
