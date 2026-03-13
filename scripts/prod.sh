#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT_DIR"

if [[ "${1:-}" == "-h" || "${1:-}" == "--help" ]]; then
  cat <<'EOF'
Uso:
  ./scripts/prod.sh [opções do docker compose...]

Exemplos:
  ./scripts/prod.sh
  ./scripts/prod.sh -d
  ./scripts/prod.sh --build
EOF
  exit 0
fi

exec docker compose -f docker-compose.prod.yml up -d --build "$@"
