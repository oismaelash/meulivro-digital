#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT_DIR"

if [[ "${1:-}" == "-h" || "${1:-}" == "--help" ]]; then
  cat <<'EOF'
Uso:
  ./scripts/dev.sh [opções do docker compose...]

Exemplos:
  ./scripts/dev.sh
  ./scripts/dev.sh -d
  ./scripts/dev.sh --build
EOF
  exit 0
fi

exec docker compose -f docker-compose.dev.yml up -d --build "$@"
