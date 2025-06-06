#!/usr/bin/env bash
set -euo pipefail

if command -v podman &>/dev/null; then
    engine=podman
else
    engine=docker
fi

mkdir -p bin

$engine run --rm \
  -v ./:/repo:ro \
  -v ./bin:/build/bin \
  -w /build \
  mcr.microsoft.com/dotnet/sdk:8.0 \
  bash -c "apt-get update && apt-get install -y zip && /repo/scripts/build-lambda.sh"
