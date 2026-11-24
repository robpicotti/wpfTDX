param(
    [Parameter(Mandatory=$true)][string]$BuildDirectory,
    [string]$Python = 'python',
    [switch]$Preview
)
$ErrorActionPreference = 'Stop'
$build = (Resolve-Path -LiteralPath $BuildDirectory).Path
$scratch = Join-Path ([IO.Path]::GetTempPath()) ('tdx-system-trades-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $scratch | Out-Null
$ready = Join-Path $scratch 'ready.txt'
$framework = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
$runner = Join-Path $build 'SystemTradesClientChecks.exe'
$fixture = Join-Path $PSScriptRoot 'system_trades_fixture_api.py'
$source = Join-Path $PSScriptRoot 'SystemTradesClientChecks.cs'
$server = $null
try {
    [xml]$navigation = [IO.File]::ReadAllText((Join-Path (Split-Path $PSScriptRoot) 'MainWindow.xaml'))
    $ns = New-Object Xml.XmlNamespaceManager($navigation.NameTable)
    $ns.AddNamespace('w', 'http://schemas.microsoft.com/winfx/2006/xaml/presentation')
    $labels = @($navigation.SelectNodes('//w:WrapPanel/w:TextBlock', $ns) | ForEach-Object { $_.Text })
    if (($labels -join '|') -ne (($labels | Sort-Object) -join '|')) { throw 'MainWindow links must remain alphabetical.' }
    Write-Output 'PASS: MainWindow navigation is alphabetical.'
    & "$framework\csc.exe" /nologo /target:exe "/out:$runner" "/reference:$build\wpfTDX.exe" "/reference:$build\Newtonsoft.Json.dll" "/reference:$framework\System.Net.Http.dll" "/reference:$framework\System.Xaml.dll" "/reference:$framework\WPF\PresentationFramework.dll" "/reference:$framework\WPF\PresentationCore.dll" "/reference:$framework\WPF\WindowsBase.dll" $source
    if ($LASTEXITCODE -ne 0) { throw 'Client checks failed to compile.' }
    $fixtureArguments = @("`"$fixture`"", '--ready-file', "`"$ready`"")
    $server = Start-Process -FilePath $Python -ArgumentList $fixtureArguments -WindowStyle Hidden -PassThru -RedirectStandardError (Join-Path $scratch 'fixture-error.txt')
    $deadline = [DateTime]::UtcNow.AddSeconds(15)
    while (-not (Test-Path -LiteralPath $ready)) {
        if ($server.HasExited -or [DateTime]::UtcNow -gt $deadline) {
            Get-Content (Join-Path $scratch 'fixture-error.txt')
            throw 'Synthetic fixture API failed to start.'
        }
        Start-Sleep -Milliseconds 100
    }
    $port = [IO.File]::ReadAllText($ready).Trim()
    if ($Preview) { & $runner "http://127.0.0.1:$port/" '--preview' }
    else { & $runner "http://127.0.0.1:$port/" }
    if ($LASTEXITCODE -ne 0) { throw 'Client checks failed.' }
} finally {
    if ($server -and -not $server.HasExited) { Stop-Process -Id $server.Id }
    Write-Output "Synthetic test diagnostics: $scratch"
}
