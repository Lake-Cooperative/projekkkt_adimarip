#!/usr/bin/env bash
# Intentionally restores into a separate EMPTY target database, not production DB.
set -euo pipefail
if [[ $# -ne 2 ]]; then echo "Usage: $0 backup.sql NEW_EMPTY_TEST_DB" >&2; exit 2; fi
backup="$1"; testdb="$2"
[[ -s "$backup" ]] || { echo "Missing/empty backup file" >&2; exit 2; }
[[ "$testdb" =~ ^[a-zA-Z][a-zA-Z0-9_]{2,62}$ ]] || { echo "Invalid database name" >&2; exit 2; }
compose_file="${COMPOSE_FILE:-docker_compose.yml}"
docker compose -f "$compose_file" exec -T db sh -c   'createdb -U "$POSTGRES_USER" "$1"' sh "$testdb"
docker compose -f "$compose_file" exec -T db sh -c   'psql -v ON_ERROR_STOP=1 -U "$POSTGRES_USER" -d "$1"' sh "$testdb" < "$backup"
echo "Restored into separate test DB $testdb; verify records before trusting backup."
