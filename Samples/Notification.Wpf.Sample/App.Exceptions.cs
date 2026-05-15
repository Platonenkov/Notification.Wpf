using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Threading;
using MathCore.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Notification.Wpf.Sample
{
    partial class App
    {
        /// <summary>Обработчик неперехваченных исключений</summary>
        private static void OnDispatcherUnhandledException([CanBeNull] object Sender, [NotNull] DispatcherUnhandledExceptionEventArgs E)
        {
#if DEBUG
            var exception_text = E.Exception.ToString();
            if (E.Exception is System.Windows.ResourceReferenceKeyNotFoundException)
                return;

            var logger = Services.GetRequiredService<ILogger<App>>();
            logger?.LogCritical($"Unhandled exception!\n{E.Exception}");
            var res = MessageBox.Show(exception_text, "Unhandled exception!", MessageBoxButton.OKCancel, E.Handled ? MessageBoxImage.Warning : MessageBoxImage.Error);
            if (res == MessageBoxResult.OK)
                E.Handled = true;

            if (Debugger.IsAttached)
            {
                if (E.Exception is ResourceReferenceKeyNotFoundException)
                {
                    E.Handled = true;
                    MessageBox.Show(exception_text, "Unhandled exception!", MessageBoxButton.OK, E.Handled ? MessageBoxImage.Warning : MessageBoxImage.Error);
                }
                else
                    Debugger.Break();
            }
            else
            {
                MessageBox.Show(exception_text, "Unhandled exception!", MessageBoxButton.OK, E.Handled ? MessageBoxImage.Warning : MessageBoxImage.Error);
            }
#endif
        }

        /// <summary>Обработчик неперехваченных исключений</summary>
        private static void OnExceptionUnhandled([CanBeNull] object Sender, [NotNull] UnhandledExceptionEventArgs E)
        {
            var log = Services.GetRequiredService<ILogger<App>>();
            log?.LogError("Exception:\r\n{0}", E.ExceptionObject);
            var exception_text = E.ExceptionObject.ToString();
#if DEBUG
            if (Debugger.IsAttached) Debugger.Break();
            else
                MessageBox.Show(E.ExceptionObject.ToString(), "Unhandled exception!", MessageBoxButton.OK, E.IsTerminating ? MessageBoxImage.Error : MessageBoxImage.Warning);
#endif
            Trace.Fail("Необработанное исключение", exception_text);
        }
    }
}
