#!/bin/sh
export DOCKER_SCAN_SUGGEST=false

set -e

stop_timeout=10
need_build=false
need_start=false
option1="$1"
option2="$2"

echo_title() {
  line=$(echo "$1" | sed -r 's/./-/g')
  printf "\n%s\n%s\n%s\n\n" "$line" "$1" "$line"
}

has_option() {
  if [ "$option1" = "$1" ] || [ "$option2" = "$1" ] ||
     [ "$option1" = "$2" ] || [ "$option2" = "$2" ] ; then
    echo "true"
  else
    echo "false"
  fi
}

script_dir="$(cd "$(dirname "$0")" && pwd)"
cd "$script_dir"

if [ "$(has_option "--force" "-f")" = "true" ] ; then
  need_pull=true
else
  need_pull=$(git fetch --dry-run 2>&1)
fi

if [ -n "$need_pull" ] ; then
  echo_title "PULLING LATEST SOURCE CODE"
  git pull --ff-only
  git log --pretty=oneline -1
  need_build=true
fi

status=$(docker compose ps --status running -q)
if [ "$need_build" = "true" ] ; then
  if [ -n "$status" ] ; then
    echo_title "STOPPING RUNNING CONTAINER"
    docker compose stop -t "$stop_timeout"
  fi
  need_start=true
elif [ -z "$status" ] ; then
  need_start=true
fi

if [ "$need_start" = "false" ] ; then
  printf "\nNo changes found. Containers are already running.\n"
elif [ "$need_build" = "true" ]; then
  echo_title "BUILDING & STARTING CONTAINER"
  docker compose up -d --build
else
  echo_title "STARTING CONTAINER"
  docker compose up -d
fi

if [ "$(has_option "--full_cleanup" "-fcu")" = "true" ] ; then
  echo_title "FULL CLEAN-UP"
  docker image prune --force
elif [ "$(has_option "--cleanup" "-cu")" = "true" ] ; then
  echo_title "CLEAN-UP"
  docker image prune --force
fi

echo ""
