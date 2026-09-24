#!/usr/bin/env bash
# Restores the ZEGU PostgreSQL database from a backup produced by
# backup-db.sh. DESTRUCTIVE: drops and recreates the target database.
#
# Usage:
#   ./scripts/restore-db.sh backups/zegu-20260924-120000.sql.gz

set -euo pipefail

if [ $# -ne 1 ]; then
    echo "Usage: $0 <path-to-backup.sql.gz>" >&2
    exit 1
fi

BACKUP_FILE="$1"
if [ ! -f "$BACKUP_FILE" ]; then
    echo "Backup file not found: $BACKUP_FILE" >&2
    exit 1
fi

read -rp "This will DROP and recreate the '${DB_NAME:-ZEGU}' database. Type 'yes' to continue: " CONFIRM
if [ "$CONFIRM" != "yes" ]; then
    echo "Aborted."
    exit 1
fi

if docker ps --format '{{.Names}}' 2>/dev/null | grep -qx "zegu-db"; then
    echo "Restoring via Docker container zegu-db..."
    DB_USER="${DB_USER:-zegu}"
    DB_NAME="${DB_NAME:-ZEGU}"
    docker exec zegu-db psql -U "$DB_USER" -d postgres -c "DROP DATABASE IF EXISTS \"$DB_NAME\";"
    docker exec zegu-db psql -U "$DB_USER" -d postgres -c "CREATE DATABASE \"$DB_NAME\";"
    gunzip -c "$BACKUP_FILE" | docker exec -i zegu-db psql -U "$DB_USER" -d "$DB_NAME"
else
    echo "Restoring via local psql..."
    export PGHOST="${PGHOST:-127.0.0.1}"
    export PGPORT="${PGPORT:-5432}"
    export PGUSER="${PGUSER:-postgres}"
    DB_NAME="${DB_NAME:-ZEGU}"
    if [ -z "${PGPASSWORD:-}" ]; then
        echo "PGPASSWORD is not set. Export it first, e.g.:" >&2
        echo "  PGPASSWORD=your-password ./scripts/restore-db.sh backup.sql.gz" >&2
        exit 1
    fi
    psql -d postgres -c "DROP DATABASE IF EXISTS \"$DB_NAME\";"
    psql -d postgres -c "CREATE DATABASE \"$DB_NAME\";"
    gunzip -c "$BACKUP_FILE" | psql -d "$DB_NAME"
fi

echo "Restore complete."
