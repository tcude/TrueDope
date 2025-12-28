#!/bin/bash
#
# Deploy TrueDope Overview Dashboard to Grafana
#
# Usage:
#   ./deploy-dashboard.sh
#   ./deploy-dashboard.sh --api-key <key>
#   GRAFANA_API_KEY=<key> ./deploy-dashboard.sh
#
# Prerequisites:
#   1. Grafana API key with Editor permissions, OR
#   2. Grafana username/password with admin access
#

set -e

# Configuration
GRAFANA_URL="${GRAFANA_URL:-http://monitoring01.tcudelocal.net:3000}"
DASHBOARD_FILE="$(dirname "$0")/dashboards/truedope-overview.json"
PROMETHEUS_DS_NAME="${PROMETHEUS_DS_NAME:-truedope-prometheus}"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo_info() { echo -e "${GREEN}[INFO]${NC} $1"; }
echo_warn() { echo -e "${YELLOW}[WARN]${NC} $1"; }
echo_error() { echo -e "${RED}[ERROR]${NC} $1"; }

# Parse arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        --api-key)
            GRAFANA_API_KEY="$2"
            shift 2
            ;;
        --url)
            GRAFANA_URL="$2"
            shift 2
            ;;
        --help)
            echo "Usage: $0 [--api-key <key>] [--url <grafana-url>]"
            echo ""
            echo "Environment variables:"
            echo "  GRAFANA_API_KEY     - API key with Editor permissions"
            echo "  GRAFANA_URL         - Grafana URL (default: http://monitoring01.tcudelocal.net:3000)"
            echo "  PROMETHEUS_DS_NAME  - Prometheus data source name (default: TrueDope Prometheus)"
            exit 0
            ;;
        *)
            echo_error "Unknown option: $1"
            exit 1
            ;;
    esac
done

# Check for dashboard file
if [[ ! -f "$DASHBOARD_FILE" ]]; then
    echo_error "Dashboard file not found: $DASHBOARD_FILE"
    exit 1
fi

# Check for API key
if [[ -z "$GRAFANA_API_KEY" ]]; then
    echo_warn "No API key provided. You can:"
    echo "  1. Set GRAFANA_API_KEY environment variable"
    echo "  2. Pass --api-key <key> argument"
    echo "  3. Create an API key in Grafana: Configuration > API keys > Add API key"
    echo ""
    echo "Would you like to use username/password authentication instead? (y/n)"
    read -r use_basic_auth

    if [[ "$use_basic_auth" == "y" ]]; then
        echo -n "Username: "
        read -r GRAFANA_USER
        echo -n "Password: "
        read -rs GRAFANA_PASS
        echo ""
        AUTH_HEADER="Authorization: Basic $(echo -n "${GRAFANA_USER}:${GRAFANA_PASS}" | base64)"
    else
        echo_error "No authentication method provided"
        exit 1
    fi
else
    AUTH_HEADER="Authorization: Bearer ${GRAFANA_API_KEY}"
fi

echo_info "Grafana URL: $GRAFANA_URL"
echo_info "Dashboard file: $DASHBOARD_FILE"

# Step 1: Get the Prometheus data source UID
echo_info "Looking up Prometheus data source: $PROMETHEUS_DS_NAME"

DS_RESPONSE=$(curl -s -H "$AUTH_HEADER" \
    "${GRAFANA_URL}/api/datasources/name/$(echo "$PROMETHEUS_DS_NAME" | sed 's/ /%20/g')")

DS_UID=$(echo "$DS_RESPONSE" | jq -r '.uid // empty')

if [[ -z "$DS_UID" ]]; then
    echo_error "Could not find data source: $PROMETHEUS_DS_NAME"
    echo "Response: $DS_RESPONSE"
    echo ""
    echo "Available data sources:"
    curl -s -H "$AUTH_HEADER" "${GRAFANA_URL}/api/datasources" | jq -r '.[].name'
    exit 1
fi

echo_info "Found data source UID: $DS_UID"

# Step 2: Process the dashboard JSON
echo_info "Processing dashboard JSON..."

# Read the dashboard and replace the data source placeholder
DASHBOARD_JSON=$(cat "$DASHBOARD_FILE" | jq --arg uid "$DS_UID" '
    .dashboard.panels |= walk(
        if type == "object" and .datasource?.uid? == "${DS_TRUEDOPE_PROMETHEUS}" then
            .datasource.uid = $uid
        else
            .
        end
    ) |
    # Remove the __inputs and __requires (import metadata)
    del(.__inputs, .__requires) |
    # Wrap for API call
    {
        dashboard: .dashboard,
        overwrite: true,
        message: "Deployed via CLI script"
    }
')

# Step 3: Check if dashboard already exists
echo_info "Checking for existing dashboard..."

EXISTING=$(curl -s -H "$AUTH_HEADER" \
    "${GRAFANA_URL}/api/search?query=TrueDope%20Overview&type=dash-db" | jq '.[0].uid // empty' -r)

if [[ -n "$EXISTING" && "$EXISTING" != "null" ]]; then
    echo_info "Found existing dashboard (UID: $EXISTING), will update"
    DASHBOARD_JSON=$(echo "$DASHBOARD_JSON" | jq --arg uid "$EXISTING" '.dashboard.uid = $uid')
else
    echo_info "No existing dashboard found, will create new"
fi

# Step 4: Deploy the dashboard
echo_info "Deploying dashboard..."

DEPLOY_RESPONSE=$(curl -s -X POST \
    -H "$AUTH_HEADER" \
    -H "Content-Type: application/json" \
    -d "$DASHBOARD_JSON" \
    "${GRAFANA_URL}/api/dashboards/db")

# Check result
STATUS=$(echo "$DEPLOY_RESPONSE" | jq -r '.status // empty')
DASHBOARD_URL=$(echo "$DEPLOY_RESPONSE" | jq -r '.url // empty')
DASHBOARD_UID=$(echo "$DEPLOY_RESPONSE" | jq -r '.uid // empty')

if [[ "$STATUS" == "success" ]]; then
    echo ""
    echo_info "Dashboard deployed successfully!"
    echo_info "UID: $DASHBOARD_UID"
    echo_info "URL: ${GRAFANA_URL}${DASHBOARD_URL}"
    echo ""
    echo "Open in browser: ${GRAFANA_URL}${DASHBOARD_URL}"
else
    echo_error "Failed to deploy dashboard"
    echo "Response: $DEPLOY_RESPONSE"
    exit 1
fi
