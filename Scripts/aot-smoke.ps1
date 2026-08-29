# NativeAOT smoke: publish the AOT client, then run a real encrypted
# round-trip against a live (JIT) WitRPC server. Fails loudly on any step.
# Used locally and by the CI gate (.github/workflows/ci.yml).

$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent

# VS 2026's vcvarsall.bat shells out to a bare `vswhere.exe`; when the VS
# installer directory is not on PATH, its stderr pollutes the linker path the
# ILC targets capture and the native link step fails with a garbled command.
if (-not (Get-Command vswhere.exe -ErrorAction SilentlyContinue))
{
    $installer = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer'
    if (Test-Path (Join-Path $installer 'vswhere.exe')) { $env:PATH = "$env:PATH;$installer" }
}

$clientProject = Join-Path $root 'Communication\OutWit.Communication.Client.AotSmoke\OutWit.Communication.Client.AotSmoke.csproj'
$serverProject = Join-Path $root 'Communication\OutWit.Communication.Client.AotSmoke.Server\OutWit.Communication.Client.AotSmoke.Server.csproj'

Write-Host '=== AOT smoke: publishing the client (NativeAOT) ==='
dotnet publish $clientProject -c Release -r win-x64
if ($LASTEXITCODE -ne 0) { Write-Error 'AOT publish failed'; exit 1 }

$exe = Join-Path $root 'Communication\OutWit.Communication.Client.AotSmoke\bin\Release\net10.0\win-x64\publish\OutWit.Communication.Client.AotSmoke.exe'
if (-not (Test-Path $exe)) { Write-Error "AOT binary not found at $exe"; exit 1 }

Write-Host '=== AOT smoke: publish-gate run (no server) ==='
& $exe
if ($LASTEXITCODE -ne 0) { Write-Error 'AOT binary failed in publish-gate mode'; exit 1 }

Write-Host '=== AOT smoke: building the server ==='
dotnet build $serverProject -c Release
if ($LASTEXITCODE -ne 0) { Write-Error 'Smoke server build failed'; exit 1 }

# A port the OS says is free right now.
$probe = [System.Net.Sockets.TcpListener]::new([System.Net.IPAddress]::Loopback, 0)
$probe.Start()
$port = ([System.Net.IPEndPoint]$probe.LocalEndpoint).Port
$probe.Stop()

$serverLog = Join-Path ([System.IO.Path]::GetTempPath()) "witrpc-aot-smoke-server-$port.log"
$serverArgs = @('run', '--project', $serverProject, '-c', 'Release', '--no-build', '--', "http://localhost:$port/smoke/")

Write-Host "=== AOT smoke: starting the server on port $port ==="
$server = Start-Process dotnet -ArgumentList $serverArgs -RedirectStandardOutput $serverLog -PassThru -NoNewWindow

try
{
    $ready = $false
    for ($i = 0; $i -lt 60; $i++)
    {
        Start-Sleep -Milliseconds 500
        if ($server.HasExited) { break }
        if ((Test-Path $serverLog) -and (Select-String -Path $serverLog -Pattern 'SMOKE-SERVER READY' -Quiet)) { $ready = $true; break }
    }

    if (-not $ready)
    {
        if (Test-Path $serverLog) { Get-Content $serverLog | Write-Host }
        Write-Error 'Smoke server did not become ready'
        exit 1
    }

    Write-Host '=== AOT smoke: running the round-trip ==='
    & $exe "ws://localhost:$port/smoke/"
    $code = $LASTEXITCODE

    if ($code -ne 0)
    {
        if (Test-Path $serverLog) { Get-Content $serverLog | Write-Host }
        Write-Error "Round-trip failed with exit code $code"
        exit $code
    }

    Write-Host '=== AOT smoke: PASSED ==='
    exit 0
}
finally
{
    if (-not $server.HasExited) { Stop-Process -Id $server.Id -Force -ErrorAction SilentlyContinue }
}
