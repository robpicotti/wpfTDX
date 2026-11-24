param([Parameter(Mandatory=$true)][string]$BuildDirectory, [string]$ArtifactDirectory)
$ErrorActionPreference = 'Stop'
if (-not $ArtifactDirectory) { $ArtifactDirectory = Join-Path $env:TEMP ('tdx-fund-risk-' + [guid]::NewGuid().ToString('N')) }
New-Item -ItemType Directory -Force -Path $ArtifactDirectory | Out-Null
$build = (Resolve-Path -LiteralPath $BuildDirectory).Path
$framework = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
$runner = Join-Path $build 'FundRiskClientChecks.exe'
& 'C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\Roslyn\csc.exe' /nologo /target:exe "/out:$runner" "/reference:$build\wpfTDX.exe" "/reference:$build\Newtonsoft.Json.dll" "/reference:$build\LiveCharts.dll" "/reference:$build\LiveCharts.Wpf.dll" "/reference:$framework\System.Net.Http.dll" "/reference:$framework\System.Xaml.dll" "/reference:$framework\WPF\PresentationFramework.dll" "/reference:$framework\WPF\PresentationCore.dll" "/reference:$framework\WPF\WindowsBase.dll" (Join-Path $PSScriptRoot 'FundRiskClientChecks.cs')
if ($LASTEXITCODE -ne 0) { throw 'Fund Risk checks failed to compile.' }
[IO.File]::WriteAllText("$runner.config", '<configuration><appSettings><add key="TradingApiBaseUrl" value="http://127.0.0.1:1"/><add key="TradingApiKey" value="synthetic-test-key"/></appSettings></configuration>')
& $runner $ArtifactDirectory
if ($LASTEXITCODE -ne 0) { throw 'Fund Risk client checks failed.' }
Write-Output "Synthetic chart artifacts: $ArtifactDirectory"
