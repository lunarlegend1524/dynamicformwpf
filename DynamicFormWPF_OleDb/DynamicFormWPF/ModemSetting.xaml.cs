namespace DynamicFormWPF
{
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
    using System.Windows.Shapes;
    using System.IO.Ports;
    using Microsoft.Win32;
    using System.Data;

    /// <summary>
    /// Interaction logic for ModemSetting.xaml
    /// </summary>
    public partial class ModemSetting : Window
    {
        public static SerialPort p = new SerialPort();
        Window form = new Window();

        public ModemSetting(Window main)
        {
            form = main;
            InitializeComponent();
            string[] BaudRates = { "1200", "2400", "4800", "9600", "19200", "38400", "57600", "115200" };
            for (int i = 0; i < BaudRates.Length; i++)
            {
                _cbbBaudRate.Items.Add(BaudRates[i]);
            }
            string[] ports = SerialPort.GetPortNames();
            for (int j = 0; j < ports.Length; j++)
            {
                _cbbPort.Items.Add(ports[j]);
            }
        }

        private void _btnConnect_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                p.PortName = _cbbPort.Text;
                p.Open();
                MessageBox.Show("Đã mở kết nối", "Thông báo");
                _windowModem.Title = "Thiết lập Modem - Đang kết nối với cổng " + p.PortName;
                _btnDisconnect.IsEnabled = true;

                if (form.GetType() == typeof(MainWindow))
                {
                    ((MainWindow)form).setPortInfo(p);
                }
                else if (form.GetType() == typeof(TargetCreator))
                {
                    ((TargetCreator)form).setPortInfo(p);
                }
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void _btnDisconnect_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                p.Close();
                MessageBox.Show("Đã đóng kết nối", "Thông báo");
                _windowModem.Title = "Thiết lập Modem - Kết nối đang đóng";
                _btnDisconnect.IsEnabled = false;

                if (form.GetType() == typeof(MainWindow))
                {
                    ((MainWindow)form).setPortInfo(p);
                }
                else if (form.GetType() == typeof(TargetCreator))
                {
                    ((TargetCreator)form).setPortInfo(p);
                }

                this.Close();
            }
            catch (Exception)
            {
                MessageBox.Show("Kết nối đang đóng", "Thông báo");
                _btnDisconnect.IsEnabled = false;
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            DataTable dt = DB.getTargetList(-1);
            if (dt == null)
            {
                MessageBox.Show("Chưa chọn tệp chứa bộ chỉ tiêu", "Thông báo");
                return;
            }

            if (p.IsOpen)
            {
                _windowModem.Title = "Thiết lập Modem - Đang kết nối với cổng " + p.PortName;
                _btnDisconnect.IsEnabled = true;
            }

            else
            {
                _windowModem.Title = "Thiết lập Modem - Kết nối đang đóng";
                _btnDisconnect.IsEnabled = false;
            }
        }

        private void _cbbBaudRate_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            p.BaudRate = Convert.ToInt32(_cbbBaudRate.SelectedItem.ToString());
        }

        private void _cbbPort_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            p.PortName = _cbbPort.SelectedItem.ToString();
        }
    }
}
