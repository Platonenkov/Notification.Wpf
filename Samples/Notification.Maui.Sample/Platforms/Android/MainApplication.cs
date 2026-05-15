using Android.App;
using Android.Runtime;
using Microsoft.Maui;

namespace Notification.Maui.Sample
{
    [Application]
    public class MainApplication : MauiApplication
    {
        public MainApplication(nint handle, JniHandleOwnership ownership)
            : base(handle, ownership)
        {
        }

        protected override Microsoft.Maui.Hosting.MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
    }
}
