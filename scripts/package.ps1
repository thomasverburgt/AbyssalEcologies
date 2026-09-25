[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$solution = Join-Path $repositoryRoot 'AbyssalEcologies.slnx'
$output = Join-Path $repositoryRoot 'src\AbyssalEcologies.Plugin\bin\Release\net472'
$artifactRoot = Join-Path $repositoryRoot 'artifacts'
$stage = Join-Path $artifactRoot 'package'
$pluginStage = Join-Path $stage 'BepInEx\plugins\AbyssalEcologies'
$archive = Join-Path $artifactRoot 'AbyssalEcologies-0.7.0.zip'

dotnet restore $solution
if ($LASTEXITCODE -ne 0) { throw 'Restore failed.' }

dotnet build $solution -c Release --no-restore
if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }

dotnet run --project (Join-Path $repositoryRoot 'tools\AbyssalEcologies.GeneratorChecks') -c Release --no-build
if ($LASTEXITCODE -ne 0) { throw 'Generator checks failed.' }

New-Item -ItemType Directory -Path $pluginStage -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $output 'AbyssalEcologies.dll') -Destination $pluginStage -Force
Copy-Item -LiteralPath (Join-Path $output 'AbyssalEcologies.Core.dll') -Destination $pluginStage -Force
Copy-Item -LiteralPath (Join-Path $repositoryRoot 'packaging\INSTALL.txt') -Destination $stage -Force

Compress-Archive -LiteralPath (Join-Path $stage 'BepInEx'), (Join-Path $stage 'INSTALL.txt') -DestinationPath $archive -Force
Write-Output "Created $archive"
