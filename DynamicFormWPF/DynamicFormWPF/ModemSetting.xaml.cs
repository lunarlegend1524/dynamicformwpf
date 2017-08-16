namespace DynamicFormWPF
{
    using System;
    using System.IO.Ports;
    using System.Windows;
    using System.Windows.Controls;
    using DynamicFormWPF.Classes_Data;

    /// <summary>
    /// Interaction logic for ModemSetting.xaml
    /// </summary>
    public partial class ModemSetting : Window
    {
        public static SerialPort p = new SerialPort();
        private Window form = new Window();

        public ModemSetting(Window main, SerialPort port)
        {
            form = main;
            p = port;
            InitializeComponent();
            string[] ports = SerialPort.GetPortNames();
            for (int j = 0; j < ports.Length; j++)
            {
                _cbbPort.Items.Add(ports[j]);
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // does not need to connect to DB beforehand
            //ArrayList al = DB.getDBNameList();
            //if (al == null)
            //{
            //    MessageBox.Show("Chưa chọn CSDL", "Thông báo");
            //    this.Close();
            //    return;
            //}

            if (p.IsOpen)
            {
                _windowModem.Title = "Thiết lập Modem - Đang kết nối với cổng " + p.PortName;
                _lblPort.Content = "Đóng cổng";

                // display the opened port
                _cbbPort.SelectedValue = p.PortName;
                _txtPhoneNumber.Focus();
            }

            else
            {
                _windowModem.Title = "Thiết lập Modem - Kết nối đang đóng";
                _lblPort.Content = "Mở cổng";

                // select the first port in list
                _cbbPort.SelectedIndex = 0;
            }
        }

        private void _btnConnect_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_lblPort.Content.ToString() == "Mở cổng")
                {
                    p.PortName = _cbbPort.Text;
                    string stt = FileTransfer.Connect(p);
                    if (stt != "OK") { MessageBox.Show(stt); }
                    else
                    {
                        _windowModem.Title = "Thiết lập Modem - Đang kết nối với cổng " + p.PortName;
                        _lblPort.Content = "Đóng cổng";
                        _txtPhoneNumber.Focus();
                    }
                    if (form.GetType() == typeof(MainWindow))
                    {
                        ((MainWindow)form).setPortInfo(p);
                    }
                    else if (form.GetType() == typeof(TargetCreator))
                    {
                        ((TargetCreator)form).setPortInfo(p);
                    }
                }
                else if (_lblPort.Content.ToString() == "Đóng cổng")
                {
                    p.Close();
                    _windowModem.Title = "Thiết lập Modem - Kết nối đang đóng";
                    _lblPort.Content = "Mở cổng";

                    if (form.GetType() == typeof(MainWindow))
                    {
                        ((MainWindow)form).setPortInfo(p);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void _cbbPort_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (!p.IsOpen)
                {
                    p.PortName = _cbbPort.SelectedItem.ToString();
                }
            }
            catch (Exception)
            {
                MessageBox.Show("Xin chọn cổng COM", "Thông báo");
                _cbbPort.Focus();
                return;
            }
        }

        private void _btnDial_Click(object sender, RoutedEventArgs e)
        {
            if (!p.IsOpen)
            {
                MessageBox.Show("Xin chọn cổng COM trước", "Thông báo");
                _cbbPort.Focus();
                return;
            }

            else if (_txtPhoneNumber.Text == string.Empty)
            {
                MessageBox.Show("Xin nhập số điện thoại", "Thông báo");
                _txtPhoneNumber.Focus();
                return;
            }

            else
            {
                string stt = FileTransfer.Dial(p, _txtPhoneNumber.Text.Trim());

                // success
                if (stt == "CONNECTED")
                {
                    MessageBox.Show("Kết nối thành công", "Thông báo");
                    this.Close();
                }
            }
        }

        private void _btnContact_Click(object sender, RoutedEventArgs e)
        {
            ClientList cl = new ClientList(this);
            cl.Show(); // do not show dialog. user can work with parent form
        }

        public void setNumber(string number)
        {
            _txtPhoneNumber.Text = number;
        }
    }
}