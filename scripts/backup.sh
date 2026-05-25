#!/usr/bin/env bash
# Nightly backup: pg_dump → MinIO bucket + off-site copy.
# Intended to run via cron on the Hetzner VM.
#
# Required env vars (source from .env):
#   POSTGRES_USER, POSTGRES_PASSWORD, POSTGRES_DB
#   MINIO_ROOT_USER, MINIO_ROOT_PASSWORD, MINIO_ENDPOINT (default http://minio:9000)
#   BACKUP_BUCKET (default farm-manager-backups)
#   OFFSITE_TARGET (optional, e.g. "b2:farm-manager-offsite")
#
# Dependencies on VM: docker, mc (MinIO client). Install once: `wget https://dl.min.io/client/mc/release/linux-amd64/mc && chmod +x mc && mv mc /usr/local/bin/`
set -euo pipefail

STAMP="$(date -u +%Y%m%d-%H%M%S)"
DUMP_NAME="farmmanager-${STAMP}.sql.gz"
MINIO_ENDPOINT="${MINIO_ENDPOINT:-http://localhost:9000}"
BACKUP_BUCKET="${BACKUP_BUCKET:-farm-manager-backups}"

: "${POSTGRES_USER:?missing}"
: "${POSTGRES_PASSWORD:?missing}"
: "${POSTGRES_DB:?missing}"
: "${MINIO_ROOT_USER:?missing}"
: "${MINIO_ROOT_PASSWORD:?missing}"

WORKDIR="$(mktemp -d)"
trap 'rm -rf "$WORKDIR"' EXIT

echo "→ Dumping $POSTGRES_DB"
docker compose -f /srv/farm-manager/docker-compose.prod.yml exec -T \
  -e PGPASSWORD="$POSTGRES_PASSWORD" postgres \
  pg_dump -U "$POSTGRES_USER" -d "$POSTGRES_DB" --no-owner --clean --if-exists \
  | gzip -9 > "$WORKDIR/$DUMP_NAME"

echo "→ Uploading to MinIO ($BACKUP_BUCKET)"
mc alias set local "$MINIO_ENDPOINT" "$MINIO_ROOT_USER" "$MINIO_ROOT_PASSWORD" >/dev/null
mc mb --ignore-existing "local/$BACKUP_BUCKET"
mc cp "$WORKDIR/$DUMP_NAME" "local/$BACKUP_BUCKET/$DUMP_NAME"

# Retain 30 daily / 12 monthly via mc lifecycle (one-off setup elsewhere).

if [ -n "${OFFSITE_TARGET:-}" ]; then
  echo "→ Mirroring to off-site $OFFSITE_TARGET"
  mc mirror --overwrite "local/$BACKUP_BUCKET" "$OFFSITE_TARGET"
fi

echo "✔ Backup complete: $DUMP_NAME"
