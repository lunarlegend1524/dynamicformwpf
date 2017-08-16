using System;
using System.Collections;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using DevExpress.Xpf.Core;
using DynamicFormWPF.Classes_Data;

namespace DynamicFormWPF
{
    /// <summary>
    /// Interaction logic for ClientList.xaml
    /// </summary>
    public partial class ClientList : Window
    {
        private Window root = new Window();

        public ClientList(Window parent)
        {
            root = parent;
            InitializeComponent();
        }

        private int clientID = 0;

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            ArrayList al = DB.getDBNameList();
            if (al == null)
            {
                MessageBox.Show("Chưa chọn CSDL", "Thông báo");
                this.Close();
                return;
            }
            ThemeManager.SetThemeName(_treeListClient, "Seven");

            // check if parent form is ModemSetting
            if (root.GetType() == typeof(ModemSetting))
            {
                _btnCopy.Visibility = Visibility.Visible;
            }
            else
            {
                _btnCopy.Visibility = Visibility.Hidden;
            }

            loadTreeList();
            loadChildCombobox();
            _cbbChild.Focus();
        }

        public void loadTreeList()
        {
            DataTable dt = DB.getClientList();
            _treeListClient.ItemsSource = dt;
            _treeListView.ExpandAllNodes();

            if (root.GetType() == typeof(TargetCreator))
            {
                ((TargetCreator)root).loadTreeList();
            }
        }

        private string validateMaxLength(string text)
        {
            string info = string.Empty;

            if (text.Length >= 255)
            {
                info = "Số ký tự vượt quá giới hạn, xin nhập lại";
            }

            return info;
        }

        private void loadChildCombobox()
        {
            DataTable dt = new DataTable();
            dt = DB.getClientListByParentID(clientID);

            if (dt == null)
            {
                return;
            }

            _cbbChild.SetBinding(ItemsControl.ItemsSourceProperty, new Binding { Source = dt });
            _cbbChild.DisplayMemberPath = "ClientName";
            _cbbChild.SelectedValuePath = "ClientID";
        }

        private void _btnClearTarget_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _txtParent.Text = string.Empty;
                _txtPhone.Text = string.Empty;
                clientID = 0;
                loadChildCombobox();
            }
            catch (Exception) { }
        }

        private void _btnAddChild_Click(object sender, RoutedEventArgs e)
        {
            if (DB.getParentID(clientID, "Client") != 0)
            {
                MessageBox.Show("Hiện tại chương trình chỉ hỗ trợ 2 cấp đơn vị", "Thông báo");
                return;
            }

            if (_cbbChild.Text == string.Empty)
            {
                MessageBox.Show("Xin nhập tên đơn vị", "Thông báo");
                return;
            }

            if (_txtPhone.Text == string.Empty && !_cbbChild.Text.Contains("1")) // LV1 client does not need phone number
            {
                MessageBox.Show("Xin nhập số điện thoại", "Thông báo");
                _txtPhone.Focus();
                return;
            }

            if (_txtParent.Text == string.Empty)
            {
                clientID = 0;
            }

            string info = validateMaxLength(_cbbChild.Text);

            if (info == string.Empty)
            {
                if (DB.isExistedParentandChildClientMode(clientID, _cbbChild.Text))
                {
                    MessageBox.Show("Chỉ tiêu đã có trong CSDL hoặc khai báo sai cấp thỉ tiêu", "Thông báo");
                    return;
                }
                else
                {
                    DB.addNewClient(_cbbChild.Text, clientID, _txtPhone.Text);
                    loadTreeList();

                    // refresh combobox after created
                    loadChildCombobox();

                    // update client list in TargetCreator
                    if (root.GetType() == typeof(TargetCreator))
                    {
                        ((TargetCreator)root).loadTreeViewClient();
                    }
                }
            }
            else
            {
                MessageBox.Show(info, "Thông báo");
                return;
            }
        }

        private void _btnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (_txtParent.Text == string.Empty)
            {
                MessageBox.Show("Xin chọn 1 đơn vị", "Thông báo");
                return;
            }

            if (DB.isChildContained(clientID, "Client"))
            {
                MessageBox.Show("Đơn vị này có chứa đơn vị cấp dưới, xin chọn đơn vị khác", "Thông báo");
                return;
            }
            else
            {
                MessageBoxResult result = MessageBox.Show("Xóa đơn vị '" + DB.getNameByID(clientID, "Client") + "' ?", "Thông báo", MessageBoxButton.OKCancel);
                if (result == MessageBoxResult.OK)
                {
                    string info = DB.deleteTargetOrClient(clientID, "Client");
                    if (info == "OK")
                    {
                        MessageBox.Show("Đã xóa thành công đơn vị", "Thông báo");
                        loadTreeList();

                        // update client list in TargetCreator
                        if (root.GetType() == typeof(TargetCreator))
                        {
                            ((TargetCreator)root).loadTreeViewClient();
                        }

                        return;
                    }
                }
            }
        }

        private void _treeListClient_GotFocus(object sender, RoutedEventArgs e)
        {
            try
            {
                clientID = Convert.ToInt32(_treeListView.GetNodeValue(_treeListView.FocusedNode, "ClientID"));
                _txtParent.Text = DB.getNameByID(clientID, "Client");
                _txtPhone.Text = DB.getTelephoneNumberByID(clientID);
                loadChildCombobox();
            }
            catch { }
        }

        private void _txtParent_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_txtParent.Text == string.Empty)
            {
                _lblLV1.Content = "Thêm đơn vị cấp 1      ";
            }

            else
            {
                _lblLV1.Content = "Thêm đơn vị cấp dưới";
            }
        }

        private void _btnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (clientID == 0)
            {
                MessageBox.Show("Xin chọn 1 đơn vị", "Thông báo");
                return;
            }
            else
            {
                ClientEditor ce = new ClientEditor(this, clientID);
                ce.ShowDialog();
            }
        }

        private void _btnCopy_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (root.GetType() == typeof(ModemSetting))
                {
                    // copy telephone number to clipboard then paste to ModemSetting
                    Clipboard.Clear();
                    Clipboard.SetText(_txtPhone.Text);
                    ((ModemSetting)root).setNumber(Clipboard.GetText());
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
    }
}