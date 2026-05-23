# Tideline UI harness: IPC smoke test.
#
# Starts the built Tideline.exe in a clean TIDELINE_DATA_DIR, sends a
# capture command via the tideline-capture helper over the named pipe,
# then asks the running app how many notes it sees. Tears the app down
# at the end. Exits non-zero on any failure.
#
# Run from repo root (Windows PowerShell or pwsh both work):
#   powershell -ExecutionPolicy Bypass -File tools/harness/Run-IpcSmoke.ps1

param(
    [string]$Configuration = 'Debug',
    [int]$Timeout = 15
)

$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$exe = Join-Path $repoRoot "src/Tideline.App/bin/$Configuration/net8.0-windows10.0.19041.0/win-x64/Tideline.exe"
$client = Join-Path $repoRoot "tools/Tideline.CaptureClient/bin/$Configuration/net8.0/win-x64/tideline-capture.exe"

if (-not (Test-Path $exe)) { throw "App binary not found at $exe. Build it first." }
if (-not (Test-Path $client)) { throw "Capture client not found at $client. Build it first." }

$dataDir = Join-Path $env:TEMP "tideline-harness-$([guid]::NewGuid().ToString('N'))"
New-Item -ItemType Directory -Force -Path $dataDir | Out-Null
$env:TIDELINE_DATA_DIR = $dataDir

$exitCode = 0
$proc = $null

try {
    Write-Host "Launching Tideline with TIDELINE_DATA_DIR=$dataDir"
    $proc = Start-Process -FilePath $exe -PassThru

    # Wait until the named pipe accepts connections.
    $deadline = (Get-Date).AddSeconds($Timeout)
    $ready = $false
    while ((Get-Date) -lt $deadline) {
        Start-Sleep -Milliseconds 400
        $probe = & $client --count 2>$null
        if ($LASTEXITCODE -eq 0) { $ready = $true; break }
    }
    if (-not $ready) { throw "Tideline did not start its IPC listener within $Timeout seconds." }
    Write-Host "Pipe is up. Pre-capture: $probe"

    $marker = "harness:$(Get-Date -Format o)"
    Write-Host "Sending capture --text via $client"
    $captureResp = & $client --text $marker
    if ($LASTEXITCODE -ne 0) { throw "Capture client exited $LASTEXITCODE." }
    Write-Host "Capture response: $captureResp"

    Start-Sleep -Milliseconds 250
    $postCount = & $client --count
    if ($LASTEXITCODE -ne 0) { throw "Count client exited $LASTEXITCODE." }
    Write-Host "Post-capture: $postCount"

    if ($postCount -notmatch '"count":\s*1') {
        throw "Expected exactly 1 note after capture, response was: $postCount"
    }
    Write-Host "PASS: capture round-tripped through the named pipe."
}
catch {
    Write-Error $_
    $exitCode = 1
}
finally {
    if ($proc -and -not $proc.HasExited) {
        Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue
    }
    Get-Process Tideline -ErrorAction SilentlyContinue | ForEach-Object { Stop-Process -Id $_.Id -Force -ErrorAction SilentlyContinue }
    if (Test-Path $dataDir) {
        try { Remove-Item -Recurse -Force $dataDir -ErrorAction SilentlyContinue } catch {}
    }
    Remove-Item Env:TIDELINE_DATA_DIR -ErrorAction SilentlyContinue
}

exit $exitCode
