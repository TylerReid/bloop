#!/usr/bin/env pwsh

function Assert($expected, $actual) {
    if ($expected -ne $actual) {
        throw "Expected $expected but got $actual"
    }
}

Write-Host "Starting test server"
$testJob = Start-Job -FilePath ./scripts/runTestServer.ps1
# todo wait until port is in use?
Start-Sleep -Seconds 3
Write-Host "Beginning tests"

Set-Location $PSScriptRoot

$list = bloop list
Write-Host "`nbloop list output:"
Write-Host $list

$echo = bloop echoquery
Write-Host "`nbloop request output:"
Write-Host $echo

Assert "intgtest" $echo

$Env:intgtestvar = "blooooooop"
$echo = bloop env
Write-Host "`nbloop env output:"
Write-Host $echo

Assert "blooooooop" $echo

$echo = bloop file
Write-Host "`nbloop file output:"
Write-Host $echo

Assert "blorp" $echo

$echo = bloop const
Write-Host "`nbloop const output:"
Write-Host $echo

Assert "something constant" $echo

$echo = bloop otherrequest
Write-Host "`nbloop otherrequest output:"
Write-Host $echo

Assert "localhost:5284" $echo

$echo = bloop script
Write-Host "`nbloop script output:"
Write-Host $echo

Assert "hello from powershell" $echo

$testJob | Stop-Job