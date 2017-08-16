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
    using System.Data;
    using System.IO;
    using System.IO.Ports;

    /// <summary>
    /// Interaction logic for TargetCreator.xaml
    /// </summary>
    public partial class TargetCreator : Window
    {
        MainWindow mainWindow;
        SerialPort port;
        public TargetCreator(MainWindow root, SerialPort p)
        {
            mainWindow = root;
            port = p;
            InitializeComponent();
        }

        private string targetLV1 = string.Empty;
        private string targetLV2 = string.Empty;
        private string targetLV3 = string.Empty;
        private string targetLV4 = string.Empty;
        private string targetLV5 = string.Empty;
        private int targetID = 0;
        private static List<string> listTargetNames = new List<string>();

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            DataTable dt = DB.getTargetList(-1);
            if (dt == null)
            {
                MessageBox.Show("Chưa chọn tệp chứa bộ chỉ tiêu", "Thông báo");
                return;
            }
            loadTreeView();
            loadComboboxes();
            setValidateTextInput(); // event handler for comboboxes
        }

        // reload target tree in main window
        private void loadMainTrees()
        {
            mainWindow.loadTreeView();
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

            _cbbLV1.SetBinding(ItemsControl.ItemsSourceProperty, new Binding { Source = dt });
            _cbbLV1.DisplayMemberPath = "TargetName";
            _cbbLV1.SelectedValuePath = "TargetID";

            _cbbLV2.ItemsSource = null;
            _cbbLV3.ItemsSource = null;
            _cbbLV4.ItemsSource = null;
            _cbbLV5.ItemsSource = null;
        }

        public void loadTreeView()
        {
            _treeViewTarget.Items.Clear();

            DataTable parentNodes = DB.getTargetList(-1);

            if (parentNodes == null)
            {
                return;
            }

            Dictionary<int, TreeViewItem> flattenedTree = new Dictionary<int, TreeViewItem>();

            foreach (DataRow node in parentNodes.Rows)
            {
                TreeViewItem treeNode = new TreeViewItem();
                treeNode.Header = node["TargetName"];
                treeNode.Tag = node["TargetID"];
                flattenedTree.Add(Convert.ToInt32(node["TargetID"]), treeNode);

                if (flattenedTree.ContainsKey(Convert.ToInt32(node["ParentID"]))) // create child nodes
                {
                    flattenedTree[Convert.ToInt32(node["ParentID"])].Items.Add(treeNode);
                }
                else // create new parent node
                {
                    _treeViewTarget.Items.Add(treeNode);
                }
                treeNode.IsExpanded = true;
            }
            loadMainTrees();
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

        private void _cbb_PreviewTextInput(object sender, TextCompositionEventArgs e) // handle when input from keyboard
        {
            if (((ComboBox)sender).Text.Length >= 255)
            {
                e.Handled = true;
            }
        }

        private void setValidateTextInput() 
        {
            //DataObject.AddPastingHandler(_cbbLV1, new DataObjectPastingEventHandler(_cbb_OnPaste));
            _cbbLV1.PreviewTextInput += new TextCompositionEventHandler(_cbb_PreviewTextInput);

            //DataObject.AddPastingHandler(_cbbLV2, new DataObjectPastingEventHandler(_cbb_OnPaste));
            _cbbLV2.PreviewTextInput += new TextCompositionEventHandler(_cbb_PreviewTextInput);

            //DataObject.AddPastingHandler(_cbbLV3, new DataObjectPastingEventHandler(_cbb_OnPaste));
            _cbbLV3.PreviewTextInput += new TextCompositionEventHandler(_cbb_PreviewTextInput);

            //DataObject.AddPastingHandler(_cbbLV4, new DataObjectPastingEventHandler(_cbb_OnPaste));
            _cbbLV4.PreviewTextInput += new TextCompositionEventHandler(_cbb_PreviewTextInput);

            //DataObject.AddPastingHandler(_cbbLV5, new DataObjectPastingEventHandler(_cbb_OnPaste));
            _cbbLV5.PreviewTextInput += new TextCompositionEventHandler(_cbb_PreviewTextInput);
        }

        private void _btnAddLV1_Click(object sender, RoutedEventArgs e)
        {
            if (_cbbLV1.Text == string.Empty)
            {
                MessageBox.Show("Xin nhập tên chỉ tiêu", "Thông báo");
                return;
            }
            targetLV1 = _cbbLV1.Text;

            string info = validateMaxLength(targetLV1);

            if (info == string.Empty)
            {
                if (DB.isExistedParentandChild(targetLV1, "null"))
                {
                    MessageBox.Show("Chỉ tiêu đã có trong CSDL hoặc khai báo sai cấp thỉ tiêu", "Thông báo");
                    return;
                }
                else
                {
                    DB.addNewTarget(targetLV1, "");
                    loadTreeView();
                    loadTargetComboboxes(_cbbLV1, 0);
                }
            }
            else 
            {
                MessageBox.Show(info, "Thông báo");
                return;
            }
        }

        private void _btnAddLV2_Click(object sender, RoutedEventArgs e)
        {
            if (_cbbLV2.Text == string.Empty)
            {
                MessageBox.Show("Xin nhập tên chỉ tiêu", "Thông báo");
                return;
            }
            targetLV1 = _cbbLV1.Text;
            targetLV2 = _cbbLV2.Text;

            string info = validateMaxLength(targetLV2);

            if (info == string.Empty)
            {
                if (DB.isExistedParentandChild(targetLV2, targetLV1))
                {
                    MessageBox.Show("Chỉ tiêu đã có trong CSDL hoặc khai báo sai cấp thỉ tiêu", "Thông báo");
                    return;
                }
                else
                {
                    DB.addNewTarget(targetLV2, targetLV1);
                    loadTreeView();
                }
            }
            else
            {
                MessageBox.Show(info, "Thông báo");
                return;
            }
        }

        private void _btnAddLV3_Click(object sender, RoutedEventArgs e)
        {
            if (_cbbLV3.Text == string.Empty)
            {
                MessageBox.Show("Xin nhập tên chỉ tiêu", "Thông báo");
                return;
            }
            targetLV2 = _cbbLV2.Text;
            targetLV3 = _cbbLV3.Text;

            string info = validateMaxLength(targetLV3);

            if (info == string.Empty)
            {
                if (DB.isExistedParentandChild(targetLV3, targetLV2))
                {
                    MessageBox.Show("Chỉ tiêu đã có trong CSDL hoặc khai báo sai cấp thỉ tiêu", "Thông báo");
                    return;
                }
                else
                {
                    DB.addNewTarget(targetLV3, targetLV2);
                    loadTreeView();
                }
            }
            else
            {
                MessageBox.Show(info, "Thông báo");
                return;
            }
        }

        private void _btnAddLV4_Click(object sender, RoutedEventArgs e)
        {
            if (_cbbLV4.Text == string.Empty)
            {
                MessageBox.Show("Xin nhập tên chỉ tiêu", "Thông báo");
                return;
            }
            targetLV3 = _cbbLV3.Text;
            targetLV4 = _cbbLV4.Text;

            string info = validateMaxLength(targetLV4);

            if (info == string.Empty)
            {
                if (DB.isExistedParentandChild(targetLV4, targetLV3))
                {
                    MessageBox.Show("Chỉ tiêu đã có trong CSDL hoặc khai báo sai cấp thỉ tiêu", "Thông báo");
                    return;
                }
                else
                {
                    DB.addNewTarget(targetLV4, targetLV3);
                    loadTreeView();
                }
            }
            else
            {
                MessageBox.Show(info, "Thông báo");
                return;
            }
        }

        private void _btnAddLV5_Click(object sender, RoutedEventArgs e)
        {
            if (_cbbLV5.Text == string.Empty)
            {
                MessageBox.Show("Xin nhập tên chỉ tiêu", "Thông báo");
                return;
            }
            targetLV4 = _cbbLV4.Text;
            targetLV5 = _cbbLV5.Text;

            string info = validateMaxLength(targetLV5);

            if (info == string.Empty)
            {
                if (DB.isExistedParentandChild(targetLV5, targetLV4))
                {
                    MessageBox.Show("Chỉ tiêu đã có trong CSDL hoặc khai báo sai cấp thỉ tiêu", "Thông báo");
                    return;
                }
                else
                {
                    DB.addNewTarget(targetLV5, targetLV4);
                    loadTreeView();
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
            _treeViewTarget.Tag = e.OriginalSource;
        }

        // get selected treeviewitem value
        private void _treeViewTarget_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue != null)
            {
                TreeViewItem item = _treeViewTarget.Tag as TreeViewItem;
                TreeViewItem node = GetDescendantByType(item, typeof(TreeViewItem)) as TreeViewItem;

                // get node.Tag as TargetID
                targetID = Convert.ToInt32(node.Tag.ToString());
                listTargetNames.Clear();
                listTargetNames = DB.getTargetNameList(targetID);

                try { _cbbLV1.Text = listTargetNames[0].ToString(); }
                catch (ArgumentOutOfRangeException) { _cbbLV1.Text = string.Empty; }

                try { _cbbLV2.Text = listTargetNames[1].ToString(); }
                catch (ArgumentOutOfRangeException) { _cbbLV2.Text = string.Empty; }

                try { _cbbLV3.Text = listTargetNames[2].ToString(); }
                catch (ArgumentOutOfRangeException) { _cbbLV3.Text = string.Empty; }

                try { _cbbLV4.Text = listTargetNames[3].ToString(); }
                catch (ArgumentOutOfRangeException) { _cbbLV4.Text = string.Empty; }

                try { _cbbLV5.Text = listTargetNames[4].ToString(); }
                catch (ArgumentOutOfRangeException) { _cbbLV5.Text = string.Empty; }
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

        // load children when select parent in combobox
        private void _cbbLV1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _cbbLV2.Text = string.Empty;

            int id = Convert.ToInt32(_cbbLV1.SelectedValue);

            if (id != 0)
            {
                loadTargetComboboxes(_cbbLV2, id);
            }
        }

        private void _cbbLV2_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _cbbLV3.Text = string.Empty;

            int id = Convert.ToInt32(_cbbLV2.SelectedValue);

            if (id != 0)
            {
                loadTargetComboboxes(_cbbLV3, id);
            }
        }

        private void _cbbLV3_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _cbbLV4.Text = string.Empty;

            int id = Convert.ToInt32(_cbbLV3.SelectedValue);

            if (id != 0)
            {
                loadTargetComboboxes(_cbbLV4, id);
            }
        }

        private void _cbbLV4_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _cbbLV5.Text = string.Empty;

            int id = Convert.ToInt32(_cbbLV4.SelectedValue);

            if (id != 0)
            {
                loadTargetComboboxes(_cbbLV5, id);
            }
        }

        // create xml of target list and send to client
        private void _btnSync_Click(object sender, RoutedEventArgs e)
        {
            if (!port.IsOpen)
            {
                MessageBoxResult result = MessageBox.Show("Chưa có kết nối với máy trạm/chủ, mở kết nối ?", "Thông báo", MessageBoxButton.OKCancel);
                if (result == MessageBoxResult.OK)
                {
                    ModemSetting mds = new ModemSetting(this);
                    mds.ShowDialog();
                }
            }

            else 
            {
                MessageBoxResult result = MessageBox.Show("Đồng bộ chỉ tiêu với máy trạm ?", "Thông báo", MessageBoxButton.OKCancel);
                if (result == MessageBoxResult.OK)
                {
                    DataTable dt = new DataTable();
                    dt = DB.getTargetList(-1); // retrieve all target
                    dt.TableName = "Target"; // must set table name before writing to XML
                    string fileName = "TargetList_" + DateTime.Today.Day + "-" + DateTime.Today.Month + "-" + DateTime.Today.Year + ".xml";
                    dt.WriteXml(@"C:\data\temp\" + fileName);
                    if (FileTransfer.FileTransferer(@"C:\data\temp\" + fileName, port) == "OK")
                    {
                        MessageBox.Show("Đã gửi yêu cầu cập nhập bộ chỉ tiêu xuống máy trạm", "Thông báo");
                        return;
                    }
                }
            }
        }

        private void _btnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (_treeViewTarget.SelectedItem == null)
            {
                MessageBox.Show("Xin chọn 1 chỉ tiêu", "Thông báo");
                return;
            }

            TargetEditor targetEditor = new TargetEditor(this, targetID);
            targetEditor.ShowDialog();
        }

        private void _btnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (_treeViewTarget.SelectedItem == null)
            {
                MessageBox.Show("Xin chọn 1 chỉ tiêu", "Thông báo");
                return;
            }

            if (DB.isChildContained(targetID))
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
                    MessageBoxResult result = MessageBox.Show("Xóa chỉ tiêu '" + DB.getTargetNameByID(targetID) + "' ?", "Thông báo", MessageBoxButton.OKCancel);
                    if (result == MessageBoxResult.OK)
                    {
                        string info = DB.deleteTarget(targetID);
                        if (info == "OK")
                        {
                            MessageBox.Show("Đã xóa thành công chỉ tiêu", "Thông báo");
                            loadTreeView();
                            return;
                        }
                    }
                }
            }
        }

        public void setPortInfo(SerialPort portModem)
        {
            port = portModem;
            port.DataReceived += new SerialDataReceivedEventHandler(FileSavedEvent);
            mainWindow.setPortInfo(port);
        }

        public static void FileSavedEvent(object obj, SerialDataReceivedEventArgs e)
        {
            FileTransfer.FileReceiver(obj);
            string path = FileTransfer.getSavedPath();
            MessageBox.Show("Đã nhận file mới tại " + path, "Thông báo");
        }
    }
}
