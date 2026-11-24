<#
    Producer race: a periodic producer keeps showing toasts while the main window closes under
    OnLastWindowClose. Runs each configuration -Iterations times and prints one line per run plus a rate
    per configuration. A run PASSES only when the process exits by itself within -ExitLimitMs of the close with code 0, the log
    has the CLOSING main and EXIT lines, and it has no ERROR, TOAST_THREAD_ERROR, STUCK or (non-cancellation) PRODUCER_ERROR line.
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
# Dedicated child folder, and only the files this script writes are removed: a mistyped -LogDir must not wipe a real directory.
$LogDir = Join-Path $LogDir 'producer-race'
New-Item -ItemType Directory -Path $LogDir -Force | Out-Null
Get-ChildItem -LiteralPath $LogDir -File | Where-Object { $_.Name -match '^p\d+-(dispatcher|timers)-\d+\.log$' } | Remove-Item -Force

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
        if (-not $exited) {
            # The process can exit between the wait and the kill; that is not a failure of the harness.
            try { $process.Kill() } catch { }
            $process.WaitForExit()
        }
        # A missing log or marker fails this run; it must not terminate the whole script under $ErrorActionPreference = 'Stop'.
        $lines = @()
        if (Test-Path -LiteralPath $log) { $lines = @(Get-Content -LiteralPath $log) }
        $closeMs = $null
        $closing = @($lines | Where-Object { $_ -match '^\d+ CLOSING main$' })
        if ($closing.Count -gt 0) { $closeMs = [int64]($closing[0].Split(' ')[0]) }
        $failure = $null
        $delta = $null
        if (-not $exited) { $failure = 'process still alive after the wait' }
        elseif ($process.ExitCode -ne 0) { $failure = "exit code $($process.ExitCode)" }
        elseif (-not (Test-Path -LiteralPath $log)) { $failure = 'no log file' }
        elseif ($null -eq $closeMs) { $failure = 'no CLOSING main line in the log' }
        elseif (@($lines | Where-Object { $_ -match '^\d+ (ERROR|TOAST_THREAD_ERROR)' }).Count -gt 0) { $failure = 'host reported an error' }
        # The timer producer logs a failed Show as PRODUCER_ERROR instead of ERROR; only a cancellation from the
        # dispatcher shutting down under it is expected.
        elseif (@($lines | Where-Object { $_ -match '^\d+ PRODUCER_ERROR ' -and $_ -notmatch ' PRODUCER_ERROR (TaskCanceledException|OperationCanceledException)$' }).Count -gt 0) { $failure = 'producer Show failed' }
        elseif (@($lines | Where-Object { $_ -match '^\d+ STUCK ' }).Count -gt 0) { $failure = 'host reported STUCK' }
        elseif (@($lines | Where-Object { $_ -match '^\d+ EXIT$' }).Count -eq 0) { $failure = 'no EXIT line, the process did not end cleanly' }
        else {
            $exitMs = ([DateTimeOffset]$process.ExitTime).ToUnixTimeMilliseconds()
            $delta = $exitMs - $closeMs
            if ($delta -gt $ExitLimitMs) { $failure = "exit $delta ms after close, limit $ExitLimitMs ms" }
        }
        $ok = ($null -eq $failure)
        if (-not $ok) { $stuck++ }
        "{0} run {1,2}: {2} {3}" -f $config, $i, $(if ($ok) { 'PASS' } else { 'FAIL' }), $(if ($ok) { "exit $delta ms after close" } else { $failure })
    }
    "RATE $config : $stuck / $Iterations stuck"
    $stuckTotal += $stuck
}
exit $stuckTotal
