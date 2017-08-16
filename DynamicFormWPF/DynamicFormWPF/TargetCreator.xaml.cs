namespace DynamicFormWPF
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Data;
    using System.IO;
    using System.IO.Ports;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Data;
    using System.Windows.Media;
    using DevExpress.Xpf.Core;
    using DynamicFormWPF.Classes_Data;

    /// <summary>
    /// Interaction logic for TargetCreator.xaml
    /// </summary>
    public partial class TargetCreator : Window
    {
        private MainWindow mainWindow;
        private SerialPort port;

        public TargetCreator(MainWindow root, SerialPort p)
        {
            mainWindow = root;
            port = p;
            InitializeComponent();

            //port.DataReceived += new SerialDataReceivedEventHandler(FileSavedEvent);
        }

        private int targetID;
        private int clientID;

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            ArrayList al = DB.getDBNameList();
            if (al == null)
            {
                MessageBox.Show("Chưa chọn CSDL", "Thông báo");
                this.Close();
                return;
            }
            ThemeManager.SetThemeName(_treeListTarget, "Seven");
            loadTreeList();
            loadComboboxes();
            loadTreeViewClient();
        }

        // reload target tree in main window
        private void loadMainTree()
        {
            //mainWindow.loadTreeView();
            mainWindow.loadTreeList();
        }

        private void loadComboboxes()
        {
            DataTable dt = new DataTable();
            dt = DB.getTargetList(0);

            if (dt == null)
            {
                return;
            }

            _cbbChild.SetBinding(ItemsControl.ItemsSourceProperty, new Binding { Source = dt });
            _cbbChild.DisplayMemberPath = "TargetName";
            _cbbChild.SelectedValuePath = "TargetID";
        }

        //public void loadTreeView()
        //{
        //    _treeViewTarget.Items.Clear();

        //    DataTable parentNodes = DB.getTargetList(-1);

        //    if (parentNodes == null)
        //    {
        //        return;
        //    }

        //    Dictionary<int, TreeViewItem> flattenedTree = new Dictionary<int, TreeViewItem>();

        //    foreach (DataRow node in parentNodes.Rows)
        //    {
        //        TreeViewItem treeNode = new TreeViewItem();
        //        treeNode.Header = node["TargetName"];
        //        treeNode.Tag = node["TargetID"];
        //        flattenedTree.Add(Convert.ToInt32(node["TargetID"]), treeNode);

        //        if (flattenedTree.ContainsKey(Convert.ToInt32(node["ParentID"]))) // create child nodes
        //        {
        //            flattenedTree[Convert.ToInt32(node["ParentID"])].Items.Add(treeNode);
        //        }
        //        else // create new parent node
        //        {
        //            _treeViewTarget.Items.Add(treeNode);
        //        }
        //        treeNode.IsExpanded = true;
        //    }
        //    loadMainTrees();
        //}

        public void loadTreeList()
        {
            DataTable dt = DB.getTargetListWithClientName();
            _treeListTarget.ItemsSource = dt;
            _treeListView.ExpandAllNodes();

            loadMainTree();
        }

        public void loadTreeViewClient()
        {
            _treeViewClient.Items.Clear();

            DataTable parentNodes = DB.getClientList();

            if (parentNodes == null)
            {
                return;
            }

            Dictionary<int, TreeViewItem> flattenedTree = new Dictionary<int, TreeViewItem>();

            foreach (DataRow node in parentNodes.Rows)
            {
                TreeViewItem treeNode = new TreeViewItem();
                treeNode.Header = node["ClientName"];
                treeNode.Tag = node["ClientID"];
                flattenedTree.Add(Convert.ToInt32(node["ClientID"]), treeNode);

                if (flattenedTree.ContainsKey(Convert.ToInt32(node["ParentID"]))) // create child nodes
                {
                    flattenedTree[Convert.ToInt32(node["ParentID"])].Items.Add(treeNode);
                }
                else // create new parent node
                {
                    _treeViewClient.Items.Add(treeNode);
                }
                treeNode.IsExpanded = true;
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

        private void _btnAddChild_Click(object sender, RoutedEventArgs e)
        {
            if (_cbbChild.Text == string.Empty)
            {
                MessageBox.Show("Xin nhập tên chỉ tiêu", "Thông báo");
                return;
            }

            // _txtParent.Text == string.Empty - now adding LV1 target
            if (_txtParent.Text == string.Empty && _expClient.Header == null)
            {
                MessageBox.Show("Xin nhập tên đơn vị", "Thông báo");
                _expClient.IsExpanded = true;
                _expClient.Focus();
                return;
            }

            if (_txtParent.Text == string.Empty)
            {
                targetID = 0;
            }

            string info = validateMaxLength(_cbbChild.Text);

            if (info == string.Empty)
            {
                if (DB.isExistedParentandChild(targetID, _cbbChild.Text))
                {
                    MessageBox.Show("Chỉ tiêu đã có trong CSDL hoặc khai báo sai cấp thỉ tiêu", "Thông báo");
                    return;
                }
                else
                {
                    // add LV1 target, add according client
                    if (targetID == 0)
                    {
                        DB.addNewTarget(_cbbChild.Text, targetID, clientID);
                    }
                    else // add childs with parent's clientID
                    {
                        DB.addNewTarget(_cbbChild.Text, targetID, DB.getClientIDByTargetID(targetID));
                    }
                    loadTreeList();

                    // refresh combobox after created
                    loadTargetComboboxes(_cbbChild, targetID);
                }
            }
            else
            {
                MessageBox.Show(info, "Thông báo");
                return;
            }
        }

        public static Visual GetDescendantByType(Visual element, Type type)
        {
            if (element == null)
            {
                return null;
            }

            if (element.GetType() == type)
            {
                return element;
            }

            Visual foundElement = null;

            if (element is FrameworkElement) (element as FrameworkElement).ApplyTemplate();

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(element); i++)
            {
                Visual visual = VisualTreeHelper.GetChild(element, i) as Visual;
                foundElement = GetDescendantByType(visual, type);
                if (foundElement != null)
                {
                    break;
                }
            }
            return foundElement;
        }

        private void TreeView_Selected(object sender, RoutedEventArgs e)
        {
            // get tag of selected node
            //if (_treeViewTarget.IsMouseOver)
            //{
            //    _treeViewTarget.Tag = e.OriginalSource;
            //}

            //else if (_treeViewClient.IsMouseOver)
            //{
            //    _treeViewClient.Tag = e.OriginalSource;
            //}
            _treeViewClient.Tag = e.OriginalSource;
        }

        // get selected treeviewitem value
        //private void _treeViewTarget_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        //{
        //    try
        //    {
        //        if (e.NewValue != null)
        //        {
        //            TreeViewItem item = _treeViewTarget.Tag as TreeViewItem;
        //            TreeViewItem node = GetDescendantByType(item, typeof(TreeViewItem)) as TreeViewItem;

        //            // get node.Tag as TargetID
        //            targetID = Convert.ToInt32(node.Tag.ToString());
        //            _txtParent.Text = DB.getNameByID(targetID, "Target");
        //            //listTargetNames.Clear();
        //            //listTargetNames = DB.getTargetNameList(targetID);

        //            // load children of selected node to combobox
        //            loadTargetComboboxes(_cbbChild, targetID);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show(ex.Message);
        //    }
        //}

        // get selected treeviewitem value
        private void _treeViewClient_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            try
            {
                if (e.NewValue != null)
                {
                    TreeViewItem item = _treeViewClient.Tag as TreeViewItem;
                    TreeViewItem node = GetDescendantByType(item, typeof(TreeViewItem)) as TreeViewItem;

                    // get node.Tag as ClientID
                    clientID = Convert.ToInt32(node.Tag.ToString());
                    _expClient.Header = DB.getNameByID(clientID, "Client");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void loadTargetComboboxes(ComboBox cbb, int id)
        {
            DataTable dt = new DataTable();
            dt = DB.getTargetList(id);

            cbb.SetBinding(ItemsControl.ItemsSourceProperty, new Binding { Source = dt });
            cbb.DisplayMemberPath = "TargetName";
            cbb.SelectedValuePath = "TargetID";
        }

        private void unLoadTargetComboboxes(ComboBox cbb)
        {
            cbb.SetBinding(ItemsControl.ItemsSourceProperty, new Binding { Source = null });
            cbb.DisplayMemberPath = "TargetName";
            cbb.SelectedValuePath = "TargetID";
        }

        // create xml of target list and send to client
        private void _btnSync_Click(object sender, RoutedEventArgs e)
        {
            if (!port.IsOpen)
            {
                MessageBoxResult result = MessageBox.Show("Chưa có kết nối với máy trạm/chủ, mở kết nối ?", "Thông báo", MessageBoxButton.OKCancel);
                if (result == MessageBoxResult.OK)
                {
                    ModemSetting mds = new ModemSetting(this, port);
                    mds.ShowDialog();
                }
            }

            else
            {
                transferFile();
            }
        }

        public void transferFile()
        {
            // check if a LV1 target has been chosen
            if (DB.getParentID(targetID, "Target") != 0 || targetID == 0)
            {
                MessageBox.Show("Xin chọn một chỉ tiêu cấp 1", "Thông báo");
                return;
            }

            MessageBoxResult result = MessageBox.Show("Gửi bộ chỉ tiêu '" + DB.getNameByID(targetID, "Target") + "' xuống cho máy trạm ?", "Thông báo", MessageBoxButton.OKCancel);
            if (result == MessageBoxResult.OK)
            {
                DataTable dt = new DataTable();

                dt = DB.getAllChildrenOfTargetID(targetID); // retrieve all children target of selected LV1 target

                dt.TableName = "Target"; // must set table name before writing to XML

                // create file name with DB name prefix [DB]_[ClientName]_[Type]_fileName.xml
                string fileName2 = @"C:\data\temp\tempData.xml";
                string clientName = DB.getClientNameByTargetID(targetID);
                string fileName = @"C:\data\temp\" + "[" + DB.getDBNameFromConnectionString() + "]"
                    + "_[" + clientName + "]"
                    + "_[Sync]"
                    + "_[" + DateTime.Today.Day + "-" + DateTime.Today.Month + "-" + DateTime.Today.Year + "].xml";
                dt.WriteXml(fileName2);

                if (File.Exists(fileName))
                {
                    File.Delete(fileName);
                }

                string info = string.Empty;

                // sign the XML document with Digital Signature
                info = DigitalSignature.signXML(fileName2);

                if (info != "OK")
                {
                    MessageBox.Show(info, "Warning");
                    return;
                }

                // encrypt the signed XML
                info = DataEncryption.EncryptFile(fileName2, fileName);

                if (info != "OK")
                {
                    MessageBox.Show(info, "Warning");
                    return;
                }

                if (FileTransfer.FileTransferer(fileName, port) == "OK")
                {
                    MessageBox.Show("Đã gửi yêu cầu cập nhập bộ chỉ tiêu xuống máy trạm", "Thông báo");
                    return;
                }
            }
        }

        private void _btnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (_txtParent.Text == string.Empty || targetID == 0)
            {
                MessageBox.Show("Xin chọn 1 chỉ tiêu", "Thông báo");
                return;
            }

            TargetEditor targetEditor = new TargetEditor(this, targetID);
            targetEditor.ShowDialog();
        }

        private void _btnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (_txtParent.Text == string.Empty || targetID == 0)
            {
                MessageBox.Show("Xin chọn 1 chỉ tiêu", "Thông báo");
                return;
            }

            if (DB.isChildContained(targetID, "Target"))
            {
                MessageBox.Show("Chỉ tiêu này có chứa chỉ tiêu con, xin chọn chỉ tiêu khác", "Thông báo");
                return;
            }
            else
            {
                if (DB.isRecordContained(targetID))
                {
                    MessageBox.Show("Đã có báo cáo sử dụng chỉ tiêu này, xin chọn chỉ tiêu khác", "Thông báo");
                    return;
                }
                else
                {
                    MessageBoxResult result = MessageBox.Show("Xóa chỉ tiêu '" + DB.getNameByID(targetID, "Target") + "' ?", "Thông báo", MessageBoxButton.OKCancel);
                    if (result == MessageBoxResult.OK)
                    {
                        string info = DB.deleteTargetOrClient(targetID, "Target");
                        if (info == "OK")
                        {
                            MessageBox.Show("Đã xóa thành công chỉ tiêu", "Thông báo");
                            loadTreeList();
                            return;
                        }
                    }
                }
            }
        }

        public void setPortInfo(SerialPort portModem)
        {
            port = portModem;

            //port.DataReceived += new SerialDataReceivedEventHandler(FileSavedEvent);
            mainWindow.setPortInfo(port);
        }

        public static void FileSavedEvent(object obj, SerialDataReceivedEventArgs e)
        {
            //FileTransfer.FileReceiver(obj, null);
            //string path = FileTransfer.getSavedPath();
            //MessageBox.Show("Đã nhận file mới tại " + path, "Thông báo");
        }

        private void _txtParent_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_txtParent.Text == string.Empty)
            {
                _lblLV1.Content = "Thêm chỉ tiêu cấp 1      ";
                _lblClientName.Visibility = Visibility.Visible;
                _expClient.Visibility = Visibility.Visible;
            }

            else
            {
                _lblLV1.Content = "Thêm chỉ tiêu cấp dưới";
                _lblClientName.Visibility = Visibility.Hidden;
                _expClient.Visibility = Visibility.Hidden;
            }
        }

        private void _btnClearTarget_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _txtParent.Text = string.Empty;
                targetID = 0;
                loadTargetComboboxes(_cbbChild, targetID);
            }
            catch (Exception) { }
        }

        private void _treeListTarget_GotFocus(object sender, RoutedEventArgs e)
        {
            try
            {
                targetID = Convert.ToInt32(_treeListView.GetNodeValue(_treeListView.FocusedNode, "TargetID"));
                _txtParent.Text = DB.getNameByID(targetID, "Target");

                // load children of selected node to combobox
                loadTargetComboboxes(_cbbChild, targetID);
            }
            catch { }
        }

        private void _btnContact_Click(object sender, RoutedEventArgs e)
        {
            ClientList cl = new ClientList(this);
            cl.Show(); // do not show dialog. user can work with parent form
        }

        private void _expClient_Expanded(object sender, RoutedEventArgs e)
        {
            _expClient.Height = 173;
        }

        private void _expClient_Collapsed(object sender, RoutedEventArgs e)
        {
            _expClient.Height = 43;
        }
    }
}