using System;
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
using System.Windows.Shapes;
using System.IO;
using System.Diagnostics;

namespace SwarmRobotControlAndCommunication
{
    /// <summary>
    /// Interaction logic for ImageGeneratorWindow.xaml
    /// </summary>
    public partial class ImageGeneratorWindow : Window
    {
        const int UI_ZONE_HEIGHT = 30;
        Style mPixelActiveStyle = Application.Current.FindResource("BlackButton") as Style;
        Style mPixelDeActiveStyle = Application.Current.FindResource("WhiteButton") as Style;

        Grid mImageZoneGrid;
        StackPanel mUserInterfaceStackPanel;
        Label mlblStatus;
        int mActivePixelCount;
        
        sbyte[] mImage;
        int mHeight;
        int mWidth;
        int mPixelSize;
        bool mShowPixelText;

        public ImageGeneratorWindow()
        {
            InitializeComponent();

            mHeight = 4;
            mWidth = 6;
            mPixelSize = 30;
            mShowPixelText = true;

            createImageGeneratorUI();
        }

        public ImageGeneratorWindow(int Height, int Width, int PixelSize, bool ShowPixelText)
        {
            InitializeComponent();

            mHeight = Height;
            mWidth = Width;
            mPixelSize = PixelSize;
            mShowPixelText = ShowPixelText;

            createImageGeneratorUI();
        }

        #region UI
        private Label titlePlotWindown = new Label();
        private Button closeButtonPlotWindow = new Button();
        private Button minimizeButtonPlotWindow = new Button();
        private Button maximizeButtonPlotWindow = new Button();
        private FrameworkElement titleButtonMainWindow = new FrameworkElement();
        private WindowState previousWindowState = new WindowState();

        private void ImageGeneratorWindow_Loaded(object sender, RoutedEventArgs e)
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
                titlePlotWindown.Content = "Image Generator v1.0";
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

        private void createImageGeneratorUI()
        {
            // Initialize data
            mImage = new sbyte[mHeight * mWidth];
            for (int i = 0; i < mImage.Length; i++)
                mImage[i] = 0;

            // Create the container grid
            Grid ContainerGrid = new Grid();
            ContainerGrid.Height = mHeight * mPixelSize + UI_ZONE_HEIGHT;
            ContainerGrid.Width = mWidth * mPixelSize;
            ContainerGrid.HorizontalAlignment = HorizontalAlignment.Left;
            ContainerGrid.VerticalAlignment = VerticalAlignment.Top;
            ContainerGrid.ShowGridLines = false;
            ContainerGrid.Background = new SolidColorBrush(Colors.White);

            // Create Container Grid Rows
            RowDefinition gridRowImage = new RowDefinition();
            gridRowImage.Height = new GridLength(mHeight * mPixelSize);
            RowDefinition gridRowUI = new RowDefinition();
            gridRowUI.Height = new GridLength(UI_ZONE_HEIGHT);
            ContainerGrid.RowDefinitions.Add(gridRowImage);
            ContainerGrid.RowDefinitions.Add(gridRowUI);

            // Create Image Gird Zone
            mImageZoneGrid = createDynamicImageZone(mHeight, mWidth, mPixelSize);
            ContainerGrid.Children.Add(mImageZoneGrid);
            
            // Create UI Zone stackpanel
            mUserInterfaceStackPanel = new StackPanel();
            mUserInterfaceStackPanel.Height = 28;
            // userInterfaceStackPanel.Width = 140;
            mUserInterfaceStackPanel.HorizontalAlignment = System.Windows.HorizontalAlignment.Right;
            mUserInterfaceStackPanel.VerticalAlignment = System.Windows.VerticalAlignment.Bottom;
            mUserInterfaceStackPanel.FlowDirection = System.Windows.FlowDirection.LeftToRight;
            mUserInterfaceStackPanel.Orientation = Orientation.Horizontal;
            Grid.SetRow(mUserInterfaceStackPanel, 1);

            #region Status Label
            mActivePixelCount = 0;
            mlblStatus = new Label();
            mlblStatus.HorizontalAlignment = System.Windows.HorizontalAlignment.Right;
            mlblStatus.VerticalAlignment = System.Windows.VerticalAlignment.Center;
            mlblStatus.Margin = new Thickness(0, 0, 0, 0);
            mUserInterfaceStackPanel.Children.Add(mlblStatus);
            #endregion

            #region Generation Button
            Style generateButtonStyle = Application.Current.FindResource("OrangeButton") as Style;
            Button btnButtonGenerate = new Button();
            btnButtonGenerate.Name = "btnGenerate";
            btnButtonGenerate.Content = "Generate";
            btnButtonGenerate.Width = 60;
            btnButtonGenerate.Height = 26;
            btnButtonGenerate.Style = generateButtonStyle;
            btnButtonGenerate.Margin = new Thickness(4.2, 1, 0, 1);
            btnButtonGenerate.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
            btnButtonGenerate.VerticalAlignment = System.Windows.VerticalAlignment.Center;
            btnButtonGenerate.FlowDirection = System.Windows.FlowDirection.RightToLeft;
            btnButtonGenerate.Click += btnButtonUI_Click;
            mUserInterfaceStackPanel.Children.Add(btnButtonGenerate);
            #endregion

            #region Reset Button
            Style resetButtonStyle = Application.Current.FindResource("SlateBlueButton") as Style;
            Button btnButtonReset = new Button();
            btnButtonReset.Name = "btnReset";
            btnButtonReset.Content = "Reset";
            btnButtonReset.Width = 60;
            btnButtonReset.Height = 26;
            btnButtonReset.Style = resetButtonStyle;
            btnButtonReset.Margin = new Thickness(4.2, 1, 0, 1);
            btnButtonReset.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
            btnButtonReset.VerticalAlignment = System.Windows.VerticalAlignment.Center;
            btnButtonReset.FlowDirection = System.Windows.FlowDirection.RightToLeft;
            btnButtonReset.Click += btnButtonUI_Click;
            mUserInterfaceStackPanel.Children.Add(btnButtonReset);
            #endregion

            ContainerGrid.Children.Add(mUserInterfaceStackPanel);

            // Display grid into a Window
            this.Content = ContainerGrid;
        }

        private Grid createDynamicImageZone(int numberOfRow, int numberOfColumn, int pixelSize)
        {
            // Create the image Grid
            Grid DynamicGrid = new Grid();
            DynamicGrid.Height = numberOfRow * pixelSize;
            DynamicGrid.Width = numberOfColumn * pixelSize;
            DynamicGrid.HorizontalAlignment = HorizontalAlignment.Left;
            DynamicGrid.VerticalAlignment = VerticalAlignment.Top;
            DynamicGrid.ShowGridLines = true;
            DynamicGrid.Background = new SolidColorBrush(Colors.White);
            Grid.SetRow(DynamicGrid, 0);
            //Grid.SetColumn(DynamicGrid, 0);

            // Create Columns
            for (int i = 0; i < numberOfColumn; i++)
            {
                ColumnDefinition gridColi = new ColumnDefinition();
                gridColi.Width = new GridLength(pixelSize);
                DynamicGrid.ColumnDefinitions.Add(gridColi);
            }

            // Create Rows
            for (int i = 0; i < numberOfRow; i++)
            {
                RowDefinition gridRowi = new RowDefinition();
                gridRowi.Height = new GridLength(pixelSize);
                DynamicGrid.RowDefinitions.Add(gridRowi);
            }

            // Add items to grid
            for (int row = 0; row < numberOfRow; row++)
            {
                for (int col = 0; col < numberOfColumn; col++)
                {
                    Button btnButton = new Button();
                    btnButton.Name = "r" + row.ToString() + "c" + col.ToString();
                    if(mShowPixelText)
                        btnButton.Content = row.ToString() + "." + col.ToString();
                    btnButton.Width = pixelSize;
                    btnButton.Height = pixelSize;
                    btnButton.Style = mPixelDeActiveStyle;
                    btnButton.Click += pixelButton_Clicked;
                    Grid.SetRow(btnButton, row);
                    Grid.SetColumn(btnButton, col);

                    DynamicGrid.Children.Add(btnButton);
                }
            }

            return DynamicGrid;
        }

        private void pixelButton_Clicked(object sender, RoutedEventArgs e)
        {
            Button enteredPixel = sender as Button;
            Match match = Regex.Match(enteredPixel.Name, @"^r([0-9]+)c([0-9]+)", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                int row = Convert.ToInt32(match.Groups[1].Value);
                int col = Convert.ToInt32(match.Groups[2].Value);
                int index = row * mWidth + col;

                if (enteredPixel.Style.Equals(mPixelDeActiveStyle))
                {
                    enteredPixel.Style = mPixelActiveStyle;
                    mImage[index] = 1;
                    mActivePixelCount++;
                }
                else
                {
                    enteredPixel.Style = mPixelDeActiveStyle;
                    mImage[index] = 0;
                    mActivePixelCount--;
                }
                mlblStatus.Content = "Point(s): " + mActivePixelCount;
            }
        }

        private void btnButtonUI_Click(object sender, RoutedEventArgs e)
        {
            Button clickedButton = (Button)sender;
            switch (clickedButton.Name)
            {
                case "btnGenerate":
                    generateTextFile();
                    break;

                case "btnReset":
                    reset();
                    break;

                default:
                    break;
            }
        }

        private void reset()
        {
            foreach (Button bt in TreeHelper.FindChildren<Button>(mImageZoneGrid))
                bt.Style = mPixelDeActiveStyle;
            
            for (int i = 0; i < mImage.Length; i++)
                mImage[i] = 0;

            mActivePixelCount = 0;
            mlblStatus.Content = "Point(s): 0"; 
        }

        private void generateTextFile()
        {
            String outputContent;
            outputContent = convertImageToStringArray(mImage);
            String outputFile = exportDataToTextFile("Output Image", "Image.txt", outputContent);
            MessageBoxResult result = MessageBox.Show("Generate completed!\nOutput file save to " + outputFile, "Do you want to view the output file?", MessageBoxButton.YesNo, MessageBoxImage.Information);
            if (result == MessageBoxResult.Yes)
                displayTextDataFile(outputFile);
            System.Diagnostics.Process.Start("explorer.exe", string.Format("/select, \"{0}\"", outputFile));
        }
        private String convertImageToStringArray(sbyte[] pImage)
        {
            StringBuilder output = new StringBuilder();

            // Image header
            output.Append(mHeight.ToString() + ' ' + mWidth.ToString() + '\n');

            // Image content
            for (int row = 0; row < mHeight; row++)
            {
                for (int col = 0; col < mWidth; col++)
                {
                    output.Append(pImage[row * mWidth + col].ToString() + ' ');
                }
                output.Replace(' ', '\n', output.Length - 1, 1);
            }
            return output.ToString(0, output.Length);
        }       
        private String exportDataToTextFile(String folderHeaderText, String fileFullName, String content)
        {
            DirectoryInfo fileDir = new DirectoryInfo(".");

            fileDir = fileDir.CreateSubdirectory(folderHeaderText + " " + String.Format("{0:yyyy'-'MM'-'dd}", System.DateTime.Now.Date));

            String fileName = String.Format("{0:hh'-'mm'-'ss tt}", System.DateTime.Now) + " " + fileFullName;

            String fileFullPath = fileDir.FullName + "\\" + fileName;

            File.WriteAllText(@fileFullPath, content);

            return fileFullPath;
        }
        private void displayTextDataFile(String fileFullPath)
        {
            string textEditor1 = @"C:\\Program Files\\Notepad++\\notepad++.exe";
            string textEditor2 = @"D:\\Program Files\\Notepad++\\notepad++.exe";
            string textEditor3 = @"E:\\Program Files\\Notepad++\\notepad++.exe";
            string textEditor4 = @"C:\\ProgramFiles(x86)\\Notepad++\\notepad++.exe";
            string textEditor5 = @"D:\\ProgramFiles(x86)\\Notepad++\\notepad++.exe";
            string textEditor6 = @"E:\\ProgramFiles(x86)\\Notepad++\\notepad++.exe";

            string[] textEditors = { textEditor1, textEditor2, textEditor3, textEditor4, textEditor5, textEditor6 };

            foreach (var item in textEditors)
            {
                if (File.Exists(item))
                {
                    Process.Start(item, fileFullPath);
                    return;
                }
            }
            Process.Start(@"notepad.exe", fileFullPath);
        }
    }
}
