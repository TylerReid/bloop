#!/usr/bin/env pwsh

param (
    [switch]$buildSelfContained
)

function Build($includeRuntime, $path) {
    $targets = @("linux-x64", "win-x64", "osx-x64")
    foreach ($target in $targets)
    {
        dotnet publish ./src/Bloop.Cli/Bloop.Cli.csproj --nologo `
            -r $target -c Release $includeRuntime -p:PublishSingleFile=true `
            /p:DebugType=None /p:DebugSymbols=false `
            -o ./releases/$path/$target/
    }
}

Build '--no-self-contained' 'smol'

if ($buildSelfContained) {
    Build '--self-contained' 'big'
}
