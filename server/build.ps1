#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Restores, builds and tests the backend solution, and optionally builds the Docker images.

.EXAMPLE
    ./build.ps1                         # Release build + unit tests
    ./build.ps1 -SqlConnection "Server=localhost,1433;User Id=sa;Password=...;TrustServerCertificate=True"
                                        # also runs the SQL Server integration tests
    ./build.ps1 -SkipTests -Docker      # build only, then build the API and worker images
#>
[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',
    [switch]$SkipTests,
    [switch]$Docker,
    [string]$SqlConnection
)

$ErrorActionPreference = 'Stop'
Set-Location $PSScriptRoot
$solution = 'ex1.sln'

function Invoke-Step([string]$Name, [scriptblock]$Command) {
    Write-Host "==> $Name" -ForegroundColor Cyan
    $timer = [Diagnostics.Stopwatch]::StartNew()
    & $Command
    if ($LASTEXITCODE -ne 0) { throw "$Name failed with exit code $LASTEXITCODE" }
    Write-Host "    done in $([math]::Round($timer.Elapsed.TotalSeconds, 1))s" -ForegroundColor DarkGray
}

Invoke-Step 'Restore' { dotnet restore $solution }
Invoke-Step "Build ($Configuration)" { dotnet build $solution -c $Configuration --no-restore }

if (-not $SkipTests) {
    if ($SqlConnection) { $env:LOTTERY_TEST_SQL = $SqlConnection }
    try {
        Invoke-Step 'Test' {
            dotnet test $solution -c $Configuration --no-build `
                --collect:'XPlat Code Coverage' --results-directory ./TestResults
        }
    }
    finally {
        Remove-Item Env:LOTTERY_TEST_SQL -ErrorAction SilentlyContinue
    }
}

if ($Docker) {
    Invoke-Step 'Docker image: api' { docker build -f ex1/Dockerfile -t gift-raffle-api . }
    Invoke-Step 'Docker image: notification-worker' {
        docker build -f Lottery.NotificationWorker/Dockerfile -t gift-raffle-worker .
    }
}

Write-Host 'Build succeeded.' -ForegroundColor Green
