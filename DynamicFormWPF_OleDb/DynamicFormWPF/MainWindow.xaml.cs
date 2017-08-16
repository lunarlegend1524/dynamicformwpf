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
    using System.Windows.Navigation;
    using System.Windows.Shapes;
    using System.IO;
    using System.Data;
    using Microsoft.Win32;
    using System.Xml.Linq;
    using System.ComponentModel;
    using ADOX;
    using System.Configuration;
    using DevExpress.Xpf.Core;
    using System.IO.Ports;
    using System.Runtime.Serialization.Formatters.Binary;
    using DevExpress.Xpf.Grid;
    using System.Windows.Threading;

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static string DBPath = string.Empty;
        private static string selectedFileName = string.Empty;
        private static List<string> ls = new List<string>();
        private MySortableBindingList<ReportInfo> listRecordInfos = new MySortableBindingList<ReportInfo>();
        private FileInfo[] xmlFiles = null;
        private DataTable dataTableXML = new DataTable();
        private string displayMode = string.Empty; // TreeViewMode and TreeListMode

        private static SerialPort port = new SerialPort();
        private string savedPath = string.Empty;

        DispatcherTimer dispatcherTimer = new DispatcherTimer();

        public MainWindow()
        {
            InitializeComponent();
            dispatcherTimer.Tick += new EventHandler(dispatcherTimer_Tick);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 1);
            dispatcherTimer.Start();
        }

        // auto load file list after 1s
        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            // will disable highligted records 
            //ListFiles();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                if (isServer())
                {
                    _menuItemReport.IsEnabled = true;
                    _btnUpdateDB.IsEnabled = true;
                }

                checkDataFolder();

                _dataGridList.AutoGenerateColumns = false;
                DataGridTextColumn columnFilePath = new DataGridTextColumn();
                columnFilePath.Header = "File Path";
                columnFilePath.Binding = new Binding("Path");
                columnFilePath.Visibility = Visibility.Collapsed;
                DataGridTextColumn columnFileName = new DataGridTextColumn();
                columnFileName.Header = "Tên báo cáo";
                columnFileName.Binding = new Binding("Name");
                columnFileName.Visibility = Visibility.Visible;
                columnFileName.Width = _dataGridList.Width - 138;
                DataGridTextColumn columnFileDate = new DataGridTextColumn();
                columnFileDate.Header = "Ngày gửi";
                columnFileDate.Binding = new Binding("CreationTime");
                columnFileDate.Visibility = Visibility.Visible;
                columnFileDate.Width = 130;

                _dataGridList.Columns.Add(columnFilePath);
                _dataGridList.Columns.Add(columnFileName);
                _dataGridList.Columns.Add(columnFileDate);

                checkExistedDB();

                this.Title = "SEIS - Kết nối đang đóng";
                ThemeManager.SetThemeName(_treeListTarget, "Seven");

                loadTreeView();
                loadTreeList();
                ListFiles();

                _gridTreeList.SetVisible(false);
                _btnSwitchMode.Content = "TreeListMode";
                displayMode = "TreeViewMode";
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        // define client or server
        private bool isServer()
        {
            bool isServer = false;

            string content = string.Empty;

            content = XMLViewer.readStringFromText(AppDomain.CurrentDomain.BaseDirectory + "server.seis");

            if (content == "1")
            {
                isServer = true;
            }
            else
            {
                MessageBox.Show(content + Environment.NewLine + "Chương trình khởi tạo dưới chế độ máy trạm", "Thông báo");
                isServer = false;
            }

            return isServer;
        }

        private void checkDataFolder()
        {
            if (!Directory.Exists(@"C:\data"))
            {
                Directory.CreateDirectory(@"C:\data");
            }

            if (!Directory.Exists(@"C:\data\temp"))
            {
                Directory.CreateDirectory(@"C:\data\temp");
            }
        }

        // check if database has been created
        private void checkExistedDB()
        {
            if (!DB.isConnectedToDB())
            {
                MessageBoxResult result = MessageBox.Show("Chưa có CSDL, Tạo mới tại đường dẫn mặc định ?", "Thông báo", MessageBoxButton.OKCancel);
                if (result == MessageBoxResult.OK)
                {
                    createNewPhysicalDatabase();
                }
                else
                {
                    MessageBoxResult result2 = MessageBox.Show("Chọn CSDL có sẵn?", "Thông báo", MessageBoxButton.OKCancel);
                    if (result2 == MessageBoxResult.OK)
                    {
                        getExistedDB();
                    }
                }
            }

            else
            {
                DBPath = DB.getDBPathFromRegistry();
                _lblDB.Content = "CSDL: " + DBPath;
            }
        }

        // create treeview from database
        public void loadTreeView()
        {
            _wrapPanelDragDrop.Children.Clear();

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

                if (flattenedTree.ContainsKey(Convert.ToInt32(node["ParentID"]))) // add node to existing root
                {
                    DockPanel dock = new DockPanel();
                    Label label = new Label();
                    label.Content = node["TargetName"].ToString();
                    TextBox textBox = new TextBox();
                    textBox.Name = "_txt" + node["TargetID"].ToString();
                    textBox.Width = 40;
                    textBox.AllowDrop = true;
                    textBox.TextAlignment = TextAlignment.Center;
                    textBox.PreviewTextInput += new TextCompositionEventHandler(this._textBox_PreviewTextInput);
                    textBox.TextChanged += new TextChangedEventHandler(this._textbox_TextChanged);
                    dock.Children.Add(textBox);
                    dock.Children.Add(label);
                    treeNode.Header = dock;
                    flattenedTree[Convert.ToInt32(node["ParentID"])].Items.Add(treeNode);
                }
                else // create new tree with LV1 target as root
                {
                    TreeView _treeViewTarget = new TreeView();
                    _treeViewTarget.Items.Clear();
                    DockPanel dock = new DockPanel();
                    Label label = new Label();
                    label.Content = node["TargetName"].ToString();
                    TextBox textBox = new TextBox();
                    textBox.Name = "_txt" + node["TargetID"].ToString();
                    textBox.Width = 40;
                    textBox.AllowDrop = true;
                    textBox.TextAlignment = TextAlignment.Center;
                    textBox.PreviewTextInput += new TextCompositionEventHandler(this._textBox_PreviewTextInput);
                    textBox.TextChanged += new TextChangedEventHandler(this._textbox_TextChanged);
                    dock.Children.Add(textBox);
                    dock.Children.Add(label);
                    treeNode.Header = dock;
                    _treeViewTarget.Items.Add(treeNode);
                    _wrapPanelDragDrop.Children.Add(_treeViewTarget);
                }
                treeNode.IsExpanded = true;
            }
        }

        public void loadTreeList()
        {
            DataTable dt = DB.getTargetList(-1);
            if (dt == null)
            {
                return;
            }
            dt.Columns.Add("QuantityValue", typeof(string));
            dt.Columns.Add("RecordTime", typeof(DateTime));
            _treeListTarget.ItemsSource = dt;
            //_treeListTarget.Columns["RecordTime"].EditSettings.DisplayFormat = "dd/MM/yyyy";
            _treeListView.ExpandAllNodes();
        }

        // occurs when input directly into textbox
        private void _textBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (!char.IsDigit(e.Text[0])) // accept only numbers
            {
                e.Handled = true;
            }
        }

        // occurs when a string is dropped into the textbox
        private void _textbox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string text = string.Empty;
            foreach (char c in ((TextBox)sender).Text)
            {
                if (char.IsDigit(c)) // accept only numbers
                {
                    text += c;
                }
                else { }
            }
            ((TextBox)sender).Text = text;
        }

        private FlowDocument getFlowDocumentText(string filePath)
        {
            FlowDocument document = new FlowDocument();
            try
            {
                Paragraph paragraph = new Paragraph();
                paragraph.Inlines.Add(File.ReadAllText(filePath));
                document = new FlowDocument(paragraph);
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
            return document;
        }

        private void _btnBrowse_Click(object sender, RoutedEventArgs e)
        {
            //OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();

            //// Set filter for file extension and default file extension 
            //dlg.DefaultExt = ".txt";
            //dlg.Filter = "Text documents (.rtf)|*.rtf";

            //// Display OpenFileDialog by calling ShowDialog method 
            //Nullable<bool> result = dlg.ShowDialog();

            //// Get the selected file name and display in a TextBox 
            //if (result == true)
            //{
            //    string filename = dlg.FileName;
            //    _txtFilePath.Text = filename;

            //    Paragraph paragraph = new Paragraph();
            //    paragraph.Inlines.Add(File.ReadAllText(filename));
            //    FlowDocument document = new FlowDocument(paragraph);
            //    _flowDoc.Document = document;
            //}
        }

        private string addRecordToDB(TextBox txt)
        {
            string info = string.Empty;
            string content = txt.Text;
            if (content != string.Empty)
            {
                info = DB.updateContent(content, txt.Name);
            }
            return info;
        }

        private void addRecordToXML(TextBox _txt)
        {
            try
            {
                int targetID = Convert.ToInt32(_txt.Name.Substring(4));

                if (_txt.Text != string.Empty)
                {
                    dataTableXML.Rows.Add(targetID, DB.getParentID(targetID), DB.getTargetNameByID(targetID), _txt.Text, 1, String.Format("{0:dd/MM/yyyy}", DateTime.Now));
                }

                else
                {
                    dataTableXML.Rows.Add(targetID, DB.getParentID(targetID), DB.getTargetNameByID(targetID), _txt.Text, 1, string.Empty);
                }

            }
            catch (Exception) { }
        }

        private void refreshDataTableXML()
        {
            dataTableXML = new DataTable("Record");
            dataTableXML.Columns.Add("TargetID", typeof(int));
            dataTableXML.Columns.Add("ParentID", typeof(int));
            dataTableXML.Columns.Add("TargetName", typeof(string));
            dataTableXML.Columns.Add("QuantityValue", typeof(string));
            dataTableXML.Columns.Add("ClientID", typeof(int));
            dataTableXML.Columns.Add("RecordTime", typeof(string)); // must use string due to a serious bug: cannot create DataTable from xml if first node does not have <RecordTime/>
        }

        private string getTextFromDataTable(DataTable dt)
        {
            string info = string.Empty;
            int i = 0;

            for (i = 0; i < dt.Columns.Count - 1; i++)
            {
                info += dt.Columns[i].ColumnName + " | ";
            }
            info += dt.Columns[i].ColumnName;
            info += Environment.NewLine;

            foreach (DataRow row in dt.Rows)
            {
                object[] array = row.ItemArray;

                for (i = 0; i < array.Length - 1; i++)
                {
                    info += array[i].ToString() + " | ";
                }
                info += (array[i].ToString());
                info += Environment.NewLine;
            }

            return info;
        }

        private void ClearReportList()
        {
            try
            {
                _dataGridList.ItemsSource = null;
                listRecordInfos.Clear();
            }
            catch (Exception) { }
        }

        private void ListFiles()
        {
            ClearReportList();
            string filePath = string.Empty;
            filePath = @"C:\data";
            DirectoryInfo di = new DirectoryInfo(filePath);
            xmlFiles = di.GetFiles("*.xml", SearchOption.TopDirectoryOnly);

            foreach (FileInfo fi in xmlFiles)
            {
                listRecordInfos.Add(new ReportInfo(fi.FullName, fi.Name, fi.CreationTime));
            }
            //_dataGridList.SetBinding(ItemsControl.ItemsSourceProperty, new Binding { Source = listReportInfos });
            _dataGridList.ItemsSource = listRecordInfos;
            _lblList.Content = _dataGridList.Items.Count + " Tệp";
        }

        private void ShowContent(string filePath)
        {
            int targetID = 0;
            string quantity = string.Empty;

            DataTable dt = XMLViewer.getDataTableFromXML(filePath);

            // check if xml file is from correct database

            if (dt == null)
            {
                return;
            }

            //if (!DB.isFromOriginalDatabase(dt) && dt != null) // very slow
            //{
            //    MessageBox.Show("Tệp này không thuộc bộ chỉ tiêu hiện tại", "Thông báo");
            //    return;
            //}

            if (displayMode == "TreeViewMode")
            {
                // set value for TreeViewMode
                foreach (DataRow row in dt.Rows)
                {
                    foreach (Control control in _wrapPanelDragDrop.Children)
                    {
                        if (control.GetType() == typeof(TreeView))
                        {
                            foreach (TreeViewItem root in ((TreeView)control).Items) // loop through LV1 nodes
                            {
                                targetID = Convert.ToInt32(row["TargetID"]);
                                quantity = row["QuantityValue"].ToString();

                                DockPanel dock = root.Header as DockPanel;
                                foreach (Control control2 in dock.Children)
                                {
                                    if (control2.GetType() == typeof(TextBox))
                                    {
                                        if (Convert.ToInt32(((TextBox)control2).Name.Substring(4)) == targetID)
                                        {
                                            ((TextBox)control2).Text = quantity;
                                        }
                                    }
                                }
                                searchByLoopingThroughNodes(root, targetID, quantity);
                            }
                        }
                    }
                }
            }

            if (displayMode == "TreeListMode")
            {
                // set value for TreeListMode
                _treeListTarget.ItemsSource = dt;
                _treeListView.ExpandAllNodes();
            }
        }

        private void DisplaySelectedFileInfo()
        {
            try
            {
                // selectedFileName = gridView.CurrentRow.Cells["ColumnPath"].Value.ToString(); //
                ReportInfo selectedItem = _dataGridList.SelectedItem as ReportInfo;
                selectedFileName = selectedItem.Path.ToString();
                ShowContent(selectedFileName);
            }
            catch (Exception) { }
        }

        private void _btnUpdateDB_Click(object sender, RoutedEventArgs e)
        {
            int number = 0;
            if (displayMode == "TreeViewMode")
            {
                MessageBoxResult result = MessageBox.Show("Nhập tất cả chỉ tiêu vào cơ sở dữ liệu?", "Thông báo", MessageBoxButton.OKCancel);
                if (result == MessageBoxResult.OK)
                {
                    // update content in all textboxes in wrapPanel into DB
                    foreach (Control control in _wrapPanelDragDrop.Children)
                    {
                        if (control.GetType() == typeof(TreeView))
                        {
                            foreach (TreeViewItem root in ((TreeView)control).Items) // loop through LV1 nodes
                            {
                                // add LV1 nodes
                                DockPanel dock = root.Header as DockPanel;
                                foreach (Control control2 in dock.Children)
                                {
                                    if (control2.GetType() == typeof(TextBox))
                                    {
                                        if (((TextBox)control2).Text != string.Empty)
                                        {
                                            addRecordToDB((TextBox)control2);
                                            number++;
                                        }
                                    }
                                }
                                addByLoopingThroughNodes(root, 0, ref number);
                            }
                        }
                    }
                    if (number != 0)
                    {
                        MessageBox.Show("Đã nhập thành công " + number + " chỉ tiêu", "Thông báo");
                    }
                }
                else { }
            }

            else if (displayMode == "TreeListMode")
            {
                string quantity = string.Empty;
                int targetID = 0;
                DateTime date = new DateTime();
                int i = 0;
                MessageBoxResult result = MessageBox.Show("Nhập tất cả chỉ tiêu vào cơ sở dữ liệu?", "Thông báo", MessageBoxButton.OKCancel);
                if (result == MessageBoxResult.OK)
                {
                    TreeListNodeIterator nodeIterator = new TreeListNodeIterator(_treeListView.Nodes, true);
                    while (nodeIterator.MoveNext())
                    {
                        i = nodeIterator.Current.RowHandle;
                        targetID = Convert.ToInt32(_treeListTarget.GetCellValue(i, "TargetID"));
                        try
                        {
                            quantity = _treeListTarget.GetCellValue(i, "QuantityValue").ToString();
                        }
                        catch (Exception)
                        {
                            quantity = string.Empty;
                        }
                        try
                        {
                            date = Convert.ToDateTime(_treeListTarget.GetCellValue(i, "RecordTime"));
                        }
                        catch (Exception)
                        {
                            date = DateTime.Today;
                        }

                        if (quantity != string.Empty && quantity != "0")
                        {
                            DB.updateContentWithDate(targetID, Convert.ToInt32(quantity), date, ref number);
                        }
                    }
                    if (number != 0)
                    {
                        MessageBox.Show("Đã nhập thành công " + number + " chỉ tiêu", "Thông báo");
                    }
                }
            }
        }

        // add to DB or XML by recursive though textbox in each node then continue with the child nodes
        private void addByLoopingThroughNodes(TreeViewItem root, int mode, ref int number) // 0 for DB only, 1 for XML only, 2 for both
        {
            foreach (TreeViewItem node in ((TreeViewItem)root).Items) // loop through LV2 nodes
            {
                if (node != null)
                {
                    DockPanel dock = node.Header as DockPanel;
                    foreach (Control control in dock.Children)
                    {
                        if (control.GetType() == typeof(TextBox))
                        {
                            if (mode == 1 || mode == 2)
                            {
                                addRecordToXML((TextBox)control);
                            }
                            if (((TextBox)control).Text != string.Empty)
                            {
                                if (mode == 0)
                                {
                                    addRecordToDB((TextBox)control);
                                }
                                else if (mode == 1)
                                {
                                    // do nothing
                                }
                                else if (mode == 2)
                                {
                                    addRecordToDB((TextBox)control); // save before sending
                                }
                                number++;
                            }
                        }
                    }
                    addByLoopingThroughNodes(node, mode, ref number); // continue looping in LV3 and deeper nodes
                }
            }
        }

        private void searchByLoopingThroughNodes(TreeViewItem root, int targetID, string quantityValue)
        {
            foreach (TreeViewItem node in ((TreeViewItem)root).Items)
            {
                if (node != null)
                {
                    DockPanel dock = node.Header as DockPanel;
                    foreach (Control control in dock.Children)
                    {
                        if (control.GetType() == typeof(TextBox))
                        {
                            if (Convert.ToInt32(((TextBox)control).Name.Substring(4)) == targetID)
                            {
                                ((TextBox)control).Text = quantityValue;
                                return; // quit after found (performance purpose)
                            }
                        }
                    }
                    searchByLoopingThroughNodes(node, targetID, quantityValue); // continue looping in LV3 and deeper nodes
                }
            }
        }

        private int checkEmptyByLoopingThroughNodes(TreeViewItem root, int i)
        {
            foreach (TreeViewItem node in ((TreeViewItem)root).Items)
            {
                if (node != null)
                {
                    DockPanel dock = node.Header as DockPanel;
                    foreach (Control control in dock.Children)
                    {
                        if (control.GetType() == typeof(TextBox))
                        {
                            if (((TextBox)control).Text == string.Empty)
                            {
                                i++;
                                //return i; // quit after found (performance purpose)
                            }
                        }
                    }
                    i = checkEmptyByLoopingThroughNodes(node, i); // continue looping in LV3 and deeper nodes
                }
            }
            return i;
        }

        private void Menu_File_Exit_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("Thoát chương trình?", "Thông báo", MessageBoxButton.OKCancel);
            if (result == MessageBoxResult.OK)
            {
                try
                {
                    Application.Current.Shutdown();
                    port.Close();
                }
                catch (Exception)
                {
                }
            }
            else
            {
                return;
            }
        }

        private void Menu_File_Open_Click(object sender, RoutedEventArgs e)
        {
            string DBPath = string.Empty;
            OpenFileDialog openDialog = new OpenFileDialog();
            openDialog.InitialDirectory = Convert.ToString(Environment.SpecialFolder.MyDocuments);
            openDialog.Filter = "XML Files (*.xml)|*.xml";
            openDialog.FilterIndex = 1;

            Nullable<bool> result = openDialog.ShowDialog();

            if (result == true)
            {
                loadTargetListFromXML(openDialog.FileName); // update target database from server
                //DBPath = openDialog.FileName;
                //MessageBox.Show(DB.createNewDB(DBPath));
            }
        }

        // load new database
        private void Menu_File_Save_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveDialog = new SaveFileDialog();
            saveDialog.InitialDirectory = @"C:\data";
            saveDialog.Filter = "XML Files (*.xml)|*.xml";
            saveDialog.FilterIndex = 1;

            if (displayMode == "TreeViewMode")
            {
                int i = 0;
                // check if a textbox has no content
                foreach (Control control in _wrapPanelDragDrop.Children)
                {
                    if (control.GetType() == typeof(TreeView))
                    {
                        foreach (TreeViewItem root in ((TreeView)control).Items) // loop through LV1 nodes
                        {
                            i += checkEmptyByLoopingThroughNodes(root, 1);
                        }
                    }
                }
                if (i > 1)
                {
                    int number = 0;
                    MessageBoxResult result2 = MessageBox.Show((i - 1) + " chỉ tiêu chưa có số liệu, tiếp tục?", "Thông báo", MessageBoxButton.OKCancel);
                    if (result2 == MessageBoxResult.OK)
                    {
                        Nullable<bool> result = saveDialog.ShowDialog();

                        if (result == true)
                        {
                            // save inputted records to xml
                            int targetID = 0;
                            refreshDataTableXML();
                            foreach (Control control in _wrapPanelDragDrop.Children)
                            {
                                if (control.GetType() == typeof(TreeView))
                                {
                                    foreach (TreeViewItem root in ((TreeView)control).Items) // loop through LV1 nodes
                                    {
                                        // add LV1 nodes to XML
                                        targetID = Convert.ToInt32(root.Tag);
                                        DockPanel dock = root.Header as DockPanel;
                                        foreach (Control control2 in dock.Children)
                                        {
                                            if (control2.GetType() == typeof(TextBox))
                                            {
                                                if (((TextBox)control2).Text == string.Empty)
                                                {
                                                    dataTableXML.Rows.Add(targetID, DB.getParentID(targetID), DB.getTargetNameByID(targetID), ((TextBox)control2).Text, 1, string.Empty);
                                                }
                                                else if (((TextBox)control2).Text != string.Empty)
                                                {
                                                    dataTableXML.Rows.Add(targetID, DB.getParentID(targetID), DB.getTargetNameByID(targetID), ((TextBox)control2).Text, 1, String.Format("{0:dd/MM/yyyy}", DateTime.Now));
                                                    number++;
                                                }
                                            }
                                        }
                                        addByLoopingThroughNodes(root, 1, ref number);
                                    }
                                }
                            }

                            // create file name with DB name prefix
                            string fileName = "[" + System.IO.Path.GetFileNameWithoutExtension(DBPath) + "]" + "_" + saveDialog.SafeFileName;
                            string filePath = System.IO.Path.GetDirectoryName(saveDialog.FileName);
                            fileName = filePath + "\\" + fileName;
                            dataTableXML.WriteXml(fileName);
                            MessageBox.Show("Đã lưu file tại: " + fileName + Environment.NewLine + " tồng cộng " + number + " chỉ tiêu", "Thông báo");
                            ListFiles();
                        }
                        else { }
                    }
                }

                else
                {
                    MessageBoxResult result2 = MessageBox.Show("Lưu tệp?", "Thông báo", MessageBoxButton.OKCancel);
                    if (result2 == MessageBoxResult.OK)
                    {
                        Nullable<bool> result = saveDialog.ShowDialog();

                        if (result == true)
                        {
                            // save inputted records to xml
                            int targetID = 0;
                            refreshDataTableXML();
                            int number = 0;
                            foreach (Control control in _wrapPanelDragDrop.Children)
                            {
                                if (control.GetType() == typeof(TreeView))
                                {
                                    foreach (TreeViewItem root in ((TreeView)control).Items) // loop through LV1 nodes
                                    {
                                        // add LV1 nodes to XML
                                        targetID = Convert.ToInt32(root.Tag);
                                        DockPanel dock = root.Header as DockPanel;
                                        foreach (Control control2 in dock.Children)
                                        {
                                            if (control2.GetType() == typeof(TextBox))
                                            {
                                                if (((TextBox)control2).Text == string.Empty)
                                                {
                                                    dataTableXML.Rows.Add(targetID, DB.getParentID(targetID), DB.getTargetNameByID(targetID), ((TextBox)control2).Text, 1, string.Empty);
                                                }
                                                else if (((TextBox)control2).Text != string.Empty)
                                                {
                                                    dataTableXML.Rows.Add(targetID, DB.getParentID(targetID), DB.getTargetNameByID(targetID), ((TextBox)control2).Text, 1, String.Format("{0:dd/MM/yyyy}", DateTime.Now));
                                                    number++;
                                                }
                                            }
                                        }
                                        addByLoopingThroughNodes(root, 1, ref number);
                                    }
                                }
                            }
                            dataTableXML.WriteXml(saveDialog.FileName);
                            MessageBox.Show("Đã lưu file tại: " + saveDialog.FileName + Environment.NewLine + " tổng cộng " + number + " chỉ tiêu", "Thông báo");
                            ListFiles();
                        }
                        else { }
                    }
                }
            }
            else if (displayMode == "TreeListMode")
            {
                bool checkAllInputted = true;
                int number = 0;
                int check = 0;
                string quantity = string.Empty;
                int targetID = 0;
                int parentID = 0;
                DateTime date = new DateTime();
                int i = 0;
                int count = 0;
                TreeListNodeIterator nodeIterator = new TreeListNodeIterator(_treeListView.Nodes, true);

                // check if all nodes has quantityValue
                while (nodeIterator.MoveNext())
                {
                    i = nodeIterator.Current.RowHandle;

                    try
                    {
                        check = Convert.ToInt32(_treeListTarget.GetCellValue(i, "QuantityValue"));
                    }
                    catch (Exception)
                    {
                        count++;
                        checkAllInputted = false;
                    }
                }

                if (checkAllInputted == false)
                {
                    MessageBoxResult result2 = MessageBox.Show(count + " chỉ tiêu chưa có số liệu, tiếp tục?", "Thông báo", MessageBoxButton.OKCancel);
                    if (result2 == MessageBoxResult.OK)
                    {
                        Nullable<bool> result = saveDialog.ShowDialog();

                        if (result == true)
                        {
                            // save inputted records to xml
                            refreshDataTableXML();
                            while (nodeIterator.MoveNext())
                            {
                                i = nodeIterator.Current.RowHandle;
                                targetID = Convert.ToInt32(_treeListTarget.GetCellValue(i, "TargetID"));
                                parentID = Convert.ToInt32(_treeListTarget.GetCellValue(i, "ParentID"));
                                try
                                {
                                    quantity = _treeListTarget.GetCellValue(i, "QuantityValue").ToString();
                                }
                                catch (Exception)
                                {
                                    quantity = string.Empty;
                                }
                                try
                                {
                                    date = Convert.ToDateTime(_treeListTarget.GetCellValue(i, "RecordTime"));
                                }
                                catch (Exception)
                                {
                                    date = DateTime.Today;
                                }

                                // insert

                                // do not insert datetime to xml if no quantity is inputted
                                if (quantity == string.Empty)
                                {
                                    dataTableXML.Rows.Add(targetID, parentID, DB.getTargetNameByID(targetID), quantity, 1);
                                }

                                else if (quantity != string.Empty)
                                {
                                    if (date == Convert.ToDateTime("1/01/0001"))
                                    {
                                        date = DateTime.Today;
                                    }

                                    dataTableXML.Rows.Add(targetID, parentID, DB.getTargetNameByID(targetID), quantity, 1, String.Format("{0:dd/MM/yyyy}", date));
                                    number++;
                                }
                            }
                            dataTableXML.WriteXml(saveDialog.FileName);
                            MessageBox.Show("Đã lưu file tại: " + saveDialog.FileName + Environment.NewLine + " tổng cộng " + number + " chỉ tiêu", "Thông báo");
                            ListFiles();
                        }
                        else { }
                    }
                }
                else
                {
                    MessageBoxResult result2 = MessageBox.Show("Lưu tệp ?", "Thông báo", MessageBoxButton.OKCancel);
                    if (result2 == MessageBoxResult.OK)
                    {
                        Nullable<bool> result = saveDialog.ShowDialog();

                        if (result == true)
                        {
                            // save inputted records to xml
                            refreshDataTableXML();
                            while (nodeIterator.MoveNext())
                            {
                                i = nodeIterator.Current.RowHandle;
                                targetID = Convert.ToInt32(_treeListTarget.GetCellValue(i, "TargetID"));
                                parentID = Convert.ToInt32(_treeListTarget.GetCellValue(i, "ParentID"));
                                try
                                {
                                    quantity = _treeListTarget.GetCellValue(i, "QuantityValue").ToString();
                                }
                                catch (Exception)
                                {
                                    quantity = string.Empty;
                                }
                                try
                                {
                                    date = Convert.ToDateTime(_treeListTarget.GetCellValue(i, "RecordTime"));
                                }
                                catch (Exception)
                                {
                                    date = DateTime.Today;
                                }

                                // insert
                                if (date == Convert.ToDateTime("1/01/0001"))
                                {
                                    date = DateTime.Today;
                                }

                                dataTableXML.Rows.Add(targetID, parentID, DB.getTargetNameByID(targetID), quantity, 1, String.Format("{0:dd/MM/yyyy}", date));
                                if (quantity != string.Empty)
                                {
                                    number++;
                                }
                            }
                            dataTableXML.WriteXml(saveDialog.FileName);
                            MessageBox.Show("Đã lưu file tại: " + saveDialog.FileName + Environment.NewLine + " tổng cộng " + number + " chỉ tiêu", "Thông báo");
                            ListFiles();
                        }
                        else { }
                    }
                }
            }
        }

        // create report
        private void Menu_File_Report_Click(object sender, RoutedEventArgs e)
        {
            ReportCreator report = new ReportCreator();
            report.ShowDialog();
        }

        private void Menu_DB_AddNewTarget_Click(object sender, RoutedEventArgs e)
        {
            createNewTarget();
        }

        private void createNewTarget()
        {
            if (DB.isConnectedToDB())
            {
                TargetCreator target = new TargetCreator(this, port);
                target.ShowDialog();
            }
            else
            {
                MessageBox.Show("Chưa kết nối CSDL", "Thông báo");
                return;
            }
        }

        private void setupModem()
        {
            if (DB.isConnectedToDB())
            {
                ModemSetting mds = new ModemSetting(this);
                mds.ShowDialog();
            }
            else
            {
                MessageBox.Show("Chưa kết nối CSDL", "Thông báo");
                return;
            }
        }

        // create new database file physically
        private void Create_New_Database_Click(object sender, RoutedEventArgs e)
        {
            createNewPhysicalDatabase();
        }

        private void Select_New_Database_Click(object sender, RoutedEventArgs e)
        {
            getExistedDB();
        }

        private void createNewPhysicalDatabase()
        {
            SaveFileDialog saveDialog = new SaveFileDialog();
            //saveDialog.InitialDirectory = Convert.ToString(Environment.SpecialFolder.MyDocuments);
            saveDialog.InitialDirectory = @"C:\data";
            saveDialog.Filter = "Access Database File (*.accdb)|*.accdb";
            saveDialog.FilterIndex = 1;

            Nullable<bool> result = saveDialog.ShowDialog();

            if (result == true)
            {
                DBPath = saveDialog.FileName;
                MessageBox.Show(DB.createNewDB(DBPath), "Thông báo");
                _lblDB.Content = "CSDL: " + DBPath;
            }

            if (isServer())
            {
                if (DB.isDataEmpty())
                {
                    MessageBoxResult result2 = MessageBox.Show("Chưa có chỉ tiêu nào, Tạo mới?", "Thông báo", MessageBoxButton.OKCancel);
                    if (result2 == MessageBoxResult.OK)
                    {
                        TargetCreator target = new TargetCreator(this, port);
                        target.ShowDialog();
                    }
                    else { }
                }
            }
            loadTreeView();
            loadTreeList();
        }

        private void getExistedDB()
        {
            OpenFileDialog openDialog = new OpenFileDialog();
            openDialog.InitialDirectory = Convert.ToString(Environment.SpecialFolder.MyDocuments);
            openDialog.Filter = "Access Database File (*.accdb)|*.accdb";
            openDialog.FilterIndex = 1;

            Nullable<bool> result = openDialog.ShowDialog();

            if (result == true)
            {
                DBPath = openDialog.FileName;
                RegistryKey appKey = Registry.CurrentUser.CreateSubKey(@"Software\" + Registry.CurrentUser.Name + @"\SEIS");
                appKey.SetValue("DatabasePath", DBPath);

                DB.changeAppConfigSetting();
                DB.refreshCSBConnectionString();

                MessageBox.Show("Đã chọn CSDL tại đường dẫn " + DBPath, "Thông báo");
                _lblDB.Content = "CSDL: " + DBPath;

                loadTreeView();
                loadTreeList();
            }
        }

        private void _dataGridList_SelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
        {
            //loadTreeView(); // clear textbox values
            //loadTreeList();
            DisplaySelectedFileInfo();
        }

        // set up transfering speed
        private void Menu_Modem_Settings_Click(object sender, RoutedEventArgs e)
        {
            setupModem();
        }

        public void setPortInfo(SerialPort portModem)
        {
            port = portModem;
            port.DataReceived += new SerialDataReceivedEventHandler(FileSavedEvent);
            if (port.IsOpen)
            {
                this.Title = "SEIS - Đang kết nối với cổng " + port.PortName;
            }
            else
            {
                this.Title = "SEIS - Kết nối đang đóng";
            }
        }

        // occurs after file has transfered to
        private void FileSavedEvent(object obj, SerialDataReceivedEventArgs e)
        {
            FileTransfer.FileReceiver(obj);
            string path = FileTransfer.getSavedPath();
            MessageBox.Show("Đã nhận file mới tại " + path, "Thông báo");
            ListFiles();
        }

        // send to server as *.xml file
        private void _btnSend_Click(object sender, RoutedEventArgs e)
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
                if (displayMode == "TreeViewMode")
                {
                    int i = 0;
                    // check if a textbox has no content
                    foreach (Control control in _wrapPanelDragDrop.Children)
                    {
                        if (control.GetType() == typeof(TreeView))
                        {
                            foreach (TreeViewItem root in ((TreeView)control).Items) // loop through LV1 nodes
                            {
                                i = checkEmptyByLoopingThroughNodes(root, 1);
                            }
                        }
                    }
                    if (i > 1)
                    {
                        int number = 0;
                        MessageBoxResult result2 = MessageBox.Show((i - 1) + " chỉ tiêu chưa có số liệu, tiếp tục?", "Thông báo", MessageBoxButton.OKCancel);
                        if (result2 == MessageBoxResult.OK)
                        {
                            // save inputted records to xml
                            int targetID = 0;
                            refreshDataTableXML();
                            foreach (Control control in _wrapPanelDragDrop.Children)
                            {
                                if (control.GetType() == typeof(TreeView))
                                {
                                    foreach (TreeViewItem root in ((TreeView)control).Items) // loop through LV1 nodes
                                    {
                                        // add LV1 nodes to XML
                                        targetID = Convert.ToInt32(root.Tag);
                                        DockPanel dock = root.Header as DockPanel;
                                        foreach (Control control2 in dock.Children)
                                        {
                                            if (control2.GetType() == typeof(TextBox))
                                            {
                                                if (((TextBox)control2).Text == string.Empty)
                                                {
                                                    dataTableXML.Rows.Add(targetID, DB.getParentID(targetID), DB.getTargetNameByID(targetID), ((TextBox)control2).Text, 1, string.Empty);
                                                }
                                                else if (((TextBox)control2).Text != string.Empty)
                                                {
                                                    dataTableXML.Rows.Add(targetID, DB.getParentID(targetID), DB.getTargetNameByID(targetID), ((TextBox)control2).Text, 1, String.Format("{0:dd/MM/yyyy}", DateTime.Now));
                                                    number++;
                                                }
                                            }
                                        }
                                        addByLoopingThroughNodes(root, 1, ref number);
                                    }
                                }
                            }
                        }
                    }

                    else
                    {
                        // save inputted records to xml
                        int targetID = 0;
                        refreshDataTableXML();
                        int number = 0;
                        foreach (Control control in _wrapPanelDragDrop.Children)
                        {
                            if (control.GetType() == typeof(TreeView))
                            {
                                foreach (TreeViewItem root in ((TreeView)control).Items) // loop through LV1 nodes
                                {
                                    // add LV1 nodes to XML
                                    targetID = Convert.ToInt32(root.Tag);
                                    DockPanel dock = root.Header as DockPanel;
                                    foreach (Control control2 in dock.Children)
                                    {
                                        if (control2.GetType() == typeof(TextBox))
                                        {
                                            if (((TextBox)control2).Text == string.Empty)
                                            {
                                                dataTableXML.Rows.Add(targetID, DB.getParentID(targetID), DB.getTargetNameByID(targetID), ((TextBox)control2).Text, 1, string.Empty);
                                            }
                                            else if (((TextBox)control2).Text != string.Empty)
                                            {
                                                dataTableXML.Rows.Add(targetID, DB.getParentID(targetID), DB.getTargetNameByID(targetID), ((TextBox)control2).Text, 1, String.Format("{0:dd/MM/yyyy}", DateTime.Now));
                                                number++;
                                            }
                                        }
                                    }
                                    addByLoopingThroughNodes(root, 1, ref number);
                                }
                            }
                        }
                    }
                }
                else if (displayMode == "TreeListMode")
                {
                    bool checkAllInputted = true;
                    int number = 0;
                    int check = 0;
                    string quantity = string.Empty;
                    int targetID = 0;
                    int parentID = 0;
                    DateTime date = new DateTime();
                    int i = 0;
                    int count = 0;
                    TreeListNodeIterator nodeIterator = new TreeListNodeIterator(_treeListView.Nodes, true);

                    // check if all nodes has quantityValue
                    while (nodeIterator.MoveNext())
                    {
                        i = nodeIterator.Current.RowHandle;

                        try
                        {
                            check = Convert.ToInt32(_treeListTarget.GetCellValue(i, "QuantityValue"));
                        }
                        catch (Exception)
                        {
                            count++;
                            checkAllInputted = false;
                        }
                    }

                    if (checkAllInputted == false)
                    {
                        MessageBoxResult result2 = MessageBox.Show(count + " chỉ tiêu chưa có số liệu, tiếp tục?", "Thông báo", MessageBoxButton.OKCancel);
                        if (result2 == MessageBoxResult.OK)
                        {
                            // save inputted records to xml
                            refreshDataTableXML();
                            while (nodeIterator.MoveNext())
                            {
                                i = nodeIterator.Current.RowHandle;
                                targetID = Convert.ToInt32(_treeListTarget.GetCellValue(i, "TargetID"));
                                parentID = Convert.ToInt32(_treeListTarget.GetCellValue(i, "ParentID"));
                                try
                                {
                                    quantity = _treeListTarget.GetCellValue(i, "QuantityValue").ToString();
                                }
                                catch (Exception)
                                {
                                    quantity = string.Empty;
                                }
                                try
                                {
                                    date = Convert.ToDateTime(_treeListTarget.GetCellValue(i, "RecordTime"));
                                }
                                catch (Exception)
                                {
                                    date = DateTime.Today;
                                }

                                // insert

                                // do not insert datetime to xml if no quantity is inputted
                                if (quantity == string.Empty)
                                {
                                    dataTableXML.Rows.Add(targetID, parentID, DB.getTargetNameByID(targetID), quantity, 1);
                                }

                                else if (quantity != string.Empty)
                                {
                                    if (date == Convert.ToDateTime("1/01/0001"))
                                    {
                                        date = DateTime.Today;
                                    }

                                    dataTableXML.Rows.Add(targetID, parentID, DB.getTargetNameByID(targetID), quantity, 1, String.Format("{0:dd/MM/yyyy}", date));
                                    number++;
                                }
                            }
                        }
                    }
                    else
                    {
                        // save inputted records to xml
                        refreshDataTableXML();
                        while (nodeIterator.MoveNext())
                        {
                            i = nodeIterator.Current.RowHandle;
                            targetID = Convert.ToInt32(_treeListTarget.GetCellValue(i, "TargetID"));
                            parentID = Convert.ToInt32(_treeListTarget.GetCellValue(i, "ParentID"));
                            try
                            {
                                quantity = _treeListTarget.GetCellValue(i, "QuantityValue").ToString();
                            }
                            catch (Exception)
                            {
                                quantity = string.Empty;
                            }
                            try
                            {
                                date = Convert.ToDateTime(_treeListTarget.GetCellValue(i, "RecordTime"));
                            }
                            catch (Exception)
                            {
                                date = DateTime.Today;
                            }

                            // insert
                            if (date == Convert.ToDateTime("1/01/0001"))
                            {
                                date = DateTime.Today;
                            }

                            dataTableXML.Rows.Add(targetID, parentID, DB.getTargetNameByID(targetID), quantity, 1, String.Format("{0:dd/MM/yyyy}", date));
                            if (quantity != string.Empty)
                            {
                                number++;
                            }
                        }
                    }
                }

                // file name must have specific client info
                dataTableXML.WriteXml(@"C:\data\temp\tempData.xml");
                if (FileTransfer.FileTransferer(@"C:\data\temp\tempData .xml", port) == "OK")
                {
                    MessageBox.Show("Đã gửi báo cáo lên máy chủ", "Thông báo");
                    return;
                }
                else { }
            }
        }

        // update targetlist from server into client database
        private void loadTargetListFromXML(string filePath)
        {
            int id = 0;
            List<int> listID = new List<int>();
            DataTable dt = XMLViewer.getDataTableFromXML(filePath);

            if (!DB.isFromOriginalDatabase(dt) && !DB.isDataEmpty())
            {
                MessageBox.Show("Bộ chỉ tiêu không thuộc cơ sở dữ liệu hiện tại, xin chọn cơ sở dữ liệu khác");
                return;
            }

            MessageBoxResult result = MessageBox.Show("Đồng bộ chỉ tiêu với máy chủ ?", "Thông báo", MessageBoxButton.OKCancel);
            if (result == MessageBoxResult.OK)
            {
                string info = string.Empty;

                int targetID = 0;
                int parentID = 0;
                string targetName = string.Empty;

                foreach (DataRow row in dt.Rows)
                {
                    try
                    {
                        targetID = Convert.ToInt32(row["TargetID"]);
                        parentID = Convert.ToInt32(row["ParentID"]);
                        targetName = row["TargetName"].ToString();
                        info = DB.updateTargetFromXMLtoDB(targetID, parentID, targetName, ref id);
                        if (info != "OK")
                        {
                            MessageBox.Show(info);
                            return;
                        }
                        else if (info == "OK" && id != 0)
                        {
                            listID.Add(id);
                        }
                    }
                    catch (Exception e)
                    {
                        MessageBox.Show(e.Message);
                        return;
                    }
                }
                if (listID.Count > 0)
                {
                    string targetNames = string.Empty;
                    loadTreeView();
                    loadTreeList();
                    foreach (int i in listID)
                    {
                        targetNames += DB.getTargetNameByID(i) + Environment.NewLine;
                    }
                    MessageBox.Show("Đã đồng bộ chỉ tiêu với máy chủ, có " + listID.Count + " chỉ tiêu bổ sung: "
                        + Environment.NewLine + targetNames, "Thông báo");
                }
                else
                {
                    MessageBox.Show("Chỉ tiêu đã được đồng bộ", "Thông báo");
                    return;
                }
            }
        }

        private void _btnSwitchMode_Click(object sender, RoutedEventArgs e)
        {
            if (_btnSwitchMode.Content.ToString() == "TreeListMode")
            {
                _gridTreeList.SetVisible(true);
                _scrollViewerTreeView.SetVisible(false);
                _btnSwitchMode.Content = "TreeViewMode";
                displayMode = "TreeListMode";

                ShowContent(selectedFileName);

                return;
            }

            if (_btnSwitchMode.Content.ToString() == "TreeViewMode")
            {
                _gridTreeList.SetVisible(false);
                _scrollViewerTreeView.SetVisible(true);
                _btnSwitchMode.Content = "TreeListMode";
                displayMode = "TreeViewMode";

                ShowContent(selectedFileName);

                return;
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            try
            {
                port.Close();
            }
            catch (Exception)
            {
            }
        }

        private void _btnClearTree_Click(object sender, RoutedEventArgs e)
        {
            if (displayMode == "TreeViewMode")
            {
                MessageBoxResult result = MessageBox.Show("Xóa hết các số liệu trong bảng, nhập lại từ đầu ?", "Thông báo", MessageBoxButton.OKCancel);
                if (result == MessageBoxResult.OK)
                {
                    loadTreeView();
                }
            }
            else
            {
                MessageBoxResult result = MessageBox.Show("Xóa hết các số liệu trong bảng, nhập lại từ đầu ?", "Thông báo", MessageBoxButton.OKCancel);
                if (result == MessageBoxResult.OK)
                {
                    loadTreeList();
                }
            }
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            
        }
    }
}
