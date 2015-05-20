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
        private const string DEFAULT_ROBOT_ID = "00BEAD00";
        private const string DEFAULT_TX_ADDRESS = "00BEADFF";
        private const string DEFAULT_RX_ADDRESS = "00C1AC02";
        private UInt32[] ROBOT_ID_LIST = { 0xBEAD01, 0xBEAD02, 0xBEAD03, 0xBEAD04, 0xBEAD05,
                                           0xBEAD06, 0xBEAD07, 0xBEAD08, 0xBEAD09};

        private String[] ROBOT_STATES = { 
            "State 0: Idle",
            "State 1: Measure Distance",
            "State 2: Exchange Table",
            "State 3: Vote Origin",
            "State 4: Rotate Coordinates",
            "State 5: Average Vector",
            "State 6: Correct Locations",
            "State 7: Locomotion",
            "State 8: Rotate To Angle Use Step Controller",
            "State 9: Forward In Period Use Step Controller",
            "State 10: Forward In Rotate Use Step Controller",
            "State 11: Test Forward In Rotate Pure Controller",
            "State 12: Test PID Controller"
         };

        //------------Commands from Robots---------------------
        private const byte ROBOT_RESPONSE_TO_HOST_OK = 0x0A;

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
        private const byte COMMAND_EEPROM_DATA_READ_BULK = 0x12;
        private const byte COMMAND_EEPROM_DATA_WRITE_BULK = 0x13;

        private const byte COMMAND_INDICATE_BATT_VOLT = 0x14; 
        private const byte COMMAND_CALIBRATE_TDOA_TX = 0x15;
        private const byte COMMAND_MOVE_WITH_PERIOD = 0x16;
        private const byte COMMAND_ROTATE_WITH_PERIOD = 0x17;
        private const byte COMMAND_TOGGLE_IR_LED = 0x18;    // Not used
        private const byte COMMAND_REQUEST_PROXIMITY_RAW = 0x19;    // Not used

        private const byte COMMAND_READ_ROBOT_IDENTITY = 0x1A;
        private const byte COMMAND_READ_NEIGHBORS_TABLE = 0x1B;
        private const byte COMMAND_READ_ONEHOP_NEIGHBORS_TABLE = 0x1C;
        private const byte COMMAND_READ_LOCATIONS_TABLE = 0x1D;
        private const byte COMMAND_SELF_CORRECT_LOCATIONS_TABLE = 0x1E;
        private const byte COMMAND_SELF_CORRECT_LOCATIONS_TABLE_EXCEPT_ROTATION_HOP = 0x1F;
        private const byte COMMAND_GOTO_STATE = 0x20;

        private const byte COMMAND_MOVE_WITH_DISTANCE = 0x21;
        private const byte COMMAND_ROTATE_WITH_ANGLE = 0x22;

        private const byte COMMAND_CONFIG_STEP_CONTROLLER = 0x23;
        private const byte COMMAND_CONFIG_STEP_FORWARD_IN_PERIOD_CONTROLLER = 0x24;
        private const byte COMMAND_CONFIG_STEP_FORWARD_IN_ROTATE_CONTOLLER = 0x25;
        private const byte COMMAND_CONFIG_PID_CONTROLLER = 0x26;

        private const byte COMMAND_UPDATE_GRADIENT_MAP = 0x27;

        enum e_MotorDirection
        {
            MOTOR_FORWARD_DIRECTION = 0,
            MOTOR_REVERSE_DIRECTION = 1
        };

        enum e_RobotRotateDirection
        {
            ROBOT_ROTATE_CCW = 0,
            ROBOT_ROTATE_CW = 1
        };

        enum e_RobotMoveDirection
        {
            ROBOT_MOVE_BACKWARD = 0,
            ROBOT_MOVE_FORWARD = 1
        };

        //==== command below is out of date ===================================
        //private const byte COMMAND_ROTATE_CLOCKWISE = 0xB6;
        //private const byte COMMAND_ROTATE_CLOCKWISE_ANGLE = 0xB7;
        //private const byte COMMAND_FORWARD_PERIOD = 0xB8;
        //private const byte COMMAND_FORWARD_DISTANCE = 0xB9;

        #endregion

        #region EEPROM Table
     
        #region Sine Table Initilization - [0 ---> 1] step = 0.5 degree, offset *32768
     
        private UInt16[] SineTableBlock0 = {	// EEPROM start address Block 2 = 0x0080
	        0,    286,  572,  858,  1144, 1429, 1715, 2000,
	        2286, 2571, 2856, 3141, 3425, 3709, 3993, 4277,
	        4560, 4843, 5126, 5408, 5690, 5971, 6252, 6533,
	        6813, 7092, 7371, 7650, 7927, 8204, 8481, 8757
        };

        private UInt16[] SineTableBlock1 = {	// EEPROM start address Block 3 = 0x00c0
	        9032,  9307,  9580,  9854,  10126, 10397, 10668, 10938,
	        11207, 11476, 11743, 12010, 12275, 12540, 12803, 13066,
	        13328, 13589, 13848, 14107, 14365, 14621, 14876, 15131,
	        15384, 15636, 15886, 16136, 16384, 16631, 16877, 17121
        };

        private UInt16[] SineTableBlock2 = {	// EEPROM start address Block 4 = 0x0100
	        17364, 17606, 17847, 18086, 18324, 18560, 18795, 19028,
	        19261, 19491, 19720, 19948, 20174, 20399, 20622, 20843,
	        21063, 21281, 21498, 21713, 21926, 22138, 22348, 22556,
	        22763, 22967, 23170, 23372, 23571, 23769, 23965, 24159
        };

        private UInt16[] SineTableBlock3 = {	// EEPROM start address Block 5 = 0x0140
	        24351, 24542, 24730, 24917, 25102, 25285, 25466, 25645,
	        25822, 25997, 26170, 26341, 26510, 26677, 26842, 27005,
	        27166, 27325, 27482, 27636, 27789, 27939, 28088, 28234,
	        28378, 28520, 28660, 28797, 28932, 29066, 29197, 29325
        };

        private UInt16[] SineTableBlock4 = {	// EEPROM start address Block 6 = 0x0180
	        29452, 29576, 29698, 29818, 29935, 30050, 30163, 30274,
	        30382, 30488, 30592, 30693, 30792, 30888, 30983, 31075,
	        31164, 31251, 31336, 31419, 31499, 31576, 31651, 31724,
	        31795, 31863, 31928, 31991, 32052, 32110, 32166, 32219
        };

        private UInt16[] SineTableBlock5 = {	// EEPROM start address Block 7 = 0x01c0
	        32270, 32319, 32365, 32408, 32449, 32488, 32524, 32557,
	        32588, 32617, 32643, 32667, 32688, 32707, 32723, 32737,
	        32748, 32757, 32763, 32767, 32768, 0,     0,     0,
	        0, 	   0, 	  0,     0,     0,     0,     0,     0
        };

        #endregion

        #region ArcSine Table Initilization - [0 ---> pi / 2] step = 1/180, offset *32768

        private UInt16[] ArcSineTableBlock0 = {	// EEPROM start address Block 8 = 0x0200
	        0,    182,  364,  546,  728,  910,  1092, 1275,
	        1457, 1639, 1821, 2004, 2186, 2369, 2551, 2734,
	        2917, 3099, 3282, 3465, 3648, 3832, 4015, 4199,
	        4382, 4566, 4750, 4934, 5118, 5302, 5487, 5672
        };

        private UInt16[] ArcSineTableBlock1 = {	// EEPROM start address Block 9 = 0x0240
	        5857,  6042,  6227,  6412,  6598,  6784,  6970,  7156,
	        7343,  7530,  7717,  7904,  8092,  8280,  8468,  8656,
	        8845,  9034,  9224,  9413,  9603,  9794,  9984,  10175,
	        10367, 10558, 10750, 10943, 11136, 11329, 11523, 11717
        };

        private UInt16[] ArcSineTableBlock2 = {	// EEPROM start address Block 10 = 0x0280
	        11911, 12106, 12302, 12498, 12694, 12891, 13088, 13286,
	        13485, 13683, 13883, 14083, 14283, 14485, 14686, 14889,
	        15091, 15295, 15499, 15704, 15909, 16116, 16323, 16530,
	        16738, 16947, 17157, 17368, 17579, 17791, 18005, 18218
        };

        private UInt16[] ArcSineTableBlock3 = {	// EEPROM start address Block 11 = 0x02c0
	        18433, 18649, 18865, 19083, 19301, 19521, 19741, 19963,
	        20185, 20409, 20633, 20859, 21086, 21314, 21544, 21774,
	        22006, 22239, 22474, 22710, 22947, 23186, 23426, 23668,
	        23912, 24157, 24404, 24652, 24902, 25154, 25408, 25664
        };

        private UInt16[] ArcSineTableBlock4 = {	// EEPROM start address Block 12 = 0x0300
	        25922, 26182, 26444, 26708, 26975, 27244, 27515, 27789,
	        28066, 28345, 28627, 28912, 29200, 29492, 29786, 30084,
	        30386, 30691, 31000, 31313, 31631, 31953, 32280, 32612,
	        32949, 33292, 33640, 33995, 34357, 34725, 35101, 35485
        };

        private UInt16[] ArcSineTableBlock5 = {	// EEPROM start address Block 13 = 0x0340
	        35878, 36280, 36693, 37116, 37551, 38000, 38463, 38942,
	        39439, 39957, 40498, 41066, 41666, 42303, 42988, 43730,
	        44551, 45481, 46583, 48016, 51472, 0,     0,     0,
	        0, 	   0, 	  0,     0,     0,     0,     0,     0
        };
        #endregion

        private List<Tuple<UInt32, UInt16[]>> listTupeEepromTable;

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
            addRobotIdListToCombobox(ref this.TXAddressCalibrationSelectBox, ROBOT_ID_LIST);
            addRobotIdListToCombobox(ref this.TXAdrrComboBoxDebug, ROBOT_ID_LIST);

            addRobotStatesToCombobox(ref this.robotStateSelectBox, ROBOT_STATES);

            listTupeEepromTable = new List<Tuple<UInt32, UInt16[]>>();

            listTupeEepromTable.Add(new Tuple<uint, ushort[]>(0x0080, SineTableBlock0));
            listTupeEepromTable.Add(new Tuple<uint, ushort[]>(0x00c0, SineTableBlock1));
            listTupeEepromTable.Add(new Tuple<uint, ushort[]>(0x0100, SineTableBlock2));
            listTupeEepromTable.Add(new Tuple<uint, ushort[]>(0x0140, SineTableBlock3));
            listTupeEepromTable.Add(new Tuple<uint, ushort[]>(0x0180, SineTableBlock4));
            listTupeEepromTable.Add(new Tuple<uint, ushort[]>(0x01c0, SineTableBlock5));

            listTupeEepromTable.Add(new Tuple<uint, ushort[]>(0x0200, ArcSineTableBlock0));
            listTupeEepromTable.Add(new Tuple<uint, ushort[]>(0x0240, ArcSineTableBlock1));
            listTupeEepromTable.Add(new Tuple<uint, ushort[]>(0x0280, ArcSineTableBlock2));
            listTupeEepromTable.Add(new Tuple<uint, ushort[]>(0x02C0, ArcSineTableBlock3));
            listTupeEepromTable.Add(new Tuple<uint, ushort[]>(0x0300, ArcSineTableBlock4));
            listTupeEepromTable.Add(new Tuple<uint, ushort[]>(0x0340, ArcSineTableBlock5));
        }
        private void addRobotIdListToCombobox(ref ComboBox target, UInt32[] content)
        {
            target.Items.Clear();
            target.Items.Add(DEFAULT_TX_ADDRESS);
            for (int i = 0; i < content.Length; i++)
                target.Items.Add(content[i].ToString("X8"));
            target.Items.Add(DEFAULT_ROBOT_ID);
            target.SelectedIndex = 0;
        }
        private void addRobotStatesToCombobox(ref ComboBox target, String[] content)
        {
            target.Items.Clear();
            foreach (var item in content)
                target.Items.Add(item);
            target.SelectedIndex = 0;
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
                this.statusDeviceAttached.Content = String.Format("File size {0:0} KB, Transfer rate {1:0.000} KB/s, Time left {2:0} s",
                    bootLoader.getLastTransferSize() / 1000, bootLoader.getProgramSpeed() / 1000, (bootLoader.getLastTransferSize() - bootLoader.getTransferedSize()) / bootLoader.getProgramSpeed());
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
        private void viewRfMode_Click(object sender, RoutedEventArgs e)
        {
            //uint[] data1 = new uint[] { 1199, 1178, 1174, 1185, 1179, 1176, 1173, 1177, 1178, 1183, 1174, 1180, 1174, 1184, 1180, 1170, 1181, 1168, 1179, 1189, 1179, 1175, 1171, 1172, 1167, 1186, 1182, 1177, 1176, 1177, 1178, 1183, 1189, 1183, 1185, 1176, 1175, 1178, 1180, 1187, 1190, 1178, 1170, 1171, 1175, 1168, 1177, 1177, 1170, 1166, 1174, 1170, 1168, 1171, 1172, 1181, 1179, 1171, 1174, 1173, 1176, 1181, 1178, 1184, 1184, 1185, 1192, 1202, 1217, 1223, 1198, 1164, 1116, 1076, 1060, 1069, 1100, 1152, 1203, 1229, 1257, 1248, 1237, 1192, 1147, 1103, 1092, 1110, 1155, 1240, 1311, 1349, 1344, 1307, 1237, 1149, 1082, 1042, 1038, 1064, 1134, 1180, 1251, 1294, 1310, 1290, 1238, 1185, 1124, 1066, 1050, 1075, 1127, 1187, 1237, 1280, 1284, 1278, 1246, 1201, 1140, 1106, 1114, 1119, 1150, 1189, 1209, 1223, 1208, 1192, 1179, 1159, 1147, 1124, 1141, 1156, 1169, 1185, 1176, 1175, 1175, 1157, 1153, 1145, 1145, 1143, 1166, 1172, 1194, 1208, 1202, 1210, 1191, 1181, 1159, 1135, 1101, 1095, 1101, 1115, 1141, 1164, 1180, 1190, 1183, 1196, 1173, 1145, 1123, 1120, 1122, 1139, 1163, 1185, 1213, 1221, 1238, 1228, 1210, 1193, 1176, 1166, 1157, 1165, 1159, 1182, 1189, 1208, 1214, 1210, 1195, 1185, 1172, 1169, 1165, 1159, 1167, 1165, 1175, 1193, 1199, 1196, 1191, 1176, 1176, 1174, 1171, 1187, 1201, 1208, 1220, 1212, 1209, 1198, 1182, 1167, 1159, 1160, 1163, 1167, 1177, 1180, 1185, 1187, 1197, 1196, 1193, 1179, 1161, 1152, 1159, 1162, 1166, 1169, 1184, 1200, 1202, 1201, 1204, 1187, 1191, 1180, 1165, 1169, 1171, 1182, 1186, 1200, 1199, 1189, 1183, 1170, 1157, 1150, 1151, 1162, 1164, 1170, 1175, 1178, 1180, 1178, 1179, 1172, 1161, 1153, 1153, 1164, 1173, 1170, 1171, 1177, 1176, 1175, 1182, 1181, 1169, 1164, 1158, 1157, 1160, 1157, 1173, 1172, 1178, 1177, 1165, 1177, 1172, 1170, 1169, 1184, 1177, 1175, 1181, 1171, 1172, 1173, 1188, 1188, 1184, 1177, 1168, 1171, 1163, 1165, 1161, 1170, 1164, 1175, 1173, 1176, 1178, 1170, 1172, 1158, 1166, 1154, 1161, 1177, 1183, 1190, 1200, 1193, 1194, 1186, 1187, 1176, 1172, 1174, 1176, 1175, 1175, 1185, 1177, 1181, 1176, 1167, 1160, 1162, 1157, 1170, 1172, 1176, 1181, 1179, 1179, 1182, 1176, 1179, 1179, 1182, 1192, 1198, 1193, 1199, 1204, 1200, 1198, 1188, 1189, 1187, 1185, 1189, 1178, 1180, 1181, 1181, 1191, 1184, 1181, 1187, 1175, 1175, 1158, 1160, 1169, 1169, 1177, 1176, 1190, 1198, 1198, 1187, 1197, 1188, 1184, 1182, 1178, 1179, 1183, 1194, 1199, 1197, 1189, 1184, 1179, 1158, 1159, 1163, 1169, 1158, 1160, 1166, 1171, 1166, 1179, 1181, 1165, 1166, 1160, 1151, 1161, 1157, 1160, 1167, 1181, 1180, 1181, 1177, 1186, 1174, 1169, 1164, 1147, 1142, 1149, 1155, 1169, 1181, 1185, 1187, 1180, 1174, 1167, 1160, 1161, 1166, 1165, 1175, 1170, 1182, 1198, 1192, 1193, 1188, 1168, 1156, 1163, 1155, 1170, 1166, 1184, 1188, 1188, 1190, 1188, 1183, 1170, 1164, 1150, 1155, 1158, 1167, 1169, 1179, 1190, 1189, 1192, 1191, 1178, 1168, 1155, 1162, 1177, 1181, 1190, 1189, 1186, 1179, 1178, 1174, 1184, 1176, 1178, 1180, 1170, 1166, 1179, 1171, 1170, 1177, 1174, 1170, 1183, 1184, 1187, 1192, 1186, 1191 };
            //uint[] data2 = new uint[] { 1182, 1168, 1174, 1182, 1185, 1173, 1177, 1168, 1161, 1170, 1168, 1167, 1169, 1166, 1166, 1162, 1161, 1171, 1177, 1174, 1176, 1167, 1163, 1170, 1169, 1171, 1163, 1159, 1176, 1175, 1184, 1170, 1170, 1165, 1167, 1176, 1178, 1172, 1166, 1164, 1164, 1177, 1182, 1177, 1179, 1169, 1177, 1174, 1171, 1165, 1170, 1173, 1170, 1163, 1177, 1172, 1176, 1172, 1167, 1168, 1161, 1160, 1165, 1161, 1171, 1172, 1186, 1198, 1194, 1200, 1190, 1165, 1131, 1094, 1065, 1069, 1089, 1134, 1182, 1226, 1253, 1261, 1241, 1203, 1143, 1096, 1086, 1094, 1134, 1199, 1263, 1311, 1316, 1308, 1243, 1167, 1092, 1028, 1020, 1035, 1100, 1173, 1237, 1291, 1320, 1306, 1256, 1183, 1107, 1064, 1052, 1066, 1103, 1164, 1219, 1274, 1298, 1283, 1250, 1199, 1149, 1106, 1088, 1102, 1142, 1154, 1174, 1197, 1206, 1202, 1172, 1164, 1136, 1128, 1141, 1139, 1153, 1161, 1167, 1167, 1160, 1153, 1151, 1148, 1147, 1161, 1179, 1178, 1188, 1192, 1192, 1187, 1183, 1166, 1147, 1118, 1104, 1092, 1107, 1114, 1132, 1141, 1160, 1166, 1160, 1160, 1156, 1136, 1128, 1123, 1127, 1139, 1159, 1179, 1197, 1199, 1200, 1201, 1199, 1187, 1185, 1188, 1189, 1178, 1177, 1176, 1183, 1192, 1190, 1196, 1184, 1175, 1172, 1165, 1164, 1161, 1150, 1153, 1149, 1166, 1161, 1178, 1171, 1172, 1181, 1183, 1175, 1175, 1184, 1194, 1204, 1206, 1206, 1213, 1200, 1193, 1189, 1181, 1155, 1164, 1156, 1172, 1182, 1175, 1170, 1170, 1166, 1172, 1157, 1147, 1143, 1141, 1156, 1167, 1168, 1183, 1186, 1181, 1182, 1180, 1183, 1181, 1177, 1176, 1177, 1184, 1180, 1174, 1181, 1181, 1179, 1179, 1165, 1162, 1173, 1156, 1161, 1160, 1165, 1172, 1165, 1163, 1172, 1163, 1163, 1161, 1151, 1156, 1150, 1148, 1148, 1150, 1148, 1153, 1144, 1146, 1138, 1135, 1139, 1146, 1160, 1177, 1191, 1195, 1184, 1180, 1175, 1172, 1160, 1159, 1156, 1159, 1170, 1173, 1186, 1179, 1180, 1169, 1156, 1150, 1146, 1142, 1137, 1148, 1168, 1161, 1165, 1163, 1170, 1159, 1156, 1152, 1166, 1156, 1164, 1170, 1177, 1180, 1175, 1181, 1182, 1179, 1177, 1180, 1176, 1176, 1182, 1173, 1171, 1172, 1179, 1168, 1172, 1168, 1165, 1158, 1166, 1163, 1162, 1158, 1163, 1163, 1158, 1159, 1158, 1160, 1166, 1165, 1176, 1188, 1186, 1187, 1183, 1179, 1183, 1169, 1165, 1167, 1166, 1180, 1197, 1204, 1202, 1211, 1207, 1204, 1182, 1169, 1148, 1154, 1163, 1168, 1172, 1188, 1203, 1200, 1194, 1184, 1183, 1159, 1155, 1150, 1150, 1156, 1165, 1181, 1190, 1188, 1195, 1187, 1176, 1160, 1159, 1151, 1155, 1159, 1171, 1173, 1182, 1176, 1174, 1180, 1159, 1163, 1151, 1152, 1153, 1154, 1156, 1154, 1151, 1157, 1151, 1158, 1152, 1140, 1156, 1151, 1162, 1164, 1155, 1167, 1164, 1160, 1160, 1164, 1175, 1184, 1176, 1170, 1172, 1158, 1162, 1144, 1142, 1151, 1155, 1169, 1180, 1176, 1173, 1169, 1167, 1151, 1156, 1144, 1145, 1158, 1155, 1176, 1181, 1187, 1184, 1186, 1180, 1180, 1173, 1158, 1156, 1163, 1161, 1166, 1176, 1179, 1180, 1185, 1188, 1173, 1165, 1165, 1157, 1154, 1154, 1144, 1154, 1165, 1178, 1175, 1180, 1171, 1167, 1160, 1158, 1159, 1161, 1167, 1169, 1166, 1179, 1183, 1185, 1177, 1168, 1171, 1166, 1170, 1169, 1177, 1181, 1178 };

            uint[] data1 = new uint[] { 1163, 1172, 1165, 1160, 1171, 1168, 1181, 1184, 1174, 1173, 1170, 1166, 1157, 1171, 1170, 1166, 1168, 1169, 1170, 1175, 1170, 1169, 1178, 1168, 1175, 1169, 1174, 1170, 1181, 1173, 1172, 1170, 1163, 1168, 1169, 1167, 1167, 1178, 1174, 1173, 1162, 1169, 1167, 1163, 1168, 1163, 1172, 1172, 1172, 1165, 1178, 1169, 1165, 1168, 1170, 1176, 1180, 1190, 1206, 1233, 1242, 1209, 1159, 1101, 1037, 998, 967, 964, 1011, 1079, 1160, 1240, 1304, 1337, 1340, 1317, 1262, 1190, 1138, 1105, 1140, 1214, 1307, 1408, 1484, 1485, 1451, 1359, 1222, 1080, 956, 846, 810, 857, 971, 1115, 1265, 1368, 1401, 1336, 1206, 1045, 888, 802, 811, 909, 1077, 1279, 1469, 1594, 1653, 1619, 1502, 1324, 1138, 968, 875, 865, 925, 1045, 1201, 1335, 1398, 1408, 1332, 1213, 1050, 907, 805, 775, 811, 907, 1054, 1208, 1345, 1412, 1438, 1388, 1299, 1207, 1114, 1056, 1056, 1092, 1153, 1220, 1286, 1326, 1324, 1278, 1203, 1110, 1030, 955, 931, 931, 970, 1050, 1139, 1204, 1252, 1276, 1268, 1224, 1182, 1167, 1149, 1144, 1190, 1230, 1279, 1315, 1346, 1353, 1332, 1285, 1231, 1185, 1149, 1118, 1096, 1104, 1120, 1137, 1136, 1132, 1129, 1129, 1129, 1132, 1141, 1150, 1176, 1204, 1223, 1232, 1241, 1246, 1237, 1206, 1196, 1189, 1183, 1181, 1183, 1187, 1199, 1210, 1212, 1190, 1179, 1166, 1144, 1125, 1109, 1090, 1101, 1115, 1135, 1151, 1172, 1192, 1202, 1211, 1215, 1204, 1201, 1204, 1192, 1186, 1176, 1168, 1159, 1144, 1141, 1137, 1131, 1134, 1122, 1135, 1135, 1130, 1144, 1142, 1155, 1172, 1175, 1177, 1195, 1189, 1202, 1195, 1176, 1174, 1163, 1161, 1163, 1164, 1167, 1157, 1172, 1172, 1159, 1160, 1148, 1134, 1131, 1129, 1121, 1111, 1128, 1135, 1156, 1166, 1174, 1177, 1174, 1186, 1186, 1176, 1183, 1165, 1164, 1164, 1152, 1157, 1163, 1167, 1167, 1160, 1163, 1151, 1162, 1157, 1158, 1160, 1160, 1159, 1169, 1178, 1185, 1199, 1206, 1203, 1206, 1203, 1193, 1194, 1189, 1184, 1174, 1177, 1183, 1175, 1176, 1178, 1180, 1172, 1167, 1164, 1166, 1169, 1177, 1175, 1195, 1196, 1207, 1216, 1211, 1204, 1200, 1193, 1194, 1200, 1196, 1192, 1203, 1195, 1193, 1199, 1189, 1170, 1154, 1150, 1145, 1141, 1135, 1134, 1128, 1118, 1139, 1152, 1167, 1182, 1183, 1185, 1187, 1191, 1187, 1197, 1201, 1216, 1219, 1219, 1233, 1231, 1220, 1194, 1189, 1167, 1144, 1137, 1135, 1147, 1153, 1168, 1171, 1175, 1154, 1142, 1112, 1090, 1093, 1095, 1106, 1144, 1172, 1196, 1205, 1205, 1200, 1183, 1171, 1150, 1139, 1135, 1157, 1179, 1212, 1241, 1245, 1245, 1213, 1158, 1122, 1089, 1062, 1052, 1070, 1095, 1136, 1178, 1199, 1229, 1230, 1212, 1179, 1138, 1112, 1097, 1100, 1118, 1168, 1201, 1238, 1241, 1254, 1234, 1210, 1168, 1129, 1106, 1106, 1114, 1129, 1158, 1187, 1218, 1236, 1242, 1210, 1190, 1153, 1116, 1103, 1113, 1113, 1146, 1172, 1200, 1224, 1248, 1249, 1227, 1204, 1189, 1161, 1138, 1129, 1139, 1157, 1157, 1186, 1196, 1222, 1225, 1208, 1193, 1195, 1175, 1153, 1153, 1158, 1154, 1183, 1190, 1198, 1204, 1203, 1201, 1187, 1172, 1159, 1161, 1150, 1161, 1179, 1181, 1194, 1199, 1210, 1197, 1195, 1187, 1156, 1150, 1141, 1139, 1141, 1155 };
            uint[] data2 = new uint[] { 1194, 1177, 1173, 1177, 1182, 1180, 1178, 1178, 1181, 1168, 1181, 1176, 1181, 1169, 1176, 1172, 1167, 1172, 1173, 1161, 1167, 1171, 1168, 1160, 1151, 1164, 1160, 1166, 1171, 1167, 1159, 1154, 1165, 1170, 1167, 1177, 1171, 1166, 1170, 1173, 1168, 1164, 1171, 1166, 1166, 1159, 1152, 1158, 1168, 1167, 1165, 1162, 1166, 1162, 1169, 1179, 1193, 1184, 1181, 1182, 1170, 1177, 1183, 1181, 1185, 1211, 1247, 1265, 1242, 1193, 1091, 984, 893, 852, 891, 998, 1184, 1376, 1529, 1619, 1625, 1548, 1364, 1143, 948, 822, 804, 890, 1089, 1325, 1506, 1577, 1524, 1340, 1090, 822, 614, 548, 609, 799, 1095, 1399, 1675, 1801, 1780, 1602, 1315, 987, 689, 511, 511, 665, 938, 1251, 1547, 1756, 1805, 1736, 1554, 1317, 1088, 914, 830, 858, 959, 1095, 1239, 1367, 1422, 1403, 1321, 1195, 1078, 966, 893, 889, 925, 1022, 1138, 1264, 1337, 1366, 1340, 1260, 1167, 1056, 983, 946, 975, 1067, 1163, 1270, 1347, 1368, 1358, 1304, 1223, 1149, 1093, 1075, 1100, 1166, 1228, 1305, 1320, 1290, 1241, 1166, 1110, 1044, 1033, 1042, 1089, 1149, 1216, 1257, 1282, 1271, 1228, 1164, 1096, 1056, 1043, 1057, 1093, 1142, 1200, 1252, 1287, 1291, 1287, 1256, 1221, 1178, 1146, 1125, 1134, 1146, 1170, 1193, 1226, 1227, 1209, 1177, 1129, 1090, 1063, 1050, 1073, 1088, 1120, 1151, 1195, 1214, 1225, 1217, 1205, 1192, 1170, 1157, 1151, 1150, 1157, 1184, 1196, 1211, 1216, 1197, 1184, 1167, 1142, 1113, 1109, 1104, 1105, 1117, 1124, 1137, 1151, 1158, 1158, 1156, 1135, 1140, 1149, 1147, 1166, 1191, 1224, 1233, 1261, 1264, 1254, 1240, 1214, 1190, 1162, 1139, 1118, 1098, 1089, 1092, 1103, 1121, 1133, 1140, 1155, 1149, 1136, 1134, 1121, 1117, 1112, 1130, 1153, 1176, 1207, 1227, 1234, 1239, 1223, 1202, 1184, 1163, 1151, 1150, 1148, 1158, 1170, 1157, 1152, 1159, 1151, 1144, 1139, 1135, 1154, 1175, 1191, 1216, 1246, 1250, 1240, 1229, 1226, 1208, 1192, 1183, 1186, 1176, 1187, 1205, 1212, 1224, 1229, 1225, 1201, 1174, 1141, 1118, 1098, 1089, 1094, 1105, 1128, 1149, 1164, 1187, 1220, 1236, 1223, 1216, 1194, 1173, 1160, 1144, 1140, 1145, 1148, 1165, 1177, 1196, 1196, 1198, 1183, 1160, 1146, 1124, 1126, 1132, 1136, 1176, 1192, 1219, 1244, 1239, 1216, 1195, 1164, 1133, 1115, 1108, 1124, 1152, 1188, 1234, 1248, 1245, 1236, 1202, 1165, 1131, 1101, 1091, 1085, 1113, 1163, 1203, 1230, 1248, 1235, 1182, 1134, 1100, 1055, 1038, 1048, 1077, 1111, 1166, 1212, 1235, 1235, 1215, 1179, 1146, 1118, 1110, 1117, 1146, 1182, 1217, 1243, 1257, 1273, 1264, 1234, 1199, 1157, 1127, 1089, 1092, 1090, 1105, 1132, 1163, 1187, 1208, 1212, 1199, 1178, 1144, 1123, 1112, 1110, 1121, 1148, 1180, 1205, 1233, 1240, 1217, 1210, 1185, 1160, 1143, 1150, 1163, 1182, 1193, 1208, 1205, 1201, 1186, 1172, 1156, 1146, 1138, 1144, 1160, 1167, 1179, 1189, 1190, 1177, 1158, 1146, 1144, 1152, 1160, 1193, 1210, 1219, 1216, 1226, 1214, 1191, 1161, 1153, 1152, 1160, 1179, 1197, 1209, 1220, 1196, 1159, 1144, 1109, 1084, 1072, 1083, 1103, 1136, 1171, 1222, 1242, 1247, 1231, 1211, 1173, 1149, 1148, 1128, 1152, 1174, 1203, 1215, 1218, 1216 };

            filterAndPlotResults("VyLong 1", data1);
            filterAndPlotResults("VyLong 2", data2);
        }

        private void plotTDOA_Click(object sender, RoutedEventArgs e)
        {
            plotPeakResultFromFile();
        }

        private void plotLocs_Click(object sender, RoutedEventArgs e)
        {
            plotLocationsTableFromFile();
        }

        private void aboutMenuItem_Click(object sender, RoutedEventArgs e)
        {
            AboutWindow aboutWindow = new AboutWindow();
            aboutWindow.ShowDialog();
        }

        private void imageGenerator_Click(object sender, RoutedEventArgs e)
        {
            ImageGeneratorWindow imageGenWindow = new ImageGeneratorWindow();
            imageGenWindow.Show();
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
            theControlBoard.broadcastCommandToRobot(COMMAND_SLEEP);
        }

        private void deepsleepButton_Click(object sender, RoutedEventArgs e)
        {
            theControlBoard.broadcastCommandToRobot(COMMAND_DEEP_SLEEP);
        }

        private void wakeUpButton_Click(object sender, RoutedEventArgs e)
        {
            theControlBoard.broadcastCommandToRobot(COMMAND_WAKE_UP);
        }

        private void resetButton_Click(object sender, RoutedEventArgs e)
        {
            theControlBoard.broadcastCommandToRobot(COMMAND_RESET);
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
            theControlBoard.broadcastCommandToRobot(COMMAND_WAKE_UP);

            Thread.Sleep(500);

            theControlBoard.broadcastCommandToRobot(COMMAND_RESET);

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
                        //this.statusDeviceAttached.Dispatcher.Invoke((Action)(() =>
                        //{
                        setStatusBarContent(String.Format("Program Size = {0:0.0000} KB", bootLoader.getLastTransferSize() / 1024.0));
                        //}));
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

                    this.statusDeviceAttached.Dispatcher.Invoke((Action)(() => {
                        setStatusBarContent(String.Format("Program Size = {0:0.0000} KB", bootLoader.getLastTransferSize() / 1024.0));
                    }));
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
                if ((channel > 125) || (channel < 0))
                    throw new Exception("The choosen channel is out of range");

                // Get output power index - 1 byte
                byte powerIndex = (byte)(this.TXPowerComboBox.SelectedIndex);

                // Get Tx address - 4 bytes
                byte[] rfTXAddress = new byte[RF_ADDRESS_WIDTH];
                address = getAddress(TX_ADDRstring, 2 * RF_ADDRESS_WIDTH);
                rfTXAddress[0] = (byte)address;
                rfTXAddress[1] = (byte)(address >> 8);
                rfTXAddress[2] = (byte)(address >> 16);
                rfTXAddress[3] = (byte)(address >> 24);

                // Get Rx address - 4 bytes
                byte[] rfRXAddress = new byte[RF_ADDRESS_WIDTH];
                string RX_ADDRstring = this.Pipe0AddressTextBox.Text;
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
        private void setTxAddress(string Tx_ADDRstring)
        {
            const byte RF_ADDRESS_WIDTH = 4;

            // Get Tx address - 4 bytes
            byte[] rfTXAddress = new byte[RF_ADDRESS_WIDTH];
            UInt32 address = getAddress(Tx_ADDRstring, 2 * RF_ADDRESS_WIDTH);
            rfTXAddress[0] = (byte)(address >> 24);
            rfTXAddress[1] = (byte)(address >> 16);
            rfTXAddress[2] = (byte)(address >> 8);
            rfTXAddress[3] = (byte)address;
           
            // Send
            theControlBoard.configureRF_TxAddress(rfTXAddress);
        }
        private UInt32 getAddress(string addrString, uint addrWidth)
        {
            if (addrString.Length != addrWidth)
            {
                string msg = String.Format("Rf address {1} must have {0} characters!", addrWidth, addrString);
                throw new Exception(msg);
            }

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
            setStatusBarContent("Configure RF: OK!");
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
            this.rfChannelTextBox.Text = "51";
            this.TXPowerComboBox.SelectedIndex = 0;

            setStatusBarContent("Default Rf configuration parameter.");
        }
        #endregion

        #region EEPROM Tab
        private void configureRFEEprom_Click(object sender, RoutedEventArgs e)
        {
            setTxAddress(this.TXAdrrEepromTextBox.Text);
            setStatusBarContent("Set RF Tx Address: " + this.TXAdrrEepromTextBox.Text);
        }
        
        private BackgroundWorker backgroundWorker = null;

        private enum e_VerifyTableReturn
        {
            SINE_OK_ARCSINE_OK = 1,
            SINE_OK_ARCSINE_ERROR = 2,
            SINE_ERROR_ARCSINE_OK = 3,
            SINE_ERROR_ARCSINE_ERROR = 4
        }
       
        private void EepromDataReadButton_Click(object sender, RoutedEventArgs e)
        {
            byte unit = 5;
            byte[] data = new byte[unit * 2 + 1];
            UInt16 ui16WordIndex;

            const byte UNIT_STEP = 6;
            uint bufferLength = (uint)unit * UNIT_STEP + 1 + 2;
            byte rxUnit;
            UInt32 ui32RxData = 0;
            Int32 i32RxData = 0;
            byte[] dataBuffer = new byte[bufferLength];
            UInt32 dataPointer = 1;

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

            /* 4 */
            ui16WordIndex = Convert.ToUInt16(this.EepromRandomSequencesWordIndexTextBox.Text);
            data[7] = (byte)((ui16WordIndex >> 8) & 0x0FF);
            data[8] = (byte)(ui16WordIndex & 0x0FF);

            /* 5 */
            ui16WordIndex = Convert.ToUInt16(this.EepromMotorWordIndexTextBox.Text);
            data[9] = (byte)((ui16WordIndex >> 8) & 0x0FF);
            data[10] = (byte)(ui16WordIndex & 0x0FF);

            try
            {
                SwarmMessageHeader header = new SwarmMessageHeader(e_MessageType.MESSAGE_TYPE_HOST_COMMAND, COMMAND_EEPROM_DATA_READ);
                SwarmMessage message = new SwarmMessage(header, data);

                theControlBoard.receivedDataFromRobot(dataBuffer, bufferLength, 1000, message);

                SwarmMessage rxMessage = SwarmMessage.ConstructFromByteArray(dataBuffer);
                byte[] messageContent;
                if (rxMessage.getHeader().getMessageType() == e_MessageType.MESSAGE_TYPE_ROBOT_RESPONSE
                    && rxMessage.getHeader().getCmd() == ROBOT_RESPONSE_TO_HOST_OK)
                {
                    messageContent = rxMessage.getData();
                    rxUnit = messageContent[0];

                    dataPointer = 1;
                    if (rxUnit == unit)
                    {
                        /* Robot ID */
                        constructWordIndexAndDataContent(ref ui16WordIndex, ref ui32RxData, messageContent, dataPointer);
                        dataPointer += UNIT_STEP;
                        this.EepromRobotIdWordIndexTextBox.Text = ui16WordIndex.ToString();
                        this.EepromRobotIdTextBox.Text = ui32RxData.ToString("X8");

                        /* Intercept */
                        constructWordIndexAndDataContent(ref ui16WordIndex, ref ui32RxData, messageContent, dataPointer);
                        dataPointer += UNIT_STEP;
                        i32RxData = (Int32)ui32RxData; 
                        this.EepromInterceptWordIndexTextBox.Text = ui16WordIndex.ToString();
                        float fIntercept = (float)(i32RxData / 32768.0);
                        this.EepromInterceptTextBox.Text = fIntercept.ToString("0.0000");

                        /* Slope */
                        constructWordIndexAndDataContent(ref ui16WordIndex, ref ui32RxData, messageContent, dataPointer);
                        dataPointer += UNIT_STEP;
                        i32RxData = (Int32)ui32RxData;
                        this.EepromSlopeWordIndexTextBox.Text = ui16WordIndex.ToString();
                        float fSlope = (float)(i32RxData / 32768.0);
                        this.EepromSlopeTextBox.Text = fSlope.ToString("0.0000");

                        /* Random W */
                        constructWordIndexAndDataContent(ref ui16WordIndex, ref ui32RxData, messageContent, dataPointer);
                        dataPointer += UNIT_STEP;
                        this.EepromRandomSequencesWordIndexTextBox.Text = ui16WordIndex.ToString();
                        this.EepromRandom7TextBox.Text = Convert.ToString((ui32RxData >> 28) & 0x0F);
                        this.EepromRandom6TextBox.Text = Convert.ToString((ui32RxData >> 24) & 0x0F);
                        this.EepromRandom5TextBox.Text = Convert.ToString((ui32RxData >> 20) & 0x0F);
                        this.EepromRandom4TextBox.Text = Convert.ToString((ui32RxData >> 16) & 0x0F);
                        this.EepromRandom3TextBox.Text = Convert.ToString((ui32RxData >> 12) & 0x0F);
                        this.EepromRandom2TextBox.Text = Convert.ToString((ui32RxData >> 8) & 0x0F);
                        this.EepromRandom1TextBox.Text = Convert.ToString((ui32RxData >> 4) & 0x0F);
                        this.EepromRandom0TextBox.Text = Convert.ToString((ui32RxData) & 0x0F);

                        /* Motor parameters */
                        constructWordIndexAndDataContent(ref ui16WordIndex, ref ui32RxData, messageContent, dataPointer);
                        dataPointer += UNIT_STEP;
                        this.EepromMotor1TextBox.Text = Convert.ToString((ui32RxData) & 0xFF);
                        this.EepromMotor2TextBox.Text = Convert.ToString((ui32RxData >> 8) & 0xFF);
                        this.EepromMotorDelayTextBox.Text = Convert.ToString((ui32RxData >> 16) & 0xFFFF);

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
            MessageBoxResult result = MessageBox.Show("This action will change robot's EEPROM data!", "Do you want to continue", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.No)
                return;

            // <DataNum><2b word index><4b data><2b word index><4b data>...<2b word index><4b data>
            try
            {
                UInt16 ui16WordIndex;
                UInt32 ui32Data;
                Int32 i32Data;
                float fData;

                byte unit = 5;
                const byte UNIT_STEP = 6;
                byte[] data = new byte[unit * UNIT_STEP + 1];
           
                data[0] = unit;

                UInt32 dataPointer = 1;

                /* 1 */
                ui16WordIndex = Convert.ToUInt16(this.EepromRobotIdWordIndexTextBox.Text);
                ui32Data = getAddress(this.EepromRobotIdTextBox.Text, 8);
                fillPairIndexAndWordToByteArray(ui16WordIndex, ui32Data, data, dataPointer);
                dataPointer += UNIT_STEP;

                /* 2 */
                ui16WordIndex = Convert.ToUInt16(this.EepromInterceptWordIndexTextBox.Text);
                float.TryParse(this.EepromInterceptTextBox.Text, out fData);
                i32Data = (Int32)(fData * 32768);
                ui32Data = (UInt32)i32Data;
                fillPairIndexAndWordToByteArray(ui16WordIndex, ui32Data, data, dataPointer);
                dataPointer += UNIT_STEP;

                /* 3 */
                ui16WordIndex = Convert.ToUInt16(this.EepromSlopeWordIndexTextBox.Text);
                float.TryParse(this.EepromSlopeTextBox.Text, out fData);
                i32Data = (Int32)(fData * 32768);
                ui32Data = (UInt32)i32Data;
                fillPairIndexAndWordToByteArray(ui16WordIndex, ui32Data, data, dataPointer);
                dataPointer += UNIT_STEP;

                /* 4 */
                ui16WordIndex = Convert.ToUInt16(this.EepromRandomSequencesWordIndexTextBox.Text);
                ui32Data = (UInt32)((Byte.Parse(this.EepromRandom0TextBox.Text)) |
                                    ((Byte.Parse(this.EepromRandom1TextBox.Text)) << 4) |
                                    ((Byte.Parse(this.EepromRandom2TextBox.Text)) << 8) |
                                    ((Byte.Parse(this.EepromRandom3TextBox.Text)) << 12) |
                                    ((Byte.Parse(this.EepromRandom4TextBox.Text)) << 16) |
                                    ((Byte.Parse(this.EepromRandom5TextBox.Text)) << 20) |
                                    ((Byte.Parse(this.EepromRandom6TextBox.Text)) << 24) |
                                    ((Byte.Parse(this.EepromRandom7TextBox.Text)) << 28));
                fillPairIndexAndWordToByteArray(ui16WordIndex, ui32Data, data, dataPointer);
                dataPointer += UNIT_STEP;

                /* 5 */
                ui16WordIndex = Convert.ToUInt16(this.EepromMotorWordIndexTextBox.Text);

                ui32Data = (UInt32)((Byte.Parse(this.EepromMotor1TextBox.Text)) |
                                    ((Byte.Parse(this.EepromMotor2TextBox.Text)) << 8) |
                                    ((UInt16.Parse(this.EepromMotorDelayTextBox.Text)) << 16));
                fillPairIndexAndWordToByteArray(ui16WordIndex, ui32Data, data, dataPointer);
                dataPointer += UNIT_STEP;

                SwarmMessageHeader header = new SwarmMessageHeader(e_MessageType.MESSAGE_TYPE_HOST_COMMAND, COMMAND_EEPROM_DATA_WRITE);
                SwarmMessage message = new SwarmMessage(header, data);

                if (theControlBoard.sendMessageToRobot(message))
                    setStatusBarContent("EEPROM Data is synchronized!");
                else
                    setStatusBarContent("Failed to synchronized EEPROM...");
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
            MessageBoxResult result = MessageBox.Show("This action will change robot's EEPROM data!", "Do you want to continue", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.No)
                return;

            assignTaskForBackgroundWorker((Button)sender, "Cancel Update");
        }

        private void EepromTableVerifyButton_Click(object sender, RoutedEventArgs e)
        {
            assignTaskForBackgroundWorker((Button)sender, "Cancel Verify");
        }

        private void EepromRandomGenButton_Click(object sender, RoutedEventArgs e)
        {
            List<int> listRandomValues = GenerateRandom(8, 0, 15);
            int[] randomBuffer = listRandomValues.ToArray();
            EepromRandom7TextBox.Text = Convert.ToString(randomBuffer[7]);
            EepromRandom6TextBox.Text = Convert.ToString(randomBuffer[6]);
            EepromRandom5TextBox.Text = Convert.ToString(randomBuffer[5]);
            EepromRandom4TextBox.Text = Convert.ToString(randomBuffer[4]);
            EepromRandom3TextBox.Text = Convert.ToString(randomBuffer[3]);
            EepromRandom2TextBox.Text = Convert.ToString(randomBuffer[2]);
            EepromRandom1TextBox.Text = Convert.ToString(randomBuffer[1]);
            EepromRandom0TextBox.Text = Convert.ToString(randomBuffer[0]);
        }
        private List<int> GenerateRandom(int count, int min, int max)
        {
            Random random = new Random((int)DateTime.Now.Ticks);

            //  initialize set S to empty
            //  for J := N-M + 1 to N do
            //    T := RandInt(1, J)
            //    if T is not in S then
            //      insert T in S
            //    else
            //      insert J in S
            //
            // adapted for C# which does not have an inclusive Next(..)
            // and to make it from configurable range not just 1.

            if (max <= min || count < 0 ||
                // max - min > 0 required to avoid overflow
                    (count > max - min && max - min > 0))
            {
                // need to use 64-bit to support big ranges (negative min, positive max)
                throw new ArgumentOutOfRangeException("Range " + min + " to " + max +
                        " (" + ((Int64)max - (Int64)min) + " values), or count " + count + " is illegal");
            }

            // generate count random values.
            HashSet<int> candidates = new HashSet<int>();

            // start count values before max, and end at max
            for (int top = max - count; top < max; top++)
            {
                // May strike a duplicate.
                // Need to add +1 to make inclusive generator
                // +1 is safe even for MaxVal max value because top < max
                if (!candidates.Add(random.Next(min, top + 1)))
                {
                    // collision, add inclusive max.
                    // which could not possibly have been added before.
                    candidates.Add(top);
                }
            }

            // load them in to a list, to sort
            List<int> result = candidates.ToList();

            // shuffle the results because HashSet has messed
            // with the order, and the algorithm does not produce
            // random-ordered results (e.g. max-1 will never be the first value)
            for (int i = result.Count - 1; i > 0; i--)
            {
                int k = random.Next(i + 1);
                int tmp = result[k];
                result[k] = result[i];
                result[i] = tmp;
            }
            return result;
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
            const byte TOTAL_BLOCK = 12; // 1->6 in Sine Table; 7->12 is ArcSine Table
            const byte NUMBER_OF_WORD = 16;
            byte currentBlock = 1;
            byte[] data = new byte[1 + 4];
            data[0] = NUMBER_OF_WORD;

            UInt32 bufferLength = 2 + 1 + 4 + 4 * NUMBER_OF_WORD;
            byte[] dataBuffer = new byte[bufferLength];
            UInt32 rxAddress;

            try
            {
                for (currentBlock = 1; currentBlock <= TOTAL_BLOCK; currentBlock++)
                {
                    if (backgroundWorker.CancellationPending)
                    {
                        e.Cancel = true;
                        break;
                    }

                    UInt32 address = listTupeEepromTable[currentBlock - 1].Item1;

                    // fill Address
                    data[1] = (byte)(address >> 24);
                    data[2] = (byte)(address >> 16);
                    data[3] = (byte)(address >> 8);
                    data[4] = (byte)address;

                    SwarmMessageHeader header = new SwarmMessageHeader(e_MessageType.MESSAGE_TYPE_HOST_COMMAND, COMMAND_EEPROM_DATA_READ_BULK);
                    SwarmMessage message = new SwarmMessage(header, data);

                    theControlBoard.receivedDataFromRobot(dataBuffer, bufferLength, 1000, message);

                    SwarmMessage rxMessage = SwarmMessage.ConstructFromByteArray(dataBuffer);

                    byte[] messageContent;

                    if (rxMessage.getHeader().getMessageType() == e_MessageType.MESSAGE_TYPE_ROBOT_RESPONSE
                        && rxMessage.getHeader().getCmd() == ROBOT_RESPONSE_TO_HOST_OK)
                    {
                        messageContent = rxMessage.getData();
                        UInt16[] pui16TableContent = listTupeEepromTable[currentBlock - 1].Item2;

                        rxAddress = (UInt32)((messageContent[1] << 24) | (messageContent[2] << 16) | 
                                    (messageContent[3] << 8) | messageContent[4]);

                        if (messageContent[0] == NUMBER_OF_WORD && rxAddress == listTupeEepromTable[currentBlock - 1].Item1)
                        { 
                            UInt16[] referenceContent = listTupeEepromTable[currentBlock - 1].Item2;
                            UInt32 contentPointer = 5;
                            for (int i = 0; i < referenceContent.Length; i++)
                            {
                                if ((messageContent[contentPointer] != (referenceContent[i] & 0xFF)) ||
                                    (messageContent[contentPointer + 1] != ((referenceContent[i] >> 8)& 0xFF)))
                                {
                                    if (currentBlock > (TOTAL_BLOCK / 2))
                                        e.Result = e_VerifyTableReturn.SINE_OK_ARCSINE_ERROR;
                                    else
                                        e.Result = e_VerifyTableReturn.SINE_ERROR_ARCSINE_ERROR;
                                    return;
                                }

                                contentPointer += 2;
                            }
                        }
                        else
                        {
                            break;
                        }
  
                        backgroundWorker.ReportProgress(currentBlock * 100 / TOTAL_BLOCK);

                        System.Threading.Thread.Sleep(500);
                    }
                    else
                    {
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                defaultExceptionHandle(ex);
            }

            if (currentBlock >= TOTAL_BLOCK)
                e.Result = e_VerifyTableReturn.SINE_OK_ARCSINE_OK;
            else
                e.Result = e_VerifyTableReturn.SINE_ERROR_ARCSINE_ERROR;
        }
        private void tableProgramming_DoWork(object sender, DoWorkEventArgs e)
        {
            const byte TOTAL_BLOCK = 12; // 1->6 in Sine Table; 7->12 is ArcSine Table
            const byte NUMBER_OF_WORD = 16;
            byte currentBlock = 1;
            byte[] data = new byte[1 + 4 + 4 * NUMBER_OF_WORD];
            data[0] = NUMBER_OF_WORD;

            for (currentBlock = 1; currentBlock <= TOTAL_BLOCK; currentBlock++)
            {
                if (backgroundWorker.CancellationPending)
                {
                    e.Cancel = true;
                    break;
                }

                UInt32 address = listTupeEepromTable[currentBlock - 1].Item1;
                UInt16[] pui16Data = listTupeEepromTable[currentBlock - 1].Item2;

                fillStartAddressAndWordContent(address, pui16Data, data, 1);

                SwarmMessageHeader header = new SwarmMessageHeader(e_MessageType.MESSAGE_TYPE_HOST_COMMAND, COMMAND_EEPROM_DATA_WRITE_BULK);
                SwarmMessage message = new SwarmMessage(header, data);

                if (!theControlBoard.sendMessageToRobot(message))
                {
                    if (currentBlock > (TOTAL_BLOCK / 2))
                        e.Result = e_VerifyTableReturn.SINE_OK_ARCSINE_ERROR;
                    else
                        e.Result = e_VerifyTableReturn.SINE_ERROR_ARCSINE_ERROR;

                    return;
                }

                backgroundWorker.ReportProgress(currentBlock  * 100 / TOTAL_BLOCK);

                System.Threading.Thread.Sleep(500);
            }

            if(currentBlock >= TOTAL_BLOCK)
                e.Result = e_VerifyTableReturn.SINE_OK_ARCSINE_OK;
            else
                e.Result = e_VerifyTableReturn.SINE_ERROR_ARCSINE_ERROR;
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

        private void fillStartAddressAndWordContent(UInt32 address, UInt16[] pui16ContentBuffer, byte[] data, uint offset)
        {
            // fill Address
            data[offset++] = (byte)(address >> 24);
            data[offset++] = (byte)(address >> 16);
            data[offset++] = (byte)(address >> 8);
            data[offset++] = (byte)address;
           
            // fill Word content
            for (int i = 0; i < pui16ContentBuffer.Length; i++)
            {
                data[offset++] = (byte)(pui16ContentBuffer[i] & 0xFF);
                data[offset++] = (byte)((pui16ContentBuffer[i] >> 8) & 0xFF);
            }
        }

        #endregion

        #region Calibration Tab

        #region TDOA
        private const int START_SAMPLES_POSTITION = 32;
        private const int FILTER_ORDER = 34;
        private float VOLT = 3.3f / (float)Math.Pow(2, 12);

        private float[] FilterCoefficient = new float[FILTER_ORDER] { -0.003472f, 0.000573f, 0.006340f, 0.014220f, 0.022208f, 0.025940f, 0.020451f, 0.002990f, -0.024527f, -0.054834f, -0.077140f, -0.081033f, -0.060950f, -0.019268f, 0.033526f, 0.081934f, 0.110796f, 0.110796f, 0.081934f, 0.033526f, -0.019268f, -0.060950f, -0.081033f, -0.077140f, -0.054834f, -0.024527f, 0.002990f, 0.020451f, 0.025940f, 0.022208f, 0.014220f, 0.006340f, 0.000573f, -0.003472f };

        private void filterAndPlotResults(string title, uint[] data)
        {
            float[] fdata = new float[data.Length];
            for (int j = 0; j < fdata.Length; j++)
            {
                fdata[j] = data[j] / 1.0f;
            }

            float[] fFilteredData = filtering(fdata, FilterCoefficient);

            float[] filteredPlotData = new float[data.Length - START_SAMPLES_POSTITION];
            for (int j = 0; j < filteredPlotData.Length; j++)
            {
                filteredPlotData[j] = fFilteredData[j + START_SAMPLES_POSTITION] * VOLT;
            }

            double peakEnvelope = 0;
            double maxEnvelope = 0;
            getDistance(fFilteredData, ref peakEnvelope, ref maxEnvelope);

            string output = String.Format("Peak = {0}, Max = {1}", peakEnvelope, maxEnvelope * VOLT);

            OxyplotWindowTwoChart oxyplotWindowTwoChart = new OxyplotWindowTwoChart(title, "Sampling Data", fdata, "Filtered Data: " + output, filteredPlotData, OxyplotWindowTwoChart.PolylineMonoY);
            oxyplotWindowTwoChart.Show();
        }

        private float[] filtering(float[] data, float[] H)
        {
            float[] output = new float[data.Length + H.Length - 1];
            for (uint n = 0; n < data.Length + H.Length - 1; n++)
            {
                uint kmin, kmax, k;
                output[n] = 0;
                kmin = (n >= H.Length - 1) ? (uint)(n - (H.Length - 1)) : (0);
                kmax = (n < data.Length - 1) ? (n) : (uint)(data.Length - 1);
                for (k = kmin; k <= kmax; k++)
                {
                    output[n] += data[k] * H[n - k];
                }
            }
            float[] outputCutoff = new float[data.Length];
            for (int i = 0; i < outputCutoff.Length; i++)
            {
                outputCutoff[i] = output[i ];
            }
            return outputCutoff;
        }

        private void getDistance(float[] fData, ref double peakEnvelope, ref double maxEnvelope)
        { 
        	float step = 0.125f;

	        double[] localPeaksPosition = new double[3]{ 0, 0, 0 };
            double[] localMaxValue = new double[3]{ 0, 0, 0 };

            float[] fTemp = new float[fData.Length - START_SAMPLES_POSTITION];
            for (int j = 0; j < fTemp.Length; j++)
            {
                fTemp[j] = fData[j + START_SAMPLES_POSTITION - 1];
            }
            fData = fTemp;

            find3LocalPeaks(fData, localPeaksPosition);
	        if (localPeaksPosition[0] != 0 && localPeaksPosition[1] != 0 && localPeaksPosition[2] != 0) {
                double[] PositionsArray = new double[3]{ 0, 0, 0 };
                double[] ValuesArray = new double[3]{ 0, 0, 0 };
		        int i;
		        for (i = 0; i < 3; i++) {
			        PositionsArray[0] = localPeaksPosition[i] - 1;
			        PositionsArray[1] = localPeaksPosition[i];
			        PositionsArray[2] = localPeaksPosition[i] + 1;
                    ValuesArray[0] = fData[(int)PositionsArray[0]];
                    ValuesArray[1] = fData[(int)PositionsArray[1]]; 
                    ValuesArray[2] = fData[(int)PositionsArray[2]];
                    localMaxValue[i] = fData[(int)localPeaksPosition[i]]; 
			        interPeak(PositionsArray, ValuesArray, localPeaksPosition[i], localMaxValue[i], step, ref localPeaksPosition[i], ref localMaxValue[i]);
		        }
		        interPeak(localPeaksPosition, localMaxValue, localPeaksPosition[1], localMaxValue[1], step, ref peakEnvelope, ref maxEnvelope);
                peakEnvelope = peakEnvelope + START_SAMPLES_POSTITION;
	        }
        }
        private void find3LocalPeaks(float[] fData, double[] LocalPeaksStoragePointer)
        {
            int SamplePosition = 0;

            int maxSamplePosition = 0;
            int i;
            for (i = START_SAMPLES_POSTITION; i < fData.Length; i++)
            {
                if (fData[i] > fData[maxSamplePosition])
                {
                    maxSamplePosition = i;
                }
            }
            LocalPeaksStoragePointer[1] = maxSamplePosition;

            SamplePosition = reachBottom(fData, (int)LocalPeaksStoragePointer[1], -1);
            if (SamplePosition != 0)
            {
                LocalPeaksStoragePointer[0] = reachPeak(fData, SamplePosition, -1);
            }

            SamplePosition = reachBottom(fData, (int)LocalPeaksStoragePointer[1], 1);
            if (SamplePosition != 0)
            {
                LocalPeaksStoragePointer[2] = reachPeak(fData, SamplePosition, 1);
            }
        }
        private int reachBottom(float[] fData, int PeakPosition, int PointerIncreaseNumber) 
        {
	        int SamplePosition = PeakPosition;

            while (SamplePosition > 1 && SamplePosition < fData.Length)
            {
		        if (fData[SamplePosition] < fData[SamplePosition + PointerIncreaseNumber]) 
                {
			        return SamplePosition;
		        }
		        else 
                {
			        SamplePosition += PointerIncreaseNumber;
		        }
	        }

	        return 0;
        }
        private int reachPeak(float[] fData, int PeakPosition, int PointerIncreaseNumber)
        {
	        int SamplePosition = PeakPosition;

            while (SamplePosition > 1 && SamplePosition < fData.Length)
            {
		        if (fData[SamplePosition] > fData[SamplePosition + PointerIncreaseNumber]) 
                {
			        return SamplePosition;
		        }
		        else 
                {
			        SamplePosition += PointerIncreaseNumber;
		        }
	        }

	        return 0;
        }
        private void interPeak(double[] PositionsArray, double[] ValuesArray, double UserPosition, double UserMaxValue, float step, ref double ReturnPosition, ref double ReturnValue) 
        {
	        double realLocalPeak = UserPosition;
	        double realLocalMax = UserMaxValue;
            double samplePosition = realLocalPeak - step;
            double interpolateValue = larange(PositionsArray, ValuesArray, samplePosition);
	        int PointerDirection = 0;
	        if (interpolateValue > UserMaxValue) 
            {
		        PointerDirection = -1;
		        realLocalPeak = samplePosition;
		        realLocalMax = interpolateValue;
	        }
	        else 
            {
		        PointerDirection = 1;
	        }
	        bool flag = true;
	        while (flag) 
            {
		        samplePosition = realLocalPeak + step*PointerDirection;
		        interpolateValue = larange(PositionsArray, ValuesArray, samplePosition);
		        if (interpolateValue >= realLocalMax) 
                {
			        realLocalMax = interpolateValue;
			        realLocalPeak = samplePosition;
		        }
		        else 
                {
			        ReturnPosition = realLocalPeak;
			        ReturnValue = realLocalMax;
			        flag = false;
		        }
	        }
        }
        private double larange(double[] PositionsArray, double[] ValuesArray, double interpolatePoint) 
        {
	        double result = 0;
	        int i, j;
            double temp;
	        for (j = 0; j < 3; j++) {
		        temp = 1;
		        for (i = 0; i < 3; i++) {
			        if (i != j)
                        temp = (interpolatePoint - PositionsArray[i]) * temp / (PositionsArray[j] - PositionsArray[i]);
		        }
		        result = result + (ValuesArray[j] * temp);
	        }
	        return result;
        }
        #endregion

        #region Basic calibration
        private void ConfigureRFCalibration_Click(object sender, RoutedEventArgs e)
        {
            setTxAddress(this.TXAddressCalibrationSelectBox.Text);
            setStatusBarContent("Set RF Tx Address: " + this.TXAddressCalibrationSelectBox.Text);
        }
        
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
                        theControlBoard.broadcastCommandToRobot(COMMAND_TOGGLE_LEDS);
                        break;

                    case "Start Sampling Mics Signals":
                        theControlBoard.broadcastCommandToRobot(COMMAND_SAMPLE_MICS_SIGNALS);
                        break;

                    case "Test Speaker":
                        theControlBoard.broadcastCommandToRobot(COMMAND_TEST_SPEAKER);
                        break;

                    case "Change Motors Speed":
                        testMotorsConfiguration(COMMAND_CHANGE_MOTOR_SPEED);
                        break;

                    case "Read Battery Voltage":
                        readBatteryVoltage();
                        break;

                    case "Indicate Battery Voltage":
                        theControlBoard.broadcastCommandToRobot(COMMAND_INDICATE_BATT_VOLT);
                        break;

                    case "Toggle IR Led":
                        theControlBoard.broadcastCommandToRobot(COMMAND_TOGGLE_IR_LED);
                        break;

                    case "Read IR Proximity Raw":
                        readIrProximityRaw();
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

            SwarmMessageHeader header = new SwarmMessageHeader(e_MessageType.MESSAGE_TYPE_HOST_COMMAND, COMMAND_TEST_RF_TRANSMISTER);
            SwarmMessage requestMessage = new SwarmMessage(header, commandContent);

            byte[] receivedData = new byte[length];
            UInt16 data = new UInt16();
            UInt16 value = 0;
            try
            {
                theControlBoard.receivedDataFromRobot(receivedData, length, 1000, requestMessage);
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
                UInt32 length = 600;

                byte[] commandContent = new byte[4];
                commandContent[0] = (byte)((length >> 24) & 0x0FF);
                commandContent[1] = (byte)((length >> 16) & 0x0FF);
                commandContent[2] = (byte)((length >> 8) & 0x0FF);
                commandContent[3] = (byte)(length & 0x0FF);

                SwarmMessageHeader header = new SwarmMessageHeader(e_MessageType.MESSAGE_TYPE_HOST_COMMAND, commandNumber);
                SwarmMessage message = new SwarmMessage(header, commandContent);

                theControlBoard.sendMessageToRobot(message);

                byte[] data = new byte[length];
                byte value = 0;
                for (int i = 0; i < length; i++)
                    data[i] = value++;

                theControlBoard.broadcastDataToRobot(data);

                uint bufferLength = 2;
                byte[] dataBuffer = new byte[bufferLength];
                theControlBoard.tryToReceivedDataFromRobot(dataBuffer, bufferLength, 1000);
                SwarmMessage rxMessage = SwarmMessage.ConstructFromByteArray(dataBuffer);
                if (rxMessage.getHeader().getMessageType() == e_MessageType.MESSAGE_TYPE_ROBOT_RESPONSE
                    && rxMessage.getHeader().getCmd() == ROBOT_RESPONSE_TO_HOST_OK)
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
                    data[0] = (byte)e_MotorDirection.MOTOR_REVERSE_DIRECTION;
                else
                    data[0] = (byte)e_MotorDirection.MOTOR_FORWARD_DIRECTION;

                data[1] = Convert.ToByte(this.motor1SpeedTextBox.Text);

                if (motor2ReverseCheckBox.IsChecked == true)
                    data[2] = (byte)e_MotorDirection.MOTOR_REVERSE_DIRECTION;
                else
                    data[2] = (byte)e_MotorDirection.MOTOR_FORWARD_DIRECTION;

                data[3] = Convert.ToByte(this.motor2SpeedTextBox.Text);

                SwarmMessageHeader header = new SwarmMessageHeader(e_MessageType.MESSAGE_TYPE_HOST_COMMAND, Command);

                SwarmMessage message = new SwarmMessage(header, data);

                theControlBoard.broadcastMessageToRobot(message);
            }
            catch (Exception ex)
            {
                throw new Exception("Test Motors Configuration " + ex.Message);
            }
        }

        private void readAdc1Button_Click(object sender, RoutedEventArgs e)
        {
            readADC(COMMAND_READ_ADC1, this.readAdc1TextBox, "Robot[0x" + this.TXAddressCalibrationSelectBox.Text + "]'s Mic 1");
            setStatusBarContent("Read ADC1 successful!");
        }

        private void readAdc2Button_Click(object sender, RoutedEventArgs e)
        {
            readADC(COMMAND_READ_ADC2, this.readAdc2TextBox, "Robot[0x" + this.TXAddressCalibrationSelectBox.Text + "]'s Mic 2");
            setStatusBarContent("Read ADC2 successful!");
        }

        private void readADC(byte Command, TextBox tBox, string comment)
        {
            uint length = 600;
            byte[] receivedData = new byte[length];
            uint[] adcData = new uint[length / 2];
            tBox.Text = "";
            try
            {
                SwarmMessageHeader header = new SwarmMessageHeader(e_MessageType.MESSAGE_TYPE_HOST_COMMAND, Command);
                SwarmMessage message = new SwarmMessage(header);

                theControlBoard.receivedDataFromRobot(receivedData, length, 1000, message);

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

                filterAndPlotResults(comment, adcData);
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
                SwarmMessageHeader header = new SwarmMessageHeader(e_MessageType.MESSAGE_TYPE_HOST_COMMAND, COMMAND_REQUEST_BATTERY_VOLT);
                SwarmMessage message = new SwarmMessage(header);

                theControlBoard.receivedDataFromRobot(receivedData, length, 1000, message);

                SwarmMessage rxMessage = SwarmMessage.ConstructFromByteArray(receivedData);
                if (rxMessage.getHeader().getMessageType() == e_MessageType.MESSAGE_TYPE_ROBOT_RESPONSE
                    && rxMessage.getHeader().getCmd() == ROBOT_RESPONSE_TO_HOST_OK)
                {
                    adcData = (rxMessage.getData()[1] << 8) | rxMessage.getData()[0];
                    BatteryVoltage = (2 * adcData) * 3330 / 4096;
                    string mess = BatteryVoltage.ToString() + "mV (ADCvalue = " + adcData.ToString() + ")";
                    setStatusBarContent("Robot's V_batt = " + mess);
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

        private void readIrProximityRaw()
        {
            uint length = 4;
            Byte[] receivedData = new Byte[length];
            int adcData;
            float IrProximityRaw;

            try
            {
                SwarmMessageHeader header = new SwarmMessageHeader(e_MessageType.MESSAGE_TYPE_HOST_COMMAND, COMMAND_REQUEST_PROXIMITY_RAW);
                SwarmMessage message = new SwarmMessage(header);

                theControlBoard.receivedDataFromRobot(receivedData, length, 1000, message);

                SwarmMessage rxMessage = SwarmMessage.ConstructFromByteArray(receivedData);
                if (rxMessage.getHeader().getMessageType() == e_MessageType.MESSAGE_TYPE_ROBOT_RESPONSE
                    && rxMessage.getHeader().getCmd() == ROBOT_RESPONSE_TO_HOST_OK)
                {
                    adcData = (rxMessage.getData()[1] << 8) | rxMessage.getData()[0];
                    IrProximityRaw = adcData * 3330 / 4096;
                    string mess = IrProximityRaw.ToString() + "mV (ADCvalue = " + adcData.ToString() + ")";
                    setStatusBarContent("Robot's Ir Proximity Raw = " + mess);
                }
                else
                {
                    setStatusBarContent("Wrong Ir Proximity Raw response from robot...");
                }

            }
            catch (Exception ex)
            {
                //defaultExceptionHandle(ex);
                setStatusBarContent("Failed to read robot's Ir Proximity Raw...");
            }
        }

        private void stopMotor1Button_Click(object sender, RoutedEventArgs e)
        {
            theControlBoard.broadcastCommandToRobot(COMMAND_STOP_MOTOR1);
            setStatusBarContent("Broadcast Command: Pause left motor");
        }

        private void stopMotor2Button_Click(object sender, RoutedEventArgs e)
        {
            theControlBoard.broadcastCommandToRobot(COMMAND_STOP_MOTOR2);
            setStatusBarContent("Broadcast Command: Pause right motor");
        }

        private void rotateButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                bool bIsClockwise = this.motorRotateClockwiseCheckBox.IsChecked == true;
                string direction = (bIsClockwise) ? ("CW") : ("CCW");
                UInt16 ui16PeriodMs = Convert.ToUInt16(this.motorRotateDelayTextBox.Text);

                Byte[] data = new Byte[3];

                data[0] = (bIsClockwise) ? ((Byte)e_RobotRotateDirection.ROBOT_ROTATE_CW) : ((Byte)e_RobotRotateDirection.ROBOT_ROTATE_CCW);
                data[1] = (Byte)((ui16PeriodMs >> 8) & 0xFF);
                data[2] = (Byte)(ui16PeriodMs & 0xFF);

                SwarmMessageHeader header = new SwarmMessageHeader(e_MessageType.MESSAGE_TYPE_HOST_COMMAND, COMMAND_ROTATE_WITH_PERIOD);
                SwarmMessage message = new SwarmMessage(header, data);
                theControlBoard.broadcastMessageToRobot(message);

                setStatusBarContent("Broadcast Command: rotate " + direction + " in " + ui16PeriodMs + "ms");
            }
            catch (Exception ex)
            {
                throw new Exception("Robot rotate " + ex.Message);
            }
        }

        private void moveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                bool bIsForward = this.motorMoveForwardCheckBox.IsChecked == true;
                string direction = (bIsForward) ? ("forward") : ("backward");
                UInt16 ui16PeriodMs = Convert.ToUInt16(this.motorMoveDelayTextBox.Text);

                Byte[] data = new Byte[3];

                data[0] = (bIsForward) ? ((Byte)e_RobotMoveDirection.ROBOT_MOVE_FORWARD) : ((Byte)e_RobotMoveDirection.ROBOT_MOVE_BACKWARD);
                data[1] = (Byte)((ui16PeriodMs >> 8) & 0xFF);
                data[2] = (Byte)(ui16PeriodMs & 0xFF);

                SwarmMessageHeader header = new SwarmMessageHeader(e_MessageType.MESSAGE_TYPE_HOST_COMMAND, COMMAND_MOVE_WITH_PERIOD);
                SwarmMessage message = new SwarmMessage(header, data);
                theControlBoard.broadcastMessageToRobot(message);

                setStatusBarContent("Broadcast Command: move " + direction + " in " + ui16PeriodMs + "ms");
            }
            catch (Exception ex)
            {
                throw new Exception("Robot move " + ex.Message);
            }         
        }
        #endregion

        #region Testing Controllers
        private void ConfigStepRotateControllerButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                int length = 6;
                byte[] messageContent = new byte[length];

                /* Activation period */
                messageContent[0] = Convert.ToByte(this.StepActivateInMsTextBox.Text);

                /* Pause period */
                messageContent[1] = Convert.ToByte(this.StepPauseInMsTextBox.Text);

                /* Ref Angle */
                float fData;
                float.TryParse(this.AngleRefTextBox.Text, out fData);
                Int32 i32Data = (Int32)(fData * 65536);
                parse32bitTo4Bytes(messageContent, 2, i32Data);

                SwarmMessageHeader header = new SwarmMessageHeader(e_MessageType.MESSAGE_TYPE_HOST_COMMAND, COMMAND_CONFIG_STEP_CONTROLLER);
                SwarmMessage message = new SwarmMessage(header, messageContent);

                theControlBoard.broadcastMessageToRobot(message);
                setStatusBarContent("Step: broadcast command Rotate To Angle " + this.AngleRefTextBox.Text + " done!");
            }
            catch (Exception ex)
            {
                throw new Exception("Set Step Button: " + ex.Message);
            }
        }

        private void ConfigStepForwardInPeriodControllerButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                int length = 6;
                byte[] messageContent = new byte[length];

                /* Active ms Motor Left */
                messageContent[0] = Convert.ToByte(this.FwActiveLeftMsTextBox.Text);

                /* Active ms Motor Right */
                messageContent[1] = Convert.ToByte(this.FwActiveRightMsTextBox.Text);

                /* Forward delay ms */
                Int32 i32Data = Convert.ToInt32(this.FwDelayMsTextBox.Text);
                parse32bitTo4Bytes(messageContent, 2, i32Data);

                SwarmMessageHeader header = new SwarmMessageHeader(e_MessageType.MESSAGE_TYPE_HOST_COMMAND, COMMAND_CONFIG_STEP_FORWARD_IN_PERIOD_CONTROLLER);
                SwarmMessage message = new SwarmMessage(header, messageContent);

                theControlBoard.broadcastMessageToRobot(message);
                setStatusBarContent("Boardcast command step forward in period controller susscess!");
            }
            catch (Exception ex)
            {
                throw new Exception("Set Step Forward Button: " + ex.Message);
            }
        }

        private void ConfigStepForwardInRotateControllerButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                int length = 7;
                byte[] messageContent = new byte[length];

                /* Motors Activate ms */
                messageContent[0] = Convert.ToByte(this.FwRtMotorsActivateMsTextBox.Text);

                /* Motors Pause ms */
                messageContent[1] = Convert.ToByte(this.FwRtMotorsPauseMsTextBox.Text);

                /* Step Count */
                messageContent[2] = Convert.ToByte(this.FwRtStepCountTextBox.Text);

                /* Step Angle */
                float fData;
                float.TryParse(this.FwRtStepAngleTextBox.Text, out fData);
                Int32 i32Data = (Int32)(fData * 65536);
                parse32bitTo4Bytes(messageContent, 3, i32Data);

                SwarmMessageHeader header = new SwarmMessageHeader(e_MessageType.MESSAGE_TYPE_HOST_COMMAND, COMMAND_CONFIG_STEP_FORWARD_IN_ROTATE_CONTOLLER);
                SwarmMessage message = new SwarmMessage(header, messageContent);

                theControlBoard.broadcastMessageToRobot(message);
                setStatusBarContent("Boardcast command step forward in rotate controller susscess!");
            }
            catch (Exception ex)
            {
                throw new Exception("Set Step Forward Rotate Button: " + ex.Message);
            }
        }

        private void ConfigPIDControllerButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                int length = 12;
                byte[] messageContent = new byte[length];

                float fData;
                Int32 i32Data;

                /* kP */
                float.TryParse(this.GrainPTextBox.Text, out fData);
                i32Data = (Int32)(fData * 65536);
                parse32bitTo4Bytes(messageContent, 0, i32Data);

                /* kI */
                float.TryParse(this.GrainITextBox.Text, out fData);
                i32Data = (Int32)(fData * 65536);
                parse32bitTo4Bytes(messageContent, 4, i32Data);

                /* kD */
                float.TryParse(this.GrainDTextBox.Text, out fData);
                i32Data = (Int32)(fData * 65536);
                parse32bitTo4Bytes(messageContent, 8, i32Data);

                SwarmMessageHeader header = new SwarmMessageHeader(e_MessageType.MESSAGE_TYPE_HOST_COMMAND, COMMAND_CONFIG_PID_CONTROLLER);
                SwarmMessage message = new SwarmMessage(header, messageContent);

                theControlBoard.broadcastMessageToRobot(message);
                setStatusBarContent("PID: broadcast forward command done!");
            }
            catch (Exception ex)
            {
                throw new Exception("PID Button: " + ex.Message);
            }
        }
        #endregion

        #region Calibrate TDOA and Graph Region
        public const UInt32 TESTING_BUFFER_SIZE = 1024;

        public UInt16 g_ui16TDOABufferPointer1 = 0;
        public float[] g_fdisctanceResultMic1X = new float[TESTING_BUFFER_SIZE];
        public float[] g_fdisctanceResultMic1Y = new float[TESTING_BUFFER_SIZE];

        public UInt16 g_ui16TDOABufferPointer2 = 0;
        public float[] g_fdisctanceResultMic2X = new float[TESTING_BUFFER_SIZE];
        public float[] g_fdisctanceResultMic2Y = new float[TESTING_BUFFER_SIZE];

        public string fileNameMic1 = "";
        public string fileNameMic2 = "";

        private void testDistacneSensingButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (isUserConfirmCalibrateTDOA() == false)
                    return;

                const byte RF_ADDRESS_WIDTH = 4;
                UInt32 rxAddress = getAddress(micsRobotTextBox.Text, 2 * RF_ADDRESS_WIDTH);
                byte testTime = Convert.ToByte(testTimesTextBox.Text);
                UInt32 delay = Convert.ToUInt32(delayTestingTextBox.Text);

                setTxAddress(speakerRobotTextBox.Text);

                uint dataLength = 9;
                byte[] messageData = new byte[dataLength];

                messageData[0] = (byte)(rxAddress >> 24);
                messageData[1] = (byte)(rxAddress >> 16);
                messageData[2] = (byte)(rxAddress >> 8);
                messageData[3] = (byte)(rxAddress);
                messageData[4] = testTime;
                messageData[5] = (byte)(delay >> 24);
                messageData[6] = (byte)(delay >> 16);
                messageData[7] = (byte)(delay >> 8);
                messageData[8] = (byte)(delay);

                SwarmMessageHeader header = new SwarmMessageHeader(e_MessageType.MESSAGE_TYPE_HOST_COMMAND, COMMAND_CALIBRATE_TDOA_TX);
                SwarmMessage message = new SwarmMessage(header, messageData);

                uint bufferLength = (uint)testTime * 4;
                byte[] dataBuffer = new byte[bufferLength];
                theControlBoard.receivedDataFromRobot(dataBuffer, bufferLength, 5000 * (uint)testTime, message);

                uint length = (uint)testTime * 2;
                float[] Mic1 = new float[testTime];
                float[] Mic2 = new float[testTime];

                String fileContent1 = "";
                bool isNewData1 = false;

                String fileContent2 = "";
                bool isNewData2 = false;

                for (int i = 0; i < dataBuffer.Length; i += 4)
                {
                    Mic1[(int)(i / 4)] = (float)((UInt16)((dataBuffer[i] << 8) | dataBuffer[i + 1]) / 256.0);
                    fileContent1 += distanceTextBox.Text + " " + Mic1[(int)(i / 4)].ToString() + "\n";

                    Mic2[(int)(i / 4)] = (float)((UInt16)((dataBuffer[i + 2] << 8) | dataBuffer[i + 3]) / 256.0);
                    fileContent2 += distanceTextBox.Text + " " + Mic2[(int)(i / 4)].ToString() + "\n";
                }

                float avr1 = 0, avr2 = 0;
                float maxMic1 = 0, minMic1 = 500;
                float maxMic2 = 0, minMic2 = 500;

                for (int i = 0; i < testTime; i++)
                {
                    avr1 += Mic1[i];
                    avr2 += Mic2[i];

                    maxMic1 = (maxMic1 >= Mic1[i]) ? (maxMic1) : (Mic1[i]);
                    minMic1 = (minMic1 <= Mic1[i]) ? (minMic1) : (Mic1[i]);

                    maxMic2 = (maxMic2 >= Mic2[i]) ? (maxMic2) : (Mic2[i]);
                    minMic2 = (minMic2 <= Mic2[i]) ? (minMic2) : (Mic2[i]);
                }

                if (minMic1 == 0 || minMic2 == 0)
                {
                    MessageBox.Show("Command transmition failed! Please try again...\n", "RF false", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                avr1 = (float)(avr1 / 10.0);
                avr2 = (float)(avr2 / 10.0);

                String mess = String.Format("Mic1 = {0} [{1}; {2}], var = {3} \nMic2 = {4} [{5}; {6}], var = {7}", avr1, minMic1, maxMic1, maxMic1 - minMic1, avr2, minMic2, maxMic2, maxMic2 - minMic2);

                MessageBoxResult result;

                if ((maxMic1 - minMic1) > 5 || (maxMic2 - minMic2) > 5)
                {
                    result = MessageBox.Show("Bad result! Do you want to keep append this data?\n" + mess, "oh...Sorry :(", MessageBoxButton.YesNo, MessageBoxImage.Error);
                    if (result == MessageBoxResult.Yes)
                    {
                        isNewData1 = true;
                        isNewData2 = true;
                    }
                    else
                    {
                        isNewData1 = false;
                        isNewData2 = false;
                    }
                }
                else if ((maxMic1 - minMic1) > 3 || (maxMic2 - minMic2) > 3)
                {
                    result = MessageBox.Show("Bad result! Do you want to keep append this data?\n" + mess, "oh...Sorry :(", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                    if (result == MessageBoxResult.Yes)
                    {
                        isNewData1 = true;
                        isNewData2 = true;
                    }
                    else
                    {
                        isNewData1 = false;
                        isNewData2 = false;
                    }
                }
                else
                {
                    MessageBox.Show("Good result:\n" + mess, "Nice :)", MessageBoxButton.OK, MessageBoxImage.Information);
                    isNewData1 = true;
                    isNewData2 = true;
                }

                if (isNewData1)
                    File.AppendAllText(@fileNameMic1, fileContent1);

                if (isNewData2)
                    File.AppendAllText(@fileNameMic2, fileContent2);
            }
            catch (Exception ex)
            {
                defaultExceptionHandle(ex);
            }
            finally
            {
                setTxAddress(this.TXAddressCalibrationSelectBox.Text);
                setStatusBarContent("Set Tx Address to " + this.TXAddressCalibrationSelectBox.Text);
            }
        }

        private bool isUserConfirmCalibrateTDOA()
        {
            MessageBoxResult result;

            result = MessageBox.Show("WATCHOUT:\nIf you want to create a new storage for TDOA testing result, click YES to confirm! \nClick NO to append data to the current file!",
                                                  "Create a new file?", MessageBoxButton.YesNoCancel, MessageBoxImage.None);
            if (result == MessageBoxResult.Cancel)
            {
                return false;
            }

            if (result == MessageBoxResult.Yes)
            {
                result = MessageBox.Show("Are you sure you want to make a new storage? ",
                      "Please confirm...", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    DirectoryInfo fileDir = new DirectoryInfo(".");
                    fileDir = fileDir.CreateSubdirectory("Output " + String.Format("{0:yyyy'-'MM'-'dd}", System.DateTime.Now.Date));

                    String currentTime = System.DateTime.Now.ToString();

                    String title = speakerRobotTextBox.Text;

                    fileNameMic1 = currentTime + "_Mic1_" + title + ".txt";
                    fileNameMic1 = fileNameMic1.Replace('/', '-');
                    fileNameMic1 = fileNameMic1.Replace(':', '_');
                    fileNameMic1 = fileDir.FullName + "\\" + fileNameMic1;

                    fileNameMic2 = currentTime + "_Mic2_" + title + ".txt";
                    fileNameMic2 = fileNameMic2.Replace('/', '-');
                    fileNameMic2 = fileNameMic2.Replace(':', '_');
                    fileNameMic2 = fileDir.FullName + "\\" + fileNameMic2;
                }
            }
            else
            {
                while (fileNameMic1.Equals(""))
                {
                    var userInputWindow = new UserInputTextWindow();
                    userInputWindow.setMessage("First FULL FILE PATH include extention for Mic 1:");
                    if (userInputWindow.ShowDialog() == false)
                    {
                        if (userInputWindow.UserConfirm)
                        {
                            fileNameMic1 = userInputWindow.inputText;
                        }
                        else
                        {
                            return false;
                        }
                    }
                }

                while (fileNameMic2.Equals(""))
                {
                    var userInputWindow = new UserInputTextWindow();
                    userInputWindow.setMessage("Second FULL FILE PATH include extention for Mic 2:");
                    if (userInputWindow.ShowDialog() == false)
                    {
                        if (userInputWindow.UserConfirm)
                        {
                            fileNameMic2 = userInputWindow.inputText;
                        }
                        else
                        {
                            return false;
                        }
                    }
                }
            }

            return true;
        }

        private void getDistanceResultToBuffer(byte command, byte testTimes, byte testDisctance)
        {

            uint length = (uint)testTimes * 2;

            Byte[] receivedData = new Byte[length];

            uint[] disctanceData = new uint[testTimes];

            try
            {
                //theControlBoard.receiveBytesFromRobot(command, length, ref receivedData, 1000);

                uint i = 0;
                for (uint pointer = 0; pointer < testTimes; pointer++)
                {
                    disctanceData[pointer] = receivedData[i + 1];
                    disctanceData[pointer] = (disctanceData[pointer] << 8) | receivedData[i];

                    //if (command == COMMAND_GET_DISTANCE_RESULT_A)
                    {
                        g_fdisctanceResultMic1Y[g_ui16TDOABufferPointer1] = disctanceData[pointer] / 256.0f;
                        g_fdisctanceResultMic1X[g_ui16TDOABufferPointer1] = testDisctance * 1.0f;
                        g_ui16TDOABufferPointer1++;
                    }
                    //else if (command == COMMAND_GET_DISTANCE_RESULT_B)
                    {
                        g_fdisctanceResultMic2Y[g_ui16TDOABufferPointer2] = disctanceData[pointer] / 256.0f;
                        g_fdisctanceResultMic2X[g_ui16TDOABufferPointer2] = testDisctance * 1.0f;
                        g_ui16TDOABufferPointer2++;
                    }

                    i += 2;
                }
            }
            catch (Exception ex)
            {
                defaultExceptionHandle(ex);
            }
        }

        private void plotPeakResultFromFile()
        {
            string title = "";

            List<float> xAxis1 = new List<float>();
            List<float> yAxis1 = new List<float>();

            List<float> xAxis2 = new List<float>();
            List<float> yAxis2 = new List<float>();

            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Filter = "Text files (*.TXT)|*.TXT" + "|All files (*.*)|*.*";
            dlg.Title = "Select Mic 1's Data file:";
            if (dlg.ShowDialog() == true)
            {
                title = dlg.SafeFileName;
                string pathToFile = dlg.FileName;

                if (getTDOADataFromFile(pathToFile, xAxis1, yAxis1) == false)
                    return;
            }
            else
                return;

            dlg.Title = "Select Mic 2's Data file:";
            if (dlg.ShowDialog() == true)
            {
                 title = dlg.SafeFileName;
                string pathToFile = dlg.FileName;

                if (getTDOADataFromFile(pathToFile, xAxis2, yAxis2) == false)
                    return;
            }
            else
                return;

            if (xAxis1.Count == yAxis1.Count && xAxis1.Count == xAxis2.Count && yAxis1.Count == yAxis2.Count)
            {
                int dataLength = xAxis1.Count;

                float[] dataX1 = new float[dataLength];
                float[] dataY1 = new float[dataLength];
                float[] dataX2 = new float[dataLength];
                float[] dataY2 = new float[dataLength];

                float[] Plot_dataX = new float[dataLength];
                float[] Plot_dataY = new float[dataLength];

                xAxis1.CopyTo(dataX1);
                yAxis1.CopyTo(dataY1);

                xAxis2.CopyTo(dataX2);
                yAxis2.CopyTo(dataY2);

                for (int i = 0; i < dataLength; i++)
                {
                    Plot_dataX[i] = (dataX1[i] + dataX2[i]) / 2f;
                    Plot_dataY[i] = (dataY1[i] + dataY2[i]) / 2f;
                }

                OxyplotWindow oxyplotWindow = new OxyplotWindow(Plot_dataX, Plot_dataY, title, OxyplotWindow.ScatterPointPlot);
                oxyplotWindow.Show();
            }
        }

        private bool getTDOADataFromFile(string pathToFile, List<float> xAxis, List<float> yAxis)
        {
            System.IO.StreamReader file = new System.IO.StreamReader(@pathToFile);

            string line;
            while ((line = file.ReadLine()) != null)
            {
                Match match = Regex.Match(line, @"([0-9]*(?:\.[0-9]+)?)\s([0-9]*(?:\.[0-9]+)?)", RegexOptions.IgnoreCase);

                if (match.Success)
                {
                    xAxis.Add(float.Parse(match.Groups[1].Value));
                    yAxis.Add(float.Parse(match.Groups[2].Value));
                }
                else
                {
                    return false;
                }
            }

            file.Close();

            return true;
        }
        #endregion
        
        #endregion

        #region Debug Tab

        private void configureRFDebug_Click_1(object sender, RoutedEventArgs e)
        {
            setTxAddress(this.TXAdrrComboBoxDebug.Text);
            setStatusBarContent("Set RF Tx Address: " + this.TXAdrrComboBoxDebug.Text);
        }

        private void sendGotoStateCommandButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Byte[] transmittedData = new Byte[32];

                string command = this.robotStateSelectBox.Text;

                setStatusBarContent("Transmit Command: " + command);

                broadcastCommandGotoState((byte)(this.robotStateSelectBox.SelectedIndex));
            }
            catch (Exception ex)
            {
                defaultExceptionHandle(ex);
            }
        }
        private void broadcastCommandGotoState(byte stateNumber)
        {
            Byte[] data = new Byte[1];

            data[0] = stateNumber;

            SwarmMessageHeader header = new SwarmMessageHeader(e_MessageType.MESSAGE_TYPE_HOST_COMMAND, COMMAND_GOTO_STATE);

            SwarmMessage message = new SwarmMessage(header, data);

            theControlBoard.broadcastMessageToRobot(message);
        }

        private void sendDebugCommandButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Byte[] transmittedData = new Byte[32];

                string command = this.debugCommandSelectBox.Text;

                setStatusBarContent("Transmit Command: " + command);

                switch (command)
                {
                    case "Read Neighbors Table":
                        readNeighborsTable();
                        break;

                    case "Read One Hop Neighbors Table":
                        readOneHopNeighborsTable();
                        break;

                    case "Read Locations Table":
                        readLocationsTable();
                        break;

                    case "Self Correct Locations Table":
                        theControlBoard.broadcastCommandToRobot(COMMAND_SELF_CORRECT_LOCATIONS_TABLE);
                        break;

                    case "Self Correct Locations Table Except Rotation Hop":
                        theControlBoard.broadcastCommandToRobot(COMMAND_SELF_CORRECT_LOCATIONS_TABLE_EXCEPT_ROTATION_HOP);
                        break;

                    case "Scan Robot Identity":
                        scanRobotIdentity((Button)sender);
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

        private void readNeighborsTable()
        {
            uint length = 60;
            byte[] receivedData = new byte[length];

            SwarmMessageHeader header = new SwarmMessageHeader(e_MessageType.MESSAGE_TYPE_HOST_COMMAND, COMMAND_READ_NEIGHBORS_TABLE);
            SwarmMessage requestMessage = new SwarmMessage(header);

            try
            {
                theControlBoard.receivedDataFromRobot(receivedData, length, 1000, requestMessage);

                String tableContent = constructNeighborsTableFromByteBuffer(receivedData);

                MessageBox.Show(tableContent, "Robot [" + this.TXAdrrComboBoxDebug.Text + "] neighbors table", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                defaultExceptionHandle(ex);
            }
        }
        private String constructNeighborsTableFromByteBuffer(byte[] receivedData)
        {
            String table = "Neighbors Table of Robot [0x" + this.TXAdrrComboBoxDebug.Text + "]:\n";

            int[] ID = new int[10];
            int[] distance = new int[10];
            int pointer = 0;

            double distanceInCm = 0;

            for (int i = 0; i < 10; i++)
            {
                ID[i] = (receivedData[pointer] << 24) | (receivedData[pointer + 1] << 16) | (receivedData[pointer + 2] << 8) | receivedData[pointer + 3];
                distance[i] = (receivedData[pointer + 4] << 8) | receivedData[pointer + 5];

                pointer += 6;

                distanceInCm = distance[i] / 256.0;
                if (ID[i] != 0 || distance[i] != 0)
                    table += String.Format("Robot [0x{0}] :: {1} cm\n", ID[i].ToString("X6"), distanceInCm.ToString("G6"));
            }

            return table;
        }

        private void readOneHopNeighborsTable()
        {
            uint length = 640;
            byte[] receivedData = new byte[length];

            SwarmMessageHeader header = new SwarmMessageHeader(e_MessageType.MESSAGE_TYPE_HOST_COMMAND, COMMAND_READ_ONEHOP_NEIGHBORS_TABLE);
            SwarmMessage requestMessage = new SwarmMessage(header);

            try
            {
                theControlBoard.receivedDataFromRobot(receivedData, length, 1000, requestMessage);

                String fileContent = constructOneHopDataFromByteBuffer(receivedData);

                String fileFullPath = exportDataToTextFile("Output OneHop", "OneHop " + this.TXAdrrComboBoxDebug.Text + ".txt", fileContent);

                MessageBoxResult result = MessageBox.Show("The table content have been saved to\n" + fileFullPath, "Robot [" + this.TXAdrrComboBoxDebug.Text + "] one hop neighbors table", MessageBoxButton.YesNo, MessageBoxImage.Information);

                if (result == MessageBoxResult.Yes)
                    displayTextDataFile(fileFullPath);
            }
            catch (Exception ex)
            {
                defaultExceptionHandle(ex);
            }
        }
        private String constructOneHopDataFromByteBuffer(byte[] receivedData)
        {
            String[] table = new String[10];
            String msg = "One Hop Neighbors Table of Robot [0x" + this.TXAdrrComboBoxDebug.Text + "]:\n";

            int[] firstID = new int[10];
            int[] ID = new int[100];
            int[] distance = new int[100];
            int pointer = 0;
            double distanceInCm = 0;

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

            return msg;
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

        private void readLocationsTable()
        {
            uint length = 120;
            byte[] receivedData = new byte[length];

            SwarmMessageHeader header = new SwarmMessageHeader(e_MessageType.MESSAGE_TYPE_HOST_COMMAND, COMMAND_READ_LOCATIONS_TABLE);
            SwarmMessage requestMessage = new SwarmMessage(header);

            try
            {
                theControlBoard.receivedDataFromRobot(receivedData, length, 1000, requestMessage);

                String tableContent = constructLocationsTableFromByteBuffer(receivedData);

                String fileFullPath = exportDataToTextFile("Output Coordinates", "Coordinates " + this.TXAdrrComboBoxDebug.Text + ".txt", tableContent);

                MessageBoxResult result = MessageBox.Show(tableContent + "\nDo you want to plot this?", "Robot [" + this.TXAdrrComboBoxDebug.Text + "] Locations table", MessageBoxButton.YesNo, MessageBoxImage.Information);

                if (result == MessageBoxResult.Yes)
                    plotLocationsTable(fileFullPath);
            }
            catch (Exception ex)
            {
                defaultExceptionHandle(ex);
            }
        }
        private String constructLocationsTableFromByteBuffer(byte[] receivedData)
        {
            String table = "Locations Table of Robot [0x" + this.TXAdrrComboBoxDebug.Text + "]:\n";

            UInt32[] ID = new UInt32[10];
            float[] x = new float[10];
            float[] y = new float[10];
            int pointer = 0;

            for (int i = 0; i < 10; i++)
            {
                ID[i] = (UInt32)((receivedData[pointer] << 24) | (receivedData[pointer + 1] << 16) | (receivedData[pointer + 2] << 8) | receivedData[pointer + 3]);
                pointer += 4;

                x[i] = (float)(((receivedData[pointer] << 24) | (receivedData[pointer + 1] << 16) | (receivedData[pointer + 2] << 8) | receivedData[pointer + 3]) / 65536.0f);
                pointer += 4;

                y[i] = (float)(((receivedData[pointer] << 24) | (receivedData[pointer + 1] << 16) | (receivedData[pointer + 2] << 8) | receivedData[pointer + 3]) / 65536.0f);
                pointer += 4;

                if (ID[i] != 0 || x[i] != 0 || y[i] != 0)
                {
                    table += String.Format("Robot:0x{0} ({1}; {2})\n", ID[i].ToString("X6"), x[i].ToString("G6"), y[i].ToString("G6"));
                }
            }

            return table;
        }
        private void plotLocationsTable(String fileFullPath)
        {
            bool isValidFile = false;

            System.IO.StreamReader file = new System.IO.StreamReader(fileFullPath);

            string title;
            if ((title = file.ReadLine()) == null)
                return;

            List<float> xAxis = new List<float>();
            List<float> yAxis = new List<float>();
            List<UInt32> ui32ID = new List<UInt32>();
            List<float> theta = new List<float>();

            int lineCounter = 1;
            string line;
            while ((line = file.ReadLine()) != null)
            {
                lineCounter++;

                Match match = Regex.Match(line, @"^Robot:(0x[A-Fa-f0-9]+)\s\W([+-]?[0-9]*(?:\.[0-9]+)?);\s([+-]?[0-9]*(?:\.[0-9]+)?)\W$", RegexOptions.IgnoreCase);

                if (match.Success)
                {
                    if (!match.Groups[1].Value.Equals("0x000000"))
                    {
                        lineCounter++;
                        line = file.ReadLine();
                        if (line == null)
                        {
                            isValidFile = false;
                            break;
                        }
                        Match match2 = Regex.Match(line, @"^Robot direction\s=\s([+-]?[0-9]*(?:\.[0-9]+)?)\sdegree$", RegexOptions.IgnoreCase);
                        if (match2.Success)
                        {
                            ui32ID.Add(UInt32.Parse(match.Groups[1].Value.Substring(2), System.Globalization.NumberStyles.HexNumber));
                            xAxis.Add(float.Parse(match.Groups[2].Value));
                            yAxis.Add(float.Parse(match.Groups[3].Value));
                            theta.Add(float.Parse(match2.Groups[1].Value));
                            isValidFile = true;
                        }
                        else
                        {
                            isValidFile = false;
                            break;
                        }
                    }
                    else
                    {
                        isValidFile = false;
                        break;
                    }
                }
            }

            file.Close();

            if (isValidFile)
            {
                float[] Plot_dataX = new float[xAxis.Count];
                float[] Plot_dataY = new float[yAxis.Count];
                UInt32[] listID = new UInt32[ui32ID.Count];
                float[] listTheta = new float[theta.Count];

                xAxis.CopyTo(Plot_dataX);
                yAxis.CopyTo(Plot_dataY);
                ui32ID.CopyTo(listID);
                theta.CopyTo(listTheta);

                OxyplotWindow oxyplotWindow = new OxyplotWindow(listID, listTheta, Plot_dataX, Plot_dataY, title, OxyplotWindow.ScatterPointAndLinePlot);

                oxyplotWindow.Show();
            }
            else
            {
                MessageBox.Show("plotLocationsTable:: Invalid line structure at line " + lineCounter);
            }
        }

        private void plotLocationsTableFromFile()
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Title = "Select your file";
            dlg.Filter = "Text files (*.TXT)|*.TXT" + "|All files (*.*)|*.*";

            // Process open file dialog box results 
            if (dlg.ShowDialog() == true)
            {
                string pathToFile = dlg.FileName;

                plotLocationsTable(pathToFile);
            }
        }

        String mRobotIdentityTextFilePath;
        private BackgroundWorker bgwScanRobotIdentity;
        private void scanRobotIdentity(Button buttonClicked)
        {
            // Hanlde for second event when scanning
            if (bgwScanRobotIdentity != null && bgwScanRobotIdentity.IsBusy)
            {
                bgwScanRobotIdentity.CancelAsync();
                return;
            }

            // Init BackgroundWorker
            bgwScanRobotIdentity = new BackgroundWorker();
            bgwScanRobotIdentity.WorkerReportsProgress = true;
            bgwScanRobotIdentity.WorkerSupportsCancellation = true;

            bgwScanRobotIdentity.DoWork += new DoWorkEventHandler(bgwScanRobotIdentity_DoWork);
            bgwScanRobotIdentity.ProgressChanged += new ProgressChangedEventHandler(bgwScanRobotIdentity_ProgressChanged);
            bgwScanRobotIdentity.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bgwScanRobotIdentity_RunWorkerCompleted);

            // Lock UI
            mRobotIdentityTextFilePath = null;
            this.debugCommandSelectBox.IsEnabled = false;
            this.sendDebugCommandButton.Content = "Stop Scanning";
            toggleAllButtonStatusExceptSelected(buttonClicked);
            setStatusBarContentAndColor("0%::scanning robot identities...", Brushes.Indigo);

            // Active BackgroundWorker
            bgwScanRobotIdentity.RunWorkerAsync();
        }
        private void bgwScanRobotIdentity_DoWork(object sender, DoWorkEventArgs e)
        {
            String outputContent = "Robot Identities Information\n";

            String foundRobot = " robot(s): ";
            int numberOfFoundRobot = 0;

            uint length = 35;
            byte[] receivedData = new byte[length];

            for (int i = 0; i < ROBOT_ID_LIST.Length; i++)
            {
                if (bgwScanRobotIdentity.CancellationPending)
                {
                    e.Cancel = true;
                    break;
                }

                Thread.Sleep(100);
                setTxAddress(ROBOT_ID_LIST[i].ToString("X8"));
                Thread.Sleep(100);
                try
                {
                    SwarmMessageHeader header = new SwarmMessageHeader(e_MessageType.MESSAGE_TYPE_HOST_COMMAND, COMMAND_READ_ROBOT_IDENTITY);
                    SwarmMessage message = new SwarmMessage(header);

                    theControlBoard.receivedDataFromRobot(receivedData, length, 2000, message);

                    SwarmMessage rxMessage = SwarmMessage.ConstructFromByteArray(receivedData);

                    UInt32 Self_ID = (UInt32)((receivedData[0] << 24) | (receivedData[1] << 16) | (receivedData[2] << 8) | receivedData[3]);
                    UInt32 Origin_ID = (UInt32)((receivedData[4] << 24) | (receivedData[5] << 16) | (receivedData[6] << 8) | receivedData[7]);
                    UInt32 RotationHop_ID = (UInt32)((receivedData[8] << 24) | (receivedData[9] << 16) | (receivedData[10] << 8) | receivedData[11]);
                    byte Self_NeighborsCount = receivedData[12];
                    byte Origin_NeighborsCount = receivedData[13];
                    byte Origin_Hopth = receivedData[14];
                    float x = (float)((Int32)((receivedData[15] << 24) | (receivedData[16] << 16) | (receivedData[17] << 8) | receivedData[18]) / 65536.0);
                    float y = (float)((Int32)((receivedData[19] << 24) | (receivedData[20] << 16) | (receivedData[21] << 8) | receivedData[22]) / 65536.0);
                    float RotationHop_x = (float)((Int32)((receivedData[23] << 24) | (receivedData[24] << 16) | (receivedData[25] << 8) | receivedData[26]) / 65536.0);
                    float RotationHop_y = (float)((Int32)((receivedData[27] << 24) | (receivedData[28] << 16) | (receivedData[29] << 8) | receivedData[30]) / 65536.0);
                    float theta = (float)((Int32)((receivedData[31] << 24) | (receivedData[32] << 16) | (receivedData[33] << 8) | receivedData[34]) / 65536.0);
                    double thetaInDeg = theta * 180.0f / Math.PI;
                    outputContent += String.Format("Robot:0x{0} ({1}; {2})\n", Self_ID.ToString("X6"), x.ToString("G6"), y.ToString("G6"));
                    outputContent += String.Format("Robot direction = {0} degree\n", thetaInDeg.ToString());
                    outputContent += String.Format("Self neighbors = {0}\n", Self_NeighborsCount.ToString());
                    outputContent += String.Format("Origin:0x{0}, neighbors = {1}, Hopth = {2}\n", Origin_ID.ToString("X6"), Origin_NeighborsCount.ToString(), Origin_Hopth.ToString());
                    outputContent += String.Format("Rotation Hop:0x{0} ({1}; {2})\n", RotationHop_ID.ToString("X6"), RotationHop_x.ToString("G6"), RotationHop_y.ToString("G6"));
                    outputContent += "\n";

                    numberOfFoundRobot += 1;
                    foundRobot = foundRobot + "0x" + ROBOT_ID_LIST[i].ToString("X6") + ", ";
                }
                catch (Exception ex)
                {
                }

                bgwScanRobotIdentity.ReportProgress(i * 100 / ROBOT_ID_LIST.Length);
            }
            setTxAddress(DEFAULT_TX_ADDRESS);

            if (numberOfFoundRobot > 0)
            {
                mRobotIdentityTextFilePath = exportDataToTextFile("Output RobotIdentity", "RobotIdentity.txt", outputContent);
                displayTextDataFile(mRobotIdentityTextFilePath);
                e.Result = "Found " + numberOfFoundRobot + foundRobot;
            }
            else
            {
                mRobotIdentityTextFilePath = null;
                e.Result = "Not found any robot";
            }
        }
        private void bgwScanRobotIdentity_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            setStatusBarContent(e.ProgressPercentage.ToString() + "% " + "::scanning robot identities...");
        }
        private void bgwScanRobotIdentity_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            setStatusBarAndButtonsAppearanceFromDeviceState();
            
            if (e.Cancelled)
                setStatusBarContent("Scanning process is terminated.");
            else
            {
                if (mRobotIdentityTextFilePath == null)
                    MessageBox.Show((string)e.Result, "Scanning completed");
                else
                {
                    MessageBoxResult result = MessageBox.Show((string)e.Result, "Do you want to plot the result?", MessageBoxButton.YesNo, MessageBoxImage.Information);
                    if (result == MessageBoxResult.Yes)
                        plotLocationsTable(mRobotIdentityTextFilePath);
                }
            }

            this.sendDebugCommandButton.Content = "Send Command";
            this.debugCommandSelectBox.IsEnabled = true;

            bgwScanRobotIdentity.DoWork -= bgwScanRobotIdentity_DoWork;
            bgwScanRobotIdentity.ProgressChanged -= bgwScanRobotIdentity_ProgressChanged;
            bgwScanRobotIdentity.RunWorkerCompleted -= bgwScanRobotIdentity_RunWorkerCompleted;
            bgwScanRobotIdentity = null;
        }

        private void rotateAngleButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                float fAngleInDegree;
                float.TryParse(this.rotateAngleTextBox.Text, out fAngleInDegree);

                Byte[] data = new Byte[4];

                Int32 i32Values = (Int32)(fAngleInDegree * 65536 + 0.5);
                data[0] = (Byte)((i32Values >> 24) & 0xFF);
                data[1] = (Byte)((i32Values >> 16) & 0xFF);
                data[2] = (Byte)((i32Values >> 8) & 0xFF);
                data[3] = (Byte)(i32Values & 0xFF);

                SwarmMessageHeader header = new SwarmMessageHeader(e_MessageType.MESSAGE_TYPE_HOST_COMMAND, COMMAND_ROTATE_WITH_ANGLE);
                SwarmMessage message = new SwarmMessage(header, data);
                theControlBoard.broadcastMessageToRobot(message);

                setStatusBarContent("Broadcast Command: rotate " + fAngleInDegree + " degree");
            }
            catch (Exception ex)
            {
                throw new Exception("rotateAngle " + ex.Message);
            }
        }

        private void moveDistanceButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                float fDistanceInCm;
                float.TryParse(this.moveDistanceTextBox.Text, out fDistanceInCm);

                Byte[] data = new Byte[4];

                Int32 i32Values = (Int32)(fDistanceInCm * 65536 + 0.5);
                data[0] = (Byte)((i32Values >> 24) & 0xFF);
                data[1] = (Byte)((i32Values >> 16) & 0xFF);
                data[2] = (Byte)((i32Values >> 8) & 0xFF);
                data[3] = (Byte)(i32Values & 0xFF);

                SwarmMessageHeader header = new SwarmMessageHeader(e_MessageType.MESSAGE_TYPE_HOST_COMMAND, COMMAND_MOVE_WITH_DISTANCE);
                SwarmMessage message = new SwarmMessage(header, data);
                theControlBoard.broadcastMessageToRobot(message);

                setStatusBarContent("Broadcast Command: move " + fDistanceInCm + "cm");
            }
            catch (Exception ex)
            {
                throw new Exception("moveDistance " + ex.Message);
            }
        }

        BackgroundWorker bgwProgramGradientMap;
        private void updateGradientMapButton_Click(object sender, RoutedEventArgs e)
        {           
            // TODO: get from txt file =======================
            //UInt32 ui32Row = 11;
            //UInt32 ui32Column = 8;
            //sbyte offsetHeight = -1;
            //sbyte offsetWidth = -1;
            //UInt32 trappedCount = 3;
            //sbyte[] pGradientMap = new sbyte[11 * 8]{	
            //    0, 0,  0,  0,  0,  0, 0, 0,
            //    0, 1,  1,  1,  1,  1, 1, 0,
            //    0, 1, -1,  1, -2, -2, 1, 0,
            //    0, 1, -1, -1,  1, -2, 1, 0,
            //    0, 1, -1,  1,  1,  1, 1, 0,
            //    0, 1,  1,  1,  1,  1, 1, 0,
            //    0, 1, -3,  1, -3, -3, 1, 0,
            //    0, 1, -3,  1,  1, -3, 1, 0,
            //    0, 1, -3, -3, -3, -3, 1, 0,
            //    0, 1,  1,  1,  1,  1, 1, 0,
            //    0, 0,  0,  0,  0,  0, 0, 0 }; // 1.9ms

            UInt32 ui32Row = 11;
            UInt32 ui32Column = 15;
            sbyte offsetHeight = -1;
            sbyte offsetWidth = -1;
            UInt32 trappedCount = 0;
            sbyte[] pGradientMap = new sbyte[11 * 15]{	
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                0, 1, 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0,
                0, 1, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0,
                0, 1, 0, 1, 0, 1, 1, 1, 1, 1, 1, 1, 0, 1, 0,
                0, 1, 0, 1, 0, 1, 0, 0, 0, 0, 0, 1, 0, 1, 0,
                0, 1, 0, 1, 0, 1, 1, 1, 1, 1, 0, 1, 0, 1, 0,
                0, 1, 0, 1, 0, 0, 0, 0, 0, 0, 0, 1, 0, 1, 0,
                0, 1, 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 1, 0,
                0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0,
                0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0,
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }; // 3.4ms
            //================================================

            sendStartUpdateGradientMapPacket(ui32Row, ui32Column, offsetHeight, offsetWidth, trappedCount);

            // Lock UI
            toggleAllButtonStatusExceptSelected(null); //(Button)sender);W
            setStatusBarContentAndColor("0%::gradient map updating", Brushes.Indigo);

            // Init BackgroundWorker
            bgwProgramGradientMap = new BackgroundWorker();
            bgwProgramGradientMap.WorkerReportsProgress = true;
            bgwProgramGradientMap.WorkerSupportsCancellation = false;

            bgwProgramGradientMap.DoWork += new DoWorkEventHandler(bgwProgramGradientMap_DoWork);
            bgwProgramGradientMap.ProgressChanged += new ProgressChangedEventHandler(bgwProgramGradientMap_ProgressChanged);
            bgwProgramGradientMap.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bgwProgramGradientMap_RunWorkerCompleted);
            
            // Active BackgroundWorker
            bgwProgramGradientMap.RunWorkerAsync(pGradientMap);
        }
        private void sendStartUpdateGradientMapPacket(UInt32 ui32Row, UInt32 ui32Column, sbyte offsetHeight, sbyte offsetWidth, UInt32 ui32TrappedCount)
        {
            // COMMAND_UPDATE_GRADIENT_MAP: <4-byte row><4-byte column><1-byte offsetHeight><1-byte offsetWidth>
            Byte[] startUpdateGradientMapPacket = new Byte[14];

            parse32bitTo4Bytes(startUpdateGradientMapPacket, 0, (Int32)ui32Row);
            parse32bitTo4Bytes(startUpdateGradientMapPacket, 4, (Int32)ui32Column);
            startUpdateGradientMapPacket[8] = (byte)offsetHeight;
            startUpdateGradientMapPacket[9] = (byte)offsetWidth;
            parse32bitTo4Bytes(startUpdateGradientMapPacket, 10, (Int32)ui32TrappedCount);

            SwarmMessageHeader headerStartUpdateGradientMapMessage = new SwarmMessageHeader(e_MessageType.MESSAGE_TYPE_HOST_COMMAND, COMMAND_UPDATE_GRADIENT_MAP);
            SwarmMessage messageStartUpdateGradientMap = new SwarmMessage(headerStartUpdateGradientMapMessage, startUpdateGradientMapPacket);
            theControlBoard.broadcastMessageToRobot(messageStartUpdateGradientMap);

            Thread.Sleep(15);
        }
        private class GradientMapFrameFormat
        {
            public const int GRADIENT_MAP_PACKET_HEADER_LENGTH = 6;
            public const int GRADIENT_MAP_PACKET_FULL_DATA_LENGTH = 8;
            public const int GRADIENT_MAP_PACKET_FULL_LENGTG = 14; // GRADIENT_MAP_PACKET_HEADER_LENGTH + GRADIENT_MAP_PACKET_FULL_DATA_LENGTH
            
            public UInt32 startIndex;
            public byte checksum;
            public sbyte[] data;

            public byte[] ToByteArray() 
            {
                byte[] byteArray = new byte[4 + 1 + 1 + data.Length];
                byteArray[0] = (byte)((startIndex >> 24) & 0xFF);
                byteArray[1] = (byte)((startIndex >> 16) & 0xFF);
                byteArray[2] = (byte)((startIndex >> 8) & 0xFF);
                byteArray[3] = (byte)(startIndex & 0xFF);
                byteArray[4] = (byte)data.Length;
                byteArray[5] = checksum;
                for (int i = 0; i < data.Length; i++)
                    byteArray[i + GRADIENT_MAP_PACKET_HEADER_LENGTH] = (byte)data[i];
                return byteArray;
            }
        };
        private void bgwProgramGradientMap_DoWork(object sender, DoWorkEventArgs e)
        {
            sbyte[] pGradientMap = (sbyte[])e.Argument;

            // Allocated memory spaces
            uint maxNumberOfDataFrame = (uint)pGradientMap.Length / GradientMapFrameFormat.GRADIENT_MAP_PACKET_FULL_DATA_LENGTH;
            byte numberOfDataNotFitInOneFrame = (byte)(pGradientMap.Length % GradientMapFrameFormat.GRADIENT_MAP_PACKET_FULL_DATA_LENGTH);
            int numberOfExtraFrame = (numberOfDataNotFitInOneFrame == 0) ? (0) : (1);
            GradientMapFrameFormat[] arrayDataFrame = new GradientMapFrameFormat[maxNumberOfDataFrame + numberOfExtraFrame];

            // Prepare all frame
            UInt32 ui32PacketID = 0;
            byte checkSum;
            for (int i = 0; i < maxNumberOfDataFrame; i++)
            {
                arrayDataFrame[i] = new GradientMapFrameFormat();
                arrayDataFrame[i].startIndex = ui32PacketID;

                arrayDataFrame[i].data = new sbyte[GradientMapFrameFormat.GRADIENT_MAP_PACKET_FULL_DATA_LENGTH];

                checkSum = 0;
                for (int j = 0; j < arrayDataFrame[i].data.Length; j++)
                {
                    arrayDataFrame[i].data[j] = pGradientMap[arrayDataFrame[i].startIndex + j];
                    checkSum = (byte)(checkSum + arrayDataFrame[i].data[j]);
                }
                checkSum = (byte)(checkSum + ((arrayDataFrame[i].startIndex >> 24) & 0xFF) + ((arrayDataFrame[i].startIndex >> 16) & 0xFF)
                    + ((arrayDataFrame[i].startIndex >> 8) & 0xFF) + ((arrayDataFrame[i].startIndex) & 0xFF) + (byte)arrayDataFrame[i].data.Length);
                arrayDataFrame[i].checksum = (byte)(~checkSum + 1);

                ui32PacketID += (uint)arrayDataFrame[i].data.Length;
            }

            // Prepare extra frame if need
            if (numberOfExtraFrame == 1)
            {
                arrayDataFrame[maxNumberOfDataFrame] = new GradientMapFrameFormat();
                arrayDataFrame[maxNumberOfDataFrame].startIndex = ui32PacketID;

                arrayDataFrame[maxNumberOfDataFrame].data = new sbyte[numberOfDataNotFitInOneFrame];
                checkSum = 0;
                for (int j = 0; j < arrayDataFrame[maxNumberOfDataFrame].data.Length; j++)
                {
                    arrayDataFrame[maxNumberOfDataFrame].data[j] = pGradientMap[arrayDataFrame[maxNumberOfDataFrame].startIndex + j];
                    checkSum = (byte)(checkSum + arrayDataFrame[maxNumberOfDataFrame].data[j]);
                }
                checkSum = (byte)(checkSum + ((arrayDataFrame[maxNumberOfDataFrame].startIndex >> 24) & 0xFF)
                    + ((arrayDataFrame[maxNumberOfDataFrame].startIndex >> 16) & 0xFF)
                    + ((arrayDataFrame[maxNumberOfDataFrame].startIndex >> 8) & 0xFF)
                    + ((arrayDataFrame[maxNumberOfDataFrame].startIndex) & 0xFF)
                    + (byte)arrayDataFrame[maxNumberOfDataFrame].data.Length);
                arrayDataFrame[maxNumberOfDataFrame].checksum = (byte)(~checkSum + 1);

                ui32PacketID += (uint)arrayDataFrame[maxNumberOfDataFrame].data.Length;
            }

            // Program Packet
            const int SINGLE_PACKET_TIMEOUT_US = 1000000;
            const int UPDATE_PACKET_WAIT_TIMES = 15;

            const int DATA_FRAME_NACK_WAIT_TIME = 15; // unit ms
            const int BACKWARD_STEP_FOR_RESEND_PACKET = 4;
            const int NUMBER_OF_RESEND_LAST_PACKET = 8;

            byte[] receivedDataBuffer = new byte[2];
            for (int i = 0; i < arrayDataFrame.Length; )
            {
                bgwProgramGradientMap.ReportProgress(i * 100 / arrayDataFrame.Length);

                if (i == (arrayDataFrame.Length - 1))
                {
                    for (int k = 0; k < NUMBER_OF_RESEND_LAST_PACKET; k++)
                    {
                        theControlBoard.broadcastDataToRobot(arrayDataFrame[i].ToByteArray());
                        if (theControlBoard.tryToReceivedDataFromRobot(receivedDataBuffer, (uint)receivedDataBuffer.Length, DATA_FRAME_NACK_WAIT_TIME))
                        {
                            // Received NACK
                            if (i < BACKWARD_STEP_FOR_RESEND_PACKET)
                                i = 0;
                            else
                                i -= BACKWARD_STEP_FOR_RESEND_PACKET;

                            break;
                        }
                    }
                    i++;
                }
                else
                {
                    theControlBoard.broadcastDataToRobot(arrayDataFrame[i].ToByteArray());

                    if (theControlBoard.tryToReceivedDataFromRobot(receivedDataBuffer, (uint)receivedDataBuffer.Length, DATA_FRAME_NACK_WAIT_TIME))
                    {
                        // Received NACK
                        if (i < BACKWARD_STEP_FOR_RESEND_PACKET)
                            i = 0;
                        else
                            i -= BACKWARD_STEP_FOR_RESEND_PACKET;
                    }
                    else
                    {
                        //TODO: prepare next packet
                        i++;
                    }
                }
            }

            e.Result = 0;
        }
        private void bgwProgramGradientMap_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            setStatusBarContent(e.ProgressPercentage.ToString() + "% " + "::gradient map updating...");
        }
        private void bgwProgramGradientMap_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            setStatusBarAndButtonsAppearanceFromDeviceState();
            setStatusBarContent("Gradient Map updated!");

            bgwProgramGradientMap.DoWork -= bgwProgramGradientMap_DoWork;
            bgwProgramGradientMap.ProgressChanged -= bgwProgramGradientMap_ProgressChanged;
            bgwProgramGradientMap.RunWorkerCompleted -= bgwProgramGradientMap_RunWorkerCompleted;
            bgwProgramGradientMap = null;
        }

        #endregion
    
        #region Helper Data manipulation methods
        private void parse32bitTo4Bytes(byte[] pBuff, int offset, Int32 i32Data)
        {
            pBuff[offset] = (byte)((i32Data >> 24) & 0xFF);
            pBuff[offset + 1] = (byte)((i32Data >> 16) & 0xFF);
            pBuff[offset + 2] = (byte)((i32Data >> 8) & 0xFF);
            pBuff[offset + 3] = (byte)(i32Data & 0xFF);
        }

        private Int32 construct4Byte(byte[] pBuff, int offset)
        {
            return ((pBuff[offset] << 24) | (pBuff[offset + 1] << 16) | (pBuff[offset + 2] << 8) | pBuff[offset + 3]);
        }

        private void parse16bitTo2Bytes(byte[] pBuff, int offset, Int16 i16Data)
        {
            pBuff[offset] = (byte)((i16Data >> 8) & 0xFF);
            pBuff[offset + 1] = (byte)(i16Data & 0xFF);
        }

        private Int16 construct2Byte(byte[] pBuff, int offset)
        {
            return (Int16)((pBuff[offset] << 8) | pBuff[offset + 1]);
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
