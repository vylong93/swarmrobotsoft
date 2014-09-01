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
using System.Windows.Navigation;
using System.Windows.Shapes;

using OxyPlot;
using OxyPlot.Annotations;
using OxyPlot.Axes;
using OxyPlot.Series;

namespace SwarmRobotControlAndCommunication
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class OxyplotWindow : Window
    {
        #region Constructor

        public PlotModel MyModel { get; private set; }
        private PlotWindowModel viewModel;

        public delegate PlotModel delegatePolyPlot(UInt32[] data, string Title);
        public delegate PlotModel delegateScatterPointPlot(float[] dataX, float[] dataY, string Title);

        public OxyplotWindow(float[] dataX, float[] dataY, String Title, delegateScatterPointPlot plot)
        {
            InitializeComponent();

            //Binding Data Manually
            viewModel = new PlotWindowModel();
            DataContext = viewModel;
             viewModel.PlotModel = plot(dataX, dataY, Title);
        }

        public OxyplotWindow(UInt32[] data, String Title, delegatePolyPlot plot)
        {
            InitializeComponent();

            //Binding Data Manually
            viewModel = new PlotWindowModel();
            DataContext = viewModel;
            viewModel.PlotModel = plot(data, Title);
        }

        private Button closeButtonPlotWindow = new Button();
        private Button minimizeButtonPlotWindow = new Button();
        private Button maximizeButtonPlotWindow = new Button();

        private WindowState previousWindowState = new WindowState();

        private FrameworkElement titleButtonMainWindow = new FrameworkElement();

        private void OxyplotWindow_Loaded(object sender, RoutedEventArgs e)
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

        //[Example("LineAnnotations on linear axes")]
        public static PlotModel LineAnnotationsonlinearaxes()
        {
            var plotModel1 = new PlotModel();
            plotModel1.Title = "LineAnnotations on linear axes";

            var linearAxis1 = new LinearAxis();
            linearAxis1.Maximum = 80;
            linearAxis1.Minimum = -20;
            linearAxis1.Position = AxisPosition.Bottom;
            plotModel1.Axes.Add(linearAxis1);

            var linearAxis2 = new LinearAxis();
            linearAxis2.Maximum = 10;
            linearAxis2.Minimum = -10;
            linearAxis2.Position = AxisPosition.Left;
            plotModel1.Axes.Add(linearAxis2);

            var lineAnnotation1 = new LineAnnotation();
            lineAnnotation1.Intercept = 1;
            lineAnnotation1.Slope = 0.1;
            lineAnnotation1.Text = "First";
            plotModel1.Annotations.Add(lineAnnotation1);

            var lineAnnotation2 = new LineAnnotation();
            lineAnnotation2.Color = OxyColors.Red;
            lineAnnotation2.Intercept = 0;
            lineAnnotation2.MaximumX = 40;
            lineAnnotation2.Slope = 0.3;
            lineAnnotation2.Text = "Second";
            plotModel1.Annotations.Add(lineAnnotation2);

            var lineAnnotation3 = new LineAnnotation();
            lineAnnotation3.Color = OxyColors.Green;
            lineAnnotation3.MaximumY = 10;
            lineAnnotation3.Type = LineAnnotationType.Vertical;
            lineAnnotation3.X = 0;
            lineAnnotation3.Text = "Vertical";
            plotModel1.Annotations.Add(lineAnnotation3);

            var lineAnnotation4 = new LineAnnotation();
            lineAnnotation4.Color = OxyColors.Gold;
            lineAnnotation4.MaximumX = 4;
            lineAnnotation4.Type = LineAnnotationType.Horizontal;
            lineAnnotation4.Y = 2;
            lineAnnotation4.Text = "Horizontal";
            plotModel1.Annotations.Add(lineAnnotation4);

            var lineAnnotation5 = new LineAnnotation();
            lineAnnotation5.Color = OxyColors.Black;
            lineAnnotation5.Type = LineAnnotationType.Horizontal;
            lineAnnotation5.LineStyle = LineStyle.Solid;
            lineAnnotation5.Y = 0;
            lineAnnotation5.Text = "Zero";
            plotModel1.Annotations.Add(lineAnnotation5);
            return plotModel1;
        }

        //[Example("PolylineAnnotation")]
        public static PlotModel PolylineMonoY(UInt32[] data, string Title)
        {
            var plotModel1 = new PlotModel();
            plotModel1.Title = Title;
            var linearAxis1 = new LinearAxis();
            linearAxis1.Maximum = 300;
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

        //
        public static PlotModel ScatterPointPlot(float[] dataX, float[] dataY, string Title) 
        {
            var plotModel1 = new PlotModel();
            plotModel1.Title = Title;
            var linearAxis1 = new LinearAxis();
            linearAxis1.Maximum = dataX.Max() + 10;
            linearAxis1.Minimum = 0;
            linearAxis1.Position = AxisPosition.Bottom;
            linearAxis1.MajorGridlineStyle = LineStyle.Solid;
            linearAxis1.MinorGridlineStyle = LineStyle.Dot;
            plotModel1.Axes.Add(linearAxis1);

            var linearAxis2 = new LinearAxis();
            linearAxis2.Maximum = dataY.Max() + dataX.Min();
            linearAxis2.Minimum = 0;
            linearAxis2.MajorGridlineStyle = LineStyle.Solid;
            linearAxis2.MinorGridlineStyle = LineStyle.Dot;
            plotModel1.Axes.Add(linearAxis2);

            var scatterSeries = new ScatterSeries();
            scatterSeries.MarkerStrokeThickness = 1;
            scatterSeries.MarkerStroke = OxyColor.FromRgb(255, 60, 60);
            scatterSeries.MarkerSize = 4;
            scatterSeries.MarkerType = MarkerType.Star;

            if (dataX.Length != dataY.Length)
                throw new Exception("Invalid length of X and Y!");

            for (int i = 0; i < dataY.Length; i++)
            {
                scatterSeries.Points.Add(new ScatterPoint(dataX[i], dataY[i]));
            }

            plotModel1.Series.Add(scatterSeries);

            //var polylineAnnotation1 = new PolylineAnnotation();

            //uint[] data = { 35, 65, 85, 115, 145, 175, 225 };
            //for (uint i = 0; i < data.Length; i++)
            //{
            //    polylineAnnotation1.Points.Add(new DataPoint((double)(i + 1) * 10, (double)data[i]));
            //}
            //plotModel1.Annotations.Add(polylineAnnotation1);

            Int32 n = dataX.Length;

            float sumX = 0;
            foreach (float item in dataX)
                sumX += (float)item;

            float sumX2 = 0;
            foreach (float item in dataX)
                sumX2 += (float)(item * item);

            float sumY = 0;
            foreach (float item in dataY)
                sumY += (float)item;

            float sumXY = 0;
            for (int i = 0; i < dataX.Length; i++)
                sumXY += ((float)dataX[i] * (float)dataY[i]);

            float detA = (n * sumX2) - (sumX * sumX);
            float detAIntercept = (sumY * sumX2) - (sumXY * sumX);
            float detASlope = (n * sumXY) - (sumX * sumY);


            var lineAnnotation1 = new LineAnnotation();
            lineAnnotation1.Intercept = detAIntercept / detA; // b
            lineAnnotation1.Slope = detASlope / detA; // a
            lineAnnotation1.Text = "Intercept(" + lineAnnotation1.Intercept.ToString() + "); Slope(" + lineAnnotation1.Slope.ToString() + ")";
            plotModel1.Annotations.Add(lineAnnotation1);

            return plotModel1;
        }

    }
}
