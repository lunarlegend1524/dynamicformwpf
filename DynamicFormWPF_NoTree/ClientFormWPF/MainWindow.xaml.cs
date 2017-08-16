using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ClientFormWPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Menu_File_Exit_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("Thoát chương trình?", "Thông báo", MessageBoxButton.OKCancel);
            if (result == MessageBoxResult.OK)
            {
                this.Close();
            }
            else { }
        }

        // connect to port COM of server
        private void Menu_Modem_Connect_Click(object sender, RoutedEventArgs e)
        {

        }

        // set up transfering speed
        private void Menu_Modem_Settings_Click(object sender, RoutedEventArgs e)
        {

        }

        // clear the input textboxes for new inputting
        private void Menu_File_New_Click(object sender, RoutedEventArgs e)
        {

        }

        // open saved *.rtf file
        private void Menu_File_Open_Click(object sender, RoutedEventArgs e)
        {

        }

        // clear the input textboxes
        private void Menu_File_Close_Click(object sender, RoutedEventArgs e)
        {

        }

        // save to *.rtf file
        private void Menu_File_Save_Click(object sender, RoutedEventArgs e)
        {

        }

        // send text content to server
        private void _btnSend_Click(object sender, RoutedEventArgs e)
        {

        }

        // occurs when change tabs
        private void _tabControlReportSender_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
    }
}
