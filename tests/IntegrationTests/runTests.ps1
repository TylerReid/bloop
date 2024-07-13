#!/usr/bin/env pwsh

function Assert-Json($expected, $actual) {
    # this is to make all whitespace the same, kind of dumb
    Assert ($expected | ConvertFrom-Json | ConvertTo-Json) ($actual | ConvertFrom-Json | ConvertTo-Json)
}

function Assert($expected, $actual) {
    if ($expected -ne $actual) {
        throw "Expected $expected but got $actual"
    }
}

Push-Location
Set-Location $PSScriptRoot

Write-Host "Starting test server"
$testJob = Start-Job -ScriptBlock {
    Set-Location ../TestApi
    dotnet run --urls=http://localhost:5284/
}

dotnet publish ../../src/Bloop.Cli/Bloop.Cli.csproj --nologo `
    -c Release `
    /p:DebugType=None /p:DebugSymbols=false `
    -p:PublishSingleFile=true -o .

$waitCount = 0
while (-not (Test-Connection -TargetName localhost -TcpPort 5284))
{
    if ($waitCount -gt 10)
    {
        throw "server is taking too long to start, check output from ./scripts/runTestServer.ps1"
    }
    Start-Sleep -MilliSeconds 500
    $waitCount = $waitCount + 1
}

Write-Host "Beginning tests"

$validate = ./bloop validate
Write-Host "`nbloop validate output:"
Write-Host $validate

Assert $null $validate

$validate = ./bloop validate -c ./bad/bad.toml
Write-Host "`nbloop validate output:"
Write-Host $validate

Assert "variable ``bad`` has a ``jpath`` set, but is missing a ``source``" $validate

$list = ./bloop list
Write-Host "`nbloop list output:"
Write-Host $list

$echo = ./bloop echoquery
Write-Host "`nbloop request output:"
Write-Host $echo

Assert "intgtest" $echo

$echo = ./bloop secondfile
Write-Host "`nbloop secondfile output:"
Write-Host $echo

Assert "intgtest" $echo

$Env:intgtestvar = "blooooooop"
$echo = ./bloop env
Write-Host "`nbloop env output:"
Write-Host $echo

Assert "blooooooop" $echo

$echo = ./bloop file
Write-Host "`nbloop file output:"
Write-Host $echo

Assert "blorp" $echo

$echo = ./bloop const
Write-Host "`nbloop const output:"
Write-Host $echo

Assert "something constant" $echo

$echo = ./bloop otherrequest
Write-Host "`nbloop otherrequest output:"
Write-Host $echo

Assert "localhost:5284" $echo

$echo = ./bloop script
Write-Host "`nbloop script output:"
Write-Host $echo

Assert "hello from powershell" $echo

$echo = ./bloop env --var env=wow
Write-Host "`nbloop env output:"
Write-Host $echo

Assert "wow" $echo

$echo = ./bloop echoform --var 'env=something cool'
Write-Host "`nbloop env cli param output:"
Write-Host $echo

Assert-Json "{`"SomeFormKey`": `"Some Form Value`", `"Test`": `"something cool`"}" $echo

$echo = ./bloop echoarray
Write-Host "`nbloop array output:"
Write-Host $echo

Assert-Json "[{`"value`": `"derp`"}, {`"value`": `"derp`"}, {`"value`": `"derp`"}]" $echo

$echo = ./bloop envfallbackfile
Write-Host "`nbloop env fallback to file output:"
Write-Host $echo

Assert "blorp" $echo

$echo = ./bloop envfallback
Write-Host "`nbloop has env and file output:"
Write-Host $echo

Assert "blooooooop" $echo

$echo = ./bloop 'default'
Write-Host "`nbloop default output:"
Write-Host $echo

Assert "this is a default value" $echo

$echo = ./bloop 'variableVariable'
Write-Host "`nbloop variableVariable output:"
Write-Host $echo

Assert "blorp" $echo

$echo = ./bloop 'needsEncode'
Write-Host "`nbloop needsEncode output:"
Write-Host $echo

Assert "this@wow&broke?" $echo

$echo = ./bloop argsWithVar
Write-Host "`nbloop argsWithVar output:"
Write-Host $echo

Assert "blorp " $echo

$echo = ./bloop variable file
Write-Host "`nbloop variable file output:"
Write-Host $echo

Assert "blorp" $echo

$echo = ./bloop variable otherrequest
Write-Host "`nbloop variable file output:"
Write-Host $echo

Assert "localhost:5284" $echo

$echo = ./bloop withQueryDict
Write-Host "`nbloop withQueryDict output:"
Write-Host $echo

Assert "blooooooopblorp" $echo

$echo = ./bloop envSpecific
Write-Host "`nbloop with no env output:"
Write-Host $echo

Assert "derp" $echo

$echo = ./bloop envSpecific -e prod
Write-Host "`nbloop with no env output:"
Write-Host $echo

Assert "prod env" $echo

$echo = ./bloop envSpecific -e dev
Write-Host "`nbloop with no env output:"
Write-Host $echo

Assert "dev env" $echo

$testJob | Stop-Job
Remove-Item ./bloop.exe -ErrorAction SilentlyContinue
Remove-Item ./bloop -ErrorAction SilentlyContinue
Pop-Location
