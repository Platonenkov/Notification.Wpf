using System;
using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

using Notification.Core;
using Notification.Wpf.Constants;
using Notification.Wpf.View;

using Notifications.Wpf.ViewModels;

namespace Notification.Wpf.Controls
{
    /// <summary>
    /// Control that hosts and displays notifications within an overlay window.
    /// </summary>
    public class NotificationArea : Control
    {
        private Window _overlayWindow;

        internal static INotificationEventService EventService { get; set; }

        #region CollapseProgressAuto : bool - Progress bar will automatically collapsed if items count more that max items

        /// <summary>Progress bar will automatically collapsed if items count more that max items</summary>
        public static readonly DependencyProperty CollapseProgressAutoProperty =
            DependencyProperty.Register(
                nameof(CollapseProgressAuto),
                typeof(bool),
                typeof(NotificationArea),
                new PropertyMetadata(default(bool)));

        /// <summary>Progress bar will automatically collapsed if items count more that max items</summary>
        public bool CollapseProgressAuto { get => (bool)GetValue(CollapseProgressAutoProperty); set => SetValue(CollapseProgressAutoProperty, value); }

        #endregion

        //public NotificationPosition Position
        //{
        //    get => (NotificationPosition)GetValue(PositionProperty);
        //    set => SetValue(PositionProperty, value);
        //}

        //// Using a DependencyProperty as the backing store for Position.  This enables animation, styling, binding, etc...
        //public static readonly DependencyProperty PositionProperty =
        //    DependencyProperty.Register("Position", typeof(NotificationPosition), typeof(NotificationArea), new PropertyMetadata(NotificationPosition.BottomRight));

        #region Position : NotificationPosition - Area position on overlay window

        /// <summary>Area position on overlay window</summary>
        public static readonly DependencyProperty PositionProperty =
            DependencyProperty.Register(
                nameof(Position),
                typeof(NotificationPosition),
                typeof(NotificationArea),
                new PropertyMetadata(NotificationPosition.BottomRight));

        /// <summary>Area position on overlay window</summary>
        public NotificationPosition Position
        {
            get => (NotificationPosition)GetValue(PositionProperty);
            set => SetValue(PositionProperty, value);
        }

        #endregion

        /// <summary>Gets or sets the maximum number of notifications displayed simultaneously.</summary>
        public int MaxItems
        {
            get => (int)GetValue(MaxItemsProperty);
            set => SetValue(MaxItemsProperty, value);
        }

        /// <summary>Identifies the <see cref="MaxItems"/> dependency property.</summary>
        public static readonly DependencyProperty MaxItemsProperty =
            DependencyProperty.Register("MaxItems", typeof(int), typeof(NotificationArea), new PropertyMetadata(int.MaxValue));

        #region IsReversed : bool - Are is reversed

        /// <summary>Are is reversed</summary>
        public static readonly DependencyProperty IsReversedProperty =
            DependencyProperty.Register(
                nameof(IsReversed),
                typeof(bool),
                typeof(NotificationArea),
                new PropertyMetadata(default(bool)));

        /// <summary>Are is reversed</summary>
        public bool IsReversed { get => (bool)GetValue(IsReversedProperty); set => SetValue(IsReversedProperty, value); }

        #endregion

        private IList _items;

        /// <summary>
        /// Initializes a new instance of the <see cref="NotificationArea"/> class.
        /// </summary>
        public NotificationArea()
        {
            Loaded += NotificationArea_Loaded;
            Unloaded += NotificationArea_Unloaded;
        }

        private void NotificationArea_Loaded(object sender, RoutedEventArgs e)
        {
            NotificationManager.AddArea(this);
        }

        private void NotificationArea_Unloaded(object sender, RoutedEventArgs e)
        {
            NotificationManager.RemoveArea(this);
        }

        static NotificationArea()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(NotificationArea),
                new FrameworkPropertyMetadata(typeof(NotificationArea)));
        }

        /// <summary>
        /// Resolves template parts and initializes the reversed layout state after the template is applied.
        /// </summary>
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            var itemsControl = GetTemplateChild("PART_Items") as Panel;
            _items = itemsControl?.Children;

            IsReversed = NotificationConstants.IsReversedPanel is { } reverse ? reverse
            : Position is NotificationPosition.BottomCenter or NotificationPosition.BottomLeft or NotificationPosition.BottomRight;
        }

        /// <summary>
        /// Displays a notification with the specified content.
        /// </summary>
        /// <param name="content">The content to display inside the notification.</param>
        /// <param name="expirationTime">The time after which the notification is closed automatically.</param>
        /// <param name="onClick">Action invoked when the notification is clicked.</param>
        /// <param name="onClose">Action invoked when the notification is closed.</param>
        /// <param name="CloseOnClick">Whether the notification should close when clicked.</param>
        /// <param name="ShowXbtn">Whether to show the close (X) button.</param>
        /// <param name="onCreated">Optional callback invoked with the created notification, used to register a programmatic dismiss handle.</param>
        public async void Show(object content, TimeSpan expirationTime, Action onClick, Action onClose, bool CloseOnClick, bool ShowXbtn,
            Action<Notification> onCreated = null)
        {
            Notification notification = new Notification(content, ShowXbtn);
            onCreated?.Invoke(notification);

            void OnMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
            {
                if (content is NotificationContent message)
                    CloseOnClick = message.CloseOnClick;

                if (CloseOnClick)
                    (sender as Notification)?.Close(_overlayWindow);

                if (onClick == null) return;
                onClick.Invoke();
                (sender as Notification)?.Close(_overlayWindow);
            }

            void OnMouseRightButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
            {
                if (content is NotificationContent message)
                    message.RightClickAction?.Invoke();
            }

            void OnClosed(object sender, RoutedEventArgs e)
            {
                onClose?.Invoke();
                notification.MouseLeftButtonDown -= OnMouseLeftButtonDown;
                notification.MouseRightButtonDown -= OnMouseRightButtonDown;
                notification.NotificationClosed -= OnClosed;
                notification.NotificationClosed -= OnNotificationClosed;
            }

            notification.MouseLeftButtonDown += OnMouseLeftButtonDown;
            notification.MouseRightButtonDown += OnMouseRightButtonDown;
            notification.NotificationClosed += OnClosed;
            notification.NotificationClosed += OnNotificationClosed;

            await OnShowContent(notification, expirationTime);
        }

        /// <summary>
        /// Displays a progress notification.
        /// </summary>
        /// <param name="progress">The progress bar view model.</param>
        /// <param name="ShowXbtn">Whether to show the close (X) button.</param>
        public async void Show(NotificationProgressViewModel progress, bool ShowXbtn)
        {
            NotificationProgress content = new NotificationProgress { DataContext = progress };
            content.Cancel.Click += progress.CancelProgress;

            Notification notification = new Notification(content, ShowXbtn);
            notification.NotificationClosed += progress.CancelProgress;
            notification.NotificationClosed += OnNotificationClosed;
            progress.NotifierProgress.SetArea(notification);

            void OnProgressClosed(object sender, RoutedEventArgs e)
            {
                content.Cancel.Click -= progress.CancelProgress;
                notification.NotificationClosed -= progress.CancelProgress;
                notification.NotificationClosed -= OnProgressClosed;
                notification.NotificationClosed -= OnNotificationClosed;
            }

            notification.NotificationClosed += OnProgressClosed;

            await OnShowContent(notification);

            try
            {
                while (progress.NotifierProgress.IsFinished != true)
                {
                    progress.Cancel.Token.ThrowIfCancellationRequested();
                    await Task.Delay(TimeSpan.FromSeconds(1), progress.Cancel.Token);
                }
            }
            catch (OperationCanceledException)
            { }
            if (!notification.IsClosing)
                notification.Close(_overlayWindow);
        }

        /// <summary>
        /// Adds a notification to the display list and schedules its automatic removal.
        /// </summary>
        /// <param name="notification">The notification to display.</param>
        /// <param name="expirationTime">The time after which the notification is closed automatically.</param>
        /// <returns>A task that completes when the notification has been shown and, if applicable, expired.</returns>
        private async Task OnShowContent(Notification notification, TimeSpan? expirationTime = null)
        {

            if (!IsLoaded)
                return;

            _overlayWindow = Window.GetWindow(this);
            var x = PresentationSource.FromVisual(_overlayWindow);
            if (x == null)
                return;
            lock (_items)
            {
                _items.Add(notification);

                if (_items.OfType<Notification>().Count(i => !i.IsClosing) > MaxItems)
                {
                    if (_items.OfType<Notification>().Where(i => i.Content is not NotificationProgress).Count(i => !i.IsClosing) > MaxItems)
                        _items.OfType<Notification>().Where(i => i.Content is not NotificationProgress).FirstOrDefault(i => !i.IsClosing)?.Close(_overlayWindow);
                    if (CollapseProgressAuto)
                        foreach (var progress in _items.OfType<Notification>()
                           .Where(i => i.Content is NotificationProgress { DataContext: NotificationProgressViewModel { Collapse: false } }))
                        {
                            var content = (NotificationProgress)progress.Content;
                            if (content is not null)
                            {
                                var model = (NotificationProgressViewModel)content.DataContext;
                                model.Collapse = true;
                            }
                        }

                    //_items.OfType<Notification>().Where(i=>i.DataContext is not NotificationProgress).First(i => !i.IsClosing).Close();
                }
            }

            if (expirationTime is null)
                return;

            if (expirationTime == TimeSpan.MaxValue)
            {
                return;
            }

            TimeSpan remaining = (TimeSpan)expirationTime;
            if (NotificationConstants.KeepNotificationVisibleOnMouseOver)
            {
                // Count down in small steps; pause while the cursor is over the notification (issue #71).
                TimeSpan step = TimeSpan.FromMilliseconds(100);
                while (remaining > TimeSpan.Zero)
                {
                    await Task.Delay(step);
                    if (notification.IsClosing)
                        return;
                    if (notification.IsMouseOver)
                        continue;
                    remaining -= step;
                }
            }
            else
            {
                await Task.Delay(remaining);
            }

            notification.Close(_overlayWindow);
        }
        private void OnNotificationClosed(object sender, RoutedEventArgs routedEventArgs)
        {
            _items.Remove(sender);
        }
    }
}