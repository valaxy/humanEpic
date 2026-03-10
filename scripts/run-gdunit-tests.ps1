$ErrorActionPreference = "Stop"

$rootPath = Resolve-Path (Join-Path $PSScriptRoot "..")
Set-Location $rootPath

$reportPath = Join-Path $rootPath "artifacts/test-results"
if (-not (Test-Path $reportPath)) {
    New-Item -ItemType Directory -Path $reportPath | Out-Null
}

Write-Host "[gdUnit4] Running tests and generating reports..."

dotnet test .\humanepic.sln --settings .\.runsettings --logger "console;verbosity=normal"

if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

Write-Host "[gdUnit4] Reports generated under: $reportPath"
Write-Host "[gdUnit4] HTML: artifacts/test-results/gdunit-report.html"
Write-Host "[gdUnit4] TRX : artifacts/test-results/gdunit-report.trx"
