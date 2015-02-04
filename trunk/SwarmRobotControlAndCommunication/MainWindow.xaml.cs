/* Try to keep this short, concise and clear with important updates only
 * //-------30/06/2014--------
 * - Update some outdated comments.
 * - Control board interface now only has commands to communicate with the control board.
 * - MainWindow now has commands that can control robots.
 * - Bootloader interface is reduced to only need two functions
 * //---------------30/06/2014
 * 
 */
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
using System.Windows.Media.Animation;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using Microsoft.Win32;
using System.Threading;
using System.Diagnostics;
using System.ComponentModel;

using SwarmRobotControlAndCommunication.CustomInterfaces;

namespace SwarmRobotControlAndCommunication
{
    /// <summary>COMMAND_IDLE
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, IDisposable
    {

        #region Constants
        //--------------------Control board-----------------------------
        private const string DEFAULT_TX_ADDRESS = "00BEADFF";
        private const string DEFAULT_RX_ADDRESS = "00C1AC02";
        //-------------------------------------------------Control board

        //------------Commands from Robots---------------------
        private const byte ROBOT_RESPONSE_OK = 0x0A;
        //---------------------------------Commands from Robots

        //------------Commands to control all Robots---------------------
        private const byte COMMAND_RESET = 0x01;
        private const byte COMMAND_SLEEP = 0x02;
        private const byte COMMAND_DEEP_SLEEP = 0x03;
        private const byte COMMAND_WAKE_UP = 0x04;

        private const byte COMMAND_TEST_RF_TRANSMISTER = 0x05;
        private const byte COMMAND_TEST_RF_RECEIVER = 0x06;
        private const byte COMMAND_TOGGLE_LEDS = 0x07;
        private const byte COMMAND_SAMPLE_MICS_SIGNALS = 0x08;
        private const byte COMMAND_READ_ADC1 = 0x09;
        private const byte COMMAND_READ_ADC2 = 0x0A;
        private const byte COMMAND_REQUEST_BATTERY_VOLT = 0x0B;
        private const byte COMMAND_TEST_SPEAKER = 0x0C;
        private const byte COMMAND_CHANGE_MOTOR_SPEED = 0x0D;
        private const byte COMMAND_STOP_MOTOR1 = 0x0E;
        private const byte COMMAND_STOP_MOTOR2 = 0x0F;

        private const byte COMMAND_EEPROM_DATA_READ = 0x10;
        private const byte COMMAND_EEPROM_DATA_WRITE = 0x11;
        private const byte COMMAND_EEPROM_TABLE_READ = 0x12;
        private const byte COMMAND_EEPROM_TABLE_UPDATE = 0x13;
        //==== out

        private const byte COMMAND_SET_RUNNING_STATUS = 0xC3;
        private const byte COMMAND_DISTANCE_SENSING = 0xA2;
        private const byte COMMAND_READ_NEIGHBORS_TABLE = 0xA6;
        private const byte COMMAND_READ_ONEHOP_TABLE = 0xA7;
        private const byte COMMAND_READ_LOCS_TABLE = 0xA8;

        private const byte COMMAND_READ_EEPROM = 0xE0;
        private const byte COMMAND_WRITE_EEPROM = 0xE1;
        private const byte COMMAND_SET_ADDRESS_EEROM = 0xE2;

        private const byte COMMAND_MEASURE_DISTANCE = 0xB0;
        private const byte COMMAND_READ_VECTOR = 0xB1;
        private const byte COMMAND_SET_LOCAL_LOOP_STOP = 0xB2;
        private const byte COMMAND_SET_STEPSIZE = 0xB3;
        private const byte COMMAND_SET_STOP1 = 0xB4;
        private const byte COMMAND_SET_STOP2 = 0xB5;
        private const byte COMMAND_ROTATE_CLOCKWISE = 0xB6;
        private const byte COMMAND_ROTATE_CLOCKWISE_ANGLE = 0xB7;
        private const byte COMMAND_FORWARD_PERIOD = 0xB8;
        private const byte COMMAND_FORWARD_DISTANCE = 0xB9;
        private const byte COMMAND_SET_ROBOT_STATE = 0xBA;
        private const byte COMMAND_ROTATE_CORRECTION_ANGLE = 0xBB;
        private const byte COMMAND_READ_CORRECTION_ANGLE = 0xBC;

        private const byte COMMAND_ROTATE_CORRECTION_ANGLE_DIFF = 0xBD;
        private const byte COMMAND_ROTATE_CORRECTION_ANGLE_SAME = 0xBE;

        private const byte MOTOR_FORWARD_DIRECTION = 0x00;
        private const byte MOTOR_REVERSE_DIRECTION = 0x01;
        //---------------------------------Commands to control all Robots
        #endregion

        #region Constructors

        /// <summary>
        /// The storyboard used to store the ellipse running effect when starting a programming sequence
        /// </summary>
        private Storyboard ellipseProgressEffect = new Storyboard();

        public static ControlBoardInterface theControlBoard = new TM4C123ControlBoard(0x04D8, 0x003F);

        public BootLoaderInterface bootLoader = new TivaBootLoader(theControlBoard, 256);

        public MainWindow()
        {
            InitializeComponent();

            ellipseProgressEffect = (Storyboard)this.Resources["ellipseProgressEffectKey"];

            theControlBoard.usbDeviceChangeEvent += new usbDeviceChangeEventsHandler(usbEvent_receiver);

            bootLoader.currentProgrammingPercentEvent += updateProgressProgramBar;

            theControlBoard.findTargetDevice();
            setStatusBarAndButtonsAppearanceFromDeviceState();

            this.TXAdrrTextBox.Text = DEFAULT_TX_ADDRESS;
            this.Pipe0AddressTextBox.Text = DEFAULT_RX_ADDRESS;
            this.TXAdrrTextBoxDebug.Text = DEFAULT_TX_ADDRESS;
        }

        private void usbEvent_receiver(object o, EventArgs e)
        {
            setStatusBarAndButtonsAppearanceFromDeviceState();
        }
        private void setStatusBarAndButtonsAppearanceFromDeviceState()
        {
            if (theControlBoard.isDeviceAttached == true)
            {
                Brush color = (SolidColorBrush)(new BrushConverter().ConvertFrom("#FF0074FF"));
                setStatusBarContentAndColor("Ready", color);

                enableAllButtons(true);
            }
            else
            {
                setStatusBarContentAndColor("The control board is not attached", Brushes.OrangeRed);

                enableAllButtons(false);
            }
        }
        private void setStatusBarContentAndColor(string content, Brush color)
        {
            this.statusDeviceAttached.Content = content;
            this.statusDeviceAttached.Background = color;
        }
        private void setStatusBarContent(string content)
        {
            this.statusDeviceAttached.Content = content;
        }
        private void enableAllButtons(bool isEnable)
        {
            foreach (Button bt in TreeHelper.FindChildren<Button>(window))
            {
                bt.IsEnabled = isEnable;
            }
        }

        private void updateProgressProgramBar(UInt32 value)
        {
            this.Dispatcher.Invoke((Action)delegate
            {
                this.progressProgramBar.Value = value;
            }
            );
        }

        #endregion

        #region Custom window stytle events mapping

        private Button closeButtonMainWindow = new Button();
        private Button minimizeButtonMainWindow = new Button();
        private Button maximizeButtonMainWindow = new Button();

        private WindowState previousWindowState = new WindowState();

        private FrameworkElement titleButtonMainWindow = new FrameworkElement();

        private void this_Loaded(object sender, RoutedEventArgs e)
        {
            this.closeButtonMainWindow = (Button)this.Template.FindName("CloseButton", this);
            if (this.closeButtonMainWindow != null)
            {
                this.closeButtonMainWindow.Click += closeApllication;
            }

            this.maximizeButtonMainWindow = (Button)this.Template.FindName("MaximizeButton", this);
            this.maximizeButtonMainWindow.IsEnabled = false;
            if (this.maximizeButtonMainWindow != null)
            {
                this.maximizeButtonMainWindow.Click += maximizeApplicationWindow;
            }

            this.minimizeButtonMainWindow = (Button)this.Template.FindName("MinimizeButton", this);
            if (this.minimizeButtonMainWindow != null)
            {
                this.minimizeButtonMainWindow.Click += minimizeApplicationWindow;
            }

            this.titleButtonMainWindow = (FrameworkElement)this.Template.FindName("Title", this);
            if (this.titleButtonMainWindow != null)
            {
                this.titleButtonMainWindow.MouseLeftButtonDown += title_MouseLeftButtonDown;
            }

        }

        private void closeApllication(object sender, RoutedEventArgs e)
        {
            //Remove the listener for usb events to avoid conflicting in WPF
            theControlBoard.usbDeviceChangeEvent -= new usbDeviceChangeEventsHandler(usbEvent_receiver);
            this.Close();
        }
        private void maximizeApplicationWindow(object sender, RoutedEventArgs e)
        {
            previousWindowState = this.WindowState;
            this.WindowState = WindowState.Maximized;
            this.maximizeButtonMainWindow.Click -= maximizeApplicationWindow;
            this.maximizeButtonMainWindow.Click += restoreApplicationWindow;
            this.maximizeButtonMainWindow.Content = Application.Current.Resources["RestoreButtonPath"];
        }
        private void restoreApplicationWindow(object sender, RoutedEventArgs e)
        {
            this.WindowState = previousWindowState;
            this.maximizeButtonMainWindow.Click -= restoreApplicationWindow;
            this.maximizeButtonMainWindow.Click += maximizeApplicationWindow;
            this.maximizeButtonMainWindow.Content = Application.Current.Resources["MaximizeButtonPath"];
        }
        private void minimizeApplicationWindow(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }
        private void title_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        public void Dispose()
        {
            theControlBoard.Dispose();
        }
        #endregion

        #region Menu Items
        private void aboutMenuItem_Click(object sender, RoutedEventArgs e)
        {
            AboutWindow aboutWindow = new AboutWindow();
            aboutWindow.ShowDialog();
        }

        private void viewModeCheckChange(object sender, RoutedEventArgs e)
        {
            try
            {
                setStatusBarAndButtonsAppearanceFromDeviceState();
                this.mainTab.InvalidateVisual();
            }
            catch (Exception ex)
            {
                defaultExceptionHandle(ex);
            }
        }

        private void defaultExceptionHandle(Exception ex)
        {
            MessageBox.Show(ex.Message, "Error!", MessageBoxButton.OK, MessageBoxImage.Error);
            forceWindowRefresh();
        }
        private void forceWindowRefresh()
        {
            //Since WPF optimizes its render process 
            //so this is the best way in this software to force a refresh
            this.Dispatcher.Invoke((Action)delegate
            {
                if (this.mainTab.SelectedIndex != this.mainTab.Items.Count - 1)
                {
                    this.mainTab.SelectedIndex = this.mainTab.SelectedIndex + 1;
                    this.mainTab.SelectedIndex = this.mainTab.SelectedIndex - 1;
                }
                else
                {
                    this.mainTab.SelectedIndex = this.mainTab.SelectedIndex - 1;
                    this.mainTab.SelectedIndex = this.mainTab.SelectedIndex + 1;
                }
            });
        }
        #endregion

        #region Control & Program Tab

        private void sleepButton_Click(object sender, RoutedEventArgs e)
        {
            theControlBoard.sendCommandToRobot(COMMAND_SLEEP);
        }

        private void deepsleepButton_Click(object sender, RoutedEventArgs e)
        {
            theControlBoard.sendCommandToRobot(COMMAND_DEEP_SLEEP);
        }

        private void wakeUpButton_Click(object sender, RoutedEventArgs e)
        {
            theControlBoard.sendCommandToRobot(COMMAND_WAKE_UP);
        }

        private void resetButton_Click(object sender, RoutedEventArgs e)
        {
            theControlBoard.sendCommandToRobot(COMMAND_RESET);
        }

        private void loadHexButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Title = "Open Hex File";
            dlg.Filter = "Hex files (*.HEX)|*.HEX" + "|All files (*.*)|*.*";

            // Process open file dialog box results 
            if (dlg.ShowDialog() == true)
            {
                string pathToFile = dlg.FileName;
                this.pathOfHexFile.Text = pathToFile;  // <?>
            }
        }

        #region programm buttons clicked
        private CancellationTokenSource cancelProgramProcess;

        private void startProgramButton_Click(object sender, RoutedEventArgs e)
        {
            Button buttonClicked = (Button)sender;

            startProgramProcedureAsync(buttonClicked, @"Start Programming");
        }
        private void wakeUpAndProgramButton_Click(object sender, RoutedEventArgs e)
        {
            theControlBoard.sendCommandToRobot(COMMAND_WAKE_UP);

            Thread.Sleep(10);

            theControlBoard.sendCommandToRobot(COMMAND_RESET);

            Thread.Sleep(500);

            // Prepare robots so they can go into bootloader mode
            // when receive the command goIntoBootloaderMode.
            Button buttonClicked = (Button)sender;

            startProgramProcedureAsync(buttonClicked, @"Wake Up & Program");
        }

        /// <summary>
        /// Step 1: Check the selected hex file 
        /// Step 2: Set UI and effect
        /// Step 3: Start programming
        /// Sub steps: Handle all exceptions, throw cancel token
        /// </summary>
        private async Task startProgramProcedureAsync(Button buttonClicked, String originalContent)
        {
            MessageBoxResult result = MessageBoxResult.Yes;
            try
            {
                if ((string)buttonClicked.Content == "Stop")
                {
                    result = MessageBox.Show("Stop the current programming process?",
                                                          "Warning!", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                    if (result == MessageBoxResult.Yes)
                        cancelProgramProcess.Cancel();
                }
                else
                {
                    UInt32 numberOfLines = await getNumberOfLinesAndCheckHexFileAsync();
                    //TODO: Find a way to throw/catch an exception from await/async to avoid double checking like this
                    if (numberOfLines == 0) return;
                    Debug.WriteLine("Number of Hex data lines = {0}", numberOfLines);
                    buttonClicked.Content = "Stop";
                    ellipseProgressEffect.Begin();
                    toggleAllButtonStatusExceptSelected(buttonClicked);
                    setStatusBarContentAndColor("Busy", Brushes.Indigo);

                    theControlBoard.configureBootloadProtocol();

                    cancelProgramProcess = new CancellationTokenSource();
                    await programRobotsAsync(cancelProgramProcess);
                }
            }
            catch (Exception ex)
            {
                defaultExceptionHandle(new Exception("Program Robots: " + ex.Message));
            }
            finally
            {
                if (result == MessageBoxResult.Yes)
                {
                    this.Dispatcher.Invoke((Action)delegate
                    {
                        buttonClicked.Content = originalContent;
                        ellipseProgressEffect.Stop();
                        this.progressProgramBar.Value = 0;
                        setStatusBarAndButtonsAppearanceFromDeviceState();

                        Thread.Sleep(100);

                        theControlBoard.configureNormalProtocol();
                    }
                    );
                }
            }
        }
        private void toggleAllButtonStatusExceptSelected(Button buttonClicked)
        {
            foreach (Button bt in TreeHelper.FindChildren<Button>(window))
            {
                if (bt != buttonClicked) bt.IsEnabled = !bt.IsEnabled;
            }
        }
        private async Task<UInt32> getNumberOfLinesAndCheckHexFileAsync()
        {
            return await Task.Run(() =>
            {
                UInt32 numberOfLines = 0;
                try
                {
                    string hexFilePath = getFilePath();
                    if (hexFilePath == "" || hexFilePath == null)
                        throw new Exception("Please choose a hex file!");

                    numberOfLines = bootLoader.getNumberOfLineAndCheckHexFile(hexFilePath);
                    if (numberOfLines == 0)
                        throw new Exception("Unrecognized File Format!");
                }
                catch (Exception ex)
                {
                    defaultExceptionHandle(new Exception("Check File: " + ex.Message));
                }
                return numberOfLines;
            });
        }
        private async Task<bool> programRobotsAsync(CancellationTokenSource cts)
        {
            return await Task.Run(() =>
            {
                try
                {
                    string hexFilePath = getFilePath();
                    bootLoader.startProgramming(hexFilePath, cts);
                }
                catch (OperationCanceledException)
                {
                    MessageBox.Show("The operation is cancelled by user!", "Warning!", MessageBoxButton.OK, MessageBoxImage.Warning);
                    forceWindowRefresh();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Programming Process: " + ex.Message, "Error!", MessageBoxButton.OK, MessageBoxImage.Error);
                    forceWindowRefresh();
                }
                return true;
            }, cts.Token);
        }
        private String getFilePath()
        {
            string hexFilePath = null;
            this.Dispatcher.Invoke((Action)delegate
            {
                hexFilePath = this.pathOfHexFile.Text;
            }
            );
            return hexFilePath;
        }
        #endregion

        #endregion

        #region RF Configure Tab
        private void configureRF(string TX_ADDRstring)
        {
            try
            {
                UInt32 address;
                const byte RF_ADDRESS_WIDTH = 4;

                // Get channel - 1 byte
                byte channel = Convert.ToByte(this.rfChannelTextBox.Text);
                if ((channel > 3) || (channel < 0))
                    throw new Exception("The chosen channel is out of range");

                // Get output power inder - 1 byte
                byte powerIndex = (byte)(this.TXPowerComboBox.SelectedIndex);

                // Get Tx address - 4 bytes
                byte[] rfTXAddress = new byte[RF_ADDRESS_WIDTH];
                // string TX_ADDRstring = this.TXAdrrTextBox.Text;
                if (TX_ADDRstring.Length != (2 * RF_ADDRESS_WIDTH))
                {
                    string msg = String.Format("TX address must have {0} characters!", 2 * RF_ADDRESS_WIDTH);
                    throw new Exception(msg);
                }
                address = getAddress(TX_ADDRstring, 2 * RF_ADDRESS_WIDTH);
                rfTXAddress[0] = (byte)address;
                rfTXAddress[1] = (byte)(address >> 8);
                rfTXAddress[2] = (byte)(address >> 16);
                rfTXAddress[3] = (byte)(address >> 24);

                // Get Rx address - 4 bytes
                byte[] rfRXAddress = new byte[RF_ADDRESS_WIDTH];
                string RX_ADDRstring = this.Pipe0AddressTextBox.Text;
                if (RX_ADDRstring.Length != (2 * RF_ADDRESS_WIDTH))
                {
                    string msg = String.Format("RX address must have {0} characters!", 2 * RF_ADDRESS_WIDTH);
                    throw new Exception(msg);
                }
                address = getAddress(RX_ADDRstring, 2 * RF_ADDRESS_WIDTH);
                rfRXAddress[0] = (byte)address;
                rfRXAddress[1] = (byte)(address >> 8);
                rfRXAddress[2] = (byte)(address >> 16);
                rfRXAddress[3] = (byte)(address >> 24);

                // Construct setup data
                byte[] setupData = new byte[3 + 2 * RF_ADDRESS_WIDTH];
                setupData[0] = channel;
                setupData[1] = powerIndex;
                setupData[2] = RF_ADDRESS_WIDTH;
                for (int i = 0; i < RF_ADDRESS_WIDTH; i++)
                {
                    setupData[3 + i] = rfTXAddress[RF_ADDRESS_WIDTH - 1 - i];
                    setupData[3 + i + RF_ADDRESS_WIDTH] = rfRXAddress[RF_ADDRESS_WIDTH - 1 - i];
                }

                // Send
                theControlBoard.configureRF(setupData);
            }
            catch (Exception ex)
            {
                defaultExceptionHandle(new Exception("Configure RF: " + ex.Message + ex.StackTrace));
            }
        }

        private UInt32 getAddress(string addrString, uint addrWidth)
        {
            UInt32 address = 0;
            for (int i = 0; i < addrWidth; i++)
            {
                address <<= 4;
                address += TivaBootLoader.convertCharToHex(addrString[i]);
            }
            return address;
        }

        private void configureRF_Click(object sender, RoutedEventArgs e)
        {
            configureRF(this.TXAdrrTextBox.Text);
        }
        private void TXAdrrTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            int codeASCII = Convert.ToInt32(e.Key);
            if (codeASCII < 34 || codeASCII > 49)
            {
                e.Handled = true;
            }
        }
        private void rfDefaultButton_Click(object sender, RoutedEventArgs e)
        {
            this.TXAdrrTextBox.Text = DEFAULT_TX_ADDRESS;
            this.Pipe0AddressTextBox.Text = DEFAULT_RX_ADDRESS;
            this.rfChannelTextBox.Text = "0";
            this.TXPowerComboBox.SelectedIndex = 1;
        }
        #endregion

        #region EEPROM Tab
        private BackgroundWorker backgroundWorker = null;

        private enum e_VerifyTableReturn
        {
            SINE_OK_ARCSINE_OK = 1,
            SINE_OK_ARCSINE_ERROR = 2,
            SINE_ERROR_ARCSINE_OK = 3,
            SINE_ERROR_ARCSINE_ERROR = 4
        }
        
        private void setAddressEeprom()
        {
            //Byte[] transmittedData = new Byte[5]; // <set address command>< address>
            //Int32 readAddress;

            //transmittedData[0] = COMMAND_SET_ADDRESS_EEROM;

            //readAddress = Convert.ToInt32(this.EepromAddressTextBox.Text);
            //transmittedData[1] = (Byte)(readAddress >> 24);
            //transmittedData[2] = (Byte)(readAddress >> 16);
            //transmittedData[3] = (Byte)(readAddress >> 8);
            //transmittedData[4] = (Byte)(readAddress & 0xFF);

            //theControlBoard.transmitBytesToRobot(transmittedData, 5, 1);
        }

        private void EepromDataReadButton_Click(object sender, RoutedEventArgs e)
        {
            byte unit = 3;
            byte[] data = new byte[unit * 2 + 1];
            UInt16 ui16WordIndex;

            uint bufferLength = (uint)unit * 6 + 1 + 2;
            byte rxUnit;
            UInt32 ui32RxData = 0;
            byte[] dataBuffer = new byte[bufferLength];

            data[0] = unit;

            /* 1 */
            ui16WordIndex = Convert.ToUInt16(this.EepromRobotIdWordIndexTextBox.Text);
            data[1] = (byte)((ui16WordIndex >> 8) & 0x0FF);
            data[2] = (byte)(ui16WordIndex & 0x0FF);

            /* 2 */
            ui16WordIndex = Convert.ToUInt16(this.EepromInterceptWordIndexTextBox.Text);
            data[3] = (byte)((ui16WordIndex >> 8) & 0x0FF);
            data[4] = (byte)(ui16WordIndex & 0x0FF);

            /* 3 */
            ui16WordIndex = Convert.ToUInt16(this.EepromSlopeWordIndexTextBox.Text);
            data[5] = (byte)((ui16WordIndex >> 8) & 0x0FF);
            data[6] = (byte)(ui16WordIndex & 0x0FF);

            try
            {
                theControlBoard.receiveBytesFromRobot(COMMAND_EEPROM_DATA_READ, data, bufferLength, ref dataBuffer, 1000);

                SwarmMessage rxMessage = SwarmMessage.ConstructFromByteArray(dataBuffer);
                byte[] messageContent;
                if (rxMessage.getHeader().getMessageType() == e_MessageType.MESSAGE_TYPE_ROBOT_RESPONSE
                    && rxMessage.getHeader().getCmd() == ROBOT_RESPONSE_OK)
                {
                    messageContent = rxMessage.getData();
                    rxUnit = messageContent[0];
                    if (rxUnit == unit)
                    {
                        constructWordIndexAndDataContent(ref ui16WordIndex, ref ui32RxData, messageContent, 1);
                        this.EepromRobotIdWordIndexTextBox.Text = ui16WordIndex.ToString();
                        this.EepromRobotIdTextBox.Text = ui32RxData.ToString("X8");

                        constructWordIndexAndDataContent(ref ui16WordIndex, ref ui32RxData, messageContent, 7);
                        this.EepromInterceptWordIndexTextBox.Text = ui16WordIndex.ToString();
                        float fIntercept = (float)(ui32RxData / 32768.0);
                        this.EepromInterceptTextBox.Text = fIntercept.ToString("0.0000");

                        constructWordIndexAndDataContent(ref ui16WordIndex, ref ui32RxData, messageContent, 13);
                        this.EepromSlopeWordIndexTextBox.Text = ui16WordIndex.ToString();
                        float fSlope = (float)(ui32RxData / 32768.0);
                        this.EepromSlopeTextBox.Text = fSlope.ToString("0.0000");

                        setStatusBarContent("EEPROM Data Read: OK!");
                    }
                }
                else
                {
                    setStatusBarContent("EEPROM data read: Wrong response...");
                }
            }
            catch (Exception ex)
            {
                defaultExceptionHandle(ex);
            }
        }
        void constructWordIndexAndDataContent(ref UInt16 ui16WordIndex, ref UInt32 ui32RxData, byte[] data, uint offset)
        {
            ui16WordIndex = (UInt16)((data[offset] << 8) | data[offset + 1]);
            ui32RxData = (UInt32)((data[offset + 2] << 24) | (data[offset + 3] << 16)
                                | (data[offset + 4] << 8) | data[offset + 5]);
        }

        private void EepromDataSynchronousButton_Click(object sender, RoutedEventArgs e)
        {
            // <DataNum><2b word index><4b data><2b word index><4b data>...<2b word index><4b data>
            try
            {
                UInt16 ui16WordIndex;
                UInt32 ui32Data;
                float fData;

                byte unit = 3;
                byte[] data = new byte[unit * 6 + 1];
                data[0] = unit;

                /* 1 */
                ui16WordIndex = Convert.ToUInt16(this.EepromRobotIdWordIndexTextBox.Text);
                ui32Data = getAddress(this.EepromRobotIdTextBox.Text, 8);
                fillPairIndexAndWordToByteArray(ui16WordIndex, ui32Data, data, 1);

                /* 2 */
                ui16WordIndex = Convert.ToUInt16(this.EepromInterceptWordIndexTextBox.Text);
                float.TryParse(this.EepromInterceptTextBox.Text, out fData);
                ui32Data = (UInt32)(fData * 32768);
                fillPairIndexAndWordToByteArray(ui16WordIndex, ui32Data, data, 7);

                /* 3 */
                ui16WordIndex = Convert.ToUInt16(this.EepromSlopeWordIndexTextBox.Text);
                float.TryParse(this.EepromSlopeTextBox.Text, out fData);
                ui32Data = (UInt32)(fData * 32768);
                fillPairIndexAndWordToByteArray(ui16WordIndex, ui32Data, data, 13);

                SwarmMessageHeader header = new SwarmMessageHeader(e_MessageType.MESSAGE_TYPE_HOST_COMMAND, COMMAND_EEPROM_DATA_WRITE);
                SwarmMessage message = new SwarmMessage(header, data);
                theControlBoard.transmitBytesToRobot(message.toByteArray(), message.getSize(), 1);

                setStatusBarContent("EEPROM Data is synchronized!");
            }
            catch (Exception ex)
            {
                throw new Exception("EEPROM Synchronous Data " + ex.Message);
            }
        }
        void fillPairIndexAndWordToByteArray(UInt16 uiIndex, UInt32 ui32Data, byte[] data, UInt32 offset)
        {
            data[offset] = (byte)((uiIndex >> 8) & 0xFF);
            data[offset + 1] = (byte)(uiIndex & 0xFF);
            data[offset + 2] = (byte)((ui32Data >> 24) & 0xFF);
            data[offset + 3] = (byte)((ui32Data >> 16) & 0xFF);
            data[offset + 4] = (byte)((ui32Data >> 8) & 0xFF);
            data[offset + 5] = (byte)(ui32Data & 0xFF);
        }

        private void EepromProgramTableButton_Click(object sender, RoutedEventArgs e)
        {
            assignTaskForBackgroundWorker((Button)sender, "Cancel Update");
        }

        private void EepromTableVerifyButton_Click(object sender, RoutedEventArgs e)
        {
            assignTaskForBackgroundWorker((Button)sender, "Cancel Verify");
        }

        #region EEPROM backgroundWorker
        private void assignTaskForBackgroundWorker(Button buttonClicked, string busyContent)
        {
            Object originalContent = buttonClicked.Content;

            bool bIsCancel = false;
            try
            {
                if (backgroundWorker == null)
                {
                    backgroundWorker = new BackgroundWorker();

                    backgroundWorker.WorkerReportsProgress = true;
                    backgroundWorker.WorkerSupportsCancellation = true;

                    backgroundWorker.DoWork += new DoWorkEventHandler(backgroundWorkerTableManipulation_DoWork);
                    backgroundWorker.ProgressChanged += new ProgressChangedEventHandler(backgroundWorkerTableManipulation_ProgressChanged);

                    switch ((string)buttonClicked.Content)
                    {
                        case "Verify Table":
                            backgroundWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(backgroundWorkerTableVerifying_RunWorkerCompleted);
                            break;

                        case "Update Table":
                            backgroundWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(backgroundWorkerTableProgramming_RunWorkerCompleted);
                            break;

                        default:
                            throw new Exception("Unrecognize button task");
                    }
                }

                if ((string)(buttonClicked.Content) == busyContent)
                {
                    backgroundWorker.CancelAsync();
                    bIsCancel = true;
                }
                else
                {
                    toggleAllButtonStatusExceptSelected(buttonClicked);
                    setStatusBarContentAndColor("Busy", Brushes.Indigo);

                    tableVerifyProgramBar.Value = 0;
                    backgroundWorker.RunWorkerAsync((string)buttonClicked.Content);

                    buttonClicked.Content = busyContent;

                    setStatusBarContent("EEPROM Table Manipulation: Simulate UI Only...");
                }

            }
            catch (Exception ex)
            {
                defaultExceptionHandle(new Exception("Verify EEPROM " + ex.Message));
            }
            finally
            {
                if (bIsCancel)
                {
                    tableVerifyProgramBar.Value = 0;
                    setStatusBarAndButtonsAppearanceFromDeviceState();
                }
            }
        }
        
        private void backgroundWorkerTableManipulation_DoWork(object sender, DoWorkEventArgs e)
        {
            string buttonClickedContent = (string)e.Argument;
            switch (buttonClickedContent)
            {
                case "Verify Table":
                    tableVerifying_DoWork(sender, e);
                    break;

                case "Update Table":
                    tableProgramming_DoWork(sender, e);
                    break;

                default:
                    break;
            }
        }
        private void tableVerifying_DoWork(object sender, DoWorkEventArgs e)
        {
            //TODO: put your task here

            //Test only-------------------------------------------------------
            int count = 10; 
            int sum = 0;
            for (int i = 1; i <= count; i++)
            {
                if (backgroundWorker.CancellationPending)
                {
                    e.Cancel = true;
                    break;
                }

                sum += i;

                backgroundWorker.ReportProgress(i * 100 / count);

                System.Threading.Thread.Sleep(300);
            }

            e.Result = e_VerifyTableReturn.SINE_ERROR_ARCSINE_ERROR;
            //-------------------------------------------------------Test only
        }
        private void tableProgramming_DoWork(object sender, DoWorkEventArgs e)
        {
            //TODO: put your task here

            //Test only-------------------------------------------------------
            int count = 28;
            int sum = 0;
            for (int i = 1; i <= count; i++)
            {
                if (backgroundWorker.CancellationPending)
                {
                    e.Cancel = true;
                    break;
                }

                sum += i;

                backgroundWorker.ReportProgress(i * 100 / count);

                System.Threading.Thread.Sleep(300);
            }

            e.Result = e_VerifyTableReturn.SINE_ERROR_ARCSINE_ERROR;
            //-------------------------------------------------------Test only
        }

        private void backgroundWorkerTableManipulation_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            tableVerifyProgramBar.Value = e.ProgressPercentage;

            Double percentSineTable;
            Double percentArcSineTable;

            if (e.ProgressPercentage == 100)
            {
                percentSineTable = 100;
                percentArcSineTable = 100;
            }
            else if (e.ProgressPercentage > 50 && (e.ProgressPercentage < 100))
            {          
                percentSineTable = 100;
                percentArcSineTable = (e.ProgressPercentage - 50) * 2;
            }
            else
            {
                percentSineTable = e.ProgressPercentage * 2;
                percentArcSineTable = 0;
            }

            SineTableStatusTextBox.Text = percentSineTable.ToString() + "%";
            ArcSineTableStatusTextBox.Text = percentArcSineTable.ToString() + "%";
        }
       
        private void backgroundWorkerTableVerifying_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            tableManipulation_RunWorkerCompleted(sender, e);

            backgroundWorker.RunWorkerCompleted -= backgroundWorkerTableVerifying_RunWorkerCompleted;
            backgroundWorker = null;

            this.EepromTableVerifyButton.Content = "Verify Table";
        }
        private void backgroundWorkerTableProgramming_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            tableManipulation_RunWorkerCompleted(sender, e);

            backgroundWorker.RunWorkerCompleted -= backgroundWorkerTableProgramming_RunWorkerCompleted;
            backgroundWorker = null;

            this.EepromTableUpdatingButton.Content = "Update Table";
        }
        private void tableManipulation_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        { 
                    if (e.Cancelled)
            {
                ArcSineTableStatusTextBox.Text = "0%";
                SineTableStatusTextBox.Text = "0%";
            }
            else
            {
                switch ((e_VerifyTableReturn)(e.Result))
                {
                    case e_VerifyTableReturn.SINE_OK_ARCSINE_OK:
                        SineTableStatusTextBox.Text = "Good!";
                        ArcSineTableStatusTextBox.Text = "Good!";
                        break;

                    case e_VerifyTableReturn.SINE_OK_ARCSINE_ERROR:
                        SineTableStatusTextBox.Text = "Good!";
                        ArcSineTableStatusTextBox.Text = "Bad...";
                        break;

                    case e_VerifyTableReturn.SINE_ERROR_ARCSINE_OK:
                        SineTableStatusTextBox.Text = "Bad...";
                        ArcSineTableStatusTextBox.Text = "Good!";
                        break;

                    case e_VerifyTableReturn.SINE_ERROR_ARCSINE_ERROR:
                        SineTableStatusTextBox.Text = "Bad...";
                        ArcSineTableStatusTextBox.Text = "Bad...";
                        break;
                }
            }

            tableVerifyProgramBar.Value = 0;
            setStatusBarAndButtonsAppearanceFromDeviceState();

            backgroundWorker.DoWork -= backgroundWorkerTableManipulation_DoWork;
            backgroundWorker.ProgressChanged -= backgroundWorkerTableManipulation_ProgressChanged;
        }
        #endregion

        #endregion

        #region Calibration Tab
        private void sendCommandButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Byte[] transmittedData = new Byte[32];

                string command = this.commandSelectBox.Text;

                setStatusBarContent("Broadcast Command: " + command);

                switch (command)
                {
                    case "Test Robot's RF Transmistter":
                        testRobotTransmitter(COMMAND_TEST_RF_TRANSMISTER);
                        break;

                    case "Test Robot's RF Receiver":
                        testRobotReceiver(COMMAND_TEST_RF_RECEIVER);
                        break;

                    case "Toggle All Status Leds":
                        theControlBoard.sendCommandToRobot(COMMAND_TOGGLE_LEDS);
                        break;

                    case "Start Sampling Mics Signals":
                        theControlBoard.sendCommandToRobot(COMMAND_SAMPLE_MICS_SIGNALS);
                        break;

                    case "Test Speaker":
                        theControlBoard.sendCommandToRobot(COMMAND_TEST_SPEAKER);
                        break;

                    case "Change Motors Speed":
                        testMotorsConfiguration(COMMAND_CHANGE_MOTOR_SPEED);
                        break;

                    case "Read Battery Voltage":
                        readBatteryVoltage();
                        break;

                    default:
                        throw new Exception("Broadcast Command: Can not recognise command!");
                }
            }
            catch (Exception ex)
            {
                defaultExceptionHandle(ex);
            }
        }

        private void testRobotTransmitter(byte Command)
        {
            uint length = 600;
            byte[] commandContent = new byte[4];
            commandContent[0] = (byte)((length >> 24) & 0xFF);
            commandContent[1] = (byte)((length >> 16) & 0xFF);
            commandContent[2] = (byte)((length >> 8) & 0xFF);
            commandContent[3] = (byte)(length & 0xFF);

            byte[] receivedData = new byte[length];
            UInt16 data = new UInt16();
            UInt16 value = 0;
            try
            {
                theControlBoard.receiveBytesFromRobot(Command, commandContent, length, ref receivedData, 1000);
                uint i = 0;
                while (true)
                {
                    data = receivedData[i + 1];
                    data = (UInt16)((data << 8) | (receivedData[i]));
                    i += 2;

                    if (data != value)
                    {
                        String message = String.Format("Received wrong data! \n Received: {0}. Expected: {1}", data, value);
                        throw new Exception(message);
                    }
                    value++;

                    if (i >= length)
                    {
                        setStatusBarContent("Robot's RF Transmister OK!");
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                defaultExceptionHandle(ex);
            }
        }

        private void testRobotReceiver(byte commandNumber)
        {
            try
            {
                //TODO: for large length (>32) 
                // we need implement a function transmitBytesToRobotWithACK(...)
                // to replace the function theControlBoard.transmitBytesToRobot(...)

                UInt32 length = 32;

                Byte[] data = new Byte[4];
                data[0] = (byte)((length >> 24) & 0x0FF);
                data[1] = (byte)((length >> 16) & 0x0FF);
                data[2] = (byte)((length >> 8) & 0x0FF);
                data[3] = (byte)(length & 0x0FF);
                SwarmMessageHeader header = new SwarmMessageHeader(e_MessageType.MESSAGE_TYPE_HOST_COMMAND, commandNumber);
                SwarmMessage message = new SwarmMessage(header, data);
                theControlBoard.transmitBytesToRobot(message.toByteArray(), message.getSize(), 1);

                data = new byte[length];
                byte value = 0;
                for (int i = 0; i < length; i++)
                    data[i] = value++;

                theControlBoard.transmitBytesToRobot(data, length, 0);

                uint bufferLength = 2;
                Byte[] dataBuffer = new Byte[bufferLength];
                theControlBoard.receiveBytesFromRobot(bufferLength, ref dataBuffer, 1000);
                SwarmMessage rxMessage = SwarmMessage.ConstructFromByteArray(dataBuffer);
                if (rxMessage.getHeader().getMessageType() == e_MessageType.MESSAGE_TYPE_ROBOT_RESPONSE
                    && rxMessage.getHeader().getCmd() == ROBOT_RESPONSE_OK)
                {
                    setStatusBarContent("Robot's RF Reciever OK!");
                }
                else
                {
                    setStatusBarContent("Robot's RF Reciever: Wrong response...");
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Test transmitting data " + ex.Message);
            }
        }

        private void testMotorsConfiguration(byte Command)
        {
            try
            {
                Byte[] data = new Byte[4];

                if (motor1ReverseCheckBox.IsChecked == true)
                    data[0] = MOTOR_REVERSE_DIRECTION;
                else
                    data[0] = MOTOR_FORWARD_DIRECTION;

                data[1] = Convert.ToByte(this.motor1SpeedTextBox.Text);

                if (motor2ReverseCheckBox.IsChecked == true)
                    data[2] = MOTOR_REVERSE_DIRECTION;
                else
                    data[2] = MOTOR_FORWARD_DIRECTION;

                data[3] = Convert.ToByte(this.motor2SpeedTextBox.Text);

                SwarmMessageHeader header = new SwarmMessageHeader(e_MessageType.MESSAGE_TYPE_HOST_COMMAND, Command);

                SwarmMessage message = new SwarmMessage(header, data);

                theControlBoard.transmitBytesToRobot(message.toByteArray(), message.getSize(), 1);
            }
            catch (Exception ex)
            {
                throw new Exception("Test Motors Configuration " + ex.Message);
            }
        }

        private void readAdc1Button_Click(object sender, RoutedEventArgs e)
        {
            readADC(COMMAND_READ_ADC1, this.readAdc1TextBox);
            setStatusBarContent("Read ADC1 successful!");
        }

        private void readAdc2Button_Click(object sender, RoutedEventArgs e)
        {
            readADC(COMMAND_READ_ADC2, this.readAdc2TextBox);
            setStatusBarContent("Read ADC2 successful!");
        }

        private void readADC(byte Command, TextBox tBox)
        {
            uint length = 600;
            byte[] receivedData = new byte[length];
            uint[] adcData = new uint[length / 2];
            tBox.Text = "";
            try
            {
                theControlBoard.receiveBytesFromRobot(Command, null, length, ref receivedData, 1000);
                uint i = 0;
                for (uint pointer = 0; pointer < length / 2; pointer++)
                {
                    adcData[pointer] = receivedData[i + 1];
                    adcData[pointer] = (adcData[pointer] << 8) | receivedData[i];
                    i += 2;
                    tBox.Text += adcData[pointer].ToString();
                    if (i >= length)
                        break;
                    tBox.Text += ", ";
                }

                OxyplotWindow oxyplotWindow = new OxyplotWindow(adcData, "Sampling Data", OxyplotWindow.PolylineMonoY);
                oxyplotWindow.Show();
            }
            catch (Exception ex)
            {
                defaultExceptionHandle(ex);
            }
        }

        private void readBatteryVoltage()
        {
            uint length = 4;
            Byte[] receivedData = new Byte[length];
            int adcData;
            float BatteryVoltage;

            try
            {
                theControlBoard.receiveBytesFromRobot(COMMAND_REQUEST_BATTERY_VOLT, null, length, ref receivedData, 1000);
                SwarmMessage rxMessage = SwarmMessage.ConstructFromByteArray(receivedData);
                if (rxMessage.getHeader().getMessageType() == e_MessageType.MESSAGE_TYPE_ROBOT_RESPONSE
                    && rxMessage.getHeader().getCmd() == ROBOT_RESPONSE_OK)
                {
                    adcData = (rxMessage.getData()[1] << 8) | rxMessage.getData()[0];
                    BatteryVoltage = (adcData * 3330) / 2048;
                    string message = BatteryVoltage.ToString() + "mV (ADCvalue = " + adcData.ToString() + ")";
                    setStatusBarContent("Robot's V_batt = " + message);
                }
                else 
                {
                    setStatusBarContent("Wrong Vbatt response from robot...");
                }

            }
            catch (Exception ex)
            {
                //defaultExceptionHandle(ex);
                setStatusBarContent("Failed to read robot's V_batt...");
            }
        }

        private void stopMotor1Button_Click(object sender, RoutedEventArgs e)
        {
            theControlBoard.sendCommandToRobot(COMMAND_STOP_MOTOR1);
            setStatusBarContent("Broadcast Command: Pause left motor");
        }

        private void stopMotor2Button_Click(object sender, RoutedEventArgs e)
        {
            theControlBoard.sendCommandToRobot(COMMAND_STOP_MOTOR2);
            setStatusBarContent("Broadcast Command: Pause right motor");
        }

        #endregion

        #region Debug Tab

        private void sendDebugCommandButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Byte[] transmittedData = new Byte[32];

                string command = this.debugCommandSelectBox.Text;

                switch (command)
                {
                    case "Start Measuring Distance":
                        measureDistance();
                        break;

                    case "Scan Robots Vector":
                        scanRobotsVector();
                        break;

                    case "Scan Robots Oriented Angle":
                        scanCorrectionAngleAndOriented();
                        break;

                    case "Read Neighbors Table":
                        ReadNeighbor();
                        break;

                    case "Read One Hop Neighbors Table":
                        ReadOneHopNeighbor();
                        break;

                    case "Draw Coordination Table":
                        DrawMap();
                        break;

                    case "Draw Coordination From File...":
                        DrawFromFile();
                        break;

                    case "Calculate Average Vector From Files...":
                        calAvrFromFile();
                        break;

                    case "Goto Locomotion State":
                        requestGotoLocomotionState();
                        break;

                    case "Rotate Correction Angle":
                        requestRotateCorrectionAngle();
                        break;

                    case "Goto T Shape State":
                        requestGotoTShapeState();
                        break;

                    case "Rotate Correction Angle Different":
                        requestRotateCorrectionAngleDifferent();
                        break;

                    case "Rotate Correction Angle Same":
                        requestRotateCorrectionAngleSame();
                        break;

                    default:
                        throw new Exception("Send Debug Command: Can not recognise command!");
                }
            }
            catch (Exception ex)
            {
                defaultExceptionHandle(ex);
            }
        }

        private void measureDistance()
        {
            theControlBoard.transmitBytesToRobot(COMMAND_MEASURE_DISTANCE);
        }

        private void scanRobotsVector()
        {
            uint length = 8;
            Byte[] receivedData = new Byte[length];

            UInt32[] Plot_id = { 0xBEAD01, 0xBEAD02, 0xBEAD03, 0xBEAD04, 0xBEAD05, 0xBEAD06 };

            List<float> xAxis = new List<float>();
            List<float> yAxis = new List<float>();
            List<UInt32> ui32ID = new List<UInt32>();

            float temp;
            UInt32[] listID;
            float[] Plot_dataX;
            float[] Plot_dataY;
            for (int i = 0; i < 6; i++)
            {
                configureRF(Plot_id[i].ToString("X6"));

                Thread.Sleep(50);
                theControlBoard.transmitBytesToRobot(COMMAND_TOGGLE_LEDS);
                Thread.Sleep(50);

                try
                {
                    theControlBoard.receiveBytesFromRobot(COMMAND_READ_VECTOR, null, length, ref receivedData, 1000);

                    temp = (float)((Int32)((receivedData[0] << 24) | (receivedData[1] << 16) | (receivedData[2] << 8) | receivedData[3]) / 65536.0);
                    xAxis.Add(temp);

                    temp = (float)((Int32)((receivedData[4] << 24) | (receivedData[5] << 16) | (receivedData[6] << 8) | receivedData[7]) / 65536.0);
                    yAxis.Add(temp);

                    ui32ID.Add(Plot_id[i]);
                }
                catch (Exception ex)
                {
                }
            }

            configureRF("BEADFF");

            Plot_dataX = new float[xAxis.Count];
            Plot_dataY = new float[yAxis.Count];
            listID = new UInt32[ui32ID.Count];


            xAxis.CopyTo(Plot_dataX);
            yAxis.CopyTo(Plot_dataY);
            ui32ID.CopyTo(listID);

            OxyplotWindow oxyplotWindow = new OxyplotWindow(listID, Plot_dataX, Plot_dataY, "Robots Real Coordinates", OxyplotWindow.ScatterPointAndLinePlot);

            oxyplotWindow.Show();
        }

        private void scanCorrectionAngleAndOriented()
        {
            uint length = 8;
            Byte[] receivedData = new Byte[length];

            UInt32[] Plot_id = { 0xBEAD01, 0xBEAD02, 0xBEAD03, 0xBEAD04, 0xBEAD05, 0xBEAD06 };

            float correctionAngleInRadian;
            bool oriented;

            List<bool> lstOriented = new List<bool>();
            List<float> lstAngle = new List<float>();

            List<UInt32> ui32ID = new List<UInt32>();

            for (int i = 0; i < 6; i++)
            {
                configureRF(Plot_id[i].ToString("X6"));

                Thread.Sleep(50);
                theControlBoard.transmitBytesToRobot(COMMAND_TOGGLE_LEDS);
                Thread.Sleep(50);

                try
                {
                    theControlBoard.receiveBytesFromRobot(COMMAND_READ_CORRECTION_ANGLE, null, length, ref receivedData, 1000);

                    correctionAngleInRadian = (float)((Int32)((receivedData[0] << 24) | (receivedData[1] << 16) | (receivedData[2] << 8) | receivedData[3]) / 65536.0);
                    lstAngle.Add(correctionAngleInRadian);

                    oriented = (receivedData[4] == 0x01);
                    lstOriented.Add(oriented);

                    ui32ID.Add(Plot_id[i]);
                }
                catch (Exception ex)
                {
                }
            }

            configureRF("BEADFF");

            String message = "Robots correction angle and oriented:\n";

            for (int i = 0; i < ui32ID.Count; i++)
            {
                string orientedString = (lstOriented[i]) ? (" SAME ") : (" DIFFERENT ");
                message += ui32ID[i].ToString("X6") + orientedString + lstAngle[i] + " (" + (lstAngle[i] * 180 / Math.PI) + " degree)\n";
            }

            MessageBox.Show(message, "Scan results", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ReadNeighbor()
        {
            uint length = 60;
            Byte[] receivedData = new Byte[length];

            String title = "Robot [" + this.TXAdrrTextBoxDebug.Text + "] neighbors table";
            String table = "Neighbors Table of Robot [0x" + this.TXAdrrTextBoxDebug.Text + "]:\n";

            int[] ID = new int[10];
            int[] distance = new int[10];
            int pointer = 0;

            double distanceInCm = 0;
            try
            {
                theControlBoard.receiveBytesFromRobot(COMMAND_READ_NEIGHBORS_TABLE, null, length, ref receivedData, 1000);

                for (int i = 0; i < 10; i++)
                {
                    ID[i] = (receivedData[pointer] << 24) | (receivedData[pointer + 1] << 16) | (receivedData[pointer + 2] << 8) | receivedData[pointer + 3];
                    distance[i] = (receivedData[pointer + 4] << 8) | receivedData[pointer + 5];

                    pointer += 6;

                    distanceInCm = distance[i] / 256.0;
                    if (ID[i] != 0 || distance[i] != 0)
                        table += String.Format("Robot [0x{0}] :: {1} cm\n", ID[i].ToString("X6"), distanceInCm.ToString("G6"));
                }

            }
            catch (Exception ex)
            {
                defaultExceptionHandle(ex);
            }

            MessageBox.Show(table, title, MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ReadOneHopNeighbor()
        {
            uint length = 640;
            Byte[] receivedData = new Byte[length];

            String[] table = new String[10];
            String title = "Robot [" + this.TXAdrrTextBoxDebug.Text + "] one hop neighbors table";
            String msg = "One Hop Neighbors Table of Robot [0x" + this.TXAdrrTextBoxDebug.Text + "]:\n";

            int[] firstID = new int[10];
            int[] ID = new int[100];
            int[] distance = new int[100];
            int pointer = 0;
            double distanceInCm = 0;

            try
            {
                theControlBoard.receiveBytesFromRobot(COMMAND_READ_ONEHOP_TABLE, null, length, ref receivedData, 1000);

                for (int i = 0; i < 10; i++)
                {
                    table[i] = "XX";

                    firstID[i] = (receivedData[pointer] << 24) | (receivedData[pointer + 1] << 16) | (receivedData[pointer + 2] << 8) | receivedData[pointer + 3];
                    pointer += 4;

                    for (int j = 0; j < 10; j++)
                    {
                        ID[i * 10 + j] = (receivedData[pointer] << 24) | (receivedData[pointer + 1] << 16) | (receivedData[pointer + 2] << 8) | receivedData[pointer + 3];

                        distance[i * 10 + j] = (receivedData[pointer + 4] << 8) | (receivedData[pointer + 5]);

                        pointer += 6;

                        distanceInCm = distance[i * 10 + j] / 256.0;
                        if (table[i].Equals("XX"))
                        {
                            table[i] = "first Hop ID = 0x" + firstID[i].ToString("X6") + ":\n";
                        }

                        if (ID[i * 10 + j] != 0 || distance[i * 10 + j] != 0)
                        {
                            table[i] += String.Format("Robot [0x{0}] :: {1} cm\n", ID[i * 10 + j].ToString("X6"), distanceInCm.ToString("G6"));
                        }
                    }

                    table[i] += "\n";
                    msg += table[i];
                }
            }
            catch (Exception ex)
            {
                defaultExceptionHandle(ex);
            }

            String fileFullPath = exportTextFile("Output OneHop", "OneHop " + this.TXAdrrTextBoxDebug.Text + ".txt", msg);

            MessageBox.Show("One Hop Neighbors Table of Robot [0x" + this.TXAdrrTextBox.Text + "] have save to\n" + fileFullPath, title, MessageBoxButton.OK, MessageBoxImage.Information);

            string textEditor1 = @"D:\\Program Files\\Notepad++\\notepad++.exe";
            string textEditor2 = @"C:\\Program Files\\Notepad++\\notepad++.exe";

            if (File.Exists(textEditor1))
            {
                Process.Start(textEditor1, fileFullPath);
            }
            else if (File.Exists(textEditor2))
            {
                Process.Start(textEditor2, fileFullPath);
            }
            else
            {
                Process.Start(@"notepad.exe", fileFullPath);
            }
        }

        private void DrawMap()
        {
            uint length = 120;
            Byte[] receivedData = new Byte[length];
            String msg = "Coordination Table of Robot [0x" + this.TXAdrrTextBoxDebug.Text + "]:\n";
            String title = "Robot [" + this.TXAdrrTextBoxDebug.Text + "] Coordination Table";

            int pointer = 0;
            uint dataCounter = 0;

            uint[] id = new uint[10];

            float[] dataX = new float[10];
            float[] dataY = new float[10];
            try
            {
                theControlBoard.receiveBytesFromRobot(COMMAND_READ_LOCS_TABLE, null,length, ref receivedData, 1000);

                for (int i = 0; i < 10; i++)
                {
                    id[i] = (uint)((receivedData[pointer] << 24) | (receivedData[pointer + 1] << 16) | (receivedData[pointer + 2] << 8) | receivedData[pointer + 3]);
                    dataX[i] = (float)((Int32)((receivedData[pointer + 4] << 24) | (receivedData[pointer + 5] << 16) | (receivedData[pointer + 6] << 8) | receivedData[pointer + 7]) / 65536.0);
                    dataY[i] = (float)((Int32)((receivedData[pointer + 8] << 24) | (receivedData[pointer + 9] << 16) | (receivedData[pointer + 10] << 8) | receivedData[pointer + 11]) / 65536.0);

                    pointer += 12;

                    msg += String.Format("Robot:0x{0} ({1}; {2})\n", id[i].ToString("X6"), dataX[i].ToString("G6"), dataY[i].ToString("G6"));

                    if (id[i] != 0)
                        dataCounter++;
                }

            }
            catch (Exception ex)
            {
                defaultExceptionHandle(ex);
            }

            UInt32[] Plot_id = new UInt32[dataCounter];
            float[] Plot_dataX = new float[dataCounter];
            float[] Plot_dataY = new float[dataCounter];

            for (int i = 0; i < dataCounter; i++)
            {
                Plot_id[i] = id[i];
                Plot_dataX[i] = dataX[i];
                Plot_dataY[i] = dataY[i];
            }

            exportTextFile("Output Coordinates", "Coordinates " + this.TXAdrrTextBoxDebug.Text + ".txt", msg);

            OxyplotWindow oxyplotWindow = new OxyplotWindow(Plot_id, Plot_dataX, Plot_dataY, title, OxyplotWindow.ScatterPointAndLinePlot);

            oxyplotWindow.Show();
        }

        private void DrawFromFile()
        {
            bool isValidFile = false;
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Title = "Select your file";
            dlg.Filter = "Text files (*.TXT)|*.TXT" + "|All files (*.*)|*.*";

            // Process open file dialog box results 
            if (dlg.ShowDialog() == true)
            {
                string pathToFile = dlg.FileName;

                System.IO.StreamReader file = new System.IO.StreamReader(@pathToFile);

                string title;
                if ((title = file.ReadLine()) == null)
                    return;

                List<float> xAxis = new List<float>();
                List<float> yAxis = new List<float>();
                List<UInt32> ui32ID = new List<UInt32>();

                string line;
                while ((line = file.ReadLine()) != null)
                {
                    Match match = Regex.Match(line, @"^Robot:(0x[A-Fa-f0-9]+)\s\W([+-]?[0-9]*(?:\.[0-9]+)?);\s([+-]?[0-9]*(?:\.[0-9]+)?)\W$", RegexOptions.IgnoreCase);

                    if (match.Success)
                    {
                        if (!match.Groups[1].Value.Equals("0x000000"))
                        {
                            ui32ID.Add(UInt32.Parse(match.Groups[1].Value.Substring(2), System.Globalization.NumberStyles.HexNumber));
                            xAxis.Add(float.Parse(match.Groups[2].Value));
                            yAxis.Add(float.Parse(match.Groups[3].Value));

                            isValidFile = true;
                        }
                    }
                }

                file.Close();

                if (isValidFile)
                {
                    float[] Plot_dataX = new float[xAxis.Count];
                    float[] Plot_dataY = new float[yAxis.Count];
                    UInt32[] listID = new UInt32[ui32ID.Count];

                    xAxis.CopyTo(Plot_dataX);
                    yAxis.CopyTo(Plot_dataY);
                    ui32ID.CopyTo(listID);

                    OxyplotWindow oxyplotWindow = new OxyplotWindow(listID, Plot_dataX, Plot_dataY, title, OxyplotWindow.ScatterPointAndLinePlot);

                    oxyplotWindow.Show();
                }
            }
        }

        private void calAvrFromFile()
        {
            List<float> xAxis = new List<float>();
            List<float> yAxis = new List<float>();
            List<UInt32> ui32ID = new List<UInt32>();
            List<int> dataCounter = new List<int>();

            string folderPath = "";

            //System.Windows.Forms.FolderBrowserDialog folderBrowserDialog1 = new System.Windows.Forms.FolderBrowserDialog();
            //if (folderBrowserDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            //{
            //    folderPath = folderBrowserDialog1.SelectedPath;
            //    //TODO: Process here
            //}

            var userInputWindow = new UserInputTextWindow();
            userInputWindow.setMessage("Please enter your output coordinates folder path:");
            if (userInputWindow.ShowDialog() == false)
            {
                if (userInputWindow.UserConfirm)
                {
                    folderPath = userInputWindow.inputText;

                    foreach (string file in Directory.EnumerateFiles(folderPath, "*.txt"))
                    {
                        System.IO.StreamReader selectedFile = new System.IO.StreamReader(file);

                        string line;
                        while ((line = selectedFile.ReadLine()) != null)
                        {
                            Match match = Regex.Match(line, @"^Robot:(0x[A-Fa-f0-9]+)\s\W([+-]?[0-9]*(?:\.[0-9]+)?);\s([+-]?[0-9]*(?:\.[0-9]+)?)\W$", RegexOptions.IgnoreCase);

                            if (match.Success)
                            {
                                if (!match.Groups[1].Value.Equals("0x000000"))
                                {
                                    UInt32 robotId = UInt32.Parse(match.Groups[1].Value.Substring(2), System.Globalization.NumberStyles.HexNumber);
                                    int index = ui32ID.IndexOf(robotId);

                                    if (index < 0)
                                    {
                                        ui32ID.Add(UInt32.Parse(match.Groups[1].Value.Substring(2), System.Globalization.NumberStyles.HexNumber));
                                        xAxis.Add(float.Parse(match.Groups[2].Value));
                                        yAxis.Add(float.Parse(match.Groups[3].Value));
                                        dataCounter.Add(1);
                                    }
                                    else
                                    {
                                        xAxis[index] += float.Parse(match.Groups[2].Value);
                                        yAxis[index] += float.Parse(match.Groups[3].Value);
                                        dataCounter[index] += 1;
                                    }
                                }
                            }
                        }

                        selectedFile.Close();
                    }

                    for (int i = 0; i < ui32ID.Count; i++)
                    {
                        xAxis[i] /= dataCounter[i];
                        yAxis[i] /= dataCounter[i];
                    }

                    float[] Plot_dataX = new float[xAxis.Count];
                    float[] Plot_dataY = new float[yAxis.Count];
                    UInt32[] listID = new UInt32[ui32ID.Count];

                    xAxis.CopyTo(Plot_dataX);
                    yAxis.CopyTo(Plot_dataY);
                    ui32ID.CopyTo(listID);

                    OxyplotWindow oxyplotWindow = new OxyplotWindow(listID, Plot_dataX, Plot_dataY, "Robot average vector", OxyplotWindow.ScatterPointAndLinePlot);

                    oxyplotWindow.Show();
                }
            }
        }

        private void requestGotoLocomotionState()
        {
            uint length = 2;
            Byte[] transmittedData = new Byte[length];

            transmittedData[0] = COMMAND_SET_ROBOT_STATE;

            transmittedData[1] = 0x06;

            theControlBoard.transmitBytesToRobot(transmittedData, length, 1);
        }

        private void requestRotateCorrectionAngle()
        {
            uint length = 1;
            Byte[] transmittedData = new Byte[length];

            transmittedData[0] = COMMAND_ROTATE_CORRECTION_ANGLE;

            theControlBoard.transmitBytesToRobot(transmittedData, length, 1);
        }

        private void requestRotateCorrectionAngleDifferent()
        {
            uint length = 1;
            Byte[] transmittedData = new Byte[length];

            transmittedData[0] = COMMAND_ROTATE_CORRECTION_ANGLE_DIFF;

            theControlBoard.transmitBytesToRobot(transmittedData, length, 1);
        }

        private void requestRotateCorrectionAngleSame()
        {
            uint length = 1;
            Byte[] transmittedData = new Byte[length];

            transmittedData[0] = COMMAND_ROTATE_CORRECTION_ANGLE_SAME;

            theControlBoard.transmitBytesToRobot(transmittedData, length, 1);
        }

        private void requestGotoTShapeState()
        {
            uint length = 2;
            Byte[] transmittedData = new Byte[length];

            transmittedData[0] = COMMAND_SET_ROBOT_STATE;

            transmittedData[1] = 0x07;

            theControlBoard.transmitBytesToRobot(transmittedData, length, 1);
        }

        private String exportTextFile(String folderHeaderText, String fileFullName, String content)
        {
            DirectoryInfo fileDir = new DirectoryInfo(".");

            fileDir = fileDir.CreateSubdirectory(folderHeaderText + " " + String.Format("{0:yyyy'-'MM'-'dd}", System.DateTime.Now.Date));

            String fileName = String.Format("{0:hh'-'mm'-'ss tt}", System.DateTime.Now) + " " + fileFullName;

            String fileFullPath = fileDir.FullName + "\\" + fileName;

            File.WriteAllText(@fileFullPath, content);

            return fileFullPath;
        }


        private void configureRFDebug_Click(object sender, RoutedEventArgs e)
        {
            configureRF(this.TXAdrrTextBoxDebug.Text);
        }

        private void setLocalLoopButton_Click(object sender, RoutedEventArgs e)
        {
            Byte[] transmittedData = new Byte[5]; // <set stop loop command><value>

            transmittedData[0] = COMMAND_SET_LOCAL_LOOP_STOP;

            Int32 value = Convert.ToInt32(this.LocalLoopTextBox.Text);
            transmittedData[1] = (Byte)(value >> 24);
            transmittedData[2] = (Byte)(value >> 16);
            transmittedData[3] = (Byte)(value >> 8);
            transmittedData[4] = (Byte)(value & 0xFF);

            theControlBoard.transmitBytesToRobot(transmittedData, 5, 1);
        }

        private void setStepSizeButton_Click(object sender, RoutedEventArgs e)
        {
            Byte[] transmittedData = new Byte[5]; // <set step size command><value>

            transmittedData[0] = COMMAND_SET_STEPSIZE;

            float valueF;

            if (float.TryParse(this.StepSizeTextBox.Text, out valueF))
            {
                Int32 value = (Int32)(valueF * 65536 + 0.5);

                transmittedData[1] = (Byte)(value >> 24);
                transmittedData[2] = (Byte)(value >> 16);
                transmittedData[3] = (Byte)(value >> 8);
                transmittedData[4] = (Byte)(value & 0xFF);

                theControlBoard.transmitBytesToRobot(transmittedData, 5, 1);
            }
            else
            {
                MessageBox.Show("Unvalid stepsize!");
            }
        }

        private void setStop1Button_Click(object sender, RoutedEventArgs e)
        {
            Byte[] transmittedData = new Byte[5]; // <set stop 1 command><value>

            transmittedData[0] = COMMAND_SET_STOP1;

            float valueF;

            if (float.TryParse(this.stop1TextBox.Text, out valueF))
            {
                Int32 value = (Int32)(valueF * 65536 + 0.5);

                transmittedData[1] = (Byte)(value >> 24);
                transmittedData[2] = (Byte)(value >> 16);
                transmittedData[3] = (Byte)(value >> 8);
                transmittedData[4] = (Byte)(value & 0xFF);

                theControlBoard.transmitBytesToRobot(transmittedData, 5, 1);
            }
            else
            {
                MessageBox.Show("Unvalid stop 1!");
            }
        }

        private void setStop2Button_Click(object sender, RoutedEventArgs e)
        {
            Byte[] transmittedData = new Byte[5]; // <set stop 2 command><value>

            transmittedData[0] = COMMAND_SET_STOP2;

            float valueF;

            if (float.TryParse(this.stop2TextBox.Text, out valueF))
            {
                Int32 value = (Int32)(valueF * 65536 + 0.5);

                transmittedData[1] = (Byte)(value >> 24);
                transmittedData[2] = (Byte)(value >> 16);
                transmittedData[3] = (Byte)(value >> 8);
                transmittedData[4] = (Byte)(value & 0xFF);

                theControlBoard.transmitBytesToRobot(transmittedData, 5, 1);
            }
            else
            {
                MessageBox.Show("Unvalid stop 2!");
            }
        }

        private void rotateClockwiseButton_Click(object sender, RoutedEventArgs e)
        {
            uint length = 5;
            Byte[] transmittedData = new Byte[length]; // <send rotate command><value>

            transmittedData[0] = COMMAND_ROTATE_CLOCKWISE;

            UInt32 ui32Value;

            if (UInt32.TryParse(this.rotatePeriodTextBox.Text, out ui32Value))
            {
                transmittedData[1] = (Byte)(ui32Value >> 24);
                transmittedData[2] = (Byte)(ui32Value >> 16);
                transmittedData[3] = (Byte)(ui32Value >> 8);
                transmittedData[4] = (Byte)(ui32Value & 0xFF);

                theControlBoard.transmitBytesToRobot(transmittedData, length, 1);
            }
            else
            {
                MessageBox.Show("Unvalid rotate clockwise period!");
            }
        }

        private void rotateAngleButton_Click(object sender, RoutedEventArgs e)
        {
            uint length = 3;
            Byte[] transmittedData = new Byte[length]; // <send rotate angle command><value>

            transmittedData[0] = COMMAND_ROTATE_CLOCKWISE_ANGLE;

            Int16 i16Value;

            if (Int16.TryParse(this.rotateAngleTextBox.Text, out i16Value))
            {
                transmittedData[1] = (Byte)(i16Value >> 8);
                transmittedData[2] = (Byte)(i16Value & 0xFF);

                theControlBoard.transmitBytesToRobot(transmittedData, length, 1);
            }
            else
            {
                MessageBox.Show("Unvalid rotate clockwise angle!");
            }
        }

        private void forwardPeriodButton_Click(object sender, RoutedEventArgs e)
        {
            uint length = 5;
            Byte[] transmittedData = new Byte[length]; // <send forward period command><value>

            transmittedData[0] = COMMAND_FORWARD_PERIOD;

            UInt32 ui32Value;

            if (UInt32.TryParse(this.forwardPeriodTextBox.Text, out ui32Value))
            {
                transmittedData[1] = (Byte)(ui32Value >> 24);
                transmittedData[2] = (Byte)(ui32Value >> 16);
                transmittedData[3] = (Byte)(ui32Value >> 8);
                transmittedData[4] = (Byte)(ui32Value & 0xFF);

                theControlBoard.transmitBytesToRobot(transmittedData, length, 1);
            }
            else
            {
                MessageBox.Show("Unvalid forward period!");
            }
        }

        private void forwardDistanceButton_Click(object sender, RoutedEventArgs e)
        {
            uint length = 5;
            Byte[] transmittedData = new Byte[length]; // <send forward distance command><value>

            transmittedData[0] = COMMAND_FORWARD_DISTANCE;

            float values;

            if (float.TryParse(this.forwardDistanceTextBox.Text, out values))
            {
                Int32 i32Values = (Int32)(values * 65536 + 0.5);

                transmittedData[1] = (Byte)(i32Values >> 24);
                transmittedData[2] = (Byte)(i32Values >> 16);
                transmittedData[3] = (Byte)(i32Values >> 8);
                transmittedData[4] = (Byte)(i32Values & 0xFF);

                theControlBoard.transmitBytesToRobot(transmittedData, length, 1);
            }
            else
            {
                MessageBox.Show("Unvalid forward distance!");
            }
        }

        #endregion
    }

    #region IValueConverter Members
    public class SubtractConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            double result = 0;
            try
            {

                result = double.Parse(value.ToString()) - double.Parse(parameter.ToString());
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }

            return result;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class DevideConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            // Default to 0. You may want to handle divide by zero 
            // and other issues differently than this.
            double result = 0;

            // Not the best code ever, but you get the idea.
            if (value != null && parameter != null)
            {
                try
                {
                    double numerator = (double)value;
                    double denominator = double.Parse(parameter.ToString());

                    if (denominator != 0)
                    {
                        result = numerator / denominator;
                    }
                    else
                    {
                        result = 1;
                    }
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.Message);
                }
            }

            return result;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    #endregion

    #region Extension Methods

    //Find all controls that match a ceratin kind
    public static class TreeHelper
    {

        #region find parent

        /// <summary>
        /// Finds a parent of a given item on the visual tree.
        /// </summary>
        /// <typeparam name="T">The type of the queried item.</typeparam>
        /// <param name="child">A direct or indirect child of the
        /// queried item.</param>
        /// <returns>The first parent item that matches the submitted
        /// type parameter. If not matching item can be found, a null
        /// reference is being returned.</returns>
        public static T TryFindParent<T>(this DependencyObject child)
            where T : DependencyObject
        {
            //get parent item
            DependencyObject parentObject = GetParentObject(child);

            //we've reached the end of the tree
            if (parentObject == null) return null;

            //check if the parent matches the type we're looking for
            T parent = parentObject as T;
            if (parent != null)
            {
                return parent;
            }
            else
            {
                //use recursion to proceed with next level
                return TryFindParent<T>(parentObject);
            }
        }

        /// <summary>
        /// This method is an alternative to WPF's
        /// <see cref="VisualTreeHelper.GetParent"/> method, which also
        /// supports content elements. Keep in mind that for content element,
        /// this method falls back to the logical tree of the element!
        /// </summary>
        /// <param name="child">The item to be processed.</param>
        /// <returns>The submitted item's parent, if available. Otherwise
        /// null.</returns>
        public static DependencyObject GetParentObject(this DependencyObject child)
        {
            if (child == null) return null;

            //handle content elements separately
            ContentElement contentElement = child as ContentElement;
            if (contentElement != null)
            {
                DependencyObject parent = ContentOperations.GetParent(contentElement);
                if (parent != null) return parent;

                FrameworkContentElement fce = contentElement as FrameworkContentElement;
                return fce != null ? fce.Parent : null;
            }

            //also try searching for parent in framework elements (such as DockPanel, etc)
            FrameworkElement frameworkElement = child as FrameworkElement;
            if (frameworkElement != null)
            {
                DependencyObject parent = frameworkElement.Parent;
                if (parent != null) return parent;
            }

            //if it's not a ContentElement/FrameworkElement, rely on VisualTreeHelper
            return VisualTreeHelper.GetParent(child);
        }

        #endregion

        #region find children

        /// <summary>
        /// Analyzes both visual and logical tree in order to find all elements of a given
        /// type that are descendants of the <paramref name="source"/> item.
        /// </summary>
        /// <typeparam name="T">The type of the queried items.</typeparam>
        /// <param name="source">The root element that marks the source of the search. If the
        /// source is already of the requested type, it will not be included in the result.</param>
        /// <returns>All descendants of <paramref name="source"/> that match the requested type.</returns>
        public static IEnumerable<T> FindChildren<T>(this DependencyObject source) where T : DependencyObject
        {
            if (source != null)
            {
                var childs = GetChildObjects(source);
                foreach (DependencyObject child in childs)
                {
                    //analyze if children match the requested type
                    if (child != null && child is T)
                    {
                        yield return (T)child;
                    }

                    //recurse tree
                    foreach (T descendant in FindChildren<T>(child))
                    {
                        yield return descendant;
                    }
                }
            }
        }


        /// <summary>
        /// This method is an alternative to WPF's
        /// <see cref="VisualTreeHelper.GetChild"/> method, which also
        /// supports content elements. Keep in mind that for content elements,
        /// this method falls back to the logical tree of the element.
        /// </summary>
        /// <param name="parent">The item to be processed.</param>
        /// <returns>The submitted item's child elements, if available.</returns>
        public static IEnumerable<DependencyObject> GetChildObjects(this DependencyObject parent)
        {
            if (parent == null) yield break;

            if (parent is ContentElement || parent is FrameworkElement)
            {
                //use the logical tree for content / framework elements
                foreach (object obj in LogicalTreeHelper.GetChildren(parent))
                {
                    var depObj = obj as DependencyObject;
                    if (depObj != null) yield return (DependencyObject)obj;
                }
            }
            else
            {
                //use the visual tree per default
                int count = VisualTreeHelper.GetChildrenCount(parent);
                for (int i = 0; i < count; i++)
                {
                    yield return VisualTreeHelper.GetChild(parent, i);
                }
            }
        }

        #endregion

        #region find from point

        /// <summary>
        /// Tries to locate a given item within the visual tree,
        /// starting with the dependency object at a given position. 
        /// </summary>
        /// <typeparam name="T">The type of the element to be found
        /// on the visual tree of the element at the given location.</typeparam>
        /// <param name="reference">The main element which is used to perform
        /// hit testing.</param>
        /// <param name="point">The position to be evaluated on the origin.</param>
        public static T TryFindFromPoint<T>(UIElement reference, Point point)
            where T : DependencyObject
        {
            DependencyObject element = reference.InputHitTest(point) as DependencyObject;

            if (element == null) return null;
            else if (element is T) return (T)element;
            else return TryFindParent<T>(element);
        }

        #endregion
    }

    //Masked Texbox
    public class TextBoxHelpers : DependencyObject
    {
        #region IsNumeric & Mapping Events
        // Using a DependencyProperty as the backing store for IsNumeric.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsNumericProperty =
         DependencyProperty.RegisterAttached("IsNumeric", typeof(bool), typeof(TextBoxHelpers), new PropertyMetadata(false, new PropertyChangedCallback((s, e) =>
         {
             TextBox targetTextbox = s as TextBox;
             if (targetTextbox != null)
             {
                 if ((bool)e.OldValue && !((bool)e.NewValue))
                 {
                     targetTextbox.PreviewTextInput -= targetTextbox_PreviewTextInput;

                 }
                 if ((bool)e.NewValue)
                 {
                     targetTextbox.PreviewTextInput += targetTextbox_PreviewTextInput;
                     targetTextbox.PreviewKeyDown += targetTextbox_PreviewKeyDown;
                     targetTextbox.TextChanged += targetTextbox_TextChanged;
                 }
             }
         })));
        public static bool GetIsNumeric(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsNumericProperty);
        }

        public static void SetIsNumeric(DependencyObject obj, bool value)
        {
            obj.SetValue(IsNumericProperty, value);
        }
        #endregion

        #region Max Value
        public static readonly DependencyProperty MaxValueProperty = DependencyProperty.Register("MaxValue", typeof(int),
        typeof(TextBoxHelpers), new PropertyMetadata(6555));
        public static int GetMaxValue(DependencyObject obj)
        {
            return (int)obj.GetValue(MaxValueProperty);
        }
        public static void SetMaxValue(DependencyObject obj, int value)
        {
            obj.SetValue(MaxValueProperty, value);
        }
        #endregion

        #region Min Value
        public static readonly DependencyProperty MinValueProperty = DependencyProperty.Register("MinValue", typeof(int),
        typeof(TextBoxHelpers), new PropertyMetadata(-6555));
        public static int GetMinValue(DependencyObject obj)
        {
            return (int)obj.GetValue(MinValueProperty);
        }
        public static void SetMinValue(DependencyObject obj, int value)
        {
            obj.SetValue(MinValueProperty, value);
        }
        #endregion

        #region IsDecimal
        public static readonly DependencyProperty IsDecimalProperty = DependencyProperty.Register("IsDecimal", typeof(bool),
            typeof(TextBoxHelpers), new PropertyMetadata(false));
        public static bool GetIsDecimal(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsDecimalProperty);
        }
        public static void SetIsDecimal(DependencyObject obj, bool value)
        {
            obj.SetValue(IsDecimalProperty, value);
        }
        #endregion

        #region Events
        private static void targetTextbox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            e.Handled = e.Key == Key.Space;
        }

        private static void targetTextbox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Char newChar = e.Text.ToString()[0];
            e.Handled = !Char.IsNumber(newChar);
            if (GetIsDecimal((DependencyObject)sender) == true)
            {
                e.Handled = !newChar.Equals('.');
            }
        }

        private static void targetTextbox_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox targetTextbox = sender as TextBox;
            int intValue;
            if (targetTextbox != null)
            {

                if (Int32.TryParse(targetTextbox.Text, out intValue))
                {
                    if (intValue > GetMaxValue((DependencyObject)sender))
                    {
                        targetTextbox.Text = GetMaxValue((DependencyObject)sender).ToString();
                    }
                    else if (intValue < GetMinValue((DependencyObject)sender))
                    {
                        targetTextbox.Text = GetMinValue((DependencyObject)sender).ToString();
                    }
                }
            }
        }
        #endregion
    }

    #endregion
}
