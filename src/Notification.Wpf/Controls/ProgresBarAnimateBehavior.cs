using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media.Animation;
using Microsoft.Xaml.Behaviors;

namespace Notification.Wpf.Controls
{
    /// <summary>
    /// Behavior that smoothly animates value changes of an attached <see cref="ProgressBar"/>.
    /// </summary>
    public class ProgresBarAnimateBehavior : Behavior<ProgressBar>
    {
        bool _IsAnimating;

        /// <summary>
        /// Called when the behavior is attached to a <see cref="ProgressBar"/>; subscribes to value changes.
        /// </summary>
        protected override void OnAttached()
        {
            base.OnAttached();
            ProgressBar progressBar = this.AssociatedObject;
            progressBar.ValueChanged += ProgressBar_ValueChanged;
        }

        private void ProgressBar_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (e.OldValue > e.NewValue)
                return;
            if (_IsAnimating)
                return;

            _IsAnimating = true;

            DoubleAnimation doubleAnimation = new DoubleAnimation
                (e.OldValue, e.NewValue, new Duration(TimeSpan.FromSeconds(0.1)), FillBehavior.Stop);
            doubleAnimation.Completed += Db_Completed;

            ((ProgressBar)sender).BeginAnimation(RangeBase.ValueProperty, doubleAnimation);

            e.Handled = true;
        }

        private void Db_Completed(object sender, EventArgs e)
        {
            _IsAnimating = false;
        }

        /// <summary>
        /// Called when the behavior is detached from the <see cref="ProgressBar"/>; unsubscribes from value changes.
        /// </summary>
        protected override void OnDetaching()
        {
            base.OnDetaching();
            ProgressBar progressBar = this.AssociatedObject;
            progressBar.ValueChanged -= ProgressBar_ValueChanged;
        }
    }
}