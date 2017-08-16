using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;
using DynamicFormWPF.Classes_Data;

namespace DynamicFormWPF
{
    /// <summary>
    /// Interaction logic for ConnectionStringModifier.xaml
    /// </summary>
    public partial class ConnectionStringModifier : Window
    {
        private string connectionString = string.Empty;
        private Window mainForm;

        public ConnectionStringModifier(Window main)
        {
            mainForm = main;
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // get current connectionstring
            _radioStringBuilder.IsChecked = true;
            try
            {
                connectionString = ConfigurationManager.ConnectionStrings["SEISConnectionString"].ConnectionString;
            }
            catch (Exception)
            {
                connectionString = string.Empty;
            }

            _txtConnectionString.Text = connectionString;

            _cbbIntegratedSecurity.Items.Add("True");
            _cbbIntegratedSecurity.Items.Add("False");
            _cbbIntegratedSecurity.Items.Add("SSPI");

            SqlConnectionStringBuilder csb = new SqlConnectionStringBuilder(connectionString);
            _txtDataSource.Text = csb.DataSource;
            _txtInitialCatalog.Text = csb.InitialCatalog;

            if (csb.IntegratedSecurity.ToString() == string.Empty)
            {
                _cbbIntegratedSecurity.SelectedValue = "False";
            }
            else
            {
                _cbbIntegratedSecurity.SelectedValue = csb.IntegratedSecurity.ToString();
            }

            // update after _cbbIntegratedSecurity's value has changed above
            updateConnectionStringTextbox();

            _txtUserID.Text = csb.UserID;
            _txtPassword.Text = csb.Password;

            // update connectionstring textbox when text changed
            _txtDataSource.TextChanged += new TextChangedEventHandler(_txtConnectionString_Update);
            _txtInitialCatalog.TextChanged += new TextChangedEventHandler(_txtConnectionString_Update);
            _txtUserID.TextChanged += new TextChangedEventHandler(_txtConnectionString_Update);
            _txtPassword.TextChanged += new TextChangedEventHandler(_txtConnectionString_Update);
        }

        private void _btnSaveTargetName_Click(object sender, RoutedEventArgs e)
        {
            if (_radioConnectionString.IsChecked == true)
            {
                connectionString = _txtConnectionString.Text;

                if (connectionString == string.Empty)
                {
                    MessageBox.Show("Xin nhập xâu kết nối", "Thông báo");
                    _txtConnectionString.Focus();
                    return;
                }

                else
                {
                    if (!testConnectionStringWorking())
                    {
                        MessageBoxResult result2 = MessageBox.Show("Xâu kết nối không chính xác, sửa lại?", "Thông báo", MessageBoxButton.OKCancel);
                        if (result2 == MessageBoxResult.OK)
                        {
                            _txtConnectionString.Focus();
                            return;
                        }

                        else
                        {
                            this.Close();
                        }
                    }
                    else
                    {
                        // update new connectionstring

                        // cannot use DB class because connectionstring has not been initialized
                        //DB.setNewConfigSettingWithConnectionString(connectionString);
                        setNewConfigSettingWithConnectionString(connectionString);

                        // refresh MainWindow

                        if (mainForm.GetType() == typeof(MainWindow))
                        {
                            ((MainWindow)mainForm).loadTreeList();
                        }

                        else if (mainForm.GetType() == typeof(DatabaseSelector))
                        {
                            ((DatabaseSelector)mainForm).loadDBList();
                        }

                        MessageBox.Show("Đã lưu tùy chỉnh", "Thông báo");
                        this.Close();
                    }
                }
            }

            else if (_radioStringBuilder.IsChecked == true)
            {
                if (_txtDataSource.Text == string.Empty)
                {
                    MessageBox.Show("Xin nhập Data Source", "Thông báo");
                    _txtDataSource.Focus();
                    return;
                }

                else if (_txtInitialCatalog.Text == string.Empty)
                {
                    MessageBox.Show("Xin nhập Initial Catalog", "Thông báo");
                    _txtInitialCatalog.Focus();
                    return;
                }

                // condition has been checked in _txtConnectionString_Update() below
                //if (_txtUserID.Text == string.Empty)
                //{
                //    connectionString = "Data Source=" + _txtDataSource.Text + ";Initial Catalog=" + _txtInitialCatalog.Text + ";Integrated Security=" + _cbbIntegratedSecurity.SelectedValue;
                //}

                //else if (_txtPassword.Text == string.Empty)
                //{
                //    connectionString = "Data Source=" + _txtDataSource.Text + ";Initial Catalog=" + _txtInitialCatalog.Text + ";Integrated Security=" + _cbbIntegratedSecurity.SelectedValue + ";User ID=" + _txtUserID.Text;
                //}

                //else
                //{
                //    connectionString = "Data Source=" + _txtDataSource.Text + ";Initial Catalog=" + _txtInitialCatalog.Text + ";Integrated Security=" + _cbbIntegratedSecurity.SelectedValue + ";User ID=" + _txtUserID.Text + ";Password=" + _txtPassword.Text;
                //}

                if (!testConnectionStringWorking())
                {
                    MessageBoxResult result2 = MessageBox.Show("Xâu kết nối không chính xác, sửa lại?", "Thông báo", MessageBoxButton.OKCancel);
                    if (result2 == MessageBoxResult.OK)
                    {
                        _txtDataSource.Focus();
                        return;
                    }

                    else
                    {
                        this.Close();
                    }
                }
                else
                {
                    MessageBoxResult result = MessageBox.Show("Lưu tùy chỉnh ?", "Thông báo", MessageBoxButton.OKCancel);
                    if (result == MessageBoxResult.OK)
                    {
                        // update new connectionstring

                        // cannot use DB class because connectionstring has not been initialized
                        //DB.setNewConfigSettingWithConnectionString(connectionString);
                        setNewConfigSettingWithConnectionString(connectionString);

                        // test new connectionstring

                        // refresh MainWindow

                        if (mainForm.GetType() == typeof(MainWindow))
                        {
                            ((MainWindow)mainForm).loadTreeList();
                        }

                        else if (mainForm.GetType() == typeof(DatabaseSelector))
                        {
                            ((DatabaseSelector)mainForm).loadDBList();
                        }

                        this.Close();
                    }
                }
            }
        }

        private void setNewConfigSettingWithConnectionString(string connectionString)
        {
            // get current connectionstring
            SqlConnectionStringBuilder csb = new SqlConnectionStringBuilder(connectionString);

            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

            // set the new values
            config.ConnectionStrings.ConnectionStrings["SEISConnectionString"].ConnectionString = csb.ConnectionString;

            // save and refresh the config file
            config.Save(ConfigurationSaveMode.Minimal);
            ConfigurationManager.RefreshSection("connectionStrings");

            // now we can use DB class
            DB.refreshCSBConnectionString();

            RegistryEditor.setConnectionStringToRegistry(csb.ConnectionString);
        }

        private void _radioStringBuilder_Checked(object sender, RoutedEventArgs e)
        {
            _radioConnectionString.IsChecked = false;

            //_gridStringBuilder.IsEnabled = true;
            _txtDataSource.IsEnabled = true;
            _txtInitialCatalog.IsEnabled = true;
            _cbbIntegratedSecurity.IsEnabled = true;
            _txtUserID.IsEnabled = true;
            _txtPassword.IsEnabled = true;

            //_gridConnectionString.IsEnabled = false;
            _txtConnectionString.IsEnabled = false;
        }

        private void _radioConnectionString_Checked(object sender, RoutedEventArgs e)
        {
            _radioStringBuilder.IsChecked = false;

            //_gridConnectionString.IsEnabled = true;
            _txtConnectionString.IsEnabled = true;

            //_gridStringBuilder.IsEnabled = false;
            _txtDataSource.IsEnabled = false;
            _txtInitialCatalog.IsEnabled = false;
            _cbbIntegratedSecurity.IsEnabled = false;
            _txtUserID.IsEnabled = false;
            _txtPassword.IsEnabled = false;
        }

        private bool testConnectionStringWorking()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    return true;
                }
            }
            catch (SqlException)
            {
                return false;
            }
        }

        private void _txtConnectionString_Update(object sender, TextChangedEventArgs e)
        {
            updateConnectionStringTextbox();
        }

        private void updateConnectionStringTextbox()
        {
            if (_txtDataSource.Text == string.Empty)
            {
                _btnSaveCS.IsEnabled = false;
            }

            if (_txtInitialCatalog.Text == string.Empty)
            {
                _btnSaveCS.IsEnabled = false;
            }

            else
            {
                _btnSaveCS.IsEnabled = true;
            }

            if (_txtUserID.Text == string.Empty)
            {
                connectionString = "Data Source=" + _txtDataSource.Text + ";Initial Catalog=" + _txtInitialCatalog.Text + ";Integrated Security=" + _cbbIntegratedSecurity.SelectedValue;
            }

            else if (_txtPassword.Text == string.Empty)
            {
                connectionString = "Data Source=" + _txtDataSource.Text + ";Initial Catalog=" + _txtInitialCatalog.Text + ";Integrated Security=" + _cbbIntegratedSecurity.SelectedValue + ";User ID=" + _txtUserID.Text;
            }

            else
            {
                connectionString = "Data Source=" + _txtDataSource.Text + ";Initial Catalog=" + _txtInitialCatalog.Text + ";Integrated Security=" + _cbbIntegratedSecurity.SelectedValue + ";User ID=" + _txtUserID.Text + ";Password=" + _txtPassword.Text;
            }

            _txtConnectionString.Text = connectionString;
        }

        private void _cbbIntegratedSecurity_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            updateConnectionStringTextbox();
        }
    }
}