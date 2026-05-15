using MathCore.Hosting.WPF;
using Microsoft.Extensions.DependencyInjection;
using Notification.Wpf.Sample.ViewModels;

namespace Notification.Wpf.Sample.Service
{
    public class ServiceLocator : ServiceLocatorHosted
    {
        public SampleWindowViewModel SampleModel => App.Services.GetRequiredService<SampleWindowViewModel>();
        public TextSettingViewModel TextSettingModel => App.Services.GetRequiredService<TextSettingViewModel>();
    }

    public static class LocatorExtension
    {
        public static IServiceCollection RegisterViewModels(this IServiceCollection services)
        {
            services.AddSingleton<SampleWindowViewModel>();
            services.AddScoped<TextSettingViewModel>();
            return services;
        }
    }
}
