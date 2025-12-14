#!/bin/bash

# TrueDope v2 Database Export Script
# This script exports the PostgreSQL database using pg_dump
# Usage: ./export-database.sh [-o output_dir] [-c container_name]

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

# Default values
OUTPUT_PATH="./exports"
CONTAINER_NAME=""
DB_USER=""
DB_NAME=""

# Parse command line arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        -o|--output)
            OUTPUT_PATH="$2"
            shift 2
            ;;
        -c|--container)
            CONTAINER_NAME="$2"
            shift 2
            ;;
        -h|--help)
            print_status $WHITE "TrueDope v2 Database Export Script"
            print_status $WHITE ""
            print_status $WHITE "Usage: $0 [OPTIONS]"
            print_status $WHITE ""
            print_status $WHITE "Options:"
            print_status $WHITE "  -o, --output PATH     Output directory (default: ./exports)"
            print_status $WHITE "  -c, --container NAME  Container name (auto-detected if not specified)"
            print_status $WHITE "  -h, --help            Show this help message"
            exit 0
            ;;
        *)
            print_status $RED "Unknown option: $1"
            print_status $YELLOW "Use -h or --help for usage information"
            exit 1
            ;;
    esac
done

# Auto-detect container name if not specified
if [ -z "$CONTAINER_NAME" ]; then
    # Try to find the PostgreSQL container
    CONTAINER_NAME=$(docker ps --filter "ancestor=postgres:15" --format "{{.Names}}" | head -1)

    if [ -z "$CONTAINER_NAME" ]; then
        # Try another pattern
        CONTAINER_NAME=$(docker ps --format "{{.Names}}" | grep -E "(db|postgres)" | head -1)
    fi

    if [ -z "$CONTAINER_NAME" ]; then
        print_status $RED "Could not auto-detect PostgreSQL container."
        print_status $YELLOW "Please specify the container name with -c option."
        print_status $YELLOW "Running containers:"
        docker ps --format "  {{.Names}}"
        exit 1
    fi
fi

# Get database credentials from container environment
print_status $CYAN "Detecting database credentials from container..."

DB_USER=$(docker exec "$CONTAINER_NAME" bash -c 'echo $POSTGRES_USER' 2>/dev/null)
DB_NAME=$(docker exec "$CONTAINER_NAME" bash -c 'echo $POSTGRES_DB' 2>/dev/null)

# Fallback to defaults if env vars not found
if [ -z "$DB_USER" ]; then
    DB_USER="truedope"
fi
if [ -z "$DB_NAME" ]; then
    DB_NAME="truedope"
fi

# Ensure the export directory exists
mkdir -p "$OUTPUT_PATH"

# Generate a timestamp for the filename
TIMESTAMP=$(date +"%Y%m%d_%H%M%S")
FILE_NAME="TrueDope_v2_Export_$TIMESTAMP.sql"
FULL_PATH="$OUTPUT_PATH/$FILE_NAME"

print_status $GREEN ""
print_status $GREEN "TrueDope v2 Database Export"
print_status $GREEN "============================"
print_status $YELLOW "Container: $CONTAINER_NAME"
print_status $YELLOW "Database:  $DB_NAME"
print_status $YELLOW "User:      $DB_USER"
print_status $YELLOW "Output:    $FULL_PATH"
print_status $GREEN ""

# Check if container is running
print_status $CYAN "Checking if database container is running..."
if ! docker ps --filter "name=$CONTAINER_NAME" --format "{{.Status}}" | grep -q "Up"; then
    print_status $RED "Database container '$CONTAINER_NAME' is not running!"
    print_status $YELLOW "Please start your development environment with: docker compose up -d"
    exit 1
fi

print_status $GREEN "Container is running"

# Export the database using pg_dump
print_status $CYAN ""
print_status $CYAN "Exporting database... This may take a few minutes for large databases."

if docker exec "$CONTAINER_NAME" pg_dump -U "$DB_USER" -d "$DB_NAME" > "$FULL_PATH"; then
    # Get file size
    if [[ "$OSTYPE" == "darwin"* ]]; then
        FILE_SIZE=$(stat -f%z "$FULL_PATH" 2>/dev/null)
    else
        FILE_SIZE=$(stat -c%s "$FULL_PATH" 2>/dev/null)
    fi
    FILE_SIZE_MB=$(echo "scale=2; $FILE_SIZE / 1024 / 1024" | bc)

    print_status $GREEN ""
    print_status $GREEN "Database exported successfully!"
    print_status $GREEN "File: $FULL_PATH"
    print_status $GREEN "Size: ${FILE_SIZE_MB} MB"

    # Create a compressed version
    ZIP_PATH="$OUTPUT_PATH/TrueDope_v2_Export_$TIMESTAMP.zip"
    if command -v zip >/dev/null 2>&1; then
        print_status $CYAN ""
        print_status $CYAN "Creating compressed version..."
        (cd "$OUTPUT_PATH" && zip -q "$FILE_NAME.zip" "$FILE_NAME")
        mv "$OUTPUT_PATH/$FILE_NAME.zip" "$ZIP_PATH"

        if [[ "$OSTYPE" == "darwin"* ]]; then
            ZIP_SIZE=$(stat -f%z "$ZIP_PATH" 2>/dev/null)
        else
            ZIP_SIZE=$(stat -c%s "$ZIP_PATH" 2>/dev/null)
        fi
        ZIP_SIZE_MB=$(echo "scale=2; $ZIP_SIZE / 1024 / 1024" | bc)

        print_status $GREEN "Compressed: $ZIP_PATH"
        print_status $GREEN "Size: ${ZIP_SIZE_MB} MB"
    fi

    print_status $GREEN ""
    print_status $GREEN "To import this database into another environment:"
    print_status $WHITE "  ./import-database.sh \"$FULL_PATH\""

else
    print_status $RED ""
    print_status $RED "Database export failed!"
    exit 1
fi
