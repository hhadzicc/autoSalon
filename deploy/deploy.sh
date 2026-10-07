#!/usr/bin/env sh
set -eu

if [ "$#" -ne 1 ]; then
    echo "Usage: $0 <container-image>" >&2
    exit 2
fi

if [ ! -f .env ]; then
    echo "Missing private .env file in $(pwd)" >&2
    exit 1
fi

export WEB_IMAGE="$1"
compose="docker compose --env-file .env -f docker-compose.yml"

$compose config --quiet
$compose pull
$compose up -d --no-build --remove-orphans

attempt=1
while [ "$attempt" -le 12 ]; do
    origin="$($compose port web 8080 2>/dev/null || true)"
    if [ -n "$origin" ] && curl --fail --silent --show-error \
        --max-time 5 "http://$origin/" >/dev/null; then
        $compose ps
        echo "Deployment health check passed."
        exit 0
    fi

    sleep 5
    attempt=$((attempt + 1))
done

$compose ps
$compose logs --tail 100 web db
echo "Deployment health check failed." >&2
exit 1
