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

namespace SwarmRobotControlAndCommunication
{
    /// <summary>
    /// Interaction logic for UserInputTextWindow.xaml
    /// </summary>
    public partial class UserInputTextWindow : Window
    {
        private bool userConfirm;

        public UserInputTextWindow()
        {
            InitializeComponent();
        }

        public void setMessage(string msg)
        {
            messageLable.Content = msg;
        }

        public string inputText
        {
            get { return inputTextBox.Text; }
        }

        public bool UserConfirm
        {
            get { return userConfirm; }
        }

        private void okButton_Click(object sender, RoutedEventArgs e)
        {
            userConfirm = true;
            this.Close();
        }

        private void cancleButton_Click(object sender, RoutedEventArgs e)
        {
            userConfirm = false;
            this.Close();
        }
    }
}
