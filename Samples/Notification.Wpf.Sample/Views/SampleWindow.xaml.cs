using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

using Notification.Wpf.Sample.ViewModels;

namespace Notification.Wpf.Sample.Views
{
    /// <summary>
    /// Логика взаимодействия для SampleWindow.xaml
    /// </summary>
    public partial class SampleWindow : Window
    {

        public SampleWindow()
        {
            InitializeComponent();
            var workArea = SystemParameters.WorkArea;
            var bounds = Taskbar.CurrentBounds;
            var bar_position = Taskbar.Position;

            Left = workArea.Left;
            Top = workArea.Top;
            Width = workArea.Width;
            Height = workArea.Height;

            switch (bar_position)
            {
                case TaskbarPosition.Unknown:
                    break;
                case TaskbarPosition.Left:
                    Width = Width - bounds.Height;
                    Left = Left + bounds.Height;
                    break;
                case TaskbarPosition.Top:
                    Height = Height - bounds.Height;
                    Top = Top + bounds.Height;
                    break;
                case TaskbarPosition.Right:
                    Width = Width - bounds.Height;
                    break;
                case TaskbarPosition.Bottom:
                    Height = Height - bounds.Height;
                    break;
                default: throw new ArgumentOutOfRangeException();
            }

            WindowState = WindowState.Maximized;
        }
    }
}
