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
using Notifications.Wpf.ViewModels;

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
            // The view model, not the caller's content, is what the template binds to: the caller's object
            // stays untouched and a second show of it gets its own commands.
            Content = content is NotificationContent source ? new NotificationViewModel(source, Session) : content;
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

            _closingAnimationTime = ComputeClosingAnimationTime(Template);

        }

        /// <summary>Computes how long the closing animation of <paramref name="template"/> runs after <see cref="NotificationCloseInvokedEvent"/>.</summary>
        internal static TimeSpan ComputeClosingAnimationTime(ControlTemplate template)
        {
            if (template == null)
                return TimeSpan.Zero;

            EventTrigger trigger = template.Triggers.OfType<EventTrigger>().FirstOrDefault(t => t.RoutedEvent == NotificationCloseInvokedEvent);
            if (trigger == null)
                return TimeSpan.Zero;

            // Only explicit durations count: an Automatic duration has no TimeSpan (reading it throws)
            // and is resolved by WPF from the children, which are inspected below.
            TimeSpan longest = TimeSpan.Zero;
            foreach (BeginStoryboard action in trigger.Actions.OfType<BeginStoryboard>())
            {
                Storyboard storyboard = action.Storyboard;
                if (storyboard == null)
                    continue;

                TimeSpan begin = storyboard.BeginTime ?? TimeSpan.Zero;
                if (storyboard.Duration.HasTimeSpan && storyboard.Duration.TimeSpan + begin > longest)
                    longest = storyboard.Duration.TimeSpan + begin;

                foreach (Timeline child in storyboard.Children)
                {
                    if (!child.Duration.HasTimeSpan)
                        continue;

                    TimeSpan end = begin + (child.BeginTime ?? TimeSpan.Zero) + child.Duration.TimeSpan;
                    if (end > longest)
                        longest = end;
                }
            }

            return longest;
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

    }
}
