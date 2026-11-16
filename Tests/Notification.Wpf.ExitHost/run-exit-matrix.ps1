<#
    Runs the Notification.Wpf process exit matrix on the machine it is started on.

    Every case starts Notification.Wpf.ExitHost.exe, waits for it to exit, reads the case log and
    prints one PASS or FAIL line. The last line is a summary; the exit code is the number of failed
    cases (0 when all pass). The host opens real windows, so run this from an interactive desktop
    session that nobody is working in.

    Pass criteria (spec 6.3):
      OnLastWindowClose  exits within 5 s of the last host window closing, and not before it
      OnMainWindowClose  exits within 5 s of the main window closing
      OnExplicitShutdown stays alive until Shutdown is called, then exits within 5 s
    Where the overlay watcher ends the sessions (OnLastWindowClose), the pending ShowAsync task or the
    progress loop must also finish before the process exits, and not while a host window is still open.

    Usage:
      .\run-exit-matrix.ps1
      .\run-exit-matrix.ps1 -ExePath C:\path\Notification.Wpf.ExitHost.exe -Filter 'OnLastWindowClose*'
#>
[CmdletBinding()]
param(
    [string] $ExePath = '',
    [string] $LogDir = '',
    [string] $Filter = '*',
    [int]    $MaxWaitSeconds = 30,
    [int]    $ExitThresholdMs = 5000,
    [switch] $SkipClaims
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# Defaults are resolved here: $PSScriptRoot is empty inside param() under Windows PowerShell 5.1 with -File.
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
if (-not $ExePath) { $ExePath = Join-Path $scriptDir 'Notification.Wpf.ExitHost.exe' }
if (-not $LogDir) { $LogDir = Join-Path ([System.IO.Path]::GetTempPath()) 'exit-matrix-logs' }

if (-not (Test-Path $ExePath)) { throw "ExitHost executable not found: $ExePath" }
if (Test-Path $LogDir) { Remove-Item $LogDir -Recurse -Force }
New-Item -ItemType Directory -Path $LogDir -Force | Out-Null

# Starts the host, waits for it to exit (killing it after the limit) and returns what happened.
function Invoke-Host {
    param([string] $Arguments, [string] $LogPath, [int] $WaitSeconds)

    $info = New-Object System.Diagnostics.ProcessStartInfo
    $info.FileName = $ExePath
    $info.Arguments = "$Arguments --log `"$LogPath`""
    $info.UseShellExecute = $false
    $process = [System.Diagnostics.Process]::Start($info)

    $exited = $process.WaitForExit($WaitSeconds * 1000)
    $exitMs = $null
    if ($exited) {
        $exitMs = ([DateTimeOffset]$process.ExitTime).ToUnixTimeMilliseconds()
    }
    else {
        $process.Kill()
        $process.WaitForExit()
    }

    $lines = @()
    if (Test-Path $LogPath) {
        foreach ($raw in Get-Content $LogPath) {
            $space = $raw.IndexOf(' ')
            if ($space -gt 0) {
                $lines += [pscustomobject]@{ Ms = [int64]$raw.Substring(0, $space); Text = $raw.Substring($space + 1) }
            }
        }
    }

    return [pscustomobject]@{ Exited = $exited; ExitMs = $exitMs; ExitCode = $(if ($exited) { $process.ExitCode } else { $null }); Lines = $lines }
}

function Find-Line {
    param($Lines, [string] $Pattern)
    # The leading comma keeps a single match an array, so .Count works under strict mode.
    return ,@($Lines | Where-Object { $_.Text -match $Pattern })
}

# Returns $null when the case passes, otherwise the reason it failed.
function Test-Case {
    param($Run, [string] $Mode, [string] $Toast, [bool] $SecondWindow)

    if (-not $Run.Exited) { return "process still alive after $MaxWaitSeconds s" }

    $errors = Find-Line $Run.Lines '^ERROR'
    if ($errors.Count -gt 0) { return "host reported an error: $($errors[0].Text)" }

    $exitLine = Find-Line $Run.Lines '^EXIT$'
    if ($exitLine.Count -eq 0) { return 'process ended without a clean EXIT line' }

    $closes = Find-Line $Run.Lines '^HOST_CLOSE'
    $expectedCloses = if ($SecondWindow) { 2 } else { 1 }
    switch ($Mode) {
        'OnLastWindowClose' {
            if ($closes.Count -ne $expectedCloses) { return "expected $expectedCloses host window closes, saw $($closes.Count)" }
            $trigger = ($closes | Measure-Object Ms -Maximum).Maximum
        }
        'OnMainWindowClose' {
            $main = Find-Line $Run.Lines '^HOST_CLOSE main$'
            if ($main.Count -eq 0) { return 'main window never closed' }
            $trigger = $main[0].Ms
        }
        'OnExplicitShutdown' {
            $shutdown = Find-Line $Run.Lines '^SHUTDOWN_CALL$'
            if ($shutdown.Count -eq 0) { return 'host never called Shutdown' }
            $trigger = $shutdown[0].Ms
        }
    }

    $delta = $Run.ExitMs - $trigger
    if ($delta -lt -100) { return "exited $(-$delta) ms before it was allowed to" }
    if ($delta -gt $ExitThresholdMs) { return "exited $delta ms after the trigger, limit $ExitThresholdMs ms" }

    # The watcher ends the sessions only under OnLastWindowClose; the other modes leave that to WPF.
    if ($Mode -eq 'OnLastWindowClose') {
        $exitMs = $exitLine[0].Ms
        $pattern = switch ($Toast) {
            'idle'     { '^ASYNC_DONE Timeout$' }
            'active'   { '^ASYNC_DONE Cancelled$' }
            'progress' { '^PROGRESS_CANCELLED$' }
        }
        $completion = Find-Line $Run.Lines $pattern
        if ($completion.Count -eq 0) { return "missing completion for toast '$Toast' (ShowAsync result or progress cancellation)" }
        if ($completion[0].Ms -gt $exitMs) { return 'completion was logged after EXIT' }
        # An overlay session must outlive every host window except the one whose close ends it.
        if ($Toast -ne 'idle' -and $completion[0].Ms -lt $trigger - 100) {
            return "sessions ended $($trigger - $completion[0].Ms) ms before the last host window closed"
        }
    }
    return $null
}

$failed = 0
$total = 0

foreach ($mode in 'OnLastWindowClose', 'OnMainWindowClose', 'OnExplicitShutdown') {
    foreach ($overlayFirst in $true, $false) {
        foreach ($toast in 'idle', 'active', 'progress') {
            foreach ($second in $false, $true) {
                $name = "$mode overlay-first=$($overlayFirst.ToString().ToLower()) toast=$toast second-window=$($second.ToString().ToLower())"
                if ($name -notlike $Filter) { continue }
                $total++

                $logPath = Join-Path $LogDir ("case-{0:D2}.log" -f $total)
                $arguments = "--mode $mode --overlay-first $($overlayFirst.ToString().ToLower()) --toast $toast --second-window $($second.ToString().ToLower())"
                $run = Invoke-Host -Arguments $arguments -LogPath $logPath -WaitSeconds $MaxWaitSeconds
                $reason = Test-Case -Run $run -Mode $mode -Toast $toast -SecondWindow $second

                if ($null -eq $reason) { "PASS  $name" }
                else { $failed++; "FAIL  $name :: $reason  [log: $logPath]" }
            }
        }
    }
}

# Late overlay: the main window has loaded before the first toast creates the overlay, so only the
# Scan() in ShowContent can track it. These are the cases that fail if that call is removed.
foreach ($toast in 'active', 'progress') {
    foreach ($second in $false, $true) {
        $name = "OnLastWindowClose late-overlay toast=$toast second-window=$($second.ToString().ToLower())"
        if ($name -notlike $Filter) { continue }
        $total++

        $logPath = Join-Path $LogDir ("case-{0:D2}.log" -f $total)
        $arguments = "--mode OnLastWindowClose --overlay-first false --toast $toast --second-window $($second.ToString().ToLower()) --toast-delay-ms 500"
        $run = Invoke-Host -Arguments $arguments -LogPath $logPath -WaitSeconds $MaxWaitSeconds
        $reason = Test-Case -Run $run -Mode 'OnLastWindowClose' -Toast $toast -SecondWindow $second

        if ($null -eq $reason) { "PASS  $name" }
        else { $failed++; "FAIL  $name :: $reason  [log: $logPath]" }
    }
}

if (-not $SkipClaims) {
    # Claims are observations, not gates: they never change the exit code.
    $v3Log = Join-Path $LogDir 'claim-v3.log'
    $v3 = Invoke-Host -Arguments '--mode OnExplicitShutdown --claim v3' -LogPath $v3Log -WaitSeconds $MaxWaitSeconds
    foreach ($line in (Find-Line $v3.Lines '^V3')) { "CLAIM v3  $($line.Text)" }
    if (-not $v3.Exited) { 'CLAIM v3  host did not exit' }

    $v6Log = Join-Path $LogDir 'claim-v6.log'
    $v6 = Invoke-Host -Arguments '--mode OnLastWindowClose --claim v6' -LogPath $v6Log -WaitSeconds $MaxWaitSeconds
    $stillAlive = (Find-Line $v6.Lines '^V6 STILL_ALIVE').Count -gt 0
    $result = if ($stillAlive) { 'application stayed alive after the only window (the overlay) closed' }
              elseif ($v6.Exited) { 'application shut down when the only window (the overlay) closed' }
              else { 'host did not exit' }
    "CLAIM v6  $result"
}

"SUMMARY  $($total - $failed)/$total passed, $failed failed"
exit $failed
