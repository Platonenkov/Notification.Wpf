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

        public int MaxItems
        {
            get => (int)GetValue(MaxItemsProperty);
            set => SetValue(MaxItemsProperty, value);
        }

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

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            var itemsControl = GetTemplateChild("PART_Items") as Panel;
            _items = itemsControl?.Children;

            IsReversed = NotificationConstants.IsReversedPanel is { } reverse ? reverse
            : Position is NotificationPosition.BottomCenter or NotificationPosition.BottomLeft or NotificationPosition.BottomRight;
        }

        public async void Show(object content, TimeSpan expirationTime, Action onClick, Action onClose, bool CloseOnClick, bool ShowXbtn)
        {
            Notification notification = new Notification(content, ShowXbtn);

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

            void OnClosed(object sender, RoutedEventArgs e)
            {
                onClose?.Invoke();
                notification.MouseLeftButtonDown -= OnMouseLeftButtonDown;
                notification.NotificationClosed -= OnClosed;
                notification.NotificationClosed -= OnNotificationClosed;
            }

            notification.MouseLeftButtonDown += OnMouseLeftButtonDown;
            notification.NotificationClosed += OnClosed;
            notification.NotificationClosed += OnNotificationClosed;

            await OnShowContent(notification, expirationTime);
        }

        /// <summary>
        /// Отображает окно прогресса
        /// </summary>
        /// <param name="progress">модель прогресс бара</param>
        /// <param name="ShowXbtn">need to show X close button</param>
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
        /// Добавляет уведомление в список отображения
        /// </summary>
        /// <param name="notification">уведомление</param>
        /// <param name="expirationTime">время отображения</param>
        /// <returns></returns>
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
            await Task.Delay((TimeSpan)expirationTime);

            notification.Close(_overlayWindow);
        }
        private void OnNotificationClosed(object sender, RoutedEventArgs routedEventArgs)
        {
            _items.Remove(sender);
        }
    }
}