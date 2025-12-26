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
