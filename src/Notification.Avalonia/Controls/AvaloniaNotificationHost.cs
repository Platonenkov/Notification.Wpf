using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using Avalonia.Threading;
using Notification.Avalonia.Extensions;
using Notification.Core;

namespace Notification.Avalonia.Controls
{
    /// <summary>
    /// Custom notification overlay panel for Avalonia.
    /// Supports both toast and progress notifications.
    /// Injects into the Window content as a Grid overlay.
    /// </summary>
    public sealed class AvaloniaNotificationHost
    {
        private readonly TopLevel _host;
        private readonly StackPanel _panel;
        private readonly Border _container;
        private readonly ConcurrentDictionary<Guid, NotificationCardState> _cards =
            new ConcurrentDictionary<Guid, NotificationCardState>();

        private bool _attached;
        private int _maxItems = 5;
        private NotificationPosition _position = NotificationPosition.BottomRight;

        /// <summary>
        /// Gets or sets the maximum number of notification cards displayed simultaneously.
        /// The value is clamped to a minimum of 1.
        /// </summary>
        public int MaxItems
        {
            get => _maxItems;
            set => _maxItems = Math.Max(1, value);
        }

        /// <summary>
        /// Gets or sets the position where notifications appear.
        /// Can be changed at runtime — takes effect immediately.
        /// </summary>
        public NotificationPosition Position
        {
            get => _position;
            set
            {
                _position = value;
                ApplyPosition();
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AvaloniaNotificationHost"/> class
        /// attached to the specified top-level window.
        /// </summary>
        /// <param name="host">The top-level window the notification overlay is injected into.</param>
        public AvaloniaNotificationHost(TopLevel host)
        {
            _host = host ?? throw new ArgumentNullException(nameof(host));

            _panel = new StackPanel
            {
                Spacing = 8,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };

            _container = new Border
            {
                Child = _panel,
                Padding = new Thickness(16),
                Width = 380,
                IsHitTestVisible = false // let clicks pass through empty areas
            };

            ApplyPosition();

            // Defer attachment until the window is shown
            if (host is Window window)
            {
                window.Opened += OnWindowOpened;
            }
        }

        private void ApplyPosition()
        {
            switch (_position)
            {
                case NotificationPosition.TopLeft:
                    _container.HorizontalAlignment = HorizontalAlignment.Left;
                    _container.VerticalAlignment = VerticalAlignment.Top;
                    break;
                case NotificationPosition.TopCenter:
                    _container.HorizontalAlignment = HorizontalAlignment.Center;
                    _container.VerticalAlignment = VerticalAlignment.Top;
                    break;
                case NotificationPosition.TopRight:
                    _container.HorizontalAlignment = HorizontalAlignment.Right;
                    _container.VerticalAlignment = VerticalAlignment.Top;
                    break;
                case NotificationPosition.BottomLeft:
                    _container.HorizontalAlignment = HorizontalAlignment.Left;
                    _container.VerticalAlignment = VerticalAlignment.Bottom;
                    break;
                case NotificationPosition.BottomCenter:
                    _container.HorizontalAlignment = HorizontalAlignment.Center;
                    _container.VerticalAlignment = VerticalAlignment.Bottom;
                    break;
                case NotificationPosition.BottomRight:
                    _container.HorizontalAlignment = HorizontalAlignment.Right;
                    _container.VerticalAlignment = VerticalAlignment.Bottom;
                    break;
                case NotificationPosition.CenterLeft:
                    _container.HorizontalAlignment = HorizontalAlignment.Left;
                    _container.VerticalAlignment = VerticalAlignment.Center;
                    break;
                case NotificationPosition.CenterRight:
                    _container.HorizontalAlignment = HorizontalAlignment.Right;
                    _container.VerticalAlignment = VerticalAlignment.Center;
                    break;
                case NotificationPosition.Center:
                    _container.HorizontalAlignment = HorizontalAlignment.Center;
                    _container.VerticalAlignment = VerticalAlignment.Center;
                    break;
                default:
                    _container.HorizontalAlignment = HorizontalAlignment.Right;
                    _container.VerticalAlignment = VerticalAlignment.Bottom;
                    break;
            }
        }

        /// <summary>
        /// For Bottom positions, new notifications are inserted at index 0
        /// so the newest appears closest to the bottom edge (stack grows upward).
        /// For Top positions, new notifications are appended (stack grows downward).
        /// </summary>
        private void AddCardToPanel(Border card)
        {
            bool isBottom = _position == NotificationPosition.BottomLeft
                         || _position == NotificationPosition.BottomCenter
                         || _position == NotificationPosition.BottomRight;

            if (isBottom)
            {
                _panel.Children.Insert(0, card);
            }
            else
            {
                _panel.Children.Add(card);
            }
        }

        private void OnWindowOpened(object sender, EventArgs e)
        {
            if (sender is Window window)
            {
                window.Opened -= OnWindowOpened;
                AttachToWindow(window);
            }
        }

        private void AttachToWindow(Window window)
        {
            if (_attached)
                return;

            Control existingContent = window.Content as Control;
            Grid overlayGrid = new Grid();
            window.Content = overlayGrid;

            if (existingContent != null)
            {
                overlayGrid.Children.Add(existingContent);
            }

            // Notification overlay on top of existing content
            overlayGrid.Children.Add(_container);
            _attached = true;
        }

        /// <summary>
        /// Ensure the host is attached (call from Show methods).
        /// </summary>
        private void EnsureAttached()
        {
            if (!_attached && _host is Window window)
            {
                AttachToWindow(window);
            }
        }

        /// <summary>
        /// Show a toast notification with auto-dismiss and optional action buttons.
        /// </summary>
        public Guid ShowToast(NotificationRequest request, TimeSpan expiration)
        {
            Guid id = request.Id;

            new Action(() =>
            {
                EnsureAttached();
                EnforceMaxItems();

                Border card = BuildToastCard(request, id);
                NotificationCardState state = new NotificationCardState(id, card, isProgress: false);
                _cards.TryAdd(id, state);
                AddCardToPanel(card);
                UpdateHitTest();
                AnimateShow(card);

                StartDismissTimer(id, expiration);
            }).InvokeOnUiThread();

            return id;
        }

        /// <summary>
        /// Show a progress notification. Returns the card state for updating.
        /// </summary>
        public ProgressCardHandle ShowProgress(string title, bool showCancel)
        {
            Guid id = Guid.NewGuid();
            ProgressCardHandle handle = new ProgressCardHandle(id);

            new Action(() =>
            {
                EnsureAttached();
                EnforceMaxItems();

                Border card = BuildProgressCard(title, showCancel, id, handle);
                NotificationCardState state = new NotificationCardState(id, card, isProgress: true);
                _cards.TryAdd(id, state);
                AddCardToPanel(card);
                UpdateHitTest();
                AnimateShow(card);
            }).InvokeOnUiThread();

            return handle;
        }

        /// <summary>
        /// Show a notification with custom Avalonia content.
        /// </summary>
        public Guid ShowCustomContent(Control content, TimeSpan expiration, NotificationColor? backgroundColor = null)
        {
            Guid id = Guid.NewGuid();

            new Action(() =>
            {
                EnsureAttached();
                EnforceMaxItems();

                IBrush background = backgroundColor.HasValue
                    ? new SolidColorBrush(Color.FromArgb(backgroundColor.Value.A, backgroundColor.Value.R,
                        backgroundColor.Value.G, backgroundColor.Value.B))
                    : new SolidColorBrush(Color.Parse("#424242"));

                Button closeButton = CreateCloseButton(id);

                Grid header = new Grid
                {
                    ColumnDefinitions = new ColumnDefinitions("*, Auto")
                };
                Grid.SetColumn(content, 0);
                Grid.SetColumn(closeButton, 1);
                header.Children.Add(content);
                header.Children.Add(closeButton);

                Border card = WrapInCard(header, background);
                NotificationCardState state = new NotificationCardState(id, card, isProgress: false);
                _cards.TryAdd(id, state);
                AddCardToPanel(card);
                UpdateHitTest();
                AnimateShow(card);

                StartDismissTimer(id, expiration);
            }).InvokeOnUiThread();

            return id;
        }

        /// <summary>
        /// Close a specific notification by ID.
        /// </summary>
        public void Close(Guid id)
        {
            Dispatcher.UIThread.Post(async () =>
            {
                NotificationCardState state;
                if (_cards.TryRemove(id, out state))
                {
                    await AnimateClose(state.Card);
                    _panel.Children.Remove(state.Card);
                    UpdateHitTest();
                }
            });
        }

        /// <summary>
        /// Close all notifications.
        /// </summary>
        public void CloseAll()
        {
            new Action(() =>
            {
                // Immediate close for "dismiss all" — no animation
                _cards.Clear();
                _panel.Children.Clear();
                UpdateHitTest();
            }).InvokeOnUiThread();
        }

        private static async void AnimateShow(Border card)
        {
            Animation fadeIn = new Animation
            {
                Duration = TimeSpan.FromMilliseconds(200),
                Easing = new CubicEaseOut(),
                Children =
                {
                    new KeyFrame
                    {
                        Cue = new Cue(0),
                        Setters =
                        {
                            new Setter(Visual.OpacityProperty, 0.0),
                            new Setter(TranslateTransform.XProperty, 50.0)
                        }
                    },
                    new KeyFrame
                    {
                        Cue = new Cue(1),
                        Setters =
                        {
                            new Setter(Visual.OpacityProperty, 1.0),
                            new Setter(TranslateTransform.XProperty, 0.0)
                        }
                    }
                }
            };

            card.RenderTransform = new TranslateTransform();
            await fadeIn.RunAsync(card);
        }

        private static async Task AnimateClose(Border card)
        {
            Animation fadeOut = new Animation
            {
                Duration = TimeSpan.FromMilliseconds(250),
                Easing = new CubicEaseIn(),
                FillMode = FillMode.Forward,
                Children =
                {
                    new KeyFrame
                    {
                        Cue = new Cue(0),
                        Setters = { new Setter(Visual.OpacityProperty, 1.0) }
                    },
                    new KeyFrame
                    {
                        Cue = new Cue(1),
                        Setters = { new Setter(Visual.OpacityProperty, 0.0) }
                    }
                }
            };

            await fadeOut.RunAsync(card);
        }

        private void UpdateHitTest()
        {
            _container.IsHitTestVisible = _panel.Children.Count > 0;
        }

        private void EnforceMaxItems()
        {
            while (_panel.Children.Count >= _maxItems)
            {
                KeyValuePair<Guid, NotificationCardState> oldest = _cards
                    .Where(c => !c.Value.IsProgress)
                    .OrderBy(c => c.Value.CreatedAt)
                    .FirstOrDefault();

                if (oldest.Value == null)
                {
                    oldest = _cards.OrderBy(c => c.Value.CreatedAt).FirstOrDefault();
                }

                if (oldest.Value != null)
                {
                    Close(oldest.Key);
                }
                else
                {
                    break;
                }
            }
        }

        private async void StartDismissTimer(Guid id, TimeSpan expiration)
        {
            TimeSpan maxSafe = TimeSpan.FromMilliseconds(int.MaxValue);
            if (expiration > maxSafe)
                return; // NeverExpires — no auto-dismiss

            try
            {
                await Task.Delay(expiration);
                Close(id);
            }
            catch (TaskCanceledException)
            {
                // Ignored
            }
        }

        #region UI Builders

        private Border BuildToastCard(NotificationRequest request, Guid id)
        {
            IBrush background = GetBackgroundBrush(request);
            IBrush foreground = GetForegroundBrush(request);

            TextBlock titleBlock = new TextBlock
            {
                Text = request.Title ?? "",
                FontWeight = FontWeight.SemiBold,
                FontSize = 14,
                Foreground = foreground,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 4)
            };

            TextBlock messageBlock = new TextBlock
            {
                Text = request.Message ?? "",
                FontSize = 13,
                Foreground = foreground,
                Opacity = 0.9,
                TextWrapping = TextWrapping.Wrap
            };

            Button closeButton = CreateCloseButton(id);

            Grid header = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("*, Auto")
            };
            Grid.SetColumn(titleBlock, 0);
            Grid.SetColumn(closeButton, 1);
            header.Children.Add(titleBlock);
            header.Children.Add(closeButton);

            StackPanel content = new StackPanel { Spacing = 2 };
            content.Children.Add(header);
            if (!string.IsNullOrEmpty(request.Message))
                content.Children.Add(messageBlock);

            // OnClick — click anywhere on the card
            bool closeOnClick = request.CloseOnClick;
            Action onClick = request.OnClick;
            Action onClose = request.OnClose;

            // Action buttons
            bool hasLeftButton = !string.IsNullOrEmpty(request.LeftButtonContent);
            bool hasRightButton = !string.IsNullOrEmpty(request.RightButtonContent);

            if (hasLeftButton || hasRightButton)
            {
                StackPanel buttonPanel = new StackPanel
                {
                    Orientation = global::Avalonia.Layout.Orientation.Horizontal,
                    Spacing = 8,
                    Margin = new Thickness(0, 8, 0, 0),
                    HorizontalAlignment = HorizontalAlignment.Right
                };

                if (hasLeftButton)
                {
                    Button leftBtn = CreateActionButton(request.LeftButtonContent, foreground);
                    Action leftAction = request.LeftButtonAction;
                    leftBtn.Click += (s, e) =>
                    {
                        leftAction?.Invoke();
                        Close(id);
                    };
                    buttonPanel.Children.Add(leftBtn);
                }

                if (hasRightButton)
                {
                    Button rightBtn = CreateActionButton(request.RightButtonContent, foreground);
                    Action rightAction = request.RightButtonAction;
                    rightBtn.Click += (s, e) =>
                    {
                        rightAction?.Invoke();
                        Close(id);
                    };
                    buttonPanel.Children.Add(rightBtn);
                }

                content.Children.Add(buttonPanel);
            }

            // Image support
            Image imageControl = BuildImageControl(request);
            if (imageControl != null)
            {
                NotificationImageData imageData = request.PlatformImage as NotificationImageData;
                if (imageData?.Position == ImagePosition.Top)
                {
                    content.Children.Insert(0, imageControl);
                }
                else
                {
                    content.Children.Add(imageControl);
                }
            }

            Border card = WrapInCard(content, background);

            // Wire up click-to-close and onClick callback on the card itself
            if (onClick != null || closeOnClick)
            {
                card.PointerPressed += (s, e) =>
                {
                    onClick?.Invoke();
                    if (closeOnClick)
                        Close(id);
                };
                card.Cursor = new global::Avalonia.Input.Cursor(global::Avalonia.Input.StandardCursorType.Hand);
            }

            return card;
        }

        private Border BuildProgressCard(string title, bool showCancel, Guid id, ProgressCardHandle handle)
        {
            IBrush background = new SolidColorBrush(Color.Parse("#424242"));
            IBrush foreground = Brushes.White;

            TextBlock titleBlock = new TextBlock
            {
                Text = title ?? "Processing...",
                FontWeight = FontWeight.SemiBold,
                FontSize = 14,
                Foreground = foreground,
                TextWrapping = TextWrapping.Wrap
            };

            ProgressBar progressBar = new ProgressBar
            {
                Minimum = 0,
                Maximum = 100,
                Value = 0,
                Height = 16,
                Margin = new Thickness(0, 8, 0, 4),
                IsIndeterminate = true
            };

            TextBlock messageBlock = new TextBlock
            {
                Text = "",
                FontSize = 12,
                Foreground = foreground,
                Opacity = 0.8,
                TextWrapping = TextWrapping.Wrap
            };

            TextBlock waitingBlock = new TextBlock
            {
                Text = "",
                FontSize = 11,
                Foreground = foreground,
                Opacity = 0.6,
                IsVisible = false
            };

            Button cancelButton = new Button
            {
                Content = "Cancel",
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 6, 0, 0),
                Padding = new Thickness(16, 4),
                IsVisible = showCancel
            };

            Button closeButton = CreateCloseButton(id);

            Grid header = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("*, Auto")
            };
            Grid.SetColumn(titleBlock, 0);
            Grid.SetColumn(closeButton, 1);
            header.Children.Add(titleBlock);
            header.Children.Add(closeButton);

            StackPanel content = new StackPanel { Spacing = 2 };
            content.Children.Add(header);
            content.Children.Add(progressBar);
            content.Children.Add(messageBlock);
            content.Children.Add(waitingBlock);
            content.Children.Add(cancelButton);

            // Wire up the handle to update UI
            handle.SetControls(titleBlock, progressBar, messageBlock, waitingBlock, cancelButton);
            handle.CloseRequested += () => Close(id);

            cancelButton.Click += (s, e) => handle.RequestCancel();

            return WrapInCard(content, background);
        }

        private Border WrapInCard(Control content, IBrush background)
        {
            return new Border
            {
                Background = background,
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(14, 10),
                Child = content,
                BoxShadow = new BoxShadows(new BoxShadow
                {
                    OffsetX = 0,
                    OffsetY = 2,
                    Blur = 8,
                    Color = Color.FromArgb(80, 0, 0, 0)
                })
            };
        }

        private Button CreateCloseButton(Guid id)
        {
            Button button = new Button
            {
                Content = "✕",
                FontSize = 14,
                Padding = new Thickness(4, 0),
                Background = Brushes.Transparent,
                Foreground = Brushes.White,
                VerticalAlignment = VerticalAlignment.Top,
                HorizontalAlignment = HorizontalAlignment.Right,
                Cursor = new global::Avalonia.Input.Cursor(global::Avalonia.Input.StandardCursorType.Hand)
            };

            button.Click += (s, e) => Close(id);
            return button;
        }

        private static Button CreateActionButton(string text, IBrush foreground)
        {
            return new Button
            {
                Content = text,
                Foreground = foreground,
                Background = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)),
                Padding = new Thickness(16, 6),
                FontSize = 13,
                FontWeight = FontWeight.SemiBold,
                Cursor = new global::Avalonia.Input.Cursor(global::Avalonia.Input.StandardCursorType.Hand)
            };
        }

        private static Image BuildImageControl(NotificationRequest request)
        {
            if (request.PlatformImage == null)
                return null;

            IImage source = null;

            // PlatformImage can be NotificationImageData (from Core builder) or Avalonia IImage directly
            if (request.PlatformImage is NotificationImageData imageData)
            {
                if (imageData.Position == ImagePosition.None)
                    return null;

                if (imageData.RawData != null && imageData.RawData.Length > 0)
                {
                    using (MemoryStream stream = new MemoryStream(imageData.RawData))
                    {
                        source = new Bitmap(stream);
                    }
                }
                else if (!string.IsNullOrEmpty(imageData.Uri))
                {
                    try
                    {
                        source = new Bitmap(imageData.Uri);
                    }
                    catch
                    {
                        return null;
                    }
                }
            }
            else if (request.PlatformImage is IImage avaloniaImage)
            {
                source = avaloniaImage;
            }

            if (source == null)
                return null;

            return new Image
            {
                Source = source,
                MaxHeight = 200,
                Stretch = Stretch.Uniform,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Margin = new Thickness(0, 4)
            };
        }

        private static ISolidColorBrush GetBackgroundBrush(NotificationRequest request)
        {
            if (request.BackgroundColor.HasValue)
            {
                NotificationColor c = request.BackgroundColor.Value;
                return new SolidColorBrush(Color.FromArgb(c.A, c.R, c.G, c.B));
            }

            return request.Type switch
            {
                NotificationType.Success => new SolidColorBrush(Color.Parse("#2E7D32")),
                NotificationType.Warning => new SolidColorBrush(Color.Parse("#F57F17")),
                NotificationType.Error => new SolidColorBrush(Color.Parse("#C62828")),
                NotificationType.Information => new SolidColorBrush(Color.Parse("#1565C0")),
                _ => new SolidColorBrush(Color.Parse("#424242"))
            };
        }

        private static ISolidColorBrush GetForegroundBrush(NotificationRequest request)
        {
            if (request.ForegroundColor.HasValue)
            {
                NotificationColor c = request.ForegroundColor.Value;
                return new SolidColorBrush(Color.FromArgb(c.A, c.R, c.G, c.B));
            }

            return new SolidColorBrush(Colors.White);
        }

        #endregion

        private sealed class NotificationCardState
        {
            public Guid Id { get; }
            public Border Card { get; }
            public bool IsProgress { get; }
            public DateTime CreatedAt { get; }

            public NotificationCardState(Guid id, Border card, bool isProgress)
            {
                Id = id;
                Card = card;
                IsProgress = isProgress;
                CreatedAt = DateTime.UtcNow;
            }
        }
    }

    /// <summary>
    /// Handle for updating a progress notification card from background threads.
    /// </summary>
    public sealed class ProgressCardHandle
    {
        /// <summary>
        /// Gets the unique identifier of the progress notification.
        /// </summary>
        public Guid Id { get; }

        private TextBlock _titleBlock;
        private ProgressBar _progressBar;
        private TextBlock _messageBlock;
        private TextBlock _waitingBlock;
        private Button _cancelButton;

        private CancellationTokenSource _cancelSource = new CancellationTokenSource();

        internal event Action CloseRequested;

        /// <summary>
        /// Gets the cancellation token signaled when the user cancels the progress operation.
        /// </summary>
        public CancellationToken CancellationToken => _cancelSource.Token;

        /// <summary>
        /// Gets the cancellation token source backing the progress operation.
        /// </summary>
        public CancellationTokenSource CancelSource => _cancelSource;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProgressCardHandle"/> class.
        /// </summary>
        /// <param name="id">The unique identifier of the progress notification.</param>
        public ProgressCardHandle(Guid id)
        {
            Id = id;
        }

        internal void SetControls(TextBlock title, ProgressBar progress, TextBlock message,
            TextBlock waiting, Button cancel)
        {
            _titleBlock = title;
            _progressBar = progress;
            _messageBlock = message;
            _waitingBlock = waiting;
            _cancelButton = cancel;
        }

        /// <summary>
        /// Update the progress notification from any thread.
        /// </summary>
        public void UpdateProgress(double? value, string message, string title, bool? showCancel)
        {
            new Action(() =>
            {
                if (value.HasValue)
                {
                    if (_progressBar.IsIndeterminate)
                        _progressBar.IsIndeterminate = false;
                    _progressBar.Value = value.Value;
                }
                else
                {
                    _progressBar.IsIndeterminate = true;
                }

                if (title != null && _titleBlock != null)
                    _titleBlock.Text = title;

                if (message != null && _messageBlock != null)
                    _messageBlock.Text = message;

                if (showCancel.HasValue && _cancelButton != null)
                    _cancelButton.IsVisible = showCancel.Value;
            }).InvokeOnUiThread();
        }

        /// <summary>
        /// Update the waiting time text.
        /// </summary>
        public void UpdateWaitingTime(string text)
        {
            new Action(() =>
            {
                if (_waitingBlock == null) return;
                _waitingBlock.Text = text ?? "";
                _waitingBlock.IsVisible = !string.IsNullOrEmpty(text);
            }).InvokeOnUiThread();
        }

        /// <summary>
        /// Close this progress notification card.
        /// </summary>
        public void Close()
        {
            CloseRequested?.Invoke();
        }

        internal void RequestCancel()
        {
            if (!_cancelSource.IsCancellationRequested)
                _cancelSource.Cancel();
        }
    }
}
