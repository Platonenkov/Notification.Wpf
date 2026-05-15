using Microsoft.Maui.Controls;

namespace Notification.Maui.Sample
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
#pragma warning disable CS0618
            MainPage = new MainPage();
#pragma warning restore CS0618
        }
    }
}
