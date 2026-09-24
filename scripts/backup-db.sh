#!/usr/bin/env bash
# Backs up the ZEGU PostgreSQL database to a timestamped, gzip-compressed
# .sql file. Run manually or from cron (not scheduled by default).
#
# Usage:
#   ./scripts/backup-db.sh                  # backup, keep last 14
#   BACKUP_DIR=/mnt/backups ./scripts/backup-db.sh
#   RETENTION_DAYS=30 ./scripts/backup-db.sh
#
# If a running "zegu-db" Docker container is found, the dump runs inside
# it (matches docker-compose.yml). Otherwise it falls back to a local
# pg_dump using PGHOST/PGPORT/PGUSER/PGPASSWORD/PGDATABASE env vars.

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
BACKUP_DIR="${BACKUP_DIR:-$SCRIPT_DIR/../backups}"
RETENTION_DAYS="${RETENTION_DAYS:-14}"
TIMESTAMP="$(date +%Y%m%d-%H%M%S)"
OUT_FILE="$BACKUP_DIR/zegu-$TIMESTAMP.sql.gz"

mkdir -p "$BACKUP_DIR"

if docker ps --format '{{.Names}}' 2>/dev/null | grep -qx "zegu-db"; then
    echo "Backing up via Docker container zegu-db..."
    docker exec zegu-db pg_dump -U "${DB_USER:-zegu}" "${DB_NAME:-ZEGU}" | gzip > "$OUT_FILE"
else
    echo "Backing up via local pg_dump..."
    export PGHOST="${PGHOST:-127.0.0.1}"
    export PGPORT="${PGPORT:-5432}"
    export PGUSER="${PGUSER:-postgres}"
    export PGDATABASE="${PGDATABASE:-ZEGU}"
    if [ -z "${PGPASSWORD:-}" ]; then
        echo "PGPASSWORD is not set. Export it first, e.g.:" >&2
        echo "  PGPASSWORD=your-password ./scripts/backup-db.sh" >&2
        exit 1
    fi
    pg_dump | gzip > "$OUT_FILE"
fi

echo "Backup written to $OUT_FILE ($(du -h "$OUT_FILE" | cut -f1))"

find "$BACKUP_DIR" -name 'zegu-*.sql.gz' -mtime "+$RETENTION_DAYS" -print -delete | sed 's/^/Deleted old backup: /'

echo "Done. $(find "$BACKUP_DIR" -name 'zegu-*.sql.gz' | wc -l) backup(s) retained."
