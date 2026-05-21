using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using MathCore.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Notification.Wpf.Sample.Logger;
using Notification.Wpf.Sample.Properties;
using Notification.Wpf.Sample.Service;

namespace Notification.Wpf.Sample
{
    /// <summary>Основной класс программы сборки</summary>
    public sealed partial class App
    {
        /// <summary>Признак работы в режиме дизайнера</summary>
        public static bool IsInDesignMode => LicenseManager.UsageMode == LicenseUsageMode.Designtime;

        [NotNull] internal static Settings Settings => Settings.Default;

        [CanBeNull] public static Window ActiveWindow => Current.Windows.Cast<Window>().FirstOrDefault(w => w.IsActive);

        [CanBeNull] public static Window FocusedWindow => Current.Windows.Cast<Window>().FirstOrDefault(w => w.IsFocused);

        [CanBeNull] public static Window CurrentWindow => FocusedWindow ?? ActiveWindow;

        public static IServiceProvider Services => Hosting.Services;
        private static IHost __Hosting;

        public static IHost Hosting => __Hosting ??=
            CreateHostBuilder(Environment.GetCommandLineArgs())
               .Build();

        public static IHostBuilder CreateHostBuilder(string[] args) => Host
           .CreateDefaultBuilder(args)
           .ConfigureServices(ConfigureServices);

        private static void ConfigureServices(HostBuilderContext host, IServiceCollection services)
        {

            services.AddLogging(sp => sp.SetMinimumLevel(LogLevel.Information));
            services.AddSingleton<INotificationManager, NotificationManager>();


            services.RegisterViewModels();
            services.AddSingleton(Settings.Default);

        }

        private static void RegisterBstoRepositories(IServiceCollection services)
        {
            //services.RegisterBstoRepositories();
            //services.RegisterBstoOverloadedRepository();
            //services.RegisterBstoServices();
            //services.AddTransient<BstoContextInitializer>();

        }


        protected override async void OnStartup(StartupEventArgs e)
        {
            var host = Hosting;

            var logger = Services.GetRequiredService<ILoggerFactory>();
            logger.AddLog4Net();

            base.OnStartup(e);
            await host.StartAsync();
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);

            using var host = Hosting;
            await host.StopAsync();
        }

        public App()
        {
#if DEBUG
			if (!Debugger.IsAttached)
			{
				Debugger.Launch();
				Debugger.Break();
			}
#endif

            AppDomain.CurrentDomain.UnhandledException += OnExceptionUnhandled;
            DispatcherUnhandledException += OnDispatcherUnhandledException;

        }

    }
}
