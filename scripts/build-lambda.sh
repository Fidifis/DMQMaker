#!/usr/bin/env bash
set -euo pipefail

cp -r /repo .

dotnet publish repo/Lambda/Lambda.csproj -c Release 

rm bin/dmq-maker.zip
zip -r -j bin/dmq-maker.zip repo/Lambda/bin/Release/net8.0/linux-x64/publish/
