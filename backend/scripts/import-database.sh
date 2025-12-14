#!/bin/bash

# TrueDope v2 Database Import Script
# This script imports a PostgreSQL database dump into the TrueDope development environment
# WARNING: This is destructive and will drop the existing database!
# Usage: ./import-database.sh <path-to-sql-file> [-c container_name]

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
WHITE='\033[1;37m'
NC='\033[0m' # No Color

print_status() {
    echo -e "${1}${2}${NC}"
}

# Check if SQL file parameter is provided
if [ $# -eq 0 ]; then
    print_status $RED "Error: SQL file path is required"
    print_status $WHITE ""
    print_status $WHITE "Usage: $0 <path-to-sql-file> [OPTIONS]"
    print_status $WHITE ""
    print_status $WHITE "Options:"
    print_status $WHITE "  -c, --container NAME  Container name (auto-detected if not specified)"
    print_status $WHITE ""
    print_status $WHITE "Example:"
    print_status $WHITE "  $0 ./exports/TrueDope_v2_Export_20241213_123456.sql"
    exit 1
fi

# Parse arguments
SQL_FILE="$1"
shift

CONTAINER_NAME=""

while [[ $# -gt 0 ]]; do
    case $1 in
        -c|--container)
            CONTAINER_NAME="$2"
            shift 2
            ;;
        *)
            shift
            ;;
    esac
done

# Validate input file
if [ ! -f "$SQL_FILE" ]; then
    print_status $RED "SQL file not found: $SQL_FILE"
    exit 1
fi

# Check if file is a ZIP file and extract if needed
ACTUAL_SQL_FILE="$SQL_FILE"
TEMP_DIR=""
if [[ "$SQL_FILE" == *.zip ]]; then
    print_status $CYAN "Detected ZIP file, extracting..."

    TEMP_DIR=$(mktemp -d)

    if unzip -q "$SQL_FILE" -d "$TEMP_DIR"; then
        SQL_FILES=($(find "$TEMP_DIR" -name "*.sql"))

        if [ ${#SQL_FILES[@]} -eq 0 ]; then
            print_status $RED "No SQL files found in ZIP archive"
            rm -rf "$TEMP_DIR"
            exit 1
        elif [ ${#SQL_FILES[@]} -gt 1 ]; then
            print_status $YELLOW "Multiple SQL files found. Using: $(basename "${SQL_FILES[0]}")"
        fi

        ACTUAL_SQL_FILE="${SQL_FILES[0]}"
        print_status $GREEN "Extracted: $(basename "$ACTUAL_SQL_FILE")"
    else
        print_status $RED "Failed to extract ZIP file"
        rm -rf "$TEMP_DIR"
        exit 1
    fi
fi

# Auto-detect container name if not specified
if [ -z "$CONTAINER_NAME" ]; then
    CONTAINER_NAME=$(docker ps --filter "ancestor=postgres:15" --format "{{.Names}}" | head -1)

    if [ -z "$CONTAINER_NAME" ]; then
        CONTAINER_NAME=$(docker ps --format "{{.Names}}" | grep -E "(db|postgres)" | head -1)
    fi

    if [ -z "$CONTAINER_NAME" ]; then
        print_status $RED "Could not auto-detect PostgreSQL container."
        print_status $YELLOW "Please specify the container name with -c option."
        exit 1
    fi
fi

# Get database credentials from container environment
DB_USER=$(docker exec "$CONTAINER_NAME" bash -c 'echo $POSTGRES_USER' 2>/dev/null)
DB_NAME=$(docker exec "$CONTAINER_NAME" bash -c 'echo $POSTGRES_DB' 2>/dev/null)

# Fallback to defaults if env vars not found
if [ -z "$DB_USER" ]; then
    DB_USER="truedope"
fi
if [ -z "$DB_NAME" ]; then
    DB_NAME="truedope"
fi

# Check if container is running
print_status $CYAN "Checking if database container is running..."
if ! docker ps --filter "name=$CONTAINER_NAME" --format "{{.Status}}" | grep -q "Up"; then
    print_status $RED "Database container '$CONTAINER_NAME' is not running!"
    print_status $YELLOW "Please start your development environment with: docker compose up -d"
    [ -n "$TEMP_DIR" ] && rm -rf "$TEMP_DIR"
    exit 1
fi

# Get file size for progress indication
if [[ "$OSTYPE" == "darwin"* ]]; then
    FILE_SIZE=$(stat -f%z "$ACTUAL_SQL_FILE" 2>/dev/null)
else
    FILE_SIZE=$(stat -c%s "$ACTUAL_SQL_FILE" 2>/dev/null)
fi
FILE_SIZE_MB=$(echo "scale=2; $FILE_SIZE / 1024 / 1024" | bc)

print_status $GREEN ""
print_status $GREEN "TrueDope v2 Database Import"
print_status $GREEN "============================"
print_status $YELLOW "Container: $CONTAINER_NAME"
print_status $YELLOW "Database:  $DB_NAME"
print_status $YELLOW "User:      $DB_USER"
print_status $YELLOW "File:      $ACTUAL_SQL_FILE"
print_status $YELLOW "Size:      ${FILE_SIZE_MB} MB"
print_status $GREEN ""

# IMPORTANT: Confirm before proceeding
print_status $RED "===================================="
print_status $RED "           WARNING"
print_status $RED "===================================="
print_status $YELLOW "This will:"
print_status $YELLOW "  1. DROP the existing '$DB_NAME' database"
print_status $YELLOW "  2. CREATE a new empty database"
print_status $YELLOW "  3. IMPORT the data from the SQL file"
print_status $YELLOW ""
print_status $YELLOW "ALL EXISTING DATA WILL BE LOST!"
print_status $GREEN ""

read -p "Are you sure you want to continue? Type 'yes' to proceed: " confirmation
if [ "$confirmation" != "yes" ]; then
    print_status $YELLOW ""
    print_status $YELLOW "Import cancelled."
    [ -n "$TEMP_DIR" ] && rm -rf "$TEMP_DIR"
    exit 0
fi

# Drop and recreate database
print_status $CYAN ""
print_status $CYAN "Step 1: Terminating existing connections..."
docker exec "$CONTAINER_NAME" psql -U "$DB_USER" -d "postgres" -c "SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname = '$DB_NAME' AND pid <> pg_backend_pid();" >/dev/null 2>&1

print_status $CYAN "Step 2: Dropping existing database..."
if ! docker exec "$CONTAINER_NAME" psql -U "$DB_USER" -d "postgres" -c "DROP DATABASE IF EXISTS \"$DB_NAME\";" >/dev/null 2>&1; then
    print_status $RED "Failed to drop existing database"
    [ -n "$TEMP_DIR" ] && rm -rf "$TEMP_DIR"
    exit 1
fi

print_status $CYAN "Step 3: Creating fresh database..."
if ! docker exec "$CONTAINER_NAME" psql -U "$DB_USER" -d "postgres" -c "CREATE DATABASE \"$DB_NAME\";" >/dev/null 2>&1; then
    print_status $RED "Failed to create new database"
    [ -n "$TEMP_DIR" ] && rm -rf "$TEMP_DIR"
    exit 1
fi

print_status $CYAN "Step 4: Importing data... This may take a few minutes."

# Execute the import
if docker exec -i "$CONTAINER_NAME" psql -U "$DB_USER" -d "$DB_NAME" < "$ACTUAL_SQL_FILE" 2>/dev/null; then
    print_status $GREEN ""
    print_status $GREEN "Database imported successfully!"
    print_status $GREEN "Processed: ${FILE_SIZE_MB} MB"
    print_status $GREEN ""
    print_status $GREEN "Your TrueDope v2 database has been restored!"
    print_status $WHITE "You can now access the application with the imported data."

    # Clean up temp files
    if [ -n "$TEMP_DIR" ]; then
        rm -rf "$TEMP_DIR"
        print_status $CYAN "Cleaned up temporary files."
    fi

    print_status $GREEN ""
    print_status $YELLOW "NOTE: You may need to restart the backend container to clear any cached data:"
    print_status $WHITE "  docker compose restart backend"

else
    print_status $RED ""
    print_status $RED "Database import failed!"
    print_status $YELLOW "Check the error messages above for details."

    [ -n "$TEMP_DIR" ] && rm -rf "$TEMP_DIR"
    exit 1
fi
