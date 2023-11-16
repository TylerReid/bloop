#!/usr/bin/env pwsh

param (
    [switch]$buildSelfContained,
    [switch]$skipResultCopy,
    [switch]$buildRelease
)

function Build($includeRuntime, $path) {
    $targets = @("linux-x64", "win-x64", "osx-x64")
    foreach ($target in $targets) {
        dotnet publish ./src/Bloop.Cli/Bloop.Cli.csproj --nologo `
            -r $target -c Release $includeRuntime -p:PublishSingleFile=true `
            /p:DebugType=None /p:DebugSymbols=false `
            -o ./releases/$path/$target/
    }
}

if ($buildSelfContained -or $buildRelease) {
    $size = 'big'
    Build '--self-contained' $size
} else {
    $size = 'smol'
    Build '--no-self-contained' $size
}

if (!$skipResultCopy) {
    if ($IsLinux) {
        . "$PSScriptRoot/copyCli.sh" "$size/linux-x64"
    }
    if ($IsMacOs) {
        . "$PSScriptRoot/copyCli.sh" "$size/osx-x64"
    }
}

if ($buildRelease) {
    $targets = @("linux-x64", "win-x64", "osx-x64")
    foreach ($target in $targets) {
        Compress-Archive -Force -Path ./releases/big/$target/bloop* -DestinationPath ./releases/$target.zip
    }
}
