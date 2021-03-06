﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
using System.Drawing;
using System.IO;

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

        public String FormTitle = "Hello World";

        public PlotModel MyModel { get; private set; }
        public PlotWindowModel viewModel;

        public delegate PlotModel delegatePolyPlot(UInt32[] data, string Title);
        public delegate PlotModel delegateScatterPointPlot(float[] dataX, float[] dataY, string Title);

        public delegate PlotModel delegateScatterPointOnlyPlot(UInt32[] id, float[] dataX, float[] dataY, string Title);
        public delegate PlotModel delegateScatterPointAndLinePlot(UInt32[] id, float[] theta, bool[] validTheta, float[] dataX, float[] dataY, string Title);

        public OxyplotWindow(UInt32[] data, String Title, delegatePolyPlot plot)
        {
            InitializeComponent();

            //Binding Data Manually
            viewModel = new PlotWindowModel();
            DataContext = viewModel;
            viewModel.PlotModel = plot(data, Title);
        }

        public OxyplotWindow(float[] dataX, float[] dataY, String Title, delegateScatterPointPlot plot)
        {
            InitializeComponent();

            //Binding Data Manually
            viewModel = new PlotWindowModel();
            DataContext = viewModel;
            viewModel.PlotModel = plot(dataX, dataY, Title);
        }

        public OxyplotWindow(UInt32[] id, float[] dataX, float[] dataY, String Title, delegateScatterPointOnlyPlot plot)
        {
            InitializeComponent();

            //Binding Data Manually
            viewModel = new PlotWindowModel();
            DataContext = viewModel;
            viewModel.PlotModel = plot(id, dataX, dataY, Title);

            //NOTE: FormTitle will be used in OxyplotWindowTwoChart_Loaded() after this function return
            FormTitle = Title;
        }

        public OxyplotWindow(UInt32[] id, float[] theta, bool[] validTheta, float[] dataX, float[] dataY, String Title, delegateScatterPointAndLinePlot plot)
        {
            InitializeComponent();

            //Binding Data Manually
            viewModel = new PlotWindowModel();
            DataContext = viewModel;
            viewModel.PlotModel = plot(id, theta, validTheta, dataX, dataY, Title);

            //NOTE: FormTitle will be used in OxyplotWindowTwoChart_Loaded() after this function return
            FormTitle = Title;
        }

        private Label titlePlotWindown = new Label();
        private Button closeButtonPlotWindow = new Button();
        private Button minimizeButtonPlotWindow = new Button();
        private Button maximizeButtonPlotWindow = new Button();
        private FrameworkElement titleButtonMainWindow = new FrameworkElement();

        private WindowState previousWindowState = new WindowState();

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

        public static PlotModel ScatterPointPlot(float[] dataX, float[] dataY, string Title) 
        {
            var plotModel1 = new PlotModel();
            plotModel1.Title = Title;

            var linearAxis1 = new LinearAxis();
            linearAxis1.Maximum = dataX.Max() + 10;
            linearAxis1.Minimum = 0;
            linearAxis1.PositionAtZeroCrossing = true;
            linearAxis1.TickStyle = TickStyle.Crossing;
            //var linearAxis1 = createOXaxis(0, dataX.Max() + 10);
            linearAxis1.Position = AxisPosition.Bottom;
            linearAxis1.MajorGridlineStyle = LineStyle.Solid;
            linearAxis1.MinorGridlineStyle = LineStyle.Dot;
            plotModel1.Axes.Add(linearAxis1);

            var linearAxis2 = new LinearAxis();
            linearAxis2.Maximum = dataY.Max() + dataX.Min();
            linearAxis2.Minimum = 0;
            linearAxis2.PositionAtZeroCrossing = true;
            linearAxis2.TickStyle = TickStyle.Crossing;
            //var linearAxis2 = createOYaxis(0, dataY.Max() + dataX.Min());
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
                scatterSeries.Points.Add(new ScatterPoint(dataX[i], dataY[i]));

            plotModel1.Series.Add(scatterSeries);

            // Least-Square calculation

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

            double errorVariance = 0;
            double sqrt_e_i = 0;
            for (int i = 0; i < n; i++)
            {
                sqrt_e_i = dataY[i] - lineAnnotation1.Intercept - lineAnnotation1.Slope * dataX[i];
                errorVariance += Math.Pow(sqrt_e_i, 2.0);
            }

            lineAnnotation1.Text = "Intercept " + lineAnnotation1.Intercept.ToString("0.00000") + ", Slope " + lineAnnotation1.Slope.ToString("0.00000") + "Var " + errorVariance.ToString("0.00000");
            plotModel1.Annotations.Add(lineAnnotation1);

            return plotModel1;
        }

        public static PlotModel ScatterPointAndLinePlot(UInt32[] id, float[] theta, bool[] validTheta, float[] dataX, float[] dataY, string Title)
        {
            List<OxyPlot.OxyColor> randomColor = new List<OxyPlot.OxyColor>();
            randomColor.Add(OxyColors.Red);
            randomColor.Add(OxyColors.Blue);
            randomColor.Add(OxyColors.Brown);
            randomColor.Add(OxyColors.DarkSeaGreen);
            randomColor.Add(OxyColors.Violet);
            randomColor.Add(OxyColors.DarkViolet);
            randomColor.Add(OxyColors.DarkCyan);
            randomColor.Add(OxyColors.Navy);
            randomColor.Add(OxyColors.Olive);
            randomColor.Add(OxyColors.DimGray);

            var plotModel1 = new PlotModel();
            plotModel1.PlotAreaBorderThickness = new OxyThickness(0, 0, 0, 0);
            plotModel1.PlotMargins = new OxyThickness(10, 10, 10, 10);
            plotModel1.Title = Title;

            var linearAxis1 = createOXaxis(-70, 70);
            plotModel1.Axes.Add(linearAxis1);

            var linearAxis2 = createOYaxis(-70, 70);
            plotModel1.Axes.Add(linearAxis2);

            if (dataX.Length != dataY.Length)
                throw new Exception("Invalid length of X and Y!");

            if (theta.Length != validTheta.Length)
                throw new Exception("Invalid length of theta and validTheta!");

            if (id.Length != dataY.Length || id.Length != validTheta.Length)
                throw new Exception("Invalid length of id!");

            for (int i = 0; i < id.Length; i++)
            {
                var circle = OxyplotWindow.createCircleAnnotations(12.5, dataX[i], dataY[i], OxyPlot.OxyColors.LightGray);
                plotModel1.Annotations.Add(circle);

                var pointAnnotation = createPointAnnotations(id[i], dataX[i], dataY[i], OxyColors.Cyan, OxyColors.DarkBlue);
                plotModel1.Annotations.Add(pointAnnotation);

                if (validTheta[i])
                {
                    var arrowAnnotation = createArrowAnnotations(6.5, theta[i], dataX[i], dataY[i], randomColor[i % randomColor.Count]);
                    plotModel1.Annotations.Add(arrowAnnotation);
                }
            }

            return plotModel1;
        }

        public static PlotModel ScatterPointOnlyPlot(UInt32[] id, float[] dataX, float[] dataY, string Title)
        {
            UInt32 ui32MainRobotId = 0;
            Match titleMatch = Regex.Match(Title, @"\[(.*?)\]", RegexOptions.IgnoreCase);
            if (titleMatch.Success)
                ui32MainRobotId = UInt32.Parse(titleMatch.Groups[1].Value.Substring(2), System.Globalization.NumberStyles.HexNumber);

            var plotModel1 = new PlotModel();
            plotModel1.PlotAreaBorderThickness = new OxyThickness(0, 0, 0, 0);
            plotModel1.PlotMargins = new OxyThickness(10, 10, 10, 10);
            plotModel1.Title = Title;

            var linearAxis1 = createOXaxis(-70, 70);
            plotModel1.Axes.Add(linearAxis1);

            var linearAxis2 = createOYaxis(-70, 70);
            plotModel1.Axes.Add(linearAxis2);

            if (dataX.Length != dataY.Length)
                throw new Exception("Invalid length of X and Y!");

            for (int i = 0; i < id.Length; i++)
            {
                var circle = createCircleAnnotations(12.5, dataX[i], dataY[i], OxyColors.LightGray);
                plotModel1.Annotations.Add(circle);

                PointAnnotation pointAnnotation;
                if (id[i] == ui32MainRobotId)
                    pointAnnotation = createPointAnnotations(id[i], dataX[i], dataY[i], OxyColors.Orange, OxyColors.Red);
                else
                    pointAnnotation = createPointAnnotations(id[i], dataX[i], dataY[i], OxyColors.Cyan, OxyColors.DarkBlue);

                plotModel1.Annotations.Add(pointAnnotation);
            }

            return plotModel1;
        }


        public static LinearAxis createOXaxis(double min, double max)
        {
            var linearAxis1 = new LinearAxis();
            linearAxis1.Maximum = max;
            linearAxis1.Minimum = min;
            linearAxis1.PositionAtZeroCrossing = true;
            linearAxis1.TickStyle = TickStyle.Crossing;
            //linearAxis1.Position = AxisPosition.Bottom;
            //linearAxis1.MajorGridlineStyle = LineStyle.Solid;
            //linearAxis1.MinorGridlineStyle = LineStyle.Dot;
            return linearAxis1;
        }

        public static LinearAxis createOYaxis(double min, double max)
        {
            var linearAxis2 = new LinearAxis();
            linearAxis2.Maximum = max;
            linearAxis2.Minimum = min;
            linearAxis2.PositionAtZeroCrossing = true;
            linearAxis2.TickStyle = TickStyle.Crossing;
            linearAxis2.Position = AxisPosition.Bottom;
            //linearAxis2.MajorGridlineStyle = LineStyle.Solid;
            //linearAxis2.MinorGridlineStyle = LineStyle.Dot;
            return linearAxis2;
        }

        public static EllipseAnnotation createCircleAnnotations(double radius, double x, double y, OxyColor color)
        {
            var circle = new OxyPlot.Annotations.EllipseAnnotation();
            circle.Width = radius;
            circle.Height = radius;
            circle.StrokeThickness = 0;
            circle.X = x;
            circle.Y = y;
            circle.Fill = OxyPlot.OxyColors.LightGray;
            circle.Stroke = OxyPlot.OxyColors.LightGray;
            circle.Layer = OxyPlot.Annotations.AnnotationLayer.BelowAxes;
            circle.Fill = color;
            circle.Stroke = color;

            return circle;
        }

        public static PointAnnotation createPointAnnotations(UInt32 id, double x, double y, OxyColor fillColor, OxyColor strokeColor)
        {
            var point = new OxyPlot.Annotations.PointAnnotation();
            point.X = x;
            point.Y = y;
            point.Fill = fillColor;
            point.Stroke = strokeColor;
            point.StrokeThickness = 3;
            point.Size = 10;
            point.Text = "0x" + id.ToString("X6");
            point.TextColor = point.Stroke;

            return point;
        }

        public static ArrowAnnotation createArrowAnnotations(double length, double fThetaInDeg, double x, double y, OxyColor color)
        {
            var arrowAnnotation = new OxyPlot.Annotations.ArrowAnnotation();
            arrowAnnotation.StrokeThickness = 3;
            arrowAnnotation.HeadLength = 3;
            arrowAnnotation.HeadWidth = 1;
            arrowAnnotation.StartPoint = new OxyPlot.DataPoint(x, y);

            OxyPlot.DataPoint unitVector = new OxyPlot.DataPoint(length, 0);
            double angle = fThetaInDeg * Math.PI / 180.0f;
            double direction = Math.Atan2(Math.Sin(angle), Math.Cos(angle));
            arrowAnnotation.EndPoint = new OxyPlot.DataPoint(unitVector.X * Math.Cos(direction) - unitVector.Y * Math.Sin(direction) + x,
            unitVector.X * Math.Sin(direction) + unitVector.Y * Math.Cos(direction) + y);
            arrowAnnotation.Color = color;

            return arrowAnnotation;
        }
   
    }
}
