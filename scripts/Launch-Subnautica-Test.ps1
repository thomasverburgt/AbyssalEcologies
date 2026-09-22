[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$testRoot = [IO.Path]::GetFullPath($PSScriptRoot).TrimEnd('\')
$gameExe = Join-Path $testRoot 'Subnautica.exe'
$appIdFile = Join-Path $testRoot 'steam_appid.txt'
$launcherLog = Join-Path $testRoot 'BepInEx\launcher.log'

function Write-LauncherLog([string]$Message) {
    $line = '{0:O} {1}' -f (Get-Date), $Message
    Add-Content -LiteralPath $launcherLog -Value $line -Encoding UTF8
}

function Show-LaunchError([string]$Message) {
    Write-LauncherLog "ERROR: $Message"
    Add-Type -AssemblyName PresentationFramework
    [System.Windows.MessageBox]::Show(
        $Message,
        'Subnautica Test Launcher',
        [System.Windows.MessageBoxButton]::OK,
        [System.Windows.MessageBoxImage]::Error) | Out-Null
}

try {
    Write-LauncherLog 'Launch requested.'

    if (-not (Test-Path -LiteralPath $gameExe -PathType Leaf)) {
        throw "The test executable was not found: $gameExe"
    }

    if (-not (Test-Path -LiteralPath $appIdFile -PathType Leaf) -or
        (Get-Content -LiteralPath $appIdFile -Raw).Trim() -ne '264710') {
        throw 'steam_appid.txt is missing or does not contain the Subnautica App ID 264710.'
    }

    $runningCopies = @(Get-CimInstance Win32_Process -Filter "Name = 'Subnautica.exe'" -ErrorAction SilentlyContinue)
    if ($runningCopies.Count -gt 0) {
        $runningPaths = ($runningCopies | ForEach-Object { $_.ExecutablePath } | Where-Object { $_ } | Sort-Object -Unique) -join "`n"
        throw "Subnautica is already running. Close it before starting the isolated test copy.`n`n$runningPaths"
    }

    $steam = Get-Process -Name steam -ErrorAction SilentlyContinue | Select-Object -First 1
    if (-not $steam) {
        $steamExe = $null
        try {
            $steamExe = (Get-ItemProperty -LiteralPath 'HKCU:\Software\Valve\Steam' -ErrorAction Stop).SteamExe
        }
        catch {
            # Fall through to the standard installation path.
        }

        if ($steamExe) {
            $steamExe = $steamExe -replace '/', '\'
        }
        else {
            $steamExe = Join-Path ${env:ProgramFiles(x86)} 'Steam\steam.exe'
        }

        if (-not (Test-Path -LiteralPath $steamExe -PathType Leaf)) {
            throw 'Steam is required as the platform service, but steam.exe could not be found.'
        }

        Write-LauncherLog "Starting Steam silently from $steamExe"
        Start-Process -FilePath $steamExe -ArgumentList '-silent' -WindowStyle Hidden

        $deadline = (Get-Date).AddSeconds(45)
        do {
            Start-Sleep -Milliseconds 500
            $steam = Get-Process -Name steam -ErrorAction SilentlyContinue | Select-Object -First 1
            $steamWebHelper = Get-Process -Name steamwebhelper -ErrorAction SilentlyContinue | Select-Object -First 1
        } while ((-not $steam -or -not $steamWebHelper) -and (Get-Date) -lt $deadline)

        if (-not $steam -or -not $steamWebHelper) {
            throw 'Steam did not become ready within 45 seconds. Start Steam normally, sign in if necessary, and try again.'
        }
    }

    Write-LauncherLog "Launching test executable directly: $gameExe"
    Start-Process -FilePath $gameExe `
        -ArgumentList '-vrmode', 'none', '-no-stereo-rendering' `
        -WorkingDirectory $testRoot
}
catch {
    Show-LaunchError $_.Exception.Message
    exit 1
}

