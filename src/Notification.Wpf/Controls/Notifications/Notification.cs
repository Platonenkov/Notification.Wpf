using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

using Notification.Wpf.Base.Interfaces.Options;
using Notification.Wpf.Base.Options;
using Notification.Wpf.Classes;
using Notification.Wpf.Constants;
using Notification.Wpf.Utils;
using Notification.Wpf.View;

using Notifications.Wpf.View;

namespace Notification.Wpf.Controls
{
    /// <summary>Represents a notification control that displays content as a toast and supports close and attach interactions.</summary>
    public partial class Notification : ContentControl
    {
        static Notification()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(Notification),
                new FrameworkPropertyMetadata(typeof(Notification)));
        }

        /// <summary>Initializes a new instance of the <see cref="Notification"/> class.</summary>
        public Notification()
        {

        }

        /// <summary>Initializes a new instance of the <see cref="Notification"/> class with the specified content and close button visibility.</summary>
        /// <param name="content">The content to display inside the notification.</param>
        /// <param name="ShowXbtn"><see langword="true"/> to show the close (X) button; otherwise, <see langword="false"/>.</param>
        public Notification(object content, bool ShowXbtn)
        {
            Content = content;
            XbtnVisibility = ShowXbtn ? Visibility.Visible : Visibility.Collapsed;

            var min = NotificationConstants.MinWidth;
            var max = NotificationConstants.MaxWidth;

            MinWidth = max > min ? min : max;
            MaxWidth = max;

            CornerRadius = NotificationConstants.NotificationCornerRadius;
        }
        /// <summary>Builds the visual tree of the control, wiring up the close and attach template buttons and computing the closing animation duration.</summary>
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            if (GetTemplateChild("PART_CloseButton") is Button closeButton)
            {
                closeButton.Click += OnCloseButtonOnClick;
            }
            if (GetTemplateChild("PART_AttachButton") is Button AttachButton)
                AttachButton.Click += OnAttachButtonOnClick;

            // 仅统计具有确定时长的动画（跳过 Automatic/Forever），避免 TimeSpan.MaxValue 参与计算导致关闭等待异常偏大
            _closingAnimationTime = TimeSpan.Zero;
            var closeTrigger = Template.Triggers.OfType<EventTrigger>().FirstOrDefault(t => t.RoutedEvent == NotificationCloseInvokedEvent);
            if (closeTrigger is not null)
            {
                foreach (var sb in closeTrigger.Actions.OfType<BeginStoryboard>().Select(a => a.Storyboard))
                {
                    var spans = sb.Children.Select(ch => ch.Duration).Append(sb.Duration).Where(d => d.HasTimeSpan).Select(d => d.TimeSpan);
                    foreach (var span in spans)
                    {
                        TimeSpan total = span + (sb.BeginTime ?? TimeSpan.Zero);
                        if (total > _closingAnimationTime)
                            _closingAnimationTime = total;
                    }
                }
            }

        }

        /// <summary>通知关闭时取消显示侧异步等待的取消源（过期倒计时、进度轮询）。</summary>
        internal readonly CancellationTokenSource CloseCts = new CancellationTokenSource();

        private bool _cleanedUp;

        /// <summary>释放通知持有的 UI 资源：停止模板动画、清除绑定与内容引用。可安全重入。</summary>
        internal void Cleanup()
        {
            if (_cleanedUp)
                return;
            _cleanedUp = true;

            if (Template?.Triggers is { } triggers)
            {
                foreach (BeginStoryboard bsb in triggers.OfType<EventTrigger>()
                    .SelectMany(t => t.Actions.OfType<BeginStoryboard>()))
                {
                    try
                    {
                        bsb.Storyboard?.Stop(this);
                        bsb.Storyboard?.Remove(this);
                    }
                    catch (InvalidOperationException) { }
                }
            }

            // 先递归清除模板子树上所有绑定（含对 NotificationContent 的属性订阅），
            // 否则 ReflectPropertyDescriptor 事件字典会继续强引用源对象（泄漏主链）
            ClearBindingsRecursive(this);

            ContentTemplate = null;
            Content = null;
            DataContext = null;
        }

        /// <summary>递归清除指定依赖对象及其全部视觉子节点上的数据绑定。</summary>
        private static void ClearBindingsRecursive(DependencyObject d)
        {
            BindingOperations.ClearAllBindings(d);
            int childCount = VisualTreeHelper.GetChildrenCount(d);
            for (int i = 0; i < childCount; i++)
                ClearBindingsRecursive(VisualTreeHelper.GetChild(d, i));
        }

        #region Свойства
        #region XbtnVisibility : Visibility - X Button visibility

        /// <summary>X Button visibility</summary>
        public static readonly DependencyProperty XbtnVisibilityProperty =
            DependencyProperty.Register(
                nameof(XbtnVisibility),
                typeof(Visibility),
                typeof(Notification),
                new PropertyMetadata(Visibility.Visible));

        /// <summary>X Button visibility</summary>
        public Visibility XbtnVisibility { get => (Visibility)GetValue(XbtnVisibilityProperty); set => SetValue(XbtnVisibilityProperty, value); }

        #endregion

        #region CornerRadius : CornerRadius - Notification card corner radius

        /// <summary>Identifies the <see cref="CornerRadius"/> dependency property.</summary>
        public static readonly DependencyProperty CornerRadiusProperty =
            DependencyProperty.Register(
                nameof(CornerRadius),
                typeof(CornerRadius),
                typeof(Notification),
                new PropertyMetadata(new CornerRadius(0)));

        /// <summary>Gets or sets the corner radius of the notification card.</summary>
        public CornerRadius CornerRadius { get => (CornerRadius)GetValue(CornerRadiusProperty); set => SetValue(CornerRadiusProperty, value); }

        #endregion

        #endregion

        #region Internal close-reason hints

        /// <summary>Internal hint set by area / close-button handlers before <see cref="Close"/> raises the routed event.</summary>
        internal global::Notification.Core.NotificationCloseReason? PendingCloseReason { get; set; }

        /// <summary>Internal hint — action button id when an action button caused the close.</summary>
        internal string PendingButtonId { get; set; }

        /// <summary>Internal hint — action button index when an action button caused the close.</summary>
        internal int? PendingButtonIndex { get; set; }

        #endregion



    }
}
