using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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

            var storyboards = Template.Triggers.OfType<EventTrigger>().FirstOrDefault(t => t.RoutedEvent == NotificationCloseInvokedEvent)?.Actions.OfType<BeginStoryboard>().Select(a => a.Storyboard);
            _closingAnimationTime = new TimeSpan(storyboards?.Max(s => Math.Min((s.Duration.HasTimeSpan ? s.Duration.TimeSpan + (s.BeginTime ?? TimeSpan.Zero) : TimeSpan.MaxValue).Ticks, s.Children.Select(ch => ch.Duration.TimeSpan + (s.BeginTime ?? TimeSpan.Zero)).Max().Ticks)) ?? 0);

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

        #endregion



    }
}
