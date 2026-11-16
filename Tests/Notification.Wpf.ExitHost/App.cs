using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;

using Notification.Core;
using Notification.Wpf.Classes;

namespace Notification.Wpf.ExitHost
{
    /// <summary>
    /// Minimal WPF application for the process exit matrix. Every scenario is driven by command line
    /// arguments and every observation goes to a log file, because the verdict is whether the process
    /// exits, which only the launching script can see.
    /// </summary>
    public sealed class App : Application
    {
        private static readonly TimeSpan SecondWindowAt = TimeSpan.FromSeconds(1);
        private static readonly TimeSpan MainCloseAt = TimeSpan.FromSeconds(2);
        private static readonly TimeSpan SecondCloseAt = TimeSpan.FromSeconds(4);
        private static readonly TimeSpan ExplicitShutdownAt = TimeSpan.FromSeconds(7);
        private static readonly TimeSpan SafetyShutdownAt = TimeSpan.FromSeconds(9);

        private readonly Dictionary<string, string> _args;
        private readonly string _logPath;
        private readonly object _logLock = new object();
        private Window? _main;
        private Window? _second;
        private NotificationManager? _manager;

        private App(Dictionary<string, string> args)
        {
            _args = args;
            _logPath = args["log"];
        }

        [STAThread]
        public static int Main(string[] argv)
        {
            Dictionary<string, string> args = ParseArgs(argv);
            if (!args.ContainsKey("log"))
                return 2;

            App app = new App(args);
            app.Log("START " + string.Join(' ', argv));
            app.DispatcherUnhandledException += (_, e) => app.Log("ERROR " + e.Exception);
            app.Exit += (_, _) => app.Log("EXIT");

            string mode = args.TryGetValue("mode", out string? value) ? value : "OnLastWindowClose";
            app.ShutdownMode = Enum.Parse<ShutdownMode>(mode);
            app.Startup += (_, _) => app.OnStarted();
            return app.Run();
        }

        private static Dictionary<string, string> ParseArgs(string[] argv)
        {
            Dictionary<string, string> result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i + 1 < argv.Length; i += 2)
                result[argv[i].TrimStart('-')] = argv[i + 1];
            return result;
        }

        private bool Flag(string name) =>
            _args.TryGetValue(name, out string? value) && value.Equals("true", StringComparison.OrdinalIgnoreCase);

        private void Log(string line)
        {
            // Milliseconds since the Unix epoch so the script can compare them with Process.ExitTime.
            string entry = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + " " + line + Environment.NewLine;
            lock (_logLock)
                File.AppendAllText(_logPath, entry);
        }

        private void OnStarted()
        {
            _manager = new NotificationManager();

            if (_args.TryGetValue("claim", out string? claim))
            {
                _ = RunClaimAsync(claim);
                return;
            }

            string toast = _args.TryGetValue("toast", out string? kind) ? kind : "idle";
            if (Flag("overlay-first"))
            {
                StartToast(toast);
                CreateMainWindow();
            }
            else
            {
                CreateMainWindow();
                StartToast(toast);
            }

            _ = RunScheduleAsync();
        }

        private void CreateMainWindow()
        {
            // MainWindow is deliberately not assigned: a normal application gets it from WPF, and the
            // overlay must not have taken that role.
            _main = new Window { Title = "ExitHost main", Width = 300, Height = 200, ShowActivated = false };
            _main.Closed += (_, _) => Log("HOST_CLOSE main");
            _main.Show();
            Log("MAIN_CREATED mainWindowIsHost=" + ReferenceEquals(MainWindow, _main));
        }

        private void StartToast(string toast)
        {
            switch (toast)
            {
                case "idle":
                    _ = ShowToastAsync(TimeSpan.FromMilliseconds(500));
                    break;
                case "active":
                    _ = ShowToastAsync(TimeSpan.FromHours(1));
                    break;
                case "progress":
                    _ = RunProgressAsync();
                    break;
                default:
                    throw new ArgumentException("Unknown toast kind: " + toast);
            }
        }

        private async Task ShowToastAsync(TimeSpan expiration)
        {
            NotificationCloseResult result = await _manager!.ShowAsync(new NotificationRequest
            {
                Title = "ExitHost",
                Message = "toast",
                ExpirationTime = expiration
            });
            Log("ASYNC_DONE " + result.Reason);
        }

        private async Task RunProgressAsync()
        {
            NotifierProgress<NotificationProgressReport> progress =
                _manager!.ShowProgressBar(new ProgressBarOptions { Title = "ExitHost progress" });
            Log("PROGRESS_START");

            // Logged synchronously when the token fires, so the verdict does not depend on how fast the
            // polling loop below notices it.
            progress.Cancel.Register(() => Log("PROGRESS_CANCELLED"));

            int step = 0;
            try
            {
                while (!progress.Cancel.IsCancellationRequested)
                {
                    progress.Report(new NotificationProgressReport(step % 100, "step " + step, null, null));
                    step++;
                    await Task.Delay(200, progress.Cancel);
                }
            }
            catch (OperationCanceledException)
            {
            }
            Log("PROGRESS_END");
        }

        private async Task RunScheduleAsync()
        {
            bool second = Flag("second-window");

            await Task.Delay(SecondWindowAt);
            if (second)
            {
                // Created after the last overlay scan, so only the rescan on a close can pick it up.
                _second = new Window { Title = "ExitHost second", Width = 300, Height = 200, Left = 400, ShowActivated = false };
                _second.Closed += (_, _) => Log("HOST_CLOSE second");
                _second.Show();
                Log("SECOND_CREATED");
            }

            await Task.Delay(MainCloseAt - SecondWindowAt);
            Log("CLOSING main");
            _main!.Close();

            if (second)
            {
                await Task.Delay(SecondCloseAt - MainCloseAt);
                Log("CLOSING second");
                _second?.Close();
            }

            if (ShutdownMode == ShutdownMode.OnExplicitShutdown)
            {
                await Task.Delay(ExplicitShutdownAt - SecondCloseAt);
                Log("SHUTDOWN_CALL");
                Shutdown();
            }
        }

        private async Task RunClaimAsync(string claim)
        {
            switch (claim)
            {
                case "v3":
                    await RunV3Async();
                    break;
                case "v6":
                    await RunV6Async();
                    break;
                default:
                    throw new ArgumentException("Unknown claim: " + claim);
            }
        }

        private async Task RunV3Async()
        {
            Window probe = new Window { ShowActivated = false };
            bool assigned = ReferenceEquals(MainWindow, probe);
            MainWindow = null;
            bool reset = MainWindow == null;
            probe.Close();
            Log($"V3 direct probeBecameMainWindow={assigned} resetToNullWorks={reset}");

            // The library path: no host window exists, so the overlay is the first window created.
            Task shown = ShowToastAsync(TimeSpan.FromHours(1));
            await Task.Delay(500);
            Log("V3 library overlayLeftAsMainWindow=" + (MainWindow != null) + " mainWindowType=" + (MainWindow?.GetType().Name ?? "null"));

            Log("SHUTDOWN_CALL");
            Shutdown();
            await Task.WhenAny(shown, Task.Delay(1000));
        }

        private async Task RunV6Async()
        {
            // No host window at all: the overlay is the only window and its toast expires.
            await ShowToastAsync(TimeSpan.FromMilliseconds(500));
            Log("V6 toast expired, waiting for shutdown");
            await Task.Delay(SafetyShutdownAt - TimeSpan.FromSeconds(1));
            Log("V6 STILL_ALIVE");
            Shutdown();
        }
    }
}
