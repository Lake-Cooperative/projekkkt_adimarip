#!/usr/bin/env bash
# Example of a consistent logical backup. Keep output outside the repository.
set -euo pipefail
if [[ $# -ne 1 ]]; then echo "Usage: $0 /secure/path/adimarip-YYYYMMDD.sql" >&2; exit 2; fi
output="$1"
if [[ -e "$output" ]]; then echo "Refusing to overwrite existing backup: $output" >&2; exit 2; fi
umask 077
compose_file="${COMPOSE_FILE:-docker_compose.yml}"
docker compose -f "$compose_file" exec -T db sh -c   'pg_dump --no-owner --no-acl -U "$POSTGRES_USER" -d "$POSTGRES_DB"' > "$output"
[[ -s "$output" ]] || { rm -f "$output"; echo 'Empty backup!' >&2; exit 1; }
echo "Backup saved: $output (TEST RESTORE BEFORE RELYING ON THIS FILE)"
