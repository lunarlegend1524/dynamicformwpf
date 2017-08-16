using System;
using System.Collections;
using System.Windows;
using System.Windows.Controls;
using DynamicFormWPF.Classes_Data;

namespace DynamicFormWPF
{
    /// <summary>
    /// Interaction logic for DatabaseSelector.xaml
    /// </summary>
    public partial class DatabaseSelector : Window
    {
        private MainWindow mainForm;
        private string selectedItem = string.Empty;

        public DatabaseSelector(MainWindow main)
        {
            mainForm = main;
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            ArrayList al = DB.getDBNameList();
            if (al == null)
            {
                MessageBoxResult result = MessageBox.Show("Xâu kết nối bị sai hoặc chưa được khởi tạo, sửa lại ?", "Thông báo", MessageBoxButton.OKCancel);
                if (result == MessageBoxResult.OK)
                {
                    ConnectionStringModifier csm = new ConnectionStringModifier(this);
                    csm.ShowDialog();
                    return;
                }
            }
            loadDBList();
        }

        public void loadDBList()
        {
            ArrayList listDBNames = DB.getDBNameList();
            _listViewDBNames.ItemsSource = listDBNames;
        }

        private void _btnSelect_Click(object sender, RoutedEventArgs e)
        {
            if (selectedItem == string.Empty)
            {
                MessageBox.Show("Xin chọn CSDL", "Thông báo");
                return;
            }

            MessageBoxResult result = MessageBox.Show("Chọn CSDL '" + selectedItem + "' ?", "Thông báo", MessageBoxButton.OKCancel);
            if (result == MessageBoxResult.OK)
            {
                // close current DB
                //string DBName = DB.getDBNameFromConnectionString();
                //DB.closeDB(DBName);

                // open new DB
                //DB.openDB(selectedItem);

                // update connectionstring with the new selected DB and write to registry
                RegistryEditor.setNewConfigSettingWithDBName(selectedItem);

                // refresh MainWindow
                mainForm.loadTreeList();

                this.Close();
            }
        }

        private void _btnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (selectedItem == string.Empty)
            {
                MessageBox.Show("Xin chọn CSDL", "Thông báo");
                return;
            }

            MessageBoxResult result = MessageBox.Show("Xóa CSDL '" + selectedItem + "' ?", "Thông báo", MessageBoxButton.OKCancel);
            if (result == MessageBoxResult.OK)
            {
                string info = DB.deleteDBFromServer(selectedItem);

                if (info == "NotOK")
                {
                    MessageBox.Show("Cơ sở dữ liệu đang được sử dụng, xin chọn cơ sở dữ liệu khác", "Thông báo");
                }

                else
                {
                    MessageBox.Show(info, "Thông báo");
                }
                loadDBList();
                return;
            }
        }

        private void _listViewDBNames_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                selectedItem = _listViewDBNames.SelectedItem.ToString();
            }
            catch (Exception)
            {
                selectedItem = string.Empty;
            }
        }
    }
}