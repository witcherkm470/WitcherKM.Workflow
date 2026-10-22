param(
    [ValidateSet('deploy', 'check', 'stop', 'logs')]
    [string]$Mode = 'deploy'
)

$ErrorActionPreference = 'Stop'
Push-Location $PSScriptRoot
try {
    $dockerCommand = Get-Command docker -ErrorAction SilentlyContinue
    if ($dockerCommand) {
        $dockerExe = $dockerCommand.Source
    } else {
        $dockerExe = @(
            (Join-Path $env:LOCALAPPDATA 'Programs\DockerDesktop\resources\bin\docker.exe'),
            'C:\Program Files\Docker\Docker\resources\bin\docker.exe'
        ) | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
    }
    if (-not $dockerExe) { throw 'Install and start Docker Desktop.' }

    function Invoke-Docker {
        param([string[]]$Arguments)
        & $dockerExe @Arguments
        if ($LASTEXITCODE -ne 0) { throw "Docker failed: $($Arguments -join ' ')" }
    }

    function Invoke-Compose {
        param([string[]]$Arguments)
        Invoke-Docker (@('compose') + $Arguments)
    }

    function Wait-ForWorkflow {
        foreach ($path in @('/', '/api/health')) {
            $url = "http://127.0.0.1:8081$path"
            $ready = $false
            for ($attempt = 0; $attempt -lt 30; $attempt++) {
                try {
                    $response = Invoke-WebRequest -Uri $url -UseBasicParsing -TimeoutSec 5
                    if ($response.StatusCode -eq 200) { $ready = $true; break }
                } catch { Start-Sleep -Seconds 2 }
            }
            if (-not $ready) {
                Invoke-Compose @('ps')
                throw "Workflow did not respond at $url. Inspect logs with: .\docker.ps1 logs"
            }
        }
    }

    Invoke-Docker @('info', '--format', '{{.ServerVersion}}')
    if ($Mode -eq 'logs') { Invoke-Compose @('logs', '-f'); return }
    if ($Mode -eq 'stop') { Invoke-Compose @('down'); return }

    Invoke-Docker @('build', '--pull', '-f', 'docker/api.Dockerfile', '--target', 'test', '-t', 'witcherkm-workflow-api-check', '.')
    Invoke-Docker @('build', '--pull', '-f', 'docker/web.Dockerfile', '--target', 'test', '-t', 'witcherkm-workflow-web-check', '.')
    if ($Mode -eq 'check') { Write-Host 'Workflow container checks passed.'; return }

    Invoke-Compose @('build')
    Invoke-Compose @('up', '-d', '--no-build')
    Wait-ForWorkflow
    Write-Host 'Workflow is ready at http://127.0.0.1:8081'
} finally {
    Pop-Location
}
