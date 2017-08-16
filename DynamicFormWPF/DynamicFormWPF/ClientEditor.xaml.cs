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
using DynamicFormWPF.Classes_Data;

namespace DynamicFormWPF
{
    /// <summary>
    /// Interaction logic for ClientEditor.xaml
    /// </summary>
    public partial class ClientEditor : Window
    {
        private static int clientID = 0;
        private ClientList parentForm;

        public ClientEditor(ClientList parent, int ID)
        {
            clientID = ID;
            parentForm = parent;
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            _txtClientNameEdit.Text = DB.getNameByID(clientID, "Client");
            _txtPhoneNumber.Text = DB.getTelephoneNumberByID(clientID);
        }

        private void _btnSaveClientInfo_Click(object sender, RoutedEventArgs e)
        {
            string info = string.Empty;

            MessageBoxResult result = MessageBox.Show("Sửa thông tin đơn vị ?", "Thông báo", MessageBoxButton.OKCancel);
            if (result == MessageBoxResult.OK)
            {
                info = DB.editClientInfo(clientID, _txtClientNameEdit.Text, _txtPhoneNumber.Text);
                parentForm.loadTreeList();
                MessageBox.Show(info, "Thông báo");
                this.Close();
            }
        }
    }
}