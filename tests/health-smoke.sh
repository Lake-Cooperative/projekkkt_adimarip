#!/usr/bin/env bash
set -euo pipefail
API_URL="${1:-http://localhost:8081}"
if ! command -v curl >/dev/null; then
  echo "Не найден curl" >&2
  exit 2
fi
body="$(curl --fail --silent --show-error --max-time 10 "${API_URL%/}/api/health")"
# Check both properties, even if whitespace differs in JSON.
printf '%s' "$body" | grep -Eq '"status"[[:space:]]*:[[:space:]]*"ok"' || { echo "Health status != ok: $body" >&2; exit 1; }
printf '%s' "$body" | grep -Eq '"service"[[:space:]]*:[[:space:]]*"adimarip-api"' || { echo "Unexpected service: $body" >&2; exit 1; }
echo "OK: GET /api/health: $body"
