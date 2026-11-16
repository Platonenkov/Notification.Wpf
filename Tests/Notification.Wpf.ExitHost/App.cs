using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

using Notification.Core;
using Notification.Wpf.Classes;

using NotificationContent = Notification.Wpf.NotificationContent;
using NotificationControl = Notification.Wpf.Controls.Notification;

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

        // Soak state. The weak table de-duplicates controls seen on consecutive scans; the list lets the
        // markers count survivors without keeping any control alive.
        private readonly ConditionalWeakTable<NotificationControl, object> _seen = new ConditionalWeakTable<NotificationControl, object>();
        private readonly List<WeakReference<NotificationControl>> _tracked = new List<WeakReference<NotificationControl>>();
        private readonly NotificationContent _sharedContent = new NotificationContent { Title = "Soak shared", Message = "one content, shown repeatedly" };
        private readonly int[] _shown = new int[5];
        private int _asyncCompleted;
        private int _progressShown;
        private bool _loadPaused;

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
            for (int i = 0; i < argv.Length; i++)
            {
                string key = argv[i].TrimStart('-');
                bool hasValue = i + 1 < argv.Length && !argv[i + 1].StartsWith("--", StringComparison.Ordinal);
                // A bare switch such as --soak is a flag.
                result[key] = hasValue ? argv[++i] : "true";
            }
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

            if (Flag("soak"))
            {
                CreateMainWindow();
                _ = RunSoakAsync();
                return;
            }

            if (_args.TryGetValue("producer-ms", out string? producerMs))
            {
                StartProducerRace(int.Parse(producerMs, CultureInfo.InvariantCulture));
                return;
            }

            if (_args.TryGetValue("claim", out string? claim))
            {
                _ = RunClaimAsync(claim);
                return;
            }

            string toast = _args.TryGetValue("toast", out string? kind) ? kind : "idle";
            if (_args.TryGetValue("toast-delay-ms", out string? delay) && int.Parse(delay) > 0)
            {
                // Late overlay: the main window has loaded and laid out before the overlay exists. This
                // covers late overlay wiring; the overlay's own Loaded/SizeChanged rescan normally tracks the
                // main window, and the Scan() in ShowContent is kept for the race where the main window
                // closes before the overlay's first layout. overlay-first is ignored in this shape.
                CreateMainWindow();
                _ = StartToastLaterAsync(toast, TimeSpan.FromMilliseconds(int.Parse(delay)));
            }
            else if (Flag("overlay-first"))
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

        private async Task StartToastLaterAsync(string toast, TimeSpan delay)
        {
            await Task.Delay(delay);
            StartToast(toast);
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
                case "thread":
                    StartToastThread();
                    break;
                default:
                    throw new ArgumentException("Unknown toast kind: " + toast);
            }
        }

        /// <summary>
        /// Runs the manager and its overlay on a second STA thread with its own dispatcher, the setup the
        /// public <c>NotificationManager(Dispatcher)</c> constructor allows. The application thread must not
        /// touch that overlay when its host windows open and close. The thread is a background thread: the
        /// process is expected to leave when the application's own windows are gone.
        /// </summary>
        private void StartToastThread()
        {
            Thread thread = new Thread(() =>
            {
                Dispatcher dispatcher = Dispatcher.CurrentDispatcher;
                // Dispatcher.Run installs this context only later; without it the first await in the library
                // would resume on a thread-pool thread.
                SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(dispatcher));
                // A manager on another thread still shares process-wide WPF state with the application thread
                // (known roadmap item), so its own rendering can fail. That is noted, not a verdict: the case is
                // about what the application thread does with this overlay.
                dispatcher.UnhandledException += (_, e) =>
                {
                    Log("TOAST_THREAD_NOTE " + e.Exception.GetType().Name + ": " + e.Exception.Message);
                    e.Handled = true;
                };

                try
                {
                    NotificationManager manager = new NotificationManager(dispatcher);
                    Task<NotificationCloseResult> shown = manager.ShowAsync(new NotificationRequest
                    {
                        Title = "ExitHost",
                        Message = "toast thread",
                        ExpirationTime = TimeSpan.FromHours(1)
                    });
                    Log("TOAST_THREAD_SHOWN");
                    _ = shown.ContinueWith(t => Log("TOAST_THREAD_DONE " + t.Result.Reason), TaskScheduler.Default);
                }
                catch (Exception ex)
                {
                    Log("TOAST_THREAD_ERROR " + ex);
                }

                Dispatcher.Run();
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.IsBackground = true;
            thread.Name = "ExitHost toast";
            thread.Start();
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

        /// <summary>
        /// Producer race: a periodic producer keeps showing toasts on the UI thread while the main window is
        /// closed under OnLastWindowClose. Reproduces the sample's 1 s timer with a shorter period so the race
        /// between the producer and the overlay teardown is not left to chance. <c>--producer-kind</c> is
        /// <c>dispatcher</c> (DispatcherTimer) or <c>timers</c> (System.Timers.Timer + Dispatcher.Invoke, as
        /// the sample). The host stops itself after <c>--stuck-after-ms</c> and logs STUCK when the process
        /// did not leave on its own.
        /// </summary>
        private void StartProducerRace(int periodMs)
        {
            string kind = _args.TryGetValue("producer-kind", out string? k) ? k : "dispatcher";
            int closeAt = _args.TryGetValue("close-ms", out string? c) ? int.Parse(c, CultureInfo.InvariantCulture) : 3500;
            int jitter = _args.TryGetValue("close-jitter-ms", out string? j) ? int.Parse(j, CultureInfo.InvariantCulture) : 0;
            int stuckAfter = _args.TryGetValue("stuck-after-ms", out string? s) ? int.Parse(s, CultureInfo.InvariantCulture) : 8000;
            closeAt += jitter > 0 ? new Random().Next(jitter) : 0;

            CreateMainWindow();
            Dispatcher.ShutdownStarted += (_, _) => Log("DISPATCHER_SHUTDOWN_STARTED");
            Dispatcher.ShutdownFinished += (_, _) => Log("DISPATCHER_SHUTDOWN_FINISHED");

            HashSet<Window> known = new HashSet<Window>();
            int lastVisible = -1;
            int shows = 0;
            bool mainClosed = false;
            _main!.Closed += (_, _) => mainClosed = true;

            // Window and toast census: the library exposes no events, so a fast poll stands in for them.
            DispatcherTimer census = new DispatcherTimer(DispatcherPriority.Send) { Interval = TimeSpan.FromMilliseconds(5) };
            census.Tick += (_, _) =>
            {
                foreach (Window window in Windows.Cast<Window>().ToArray())
                {
                    if (!known.Add(window))
                        continue;
                    string name = window.GetType().Name + "#" + window.GetHashCode();
                    Log("WINDOW_ADDED " + name);
                    window.Closing += (_, _) => Log("WINDOW_CLOSING " + name);
                    window.Closed += (_, _) => Log("WINDOW_CLOSED " + name);
                }

                int visible = CountVisibleControls();
                if (visible != lastVisible)
                {
                    Log("VISIBLE " + lastVisible + "->" + visible + " windows=" + Windows.Count + " mainClosed=" + mainClosed);
                    lastVisible = visible;
                }
            };
            census.Start();

            void ShowOne(string source)
            {
                int n = ++shows;
                Log("SHOW " + n + " via=" + source + " ui=" + Dispatcher.CheckAccess() + " visibleBefore=" + CountVisibleControls()
                    + " windows=" + Windows.Count + " mainClosed=" + mainClosed);
                _manager!.Show("Pink", "string " + n, NotificationType.Information, expirationTime: TimeSpan.FromSeconds(5));
            }

            if (kind == "timers")
            {
                System.Timers.Timer timer = new System.Timers.Timer(periodMs);
                timer.Elapsed += (_, _) =>
                {
                    try
                    {
                        Dispatcher.Invoke(() => ShowOne("timers"));
                    }
                    catch (Exception ex)
                    {
                        Log("PRODUCER_ERROR " + ex.GetType().Name);
                    }
                };
                timer.Start();
                Exit += (_, _) => timer.Stop();
            }
            else
            {
                DispatcherTimer timer = new DispatcherTimer(DispatcherPriority.Normal) { Interval = TimeSpan.FromMilliseconds(periodMs) };
                timer.Tick += (_, _) => ShowOne("dispatcher");
                timer.Start();
            }

            _ = RunProducerScheduleAsync(closeAt, stuckAfter);
        }

        private async Task RunProducerScheduleAsync(int closeAt, int stuckAfter)
        {
            await Task.Delay(closeAt);
            Log("CLOSING main");
            _main!.Close();

            await Task.Delay(stuckAfter);
            Log("STUCK windows=" + string.Join(',', Windows.Cast<Window>().Select(w => w.GetType().Name)) + " visible=" + CountVisibleControls());
            Shutdown();
        }

        private async Task RunScheduleAsync()
        {
            bool second = Flag("second-window");

            await Task.Delay(SecondWindowAt);
            if (second)
            {
                // Opened after the last explicit overlay scan: the Loaded/SizeChanged rescan or the rescan on a close must pick it up.
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

        private async Task RunSoakAsync()
        {
            double minutes = double.Parse(_args["minutes"], CultureInfo.InvariantCulture);
            double firstMarkerMinutes = _args.TryGetValue("first-marker-minutes", out string? first)
                ? double.Parse(first, CultureInfo.InvariantCulture)
                : 10;
            string markerPath = _args["dump-marker"];

            Resources.Add(new DataTemplateKey(typeof(SoakItem)), CreateSoakTemplate());

            // Controls live a couple of seconds, so a 100 ms scan sees every one of them.
            DispatcherTimer scanTimer = new DispatcherTimer(DispatcherPriority.Background) { Interval = TimeSpan.FromMilliseconds(100) };
            scanTimer.Tick += (_, _) => ScanForControls();
            scanTimer.Start();

            // 200 ms per tick is the 5 notifications per second of the spec; progress comes on top of that.
            int tick = 0;
            DispatcherTimer loadTimer = new DispatcherTimer(DispatcherPriority.Normal) { Interval = TimeSpan.FromMilliseconds(200) };
            loadTimer.Tick += (_, _) =>
            {
                if (_loadPaused)
                    return;
                ShowSoakNotification(tick);
                if (tick % 50 == 49)
                    _ = RunSoakProgressAsync();
                tick++;
            };

            // A periodic summary makes a slow leak visible in the log before the markers.
            DispatcherTimer statusTimer = new DispatcherTimer(DispatcherPriority.Background) { Interval = TimeSpan.FromSeconds(30) };
            statusTimer.Tick += (_, _) => Log(
                "SOAK_STATUS shown=" + string.Join('/', _shown) + " progress=" + _progressShown
                + " asyncDone=" + _asyncCompleted + " visible=" + CountVisibleControls()
                + " heap=" + GC.GetTotalMemory(false) + " ws=" + GetWorkingSet());
            statusTimer.Start();

            Log("SOAK_START minutes=" + minutes + " firstMarkerMinutes=" + firstMarkerMinutes);
            loadTimer.Start();

            bool hasFirstMarker = firstMarkerMinutes < minutes;
            long firstHeap = -1;
            int firstLive = 0;

            if (hasFirstMarker)
            {
                await Task.Delay(TimeSpan.FromMinutes(firstMarkerMinutes));
                (firstLive, firstHeap) = await TakeMarkerAsync("1", markerPath);
                minutes -= firstMarkerMinutes;
            }

            await Task.Delay(TimeSpan.FromMinutes(minutes));
            (int endLive, long endHeap) = await TakeMarkerAsync("end", markerPath);

            loadTimer.Stop();
            // A survivor stays in _tracked until it is collected, so zero live controls at the end marker also
            // proves that nothing leaked earlier. The first marker is reported only: R15 accepts a transient
            // survivor there (the last closed control, still referenced by WPF while the load is paused).
            bool noLive = endLive == 0;
            bool heapOk = true;
            string heapChange = "n/a";
            if (hasFirstMarker)
            {
                double change = (endHeap - firstHeap) * 100.0 / firstHeap;
                heapOk = change <= 10;
                heapChange = change.ToString("F1", CultureInfo.InvariantCulture) + "%";
            }
            Log("SOAK_RESULT " + (noLive && heapOk ? "PASS" : "FAIL") + " noLiveClosed=" + noLive + " firstMarkerLive=" + firstLive
                + " heapChange=" + heapChange + " shown=" + string.Join('/', _shown) + " progress=" + _progressShown
                + " asyncDone=" + _asyncCompleted);
            Shutdown();
        }

        private static DataTemplate CreateSoakTemplate()
        {
            FrameworkElementFactory text = new FrameworkElementFactory(typeof(TextBlock));
            text.SetBinding(TextBlock.TextProperty, new System.Windows.Data.Binding(nameof(SoakItem.Text)));
            text.SetValue(TextBlock.PaddingProperty, new Thickness(12));
            return new DataTemplate(typeof(SoakItem)) { VisualTree = text };
        }

        private void ShowSoakNotification(int tick)
        {
            TimeSpan expiration = TimeSpan.FromSeconds(2);
            int kind = tick % 5;
            _shown[kind]++;
            switch (kind)
            {
                case 0:
                    _manager!.Show("Soak", "plain text " + tick, NotificationType.Information, expirationTime: expiration);
                    break;
                case 1:
                    Task<NotificationCloseResult> pending = _manager!.ShowAsync(new NotificationRequest
                    {
                        Title = "Soak buttons",
                        Message = "action buttons " + tick,
                        ExpirationTime = expiration,
                        Buttons = new[]
                        {
                            new NotificationButton { Id = "ok", Content = "OK" },
                            new NotificationButton { Id = "later", Content = "Later" }
                        }
                    });
                    _ = pending.ContinueWith(_ => Interlocked.Increment(ref _asyncCompleted), TaskScheduler.Default);
                    break;
                case 2:
                    _manager!.Show(new SoakItem { Text = "data template " + tick }, expirationTime: expiration);
                    break;
                case 3:
                    // The same instance every time: the library must neither mutate nor retain it per show.
                    _manager!.Show(_sharedContent, expirationTime: expiration);
                    break;
                default:
                    _manager!.Show("Soak", "trimmed text " + tick, NotificationType.Warning, expirationTime: expiration,
                        trim: NotificationTextTrimType.AttachIfMoreRows, RowsCountWhenTrim: 1);
                    break;
            }
        }

        private async Task RunSoakProgressAsync()
        {
            _progressShown++;
            NotifierProgress<NotificationProgressReport> progress =
                _manager!.ShowProgressBar(new ProgressBarOptions { Title = "Soak progress" });
            for (int step = 1; step <= 5; step++)
            {
                progress.Report(new NotificationProgressReport(step * 20, "step " + step, null, null));
                await Task.Delay(400);
            }
            progress.Dispose();
        }

        private async Task<(int Live, long Heap)> TakeMarkerAsync(string name, string markerPath)
        {
            _loadPaused = true;
            Log("MARKER_PAUSE " + name);

            // Wait until the overlay holds no notification, so a survivor cannot be a visible one.
            Stopwatch drain = Stopwatch.StartNew();
            while (CountVisibleControls() > 0 && drain.Elapsed < TimeSpan.FromSeconds(90))
                await Task.Delay(500);
            Log("MARKER_DRAINED " + name + " visible=" + CountVisibleControls() + " afterMs=" + drain.ElapsedMilliseconds);

            int live = 0;
            for (int round = 1; round <= 3; round++)
            {
                // Idle yields let WPF's weak-event and dispatcher-queued cleanup run before each collection.
                await Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
                await Task.Delay(500);
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
                live = CountLiveTracked();
                Log("MARKER_ROUND " + name + " round=" + round + " live=" + live);
                if (live > 0)
                    LogLiveControls();
            }

            // Dead entries are only this harness's own bookkeeping. Left in, the list grows with the run length
            // and the heap verdict would measure the harness instead of the library (R14).
            int total = _tracked.Count;
            int pruned = _tracked.RemoveAll(reference => !reference.TryGetTarget(out NotificationControl? _));
            _tracked.TrimExcess();
            long heap = GC.GetTotalMemory(true);
            Log("MARKER " + name + " live=" + live + " tracked=" + total + " pruned=" + pruned + " visible=" + CountVisibleControls()
                + " totalMemory=" + heap + " workingSet=" + GetWorkingSet()
                + " shown=" + string.Join('/', _shown) + " progress=" + _progressShown + " asyncDone=" + _asyncCompleted);
            File.AppendAllText(markerPath, name + " " + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + Environment.NewLine);

            _loadPaused = false;
            return (live, heap);
        }

        private static long GetWorkingSet()
        {
            using Process process = Process.GetCurrentProcess();
            return process.WorkingSet64;
        }

        // Survivor details are the retention hint when a marker fails: kind of content and whether the
        // control is still attached to a visual tree.
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void LogLiveControls()
        {
            foreach (WeakReference<NotificationControl> reference in _tracked)
            {
                if (reference.TryGetTarget(out NotificationControl? control))
                {
                    Log("LIVE content=" + (control.Content?.GetType().Name ?? "null") + " text=" + control.Content
                        + " isLoaded=" + control.IsLoaded + " visualParent=" + (VisualTreeHelper.GetParent(control) != null)
                        + " dataContext=" + (control.DataContext?.GetType().Name ?? "null"));
                }
            }
        }

        private int CountLiveTracked()
        {
            int live = 0;
            foreach (WeakReference<NotificationControl> reference in _tracked)
            {
                if (reference.TryGetTarget(out NotificationControl? _))
                    live++;
            }
            return live;
        }

        // Kept out of line so no scan local can keep a control reachable from the stack of a marker.
        [MethodImpl(MethodImplOptions.NoInlining)]
        private int CountVisibleControls()
        {
            int count = 0;
            foreach (Window window in Windows.Cast<Window>().ToArray())
                count += Walk(window, register: false);
            return count;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void ScanForControls()
        {
            foreach (Window window in Windows.Cast<Window>().ToArray())
                Walk(window, register: true);
        }

        private int Walk(DependencyObject node, bool register)
        {
            int count = 0;
            if (node is NotificationControl control)
            {
                count++;
                if (register && _seen.TryAdd(control, new object()))
                    _tracked.Add(new WeakReference<NotificationControl>(control));
            }

            int children = node is Visual ? VisualTreeHelper.GetChildrenCount(node) : 0;
            for (int i = 0; i < children; i++)
                count += Walk(VisualTreeHelper.GetChild(node, i), register);
            return count;
        }

        public sealed class SoakItem
        {
            public string Text { get; set; } = "";
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
