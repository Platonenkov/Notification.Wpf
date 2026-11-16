using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;

using Xunit;

namespace Notification.Wpf.Tests.Lifecycle
{
    /// <summary>
    /// Proves the WPF behaviors the lifecycle design rests on. These tests exercise the
    /// framework only, so they pass on any build of the library.
    /// </summary>
    public class WpfClaimsTests
    {
        private sealed class ExposedButton : Button
        {
            public void RaiseClick() => OnClick();
        }

        private sealed class DelegateCommand : ICommand
        {
            private readonly Action _execute;

            public DelegateCommand(Action execute) => _execute = execute;

            public event EventHandler? CanExecuteChanged
            {
                add { }
                remove { }
            }

            public bool CanExecute(object? parameter) => true;

            public void Execute(object? parameter) => _execute();
        }

        private sealed class PlainSource
        {
            public string Text { get; set; } = "plain";
        }

        private sealed class InpcSource : INotifyPropertyChanged
        {
            public event PropertyChangedEventHandler? PropertyChanged
            {
                add { }
                remove { }
            }

            public string Text { get; set; } = "inpc";
        }

        public sealed class TemplateItem
        {
            public string Name { get; set; } = "item";
        }

        [Fact]
        public void V1_ButtonClick_RaisesClickBeforeCommand()
        {
            StaTask.Run(() =>
            {
                List<string> order = new List<string>();
                ExposedButton button = new ExposedButton
                {
                    Command = new DelegateCommand(() => order.Add("command"))
                };
                button.Click += (_, _) => order.Add("click");

                button.RaiseClick();

                Assert.Equal(new[] { "click", "command" }, order);
                return Task.CompletedTask;
            });
        }

        [Fact]
        public void V2_PlainObjectBindingSource_IsNotCollected() =>
            StaTask.Run(async () => await GcAssert.NotCollectedAsync(BindPlainSource()));

        [Fact]
        public void V2_InpcBindingSource_IsCollected() =>
            StaTask.Run(async () => await GcAssert.CollectedAsync(BindInpcSource()));

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static WeakReference BindPlainSource()
        {
            PlainSource source = new PlainSource();
            TextBlock target = new TextBlock();
            target.SetBinding(TextBlock.TextProperty, new Binding(nameof(PlainSource.Text))
            {
                Source = source,
                Mode = BindingMode.OneWay
            });
            return new WeakReference(source);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static WeakReference BindInpcSource()
        {
            InpcSource source = new InpcSource();
            TextBlock target = new TextBlock();
            target.SetBinding(TextBlock.TextProperty, new Binding(nameof(InpcSource.Text))
            {
                Source = source,
                Mode = BindingMode.OneWay
            });
            return new WeakReference(source);
        }

        [Fact]
        public void V7_ClearAllBindings_LeavesTemplateBindingInPlace()
        {
            StaTask.Run(() =>
            {
                DataTemplate template = (DataTemplate)XamlReader.Parse(
                    "<DataTemplate xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\">" +
                    "<TextBlock Text=\"{Binding Name}\"/></DataTemplate>");
                ContentControl control = new ContentControl
                {
                    Content = new TemplateItem(),
                    ContentTemplate = template
                };

                control.ApplyTemplate();
                control.Measure(new Size(200, 100));
                control.Arrange(new Rect(0, 0, 200, 100));

                TextBlock? textBlock = FindTextBlock(control);
                Assert.NotNull(textBlock);

                BindingOperations.ClearAllBindings(textBlock!);

                Assert.NotNull(BindingOperations.GetBindingExpression(textBlock!, TextBlock.TextProperty));
                return Task.CompletedTask;
            });
        }

        private static TextBlock? FindTextBlock(DependencyObject root)
        {
            int count = VisualTreeHelper.GetChildrenCount(root);
            for (int i = 0; i < count; i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(root, i);
                if (child is TextBlock found)
                    return found;
                TextBlock? nested = FindTextBlock(child);
                if (nested != null)
                    return nested;
            }

            return null;
        }
    }

    [Collection("Gui")]
    [Trait("Category", "Gui")]
    public class GuiHostTests
    {
        [Fact]
        public void GuiHost_LoadsArea()
        {
            StaTask.Run(async () =>
            {
                using GuiHost host = await GuiHost.CreateAsync();
                Assert.True(host.Area.IsLoaded);
            });
        }
    }
}
