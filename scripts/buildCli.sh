#!/usr/bin/env bash
set -euo pipefail

cd ./src/Bloop.Cli
dotnet publish -r linux-x64 -c Release --no-self-contained -p:PublishSingleFile=true
sudo cp ./bin/Release/net6.0/linux-x64/publish/Bloop.Cli /usr/local/bin/bloop
