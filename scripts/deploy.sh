#!/usr/bin/env bash
# Manual SSH deploy to the Hetzner VM. Use the GitHub Actions deploy.yml in normal flow.
#
# Required env vars (use a .env file alongside this script or export them):
#   DEPLOY_HOST       — VM hostname or IPv4
#   DEPLOY_USER       — non-root deploy user
#   DEPLOY_PATH       — target path on the VM (e.g. /srv/farm-manager)
#   IMAGE_TAG         — image tag to deploy (default: latest)
set -euo pipefail

: "${DEPLOY_HOST:?missing}"
: "${DEPLOY_USER:?missing}"
: "${DEPLOY_PATH:?missing}"
IMAGE_TAG="${IMAGE_TAG:-latest}"

ROOT="$(cd "$(dirname "$0")/.." && pwd)"

echo "→ Copying compose + Caddyfile to $DEPLOY_USER@$DEPLOY_HOST:$DEPLOY_PATH"
scp "$ROOT/infra/docker-compose.prod.yml" "$DEPLOY_USER@$DEPLOY_HOST:$DEPLOY_PATH/docker-compose.prod.yml"
scp "$ROOT/infra/Caddyfile"                "$DEPLOY_USER@$DEPLOY_HOST:$DEPLOY_PATH/Caddyfile"

echo "→ Pulling images and restarting (IMAGE_TAG=$IMAGE_TAG)"
ssh "$DEPLOY_USER@$DEPLOY_HOST" "
  set -euo pipefail
  cd '$DEPLOY_PATH'
  IMAGE_TAG='$IMAGE_TAG' docker compose -f docker-compose.prod.yml --env-file .env pull
  IMAGE_TAG='$IMAGE_TAG' docker compose -f docker-compose.prod.yml --env-file .env up -d
"

echo "✔ Deploy complete"
