<#
    Producer race: a periodic producer keeps showing toasts while the main window closes under
    OnLastWindowClose. Runs each configuration -Iterations times and prints one line per run plus a rate
    per configuration. A run PASSES when the process exits by itself within -ExitLimitMs of the close.
    Run it from an interactive desktop session that nobody is working in.

    Usage: .\run-producer-race.ps1 -Iterations 20 -Configs 50:dispatcher+1000:timers
    Config list: configs separated by '+', each <producerMs>:<dispatcher|timers>
#>
[CmdletBinding()]
param(
    [string] $ExePath = '',
    [string] $LogDir = '',
    [int]    $Iterations = 20,
    [string] $Configs = '50:dispatcher+50:timers+1000:timers',
    [int]    $CloseJitterMs = 1000,
    [int]    $ExitLimitMs = 5000
)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
if (-not $ExePath) { $ExePath = Join-Path $scriptDir 'Notification.Wpf.ExitHost.exe' }
if (-not $LogDir) { $LogDir = Join-Path ([System.IO.Path]::GetTempPath()) 'producer-race-logs' }
if (Test-Path $LogDir) { Remove-Item $LogDir -Recurse -Force }
New-Item -ItemType Directory -Path $LogDir -Force | Out-Null

$stuckTotal = 0
foreach ($config in $Configs.Split('+')) {
    $period, $kind = $config.Split(':')
    $stuck = 0
    for ($i = 1; $i -le $Iterations; $i++) {
        $log = Join-Path $LogDir "p$period-$kind-$i.log"
        $info = New-Object System.Diagnostics.ProcessStartInfo
        $info.FileName = $ExePath
        $info.Arguments = "--mode OnLastWindowClose --producer-ms $period --producer-kind $kind --close-jitter-ms $CloseJitterMs --stuck-after-ms 8000 --log `"$log`""
        $info.UseShellExecute = $false
        $process = [System.Diagnostics.Process]::Start($info)
        $exited = $process.WaitForExit(20000)
        if (-not $exited) { $process.Kill(); $process.WaitForExit() }
        $lines = Get-Content $log
        $closing = ($lines | Where-Object { $_ -match ' CLOSING main$' } | Select-Object -First 1)
        $closeMs = [int64]($closing.Split(' ')[0])
        $exitMs = ([DateTimeOffset]$process.ExitTime).ToUnixTimeMilliseconds()
        $isStuck = [bool]($lines | Where-Object { $_ -match ' STUCK ' })
        $delta = $exitMs - $closeMs
        $ok = (-not $isStuck) -and ($delta -le $ExitLimitMs)
        if (-not $ok) { $stuck++ }
        "{0} run {1,2}: {2} exit {3} ms after close" -f $config, $i, $(if ($ok) { 'PASS' } else { 'FAIL' }), $delta
    }
    "RATE $config : $stuck / $Iterations stuck"
    $stuckTotal += $stuck
}
exit $stuckTotal
