#!/bin/bash

# TrueDope v2 Database Sync Script
# Unified script for exporting and importing databases between environments
#
# Usage:
#   ./db-sync.sh export [--source local|remote] [--output <path>]
#   ./db-sync.sh import <file> [--target local|remote]
#   ./db-sync.sh list                    # List available exports
#
# Examples:
#   ./db-sync.sh export                  # Export from local Docker
#   ./db-sync.sh export --source remote  # Export from remote/prod database
#   ./db-sync.sh import exports/backup.sql           # Import to local Docker
#   ./db-sync.sh import exports/backup.sql --target remote  # Import to remote/prod

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
WHITE='\033[1;37m'
BOLD='\033[1m'
NC='\033[0m' # No Color

# Script directory
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
TRUEDOPE_DIR="$(cd "$SCRIPT_DIR/../.." && pwd)"
EXPORTS_DIR="$SCRIPT_DIR/../exports"

print_status() {
    echo -e "${1}${2}${NC}"
}

print_header() {
    echo ""
    print_status "$GREEN" "============================================"
    print_status "$GREEN" "  TrueDope v2 Database Sync"
    print_status "$GREEN" "============================================"
    echo ""
}

show_help() {
    print_header
    print_status "$WHITE" "USAGE:"
    echo "  $0 export [OPTIONS]    Export database to SQL file"
    echo "  $0 import <file> [OPTIONS]  Import SQL file to database"
    echo "  $0 list               List available export files"
    echo ""
    print_status "$WHITE" "EXPORT OPTIONS:"
    echo "  --source local        Export from local Docker (default)"
    echo "  --source remote       Export from remote database"
    echo "  --output, -o <path>   Output directory (default: ./exports)"
    echo "  --container, -c <name> Docker container name (local only)"
    echo ""
    print_status "$WHITE" "IMPORT OPTIONS:"
    echo "  --target local        Import to local Docker (default)"
    echo "  --target remote       Import to remote database"
    echo "  --container, -c <name> Docker container name (local only)"
    echo "  --yes, -y             Skip confirmation prompt"
    echo ""
    print_status "$WHITE" "REMOTE DATABASE CONFIG:"
    echo "  Configure via environment variables or .env.remote file:"
    echo "    REMOTE_DB_HOST      Remote database host"
    echo "    REMOTE_DB_PORT      Remote database port (default: 5432)"
    echo "    REMOTE_DB_NAME      Remote database name"
    echo "    REMOTE_DB_USER      Remote database user"
    echo "    REMOTE_DB_PASSWORD  Remote database password"
    echo ""
    print_status "$WHITE" "EXAMPLES:"
    echo "  # Export local dev database"
    echo "  $0 export"
    echo ""
    echo "  # Export production database"
    echo "  $0 export --source remote"
    echo ""
    echo "  # Import to local dev (typical: restore prod data locally)"
    echo "  $0 import exports/TrueDope_v2_Export_20241226.sql"
    echo ""
    echo "  # Import local export to production (use with caution!)"
    echo "  $0 import exports/TrueDope_v2_Export_20241226.sql --target remote"
    echo ""
}

# Load environment variables
load_env() {
    # First, try loading from .env in TrueDope directory
    if [ -f "$TRUEDOPE_DIR/.env" ]; then
        set -a
        source "$TRUEDOPE_DIR/.env" 2>/dev/null || true
        set +a
    fi

    # Then, try loading remote config from .env.remote if it exists
    if [ -f "$SCRIPT_DIR/.env.remote" ]; then
        set -a
        source "$SCRIPT_DIR/.env.remote" 2>/dev/null || true
        set +a
    fi
}

# Get local Docker database credentials
get_local_db_config() {
    local container_name="$1"

    # Auto-detect container if not specified
    if [ -z "$container_name" ]; then
        container_name=$(docker ps --filter "ancestor=postgres:15" --format "{{.Names}}" | head -1)
        if [ -z "$container_name" ]; then
            container_name=$(docker ps --format "{{.Names}}" | grep -E "(db|postgres)" | head -1)
        fi
    fi

    if [ -z "$container_name" ]; then
        print_status "$RED" "Error: Could not auto-detect PostgreSQL container."
        print_status "$YELLOW" "Make sure Docker is running: docker compose up -d"
        print_status "$YELLOW" "Or specify container with --container <name>"
        exit 1
    fi

    # Check if container is running
    if ! docker ps --filter "name=$container_name" --format "{{.Status}}" | grep -q "Up"; then
        print_status "$RED" "Error: Container '$container_name' is not running!"
        print_status "$YELLOW" "Start with: cd $TRUEDOPE_DIR && docker compose up -d"
        exit 1
    fi

    LOCAL_CONTAINER="$container_name"
    LOCAL_DB_USER=$(docker exec "$container_name" bash -c 'echo $POSTGRES_USER' 2>/dev/null || echo "postgres")
    LOCAL_DB_NAME=$(docker exec "$container_name" bash -c 'echo $POSTGRES_DB' 2>/dev/null || echo "truedope")

    if [ -z "$LOCAL_DB_USER" ]; then
        LOCAL_DB_USER="postgres"
    fi
    if [ -z "$LOCAL_DB_NAME" ]; then
        LOCAL_DB_NAME="truedope"
    fi
}

# Validate remote database configuration
validate_remote_config() {
    local missing=""

    [ -z "$REMOTE_DB_HOST" ] && missing="$missing REMOTE_DB_HOST"
    [ -z "$REMOTE_DB_NAME" ] && missing="$missing REMOTE_DB_NAME"
    [ -z "$REMOTE_DB_USER" ] && missing="$missing REMOTE_DB_USER"
    [ -z "$REMOTE_DB_PASSWORD" ] && missing="$missing REMOTE_DB_PASSWORD"

    if [ -n "$missing" ]; then
        print_status "$RED" "Error: Missing remote database configuration:"
        print_status "$RED" " $missing"
        echo ""
        print_status "$YELLOW" "Configure via environment variables or create:"
        print_status "$WHITE" "  $SCRIPT_DIR/.env.remote"
        echo ""
        print_status "$YELLOW" "Example .env.remote:"
        echo "REMOTE_DB_HOST=your-db-host.com"
        echo "REMOTE_DB_PORT=5432"
        echo "REMOTE_DB_NAME=truedope"
        echo "REMOTE_DB_USER=truedope_user"
        echo "REMOTE_DB_PASSWORD=your_password"
        exit 1
    fi

    # Default port
    [ -z "$REMOTE_DB_PORT" ] && REMOTE_DB_PORT="5432"
}

# Check for required tools
check_requirements() {
    local mode="$1"

    if [ "$mode" = "remote" ]; then
        if ! command -v psql >/dev/null 2>&1; then
            print_status "$RED" "Error: psql not found. Install PostgreSQL client:"
            print_status "$WHITE" "  macOS: brew install libpq && brew link --force libpq"
            print_status "$WHITE" "  Ubuntu: sudo apt install postgresql-client"
            exit 1
        fi

        if ! command -v pg_dump >/dev/null 2>&1; then
            print_status "$RED" "Error: pg_dump not found. Install PostgreSQL client tools."
            exit 1
        fi
    else
        if ! command -v docker >/dev/null 2>&1; then
            print_status "$RED" "Error: docker not found. Please install Docker."
            exit 1
        fi
    fi
}

# Export database
do_export() {
    local source="local"
    local output_dir="$EXPORTS_DIR"
    local container_name=""

    # Parse export arguments
    while [[ $# -gt 0 ]]; do
        case $1 in
            --source)
                source="$2"
                shift 2
                ;;
            --output|-o)
                output_dir="$2"
                shift 2
                ;;
            --container|-c)
                container_name="$2"
                shift 2
                ;;
            *)
                shift
                ;;
        esac
    done

    check_requirements "$source"
    mkdir -p "$output_dir"

    local timestamp=$(date +"%Y%m%d_%H%M%S")
    local filename="TrueDope_v2_Export_${source}_${timestamp}.sql"
    local filepath="$output_dir/$filename"

    print_header

    if [ "$source" = "remote" ]; then
        validate_remote_config

        print_status "$CYAN" "Source:    REMOTE DATABASE"
        print_status "$YELLOW" "Host:      $REMOTE_DB_HOST:$REMOTE_DB_PORT"
        print_status "$YELLOW" "Database:  $REMOTE_DB_NAME"
        print_status "$YELLOW" "User:      $REMOTE_DB_USER"
        print_status "$YELLOW" "Output:    $filepath"
        echo ""

        print_status "$CYAN" "Exporting from remote database..."

        PGPASSWORD="$REMOTE_DB_PASSWORD" pg_dump \
            -h "$REMOTE_DB_HOST" \
            -p "$REMOTE_DB_PORT" \
            -U "$REMOTE_DB_USER" \
            -d "$REMOTE_DB_NAME" \
            --no-owner \
            --no-acl \
            > "$filepath"
    else
        get_local_db_config "$container_name"

        print_status "$CYAN" "Source:    LOCAL DOCKER"
        print_status "$YELLOW" "Container: $LOCAL_CONTAINER"
        print_status "$YELLOW" "Database:  $LOCAL_DB_NAME"
        print_status "$YELLOW" "User:      $LOCAL_DB_USER"
        print_status "$YELLOW" "Output:    $filepath"
        echo ""

        print_status "$CYAN" "Exporting from local Docker database..."

        docker exec "$LOCAL_CONTAINER" pg_dump \
            -U "$LOCAL_DB_USER" \
            -d "$LOCAL_DB_NAME" \
            --no-owner \
            --no-acl \
            > "$filepath"
    fi

    # Get file size
    local file_size
    if [[ "$OSTYPE" == "darwin"* ]]; then
        file_size=$(stat -f%z "$filepath" 2>/dev/null)
    else
        file_size=$(stat -c%s "$filepath" 2>/dev/null)
    fi
    local file_size_mb=$(echo "scale=2; $file_size / 1024 / 1024" | bc)

    echo ""
    print_status "$GREEN" "Export completed successfully!"
    print_status "$GREEN" "File: $filepath"
    print_status "$GREEN" "Size: ${file_size_mb} MB"

    # Create compressed version
    if command -v zip >/dev/null 2>&1; then
        local zip_path="${filepath%.sql}.zip"
        print_status "$CYAN" "Creating compressed version..."
        (cd "$output_dir" && zip -q "${filename%.sql}.zip" "$filename")

        local zip_size
        if [[ "$OSTYPE" == "darwin"* ]]; then
            zip_size=$(stat -f%z "$zip_path" 2>/dev/null)
        else
            zip_size=$(stat -c%s "$zip_path" 2>/dev/null)
        fi
        local zip_size_mb=$(echo "scale=2; $zip_size / 1024 / 1024" | bc)

        print_status "$GREEN" "Compressed: $zip_path (${zip_size_mb} MB)"
    fi

    echo ""
    print_status "$WHITE" "To import this export:"
    print_status "$WHITE" "  $0 import \"$filepath\""
    print_status "$WHITE" "  $0 import \"$filepath\" --target remote  # (to production)"
}

# Import database
do_import() {
    local sql_file=""
    local target="local"
    local container_name=""
    local skip_confirm=false

    # First argument should be the file
    if [[ $# -gt 0 && ! "$1" =~ ^-- ]]; then
        sql_file="$1"
        shift
    fi

    # Parse remaining arguments
    while [[ $# -gt 0 ]]; do
        case $1 in
            --target)
                target="$2"
                shift 2
                ;;
            --container|-c)
                container_name="$2"
                shift 2
                ;;
            --yes|-y)
                skip_confirm=true
                shift
                ;;
            *)
                if [ -z "$sql_file" ]; then
                    sql_file="$1"
                fi
                shift
                ;;
        esac
    done

    if [ -z "$sql_file" ]; then
        print_status "$RED" "Error: SQL file path is required"
        echo "Usage: $0 import <file> [--target local|remote]"
        exit 1
    fi

    # Resolve relative paths
    if [[ ! "$sql_file" = /* ]]; then
        sql_file="$(pwd)/$sql_file"
    fi

    if [ ! -f "$sql_file" ]; then
        print_status "$RED" "Error: File not found: $sql_file"
        exit 1
    fi

    check_requirements "$target"

    # Handle ZIP files
    local actual_sql_file="$sql_file"
    local temp_dir=""

    if [[ "$sql_file" == *.zip ]]; then
        print_status "$CYAN" "Extracting ZIP file..."
        temp_dir=$(mktemp -d)

        if ! unzip -q "$sql_file" -d "$temp_dir"; then
            print_status "$RED" "Failed to extract ZIP file"
            rm -rf "$temp_dir"
            exit 1
        fi

        actual_sql_file=$(find "$temp_dir" -name "*.sql" | head -1)
        if [ -z "$actual_sql_file" ]; then
            print_status "$RED" "No SQL file found in ZIP archive"
            rm -rf "$temp_dir"
            exit 1
        fi

        print_status "$GREEN" "Extracted: $(basename "$actual_sql_file")"
    fi

    # Get file size
    local file_size
    if [[ "$OSTYPE" == "darwin"* ]]; then
        file_size=$(stat -f%z "$actual_sql_file" 2>/dev/null)
    else
        file_size=$(stat -c%s "$actual_sql_file" 2>/dev/null)
    fi
    local file_size_mb=$(echo "scale=2; $file_size / 1024 / 1024" | bc)

    print_header

    if [ "$target" = "remote" ]; then
        validate_remote_config

        print_status "$CYAN" "Target:    REMOTE DATABASE"
        print_status "$YELLOW" "Host:      $REMOTE_DB_HOST:$REMOTE_DB_PORT"
        print_status "$YELLOW" "Database:  $REMOTE_DB_NAME"
        print_status "$YELLOW" "User:      $REMOTE_DB_USER"
        print_status "$YELLOW" "File:      $actual_sql_file"
        print_status "$YELLOW" "Size:      ${file_size_mb} MB"
    else
        get_local_db_config "$container_name"

        print_status "$CYAN" "Target:    LOCAL DOCKER"
        print_status "$YELLOW" "Container: $LOCAL_CONTAINER"
        print_status "$YELLOW" "Database:  $LOCAL_DB_NAME"
        print_status "$YELLOW" "User:      $LOCAL_DB_USER"
        print_status "$YELLOW" "File:      $actual_sql_file"
        print_status "$YELLOW" "Size:      ${file_size_mb} MB"
    fi

    echo ""
    print_status "$RED" "============================================"
    print_status "$RED" "              WARNING"
    print_status "$RED" "============================================"
    print_status "$YELLOW" "This will:"
    print_status "$YELLOW" "  1. DROP the existing database"
    print_status "$YELLOW" "  2. CREATE a new empty database"
    print_status "$YELLOW" "  3. IMPORT all data from the SQL file"
    echo ""
    print_status "$RED" "  ALL EXISTING DATA WILL BE LOST!"
    echo ""

    if [ "$target" = "remote" ]; then
        print_status "$RED" "  YOU ARE IMPORTING TO PRODUCTION!"
        echo ""
    fi

    if [ "$skip_confirm" = false ]; then
        read -p "Type 'yes' to proceed: " confirmation
        if [ "$confirmation" != "yes" ]; then
            print_status "$YELLOW" "Import cancelled."
            [ -n "$temp_dir" ] && rm -rf "$temp_dir"
            exit 0
        fi
    fi

    echo ""

    if [ "$target" = "remote" ]; then
        # Remote import
        print_status "$CYAN" "Step 1: Terminating existing connections..."
        PGPASSWORD="$REMOTE_DB_PASSWORD" psql \
            -h "$REMOTE_DB_HOST" \
            -p "$REMOTE_DB_PORT" \
            -U "$REMOTE_DB_USER" \
            -d "postgres" \
            -c "SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname = '$REMOTE_DB_NAME' AND pid <> pg_backend_pid();" \
            >/dev/null 2>&1 || true

        print_status "$CYAN" "Step 2: Dropping existing database..."
        PGPASSWORD="$REMOTE_DB_PASSWORD" psql \
            -h "$REMOTE_DB_HOST" \
            -p "$REMOTE_DB_PORT" \
            -U "$REMOTE_DB_USER" \
            -d "postgres" \
            -c "DROP DATABASE IF EXISTS \"$REMOTE_DB_NAME\";" \
            >/dev/null 2>&1

        print_status "$CYAN" "Step 3: Creating fresh database..."
        PGPASSWORD="$REMOTE_DB_PASSWORD" psql \
            -h "$REMOTE_DB_HOST" \
            -p "$REMOTE_DB_PORT" \
            -U "$REMOTE_DB_USER" \
            -d "postgres" \
            -c "CREATE DATABASE \"$REMOTE_DB_NAME\";" \
            >/dev/null 2>&1

        print_status "$CYAN" "Step 4: Importing data (this may take a while)..."
        PGPASSWORD="$REMOTE_DB_PASSWORD" psql \
            -h "$REMOTE_DB_HOST" \
            -p "$REMOTE_DB_PORT" \
            -U "$REMOTE_DB_USER" \
            -d "$REMOTE_DB_NAME" \
            -f "$actual_sql_file" \
            >/dev/null 2>&1
    else
        # Local Docker import
        print_status "$CYAN" "Step 1: Terminating existing connections..."
        docker exec "$LOCAL_CONTAINER" psql -U "$LOCAL_DB_USER" -d "postgres" \
            -c "SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname = '$LOCAL_DB_NAME' AND pid <> pg_backend_pid();" \
            >/dev/null 2>&1 || true

        print_status "$CYAN" "Step 2: Dropping existing database..."
        docker exec "$LOCAL_CONTAINER" psql -U "$LOCAL_DB_USER" -d "postgres" \
            -c "DROP DATABASE IF EXISTS \"$LOCAL_DB_NAME\";" \
            >/dev/null 2>&1

        print_status "$CYAN" "Step 3: Creating fresh database..."
        docker exec "$LOCAL_CONTAINER" psql -U "$LOCAL_DB_USER" -d "postgres" \
            -c "CREATE DATABASE \"$LOCAL_DB_NAME\";" \
            >/dev/null 2>&1

        print_status "$CYAN" "Step 4: Importing data (this may take a while)..."
        docker exec -i "$LOCAL_CONTAINER" psql -U "$LOCAL_DB_USER" -d "$LOCAL_DB_NAME" \
            < "$actual_sql_file" \
            >/dev/null 2>&1
    fi

    # Cleanup
    [ -n "$temp_dir" ] && rm -rf "$temp_dir"

    echo ""
    print_status "$GREEN" "============================================"
    print_status "$GREEN" "  Import completed successfully!"
    print_status "$GREEN" "============================================"
    print_status "$GREEN" "Imported: ${file_size_mb} MB"
    echo ""

    if [ "$target" = "local" ]; then
        print_status "$YELLOW" "Restart the backend to clear cached data:"
        print_status "$WHITE" "  cd $TRUEDOPE_DIR && docker compose restart backend"
    else
        print_status "$YELLOW" "You may need to restart your production backend service."
    fi
}

# List available exports
do_list() {
    print_header
    print_status "$CYAN" "Available exports in: $EXPORTS_DIR"
    echo ""

    if [ ! -d "$EXPORTS_DIR" ]; then
        print_status "$YELLOW" "No exports directory found."
        exit 0
    fi

    local count=0
    shopt -s nullglob
    for file in "$EXPORTS_DIR"/*.sql "$EXPORTS_DIR"/*.zip; do
        [ -f "$file" ] || continue

        local basename=$(basename "$file")
        local file_size
        if [[ "$OSTYPE" == "darwin"* ]]; then
            file_size=$(stat -f%z "$file" 2>/dev/null)
        else
            file_size=$(stat -c%s "$file" 2>/dev/null)
        fi
        local file_size_mb=$(echo "scale=2; $file_size / 1024 / 1024" | bc)
        local file_date=$(date -r "$file" "+%Y-%m-%d %H:%M:%S" 2>/dev/null || stat -c "%y" "$file" 2>/dev/null | cut -d'.' -f1)

        printf "  %-50s %8s MB  %s\n" "$basename" "$file_size_mb" "$file_date"
        count=$((count + 1))
    done

    if [ $count -eq 0 ]; then
        print_status "$YELLOW" "No export files found."
        echo ""
        print_status "$WHITE" "Create an export with:"
        print_status "$WHITE" "  $0 export"
    else
        echo ""
        print_status "$WHITE" "Total: $count file(s)"
    fi
}

# Main
load_env

case "${1:-}" in
    export)
        shift
        do_export "$@"
        ;;
    import)
        shift
        do_import "$@"
        ;;
    list)
        do_list
        ;;
    -h|--help|help|"")
        show_help
        ;;
    *)
        print_status "$RED" "Unknown command: $1"
        echo "Use '$0 --help' for usage information."
        exit 1
        ;;
esac
