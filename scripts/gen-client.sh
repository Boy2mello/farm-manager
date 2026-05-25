#!/usr/bin/env bash
# Regenerate the typed OpenAPI client used by apps/web from the running API's Swagger spec.
#
# Usage:  scripts/gen-client.sh [API_URL]
set -euo pipefail

API_URL="${1:-${API_URL:-http://localhost:5000}}"
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
OUT_DIR="$ROOT/apps/web/lib/api/generated"

mkdir -p "$OUT_DIR"

echo "→ Fetching $API_URL/swagger/v1/swagger.json"
cd "$ROOT/apps/web"
pnpm dlx openapi-typescript "$API_URL/swagger/v1/swagger.json" -o "$OUT_DIR/schema.ts"

echo "✔ Generated $OUT_DIR/schema.ts"
