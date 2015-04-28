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

using OxyPlot;
using OxyPlot.Annotations;
using OxyPlot.Axes;
using OxyPlot.Series;

namespace SwarmRobotControlAndCommunication
{
    /// <summary>
    /// Interaction logic for OxyplotWindowTwoChart.xaml
    /// </summary>
    public partial class OxyplotWindowTwoChart : Window
    {
        private String FormTitle = "Hello World";

        private PlotWindowModel viewModel1;
        private PlotWindowModel viewModel2;

        public delegate PlotModel delegatePolyPlot(string title, float[] data);

        public OxyplotWindowTwoChart(string windownTitle, string title1, float[] data1, string title2, float[] data2, delegatePolyPlot plot)
        {
            InitializeComponent();

            // Binding Data Manually
            viewModel1 = new PlotWindowModel();
            viewModel2 = new PlotWindowModel();
            this.Plot1.DataContext = viewModel1;
            this.Plot2.DataContext = viewModel2;
            viewModel1.PlotModel = plot(title1, data1);
            viewModel2.PlotModel = plot(title2, data2);

            //NOTE: FormTitle will be used in OxyplotWindowTwoChart_Loaded() after this function return
            FormTitle = windownTitle;
        }

        #region UI
        private Label titlePlotWindown = new Label();
        private Button closeButtonPlotWindow = new Button();
        private Button minimizeButtonPlotWindow = new Button();
        private Button maximizeButtonPlotWindow = new Button();
        private FrameworkElement titleButtonMainWindow = new FrameworkElement();
        private WindowState previousWindowState = new WindowState();

        private void OxyplotWindowTwoChart_Loaded(object sender, RoutedEventArgs e)
        {
            this.closeButtonPlotWindow = (Button)this.Template.FindName("PlotCloseButton", this);
            if (this.closeButtonPlotWindow != null)
            {
                this.closeButtonPlotWindow.Click += ((o, ex) => this.Close());
            }

            this.maximizeButtonPlotWindow = (Button)this.Template.FindName("PlotMaximizeButton", this);
            if (this.maximizeButtonPlotWindow != null)
            {
                this.maximizeButtonPlotWindow.Click += PlotWindowMaximizeApplicationWindow;
            }

            this.minimizeButtonPlotWindow = (Button)this.Template.FindName("PlotMinimizeButton", this);
            if (this.minimizeButtonPlotWindow != null)
            {
                this.minimizeButtonPlotWindow.Click += PlotWindowMinimizeApplicationWindow;
            }

            this.titlePlotWindown = (Label)this.Template.FindName("PlotTitle", this);
            if (this.titlePlotWindown != null)
            {
                titlePlotWindown.Content = FormTitle;
            }

            this.titleButtonMainWindow = (FrameworkElement)this.Template.FindName("Title", this);
            if (this.titleButtonMainWindow != null)
            {
                this.titleButtonMainWindow.MouseLeftButtonDown += PlotWindowTitle_MouseLeftButtonDown;
            }
        }
        private void PlotWindowMaximizeApplicationWindow(object sender, RoutedEventArgs e)
        {
            previousWindowState = this.WindowState;
            this.WindowState = WindowState.Maximized;
            this.maximizeButtonPlotWindow.Click -= PlotWindowMaximizeApplicationWindow;
            this.maximizeButtonPlotWindow.Click += PlotWindowRestoreApplicationWindow;
            this.maximizeButtonPlotWindow.Content = Application.Current.Resources["RestoreButtonPath"];
        }
        private void PlotWindowRestoreApplicationWindow(object sender, RoutedEventArgs e)
        {
            this.WindowState = previousWindowState;
            this.maximizeButtonPlotWindow.Click -= PlotWindowRestoreApplicationWindow;
            this.maximizeButtonPlotWindow.Click += PlotWindowMaximizeApplicationWindow;
            this.maximizeButtonPlotWindow.Content = Application.Current.Resources["MaximizeButtonPath"];
        }
        private void PlotWindowMinimizeApplicationWindow(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }
        private void PlotWindowTitle_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
            if (e.ClickCount == 2)
            {
                this.maximizeButtonPlotWindow.RaiseEvent(new RoutedEventArgs(Button.ClickEvent, this.maximizeButtonPlotWindow));
            }
        }
        #endregion

        public static PlotModel PolylineMonoY(string title, float[] data)
        {
            var plotModel1 = new PlotModel();
            plotModel1.Title = title;
            var linearAxis1 = new LinearAxis();
            linearAxis1.Maximum = data.Length;
            linearAxis1.Minimum = 0;
            linearAxis1.Position = AxisPosition.Bottom;
            plotModel1.Axes.Add(linearAxis1);
            var linearAxis2 = new LinearAxis();
            linearAxis2.Maximum = data.Max();
            linearAxis2.Minimum = data.Min();
            plotModel1.Axes.Add(linearAxis2);
            var polylineAnnotation1 = new PolylineAnnotation();

            for (uint i = 0; i < data.Length; i++)
            {
                polylineAnnotation1.Points.Add(new DataPoint((double)i, (double)data[i]));
            }
            plotModel1.Annotations.Add(polylineAnnotation1);
            return plotModel1;
        }
    }
}
