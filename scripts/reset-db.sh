#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT_DIR"

usage() {
  cat <<'EOF'
Uso:
  ./scripts/reset-db.sh [dev|prod|default] [--up] [--build]

O que faz:
  - Para e remove os containers do compose (sem apagar volumes)
  - Remove apenas o volume de dados do Postgres (reset total do banco)

Opções:
  dev       Usa docker-compose.dev.yml (padrão)
  prod      Usa docker-compose.prod.yml
  default   Usa docker-compose.yml
  --up      Sobe o compose ao final (up -d)
  --build   Com --up, também faz build (up -d --build)

Exemplos:
  ./scripts/reset-db.sh
  ./scripts/reset-db.sh dev --up
  ./scripts/reset-db.sh prod --up --build
EOF
}

mode="dev"
up="false"
build="false"

while [[ $# -gt 0 ]]; do
  case "$1" in
    -h|--help)
      usage
      exit 0
      ;;
    dev|prod|default)
      mode="$1"
      shift
      ;;
    --up)
      up="true"
      shift
      ;;
    --build)
      build="true"
      shift
      ;;
    *)
      echo "Argumento inválido: $1" >&2
      echo >&2
      usage >&2
      exit 2
      ;;
  esac
done

case "$mode" in
  dev)
    compose_file="docker-compose.dev.yml"
    volume_key="postgres_data_dev"
    ;;
  prod)
    compose_file="docker-compose.prod.yml"
    volume_key="postgres_data_prod"
    ;;
  default)
    compose_file="docker-compose.yml"
    volume_key="postgres_data"
    ;;
  *)
    echo "Modo inválido: $mode" >&2
    exit 2
    ;;
esac

project_name="${COMPOSE_PROJECT_NAME:-$(basename "$ROOT_DIR")}"
volume_name="${project_name}_${volume_key}"

echo "Compose: $compose_file"
echo "Projeto: $project_name"
echo "Volume:  $volume_name"

docker compose -f "$compose_file" down --remove-orphans

if docker volume inspect "$volume_name" >/dev/null 2>&1; then
  docker volume rm -f "$volume_name" >/dev/null
  echo "Banco resetado (volume removido)."
else
  echo "Volume não encontrado; nada para remover."
fi

if [[ "$up" == "true" ]]; then
  if [[ "$build" == "true" ]]; then
    docker compose -f "$compose_file" up -d --build
  else
    docker compose -f "$compose_file" up -d
  fi
fi

