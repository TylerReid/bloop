#!/usr/bin/env pwsh


cd ./src/Bloop.Cli
dotnet publish -r linux-x64 -c Release --no-self-contained -p:PublishSingleFile=true
Copy-Item -Path ./bin/Release/net6.0/linux-x64/publish/Bloop.Cli -Destination /usr/local/bin/bloop