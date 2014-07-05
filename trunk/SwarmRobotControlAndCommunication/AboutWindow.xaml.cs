using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Navigation;
using System.Diagnostics;

namespace SwarmRobotControlAndCommunication
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class AboutWindow : Window
    {
        public AboutWindow()
        {
            InitializeComponent();
        }

        private Button closeButtonAboutWindow = new Button();
        private FrameworkElement titleAboutWindow = new Button();

        private void aboutWindow_Loaded(object sender, RoutedEventArgs e)
        {
            this.closeButtonAboutWindow = (Button)this.Template.FindName("CloseButtonAboutWindow", this);
            if (this.closeButtonAboutWindow != null)
            {
                this.closeButtonAboutWindow.Click += ((o, ex) => this.Close());
            }

            this.titleAboutWindow = (FrameworkElement)this.Template.FindName("Title", this);
            if (this.titleAboutWindow != null)
            {
                this.titleAboutWindow.MouseLeftButtonDown += aboutWindow_MouseLeftButtonDown;
            }
        }

        private void aboutWindow_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }
    }
}
