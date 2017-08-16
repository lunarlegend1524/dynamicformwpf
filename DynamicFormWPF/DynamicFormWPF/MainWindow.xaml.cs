namespace DynamicFormWPF
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Data;
    using System.Data.SqlClient;
    using System.IO;
    using System.IO.Ports;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Data;
    using System.Windows.Documents;
    using System.Windows.Input;
    using System.Windows.Threading;
    using DevExpress.Xpf.Core;
    using DevExpress.Xpf.Grid;
    using DynamicFormWPF.Classes_Data;
    using Microsoft.Win32;

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static string selectedFileName = string.Empty;
        private static DateTime selectedCreationTime = new DateTime();
        private static List<string> ls = new List<string>();
        private FileDisplayBindingList<ReportInfo> listRecordInfos = new FileDisplayBindingList<ReportInfo>();
        private FileInfo[] xmlFiles = null;
        private DataTable dataTableXML = new DataTable();
        private string displayMode = string.Empty; // TreeViewMode and TreeListMode

        private static SerialPort port = new SerialPort();
        private static string dialStatus = string.Empty;

        private string savedPath = string.Empty;
        private bool isClient = false;

        private DispatcherTimer dispatcherTimer = new DispatcherTimer();

        public MainWindow()
        {
            InitializeComponent();

            //port.DataReceived += new SerialDataReceivedEventHandler(FileSavedEvent);

            //dispatcherTimer.Tick += new EventHandler(dispatcherTimer_Tick);
            //dispatcherTimer.Interval = new TimeSpan(0, 0, 5);
            //dispatcherTimer.Start();

            port = new SerialPort();
        }

        // auto load file list after 1s
        //private void dispatcherTimer_Tick(object sender, EventArgs e)
        //{
        // will disable highlighted records
        //ListFiles();
        //}

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // check if machine is server or client and return global variable 'isClient'
                if (!RegistryEditor.isServer(ref isClient))
                {
                    _menuItemDB.IsEnabled = false;
                    Menu_File_Report.IsEnabled = false;
                    _btnUpdateDB.IsEnabled = false;
                    _lblDB.Content = "Bộ chỉ tiêu mặc định: " + System.IO.Path.GetFileNameWithoutExtension(RegistryEditor.getConnectionStringFromRegistry());
                    Menu_File_Open.Header = "Mở tệp (chọn bộ chỉ tiêu mặc định)";
                }

                // create default folders in C:\
                RegistryEditor.checkDataFolder();

                _dataGridList.AutoGenerateColumns = false;
                DataGridTextColumn columnFilePath = new DataGridTextColumn();
                columnFilePath.Header = "File Path";
                columnFilePath.Binding = new Binding("Path");
                columnFilePath.Visibility = Visibility.Collapsed;
                DataGridTextColumn columnFileName = new DataGridTextColumn();
                columnFileName.Header = "Tên báo cáo";
                columnFileName.Binding = new Binding("Name");
                columnFileName.Visibility = Visibility.Visible;
                columnFileName.Width = _dataGridList.Width - 130;
                DataGridTextColumn columnFileDate = new DataGridTextColumn();
                columnFileDate.Header = "Ngày tạo";
                columnFileDate.Binding = new Binding("CreationTime");
                columnFileDate.Visibility = Visibility.Visible;
                columnFileDate.Width = 70;
                DataGridCheckBoxColumn columnFileSaved = new DataGridCheckBoxColumn();
                columnFileSaved.Header = "Đã lưu";
                columnFileSaved.Binding = new Binding("IsSaved");
                columnFileSaved.Visibility = Visibility.Visible;
                columnFileSaved.Width = 50;

                _dataGridList.Columns.Add(columnFilePath);
                _dataGridList.Columns.Add(columnFileName);
                _dataGridList.Columns.Add(columnFileDate);
                _dataGridList.Columns.Add(columnFileSaved);
                _expanderFilter.Visibility = Visibility.Hidden;

                this.Title = "SEIS - Kết nối đang đóng";

                ThemeManager.SetThemeName(_treeListTarget, "Seven");

                _gridTreeList.SetVisible(true);
                _btnSwitchMode.Content = "TreeViewMode";
                displayMode = "TreeListMode";

                // use only TreeListView
                _btnSwitchMode.Visibility = Visibility.Hidden;

                // get the connectionstring from registry then update to app.config
                try
                {
                    RegistryEditor.setNewConfigSettingWithConnectionString(RegistryEditor.getConnectionStringFromRegistry(), isClient);
                    checkExistedDB();
                }
                catch { }

                ListFiles();

                //loadTreeView();
                loadTreeList();
            }
            catch (Exception)
            {
                // usually happens when using client and server in the same machine, which leads to mistaken in connectionstring
            }
        }

        // check if database has been created
        private void checkExistedDB()
        {
            if (isClient)
            {
                if (!testConnectionStringWorking())
                {
                    MessageBoxResult result = MessageBox.Show("Không tìm thấy file chứa bộ chỉ tiêu, chọn file khác", "Thông báo", MessageBoxButton.OKCancel);
                    if (result == MessageBoxResult.OK)
                    {
                        string fileName = string.Empty;
                        OpenFileDialog openDialog = new OpenFileDialog();
                        openDialog.InitialDirectory = Convert.ToString(Environment.SpecialFolder.MyDocuments);
                        openDialog.Filter = "XML Files (*.xml)|*.xml";
                        openDialog.FilterIndex = 1;

                        Nullable<bool> result2 = openDialog.ShowDialog();

                        if (result2 == true)
                        {
                            fileName = openDialog.FileName;
                            loadTargetListFromXML(fileName);
                        }
                        else
                        {
                            _lblDB.Content = "Chưa chọn bộ chỉ tiêu mặc định";
                        }
                    }

                    else
                    {
                        _lblDB.Content = "Chưa chọn bộ chỉ tiêu mặc định";
                    }
                }
            }
            else
            {
                if (!testConnectionStringWorking())
                {
                    MessageBoxResult result = MessageBox.Show("Chưa có kết nối với CSDL, tùy chỉnh xâu kết nối ?", "Thông báo", MessageBoxButton.OKCancel);
                    if (result == MessageBoxResult.OK)
                    {
                        ConnectionStringModifier csm = new ConnectionStringModifier(this);
                        csm.ShowDialog();
                    }
                    else
                    {
                    }
                }

                else
                {
                    _lblDB.Content = "CSDL: " + DB.getDBNameFromConnectionString();
                }
            }
        }

        private bool testConnectionStringWorking()
        {
            if (isClient)
            {
                DataTable dt = new DataTable();

                dt = XMLProcessor.getDataTableFromXML(RegistryEditor.getConnectionStringFromRegistry());

                if (dt != null)
                {
                    return true;
                }

                else
                {
                    return false;
                }
            }

            else
            {
                try
                {
                    using (SqlConnection conn = new SqlConnection(getConnectionStringFromRegistry()))
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
        }

        public static string getConnectionStringFromRegistry()
        {
            string info = string.Empty;
            RegistryKey appKey = Registry.CurrentUser.OpenSubKey(@"Software\SEIS");
            try
            {
                info = appKey.GetValue("DatabasePath", "").ToString();
            }
            catch (Exception)
            {
                info = "";
            }
            return info;
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
            if (isClient)
            {
                // load target list from xml file saved to registry
                ShowContent(RegistryEditor.getConnectionStringFromRegistry());
            }

            else
            {
                DataTable dt = DB.getTargetList(-1);

                if (dt != null)
                {
                    dt.Columns.Add("QuantityValue", typeof(string));
                    dt.Columns.Add("RecordTime", typeof(DateTime));
                }

                _lblDB.Content = "CSDL: " + DB.getDBNameFromConnectionString();
                _treeListTarget.ItemsSource = dt;

                //_treeListTarget.Columns["RecordTime"].EditSettings.DisplayFormat = "dd/MM/yyyy";
                _treeListView.ExpandAllNodes();
                ListFiles();
            }
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
                    dataTableXML.Rows.Add(targetID, DB.getParentID(targetID, "Target"), DB.getNameByID(targetID, "Target"), _txt.Text, 1, String.Format("{0:dd/MM/yyyy}", DateTime.Now));
                }

                else
                {
                    dataTableXML.Rows.Add(targetID, DB.getParentID(targetID, "Target"), DB.getNameByID(targetID, "Target"), _txt.Text, 1, string.Empty);
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

        private void LoadExpander()
        {
        }

        private void ListFiles()
        {
            ClearReportList();
            string filePath = string.Empty;
            filePath = @"C:\data";
            DirectoryInfo di = new DirectoryInfo(filePath);
            xmlFiles = di.GetFiles("*.xml", SearchOption.TopDirectoryOnly);

            if (isClient)
            {
                foreach (FileInfo fi in xmlFiles)
                {
                    listRecordInfos.Add(new ReportInfo(fi.FullName, fi.Name, fi.CreationTime, false));
                }
            }

            else
            {
                foreach (FileInfo fi in xmlFiles)
                {
                    if (checkFileFromCurrentDB(fi.Name) && checkFileIsRecordOrSync(fi.Name) == "Report")
                    {
                        // file of which value has not been inserted to DB can be listed here
                        if (!DB.isExistedFile(fi.Name, fi.CreationTime))
                        {
                            DB.addNewFile(fi.Name, fi.CreationTime);
                            listRecordInfos.Add(new ReportInfo(fi.FullName, fi.Name, fi.CreationTime, false));
                        }

                        else
                        {
                            if (!DB.isFileSavedToDB(fi.Name, fi.CreationTime))
                            {
                                listRecordInfos.Add(new ReportInfo(fi.FullName, fi.Name, fi.CreationTime, false));
                            }
                            else
                            {
                                listRecordInfos.Add(new ReportInfo(fi.FullName, fi.Name, fi.CreationTime, true));
                            }
                        }
                    }
                }
            }

            //_dataGridList.SetBinding(ItemsControl.ItemsSourceProperty, new Binding { Source = listReportInfos });
            try
            {
                _dataGridList.ItemsSource = listRecordInfos;
                _lblList.Content = _dataGridList.Items.Count + " Tệp";
            }
            catch (Exception) { }
        }

        // check if file is created using current database
        private bool checkFileFromCurrentDB(string fileName)
        {
            string[] s;

            try
            {
                s = fileName.Split('_');

                // s[0] = "[DBName]"
                fileName = s[0];
                fileName = fileName.Substring(1, fileName.Length - 1);
                fileName = fileName.Substring(0, fileName.Length - 1);

                if (fileName == DB.getDBNameFromConnectionString())
                {
                    return true;
                }

                else
                {
                    return false;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        private string getDBNameFromFileName(string fileName)
        {
            string[] s;
            fileName = System.IO.Path.GetFileName(fileName);

            try
            {
                s = fileName.Split('_');

                // s[0] = "[DBName]"
                fileName = s[0];
                fileName = fileName.Substring(1, fileName.Length - 1);
                fileName = fileName.Substring(0, fileName.Length - 1);

                return fileName;
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }

        private string getClientNameFromFileName(string fileName)
        {
            string[] s;
            fileName = System.IO.Path.GetFileName(fileName);

            try
            {
                s = fileName.Split('_');

                // s[1] = "[ClientName]"
                fileName = s[1];
                fileName = fileName.Substring(1, fileName.Length - 1);
                fileName = fileName.Substring(0, fileName.Length - 1);

                return fileName;
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }

        // [Type] = [Report] or [Sync]
        private string checkFileIsRecordOrSync(string fileName)
        {
            string[] s;

            try
            {
                s = fileName.Split('_');

                // s[2] = "[Type]"
                fileName = s[2];
                fileName = fileName.Substring(1, fileName.Length - 1);
                fileName = fileName.Substring(0, fileName.Length - 1);

                return fileName;
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }

        private void ShowContent(string filePath)
        {
            int targetID = 0;
            string quantity = string.Empty;

            DataTable dt = XMLProcessor.getDataTableFromXML(filePath);

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
                // in case user loads a targetlist file
                if (!dt.Columns.Contains("QuantityValue") && !dt.Columns.Contains("RecordTime"))
                {
                    dt.Columns.Add("QuantityValue", typeof(string));
                    dt.Columns.Add("RecordTime", typeof(DateTime));
                }

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
                selectedCreationTime = selectedItem.CreationTime;

                DataTable dt = XMLProcessor.getDataTableFromXML(selectedFileName);

                if (dt == null)
                {
                    MessageBox.Show("Tệp bị lỗi hoặc không hợp lệ", "Thông báo");
                    return;
                }

                else
                {
                    ShowContent(selectedFileName);
                }
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
                string error = string.Empty;
                string quantity = string.Empty;
                int targetID = 0;
                DateTime date = new DateTime();
                int i = 0;

                if (DB.isFileSavedToDB(System.IO.Path.GetFileName(selectedFileName), selectedCreationTime))
                {
                    MessageBoxResult result = MessageBox.Show("Tệp '" + System.IO.Path.GetFileName(selectedFileName) + "' " + Environment.NewLine + " đã được lưu vào CSDL, lưu lại ?", "Thông báo", MessageBoxButton.OKCancel);
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
                                error += DB.updateContentWithDate(targetID, Convert.ToInt32(quantity), date, ref number);
                            }
                        }

                        if (error.Contains("NotOK"))
                        {
                            MessageBox.Show("Có lỗi khi nhập dữ liệu, xin kiểm tra lại cơ sở dữ liệu", "Thông báo");
                            return;
                        }

                        else
                        {
                            // no need to set again
                            //DB.setFileIsSaved(System.IO.Path.GetFileName(selectedFileName), selectedCreationTime);
                        }

                        ListFiles();
                        if (number != 0)
                        {
                            MessageBox.Show("Đã nhập thành công " + number + " chỉ tiêu", "Thông báo");
                        }
                    }
                }

                else
                {
                    MessageBoxResult result2 = MessageBox.Show("Nhập dữ liệu từ tệp '" + System.IO.Path.GetFileName(selectedFileName) + "' vào CSDL ?", "Thông báo", MessageBoxButton.OKCancel);
                    if (result2 == MessageBoxResult.OK)
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
                                error += DB.updateContentWithDate(targetID, Convert.ToInt32(quantity), date, ref number);
                            }
                        }

                        if (error.Contains("NotOK"))
                        {
                            MessageBox.Show("Có lỗi khi nhập dữ liệu, xin kiểm tra lại cơ sở dữ liệu", "Thông báo");
                            return;
                        }

                        else
                        {
                            DB.setFileIsSaved(System.IO.Path.GetFileName(selectedFileName), selectedCreationTime);
                        }

                        ListFiles();
                        if (number != 0)
                        {
                            MessageBox.Show("Đã nhập thành công " + number + " chỉ tiêu", "Thông báo");
                        }
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
                    port.Close();
                    Application.Current.Shutdown();
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
            string fileName = string.Empty;
            OpenFileDialog openDialog = new OpenFileDialog();
            openDialog.InitialDirectory = Convert.ToString(Environment.SpecialFolder.MyDocuments);
            openDialog.Filter = "XML Files (*.xml)|*.xml";
            openDialog.FilterIndex = 1;

            Nullable<bool> result = openDialog.ShowDialog();

            if (result == true)
            {
                fileName = openDialog.FileName;

                DataTable dt = XMLProcessor.getDataTableFromXML(fileName);

                if (dt == null)
                {
                    MessageBox.Show("Tệp bị lỗi hoặc không hợp lệ", "Thông báo");
                    Menu_File_Open_Click(null, null);
                }

                else
                {
                    if (isClient)
                    {
                        loadTargetListFromXML(fileName);
                        ShowContent(fileName);
                    }

                    else
                    {
                        if (checkFileIsRecordOrSync(fileName) == "Sync")
                        {
                            MessageBoxResult result2 = MessageBox.Show("File chứa dữ liệu đồng bộ, thực hiện đồng bộ ?", "Thông báo", MessageBoxButton.OKCancel);
                            if (result2 == MessageBoxResult.OK)
                            {
                                loadTargetListFromXML(fileName);
                            }
                        }

                        else
                        {
                            ShowContent(fileName);
                        }
                    }
                }
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
                                                    dataTableXML.Rows.Add(targetID, DB.getParentID(targetID, "Target"), DB.getNameByID(targetID, "Target"), ((TextBox)control2).Text, 1, string.Empty);
                                                }
                                                else if (((TextBox)control2).Text != string.Empty)
                                                {
                                                    dataTableXML.Rows.Add(targetID, DB.getParentID(targetID, "Target"), DB.getNameByID(targetID, "Target"), ((TextBox)control2).Text, 1, String.Format("{0:dd/MM/yyyy}", DateTime.Now));
                                                    number++;
                                                }
                                            }
                                        }
                                        addByLoopingThroughNodes(root, 1, ref number);
                                    }
                                }
                            }

                            // create file name with DB name prefix
                            string fileName = "[" + DB.getDBNameFromConnectionString() + "]" + "_" + saveDialog.SafeFileName;
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
                                                    dataTableXML.Rows.Add(targetID, DB.getParentID(targetID, "Target"), DB.getNameByID(targetID, "Target"), ((TextBox)control2).Text, 1, string.Empty);
                                                }
                                                else if (((TextBox)control2).Text != string.Empty)
                                                {
                                                    dataTableXML.Rows.Add(targetID, DB.getParentID(targetID, "Target"), DB.getNameByID(targetID, "Target"), ((TextBox)control2).Text, 1, String.Format("{0:dd/MM/yyyy}", DateTime.Now));
                                                    number++;
                                                }
                                            }
                                        }
                                        addByLoopingThroughNodes(root, 1, ref number);
                                    }
                                }
                            }

                            // create file name with DB name prefix
                            string fileName = "[" + DB.getDBNameFromConnectionString() + "]" + "_" + saveDialog.SafeFileName;
                            string filePath = System.IO.Path.GetDirectoryName(saveDialog.FileName);
                            fileName = filePath + "\\" + fileName;
                            dataTableXML.WriteXml(fileName);
                            MessageBox.Show("Đã lưu file tại: " + fileName + Environment.NewLine + " tổng cộng " + number + " chỉ tiêu", "Thông báo");
                            ListFiles();
                        }
                        else { }
                    }
                }
            }
            else if (displayMode == "TreeListMode")
            {
                bool checkAllInputted = true;
                int check = 0;
                string quantity = string.Empty;
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
                            string DBName = getDBNameFromFileName(RegistryEditor.getConnectionStringFromRegistry());
                            string clientName = getClientNameFromFileName(RegistryEditor.getConnectionStringFromRegistry());
                            string info = XMLProcessor.saveXMLFileTLM(saveDialog.FileName, DBName, clientName, _treeListTarget, _treeListView, isClient);

                            MessageBox.Show("Đã lưu file " + System.IO.Path.GetFileName(info) + Environment.NewLine + " tại: " + System.IO.Path.GetDirectoryName(info) + "", "Thông báo");

                            ListFiles();
                        }
                        else { }
                    }
                }
                else
                {
                    Nullable<bool> result = saveDialog.ShowDialog();

                    if (result == true)
                    {
                        string DBName = getDBNameFromFileName(RegistryEditor.getConnectionStringFromRegistry());
                        string clientName = getClientNameFromFileName(RegistryEditor.getConnectionStringFromRegistry());
                        string info = XMLProcessor.saveXMLFileTLM(saveDialog.FileName, DBName, clientName, _treeListTarget, _treeListView, isClient);

                        MessageBox.Show("Đã lưu file " + System.IO.Path.GetFileName(info) + Environment.NewLine + " tại: " + System.IO.Path.GetDirectoryName(info) + "", "Thông báo");

                        ListFiles();
                    }
                    else { }
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

        private void Menu_DB_AddNewClient_Click(object sender, RoutedEventArgs e)
        {
            ClientList cl = new ClientList(this);
            cl.Show(); // do not show dialog. user can work with parent form
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
            ModemSetting mds = new ModemSetting(this, port);
            mds.ShowDialog();
        }

        // create new database file physically
        private void Create_New_Database_Click(object sender, RoutedEventArgs e)
        {
            //createNewPhysicalDatabase();
            createNewDatabase();
        }

        private void Select_New_Database_Click(object sender, RoutedEventArgs e)
        {
            getExistedDB();
        }

        private void Modify_ConnectionString_Click(object sender, RoutedEventArgs e)
        {
            ConnectionStringModifier csm = new ConnectionStringModifier(this);
            csm.ShowDialog();
        }

        //private void createNewPhysicalDatabase()
        //{
        //    SaveFileDialog saveDialog = new SaveFileDialog();
        //    //saveDialog.InitialDirectory = Convert.ToString(Environment.SpecialFolder.MyDocuments);
        //    saveDialog.InitialDirectory = @"C:\data";
        //    saveDialog.Filter = "Access Database File (*.accdb)|*.accdb";
        //    saveDialog.FilterIndex = 1;

        //    Nullable<bool> result = saveDialog.ShowDialog();

        //    if (result == true)
        //    {
        //        DBPath = saveDialog.FileName;
        //        MessageBox.Show(DB.createNewDB(DBPath), "Thông báo");
        //        _lblDB.Content = "CSDL: " + DBPath;
        //    }

        //    if (isServer())
        //    {
        //        if (DB.isDataEmpty())
        //        {
        //            MessageBoxResult result2 = MessageBox.Show("Chưa có chỉ tiêu nào, Tạo mới?", "Thông báo", MessageBoxButton.OKCancel);
        //            if (result2 == MessageBoxResult.OK)
        //            {
        //                TargetCreator target = new TargetCreator(this, port);
        //                target.ShowDialog();
        //            }
        //            else { }
        //        }
        //    }
        //    loadTreeView();
        //    loadTreeList();
        //}

        private void createNewDatabase()
        {
            DatabaseCreator dc = new DatabaseCreator(this);
            dc.ShowDialog();
        }

        private void getExistedDB()
        {
            //OpenFileDialog openDialog = new OpenFileDialog();
            //openDialog.InitialDirectory = Convert.ToString(Environment.SpecialFolder.MyDocuments);
            //openDialog.Filter = "Access Database File (*.accdb)|*.accdb";
            //openDialog.FilterIndex = 1;

            //Nullable<bool> result = openDialog.ShowDialog();

            //if (result == true)
            //{
            //    DBPath = openDialog.FileName;
            //    RegistryKey appKey = Registry.CurrentUser.CreateSubKey(@"Software\" + Registry.CurrentUser.Name + @"\SEIS");
            //    appKey.SetValue("DatabasePath", DBPath);

            //    DB.changeAppConfigSetting();
            //    DB.refreshCSBConnectionString();

            //    MessageBox.Show("Đã chọn CSDL " + DBPath, "Thông báo");
            //    _lblDB.Content = "CSDL: " + DBPath;

            //    loadTreeView();
            //    loadTreeList();
            //}

            // select from a list of database's names

            DatabaseSelector ds = new DatabaseSelector(this);
            ds.ShowDialog();
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
            FileTransfer.FileReceiver(obj, e);

            string warning = FileTransfer.getWarning();
            string encryptedFile = FileTransfer.getSavedPath();

            if (warning == "Disconnect")
            {
                MessageBox.Show("Có lỗi trong quá trình gửi nhận file, xin kiểm tra lại đường truyền", "Thông báo");
                return;
            }

            // complete receive file
            else if (warning == "OK")
            {
                string decryptedFile = @"C:\data\" + System.IO.Path.GetFileName(encryptedFile);

                if (File.Exists(decryptedFile))
                {
                    try
                    {
                        File.Delete(decryptedFile);
                    }
                    catch (IOException) { }
                }

                string info = DataEncryption.DecryptFile(encryptedFile, decryptedFile);

                //if (info != "OK")
                //{
                //    MessageBox.Show(info, "Warning");
                //    return;
                //}

                // check if new file is [Sync] or [Report]
                string type = checkFileIsRecordOrSync(System.IO.Path.GetFileName(decryptedFile));

                if (type == "Report")
                {
                    if (decryptedFile != @"C:\data")
                    {
                        //MessageBox.Show("Đã nhận file mới tại " + path, "Thông báo");

                        // call ListFiles() directly will throw InvalidOperationException because a different thread owns _dataGridList
                        this.Dispatcher.Invoke((Action)(() =>
                        {
                            ListFiles();
                        }));
                    }
                }

                else if (type == "Sync")
                {
                    MessageBoxResult result = MessageBox.Show("Có yêu cầu đồng bộ từ máy chủ, xác nhận chữ ký điện tử ?", "Thông báo", MessageBoxButton.OKCancel);
                    if (result == MessageBoxResult.OK)
                    {
                        if (DigitalSignature.verifyXML(decryptedFile) == "OK") // valid sign
                        {
                            MessageBoxResult result2 = MessageBox.Show("Xác nhận chữ ký điện tử hợp lệ, thực hiện đồng bộ ?", "Thông báo", MessageBoxButton.OKCancel);
                            if (result2 == MessageBoxResult.OK)
                            {
                                this.Dispatcher.Invoke((Action)(() =>
                                {
                                    loadTargetListFromXML(decryptedFile); // update target database from server
                                    ListFiles();
                                }));
                            }
                            else { }
                        }
                        else // invalid sign
                        {
                            MessageBox.Show("Chữ ký điện tử không hợp lệ, đã hủy đồng bộ", "Thông báo");

                            try
                            {
                                File.Delete(encryptedFile);
                            }
                            catch (IOException) { }

                            return;
                        }
                    }
                    else { }
                }
            }

            else { }
        }

        // send to server as *.xml file
        private void _btnSend_Click(object sender, RoutedEventArgs e)
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
                                                    dataTableXML.Rows.Add(targetID, DB.getParentID(targetID, "Target"), DB.getNameByID(targetID, "Target"), ((TextBox)control2).Text, 1, string.Empty);
                                                }
                                                else if (((TextBox)control2).Text != string.Empty)
                                                {
                                                    dataTableXML.Rows.Add(targetID, DB.getParentID(targetID, "Target"), DB.getNameByID(targetID, "Target"), ((TextBox)control2).Text, 1, String.Format("{0:dd/MM/yyyy}", DateTime.Now));
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
                                                dataTableXML.Rows.Add(targetID, DB.getParentID(targetID, "Target"), DB.getNameByID(targetID, "Target"), ((TextBox)control2).Text, 1, string.Empty);
                                            }
                                            else if (((TextBox)control2).Text != string.Empty)
                                            {
                                                dataTableXML.Rows.Add(targetID, DB.getParentID(targetID, "Target"), DB.getNameByID(targetID, "Target"), ((TextBox)control2).Text, 1, String.Format("{0:dd/MM/yyyy}", DateTime.Now));
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
                    int check = 0;
                    string quantity = string.Empty;
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
                        if (count == _treeListTarget.VisibleRowCount)
                        {
                            MessageBox.Show("Xin nhập ít nhất 1 chỉ tiêu", "Thông báo");
                            return;
                        }

                        MessageBoxResult result2 = MessageBox.Show(count + " chỉ tiêu chưa nhập số liệu, tiếp tục?", "Thông báo", MessageBoxButton.OKCancel);
                        if (result2 == MessageBoxResult.OK)
                        {
                            // save to xml first
                            string DBName = getDBNameFromFileName(RegistryEditor.getConnectionStringFromRegistry());
                            string clientName = getClientNameFromFileName(RegistryEditor.getConnectionStringFromRegistry());
                            string fileName = XMLProcessor.saveXMLFileBeforeSendTLM(DBName, clientName, _treeListTarget, _treeListView, isClient);

                            // transfer the file
                            if (FileTransfer.FileTransferer(fileName, port) == "OK")
                            {
                                MessageBox.Show("Đã gửi báo cáo lên máy chủ", "Thông báo");
                                return;
                            }
                            else
                            {
                                MessageBox.Show("Gửi thất bại, xin thử lại", "Thông báo");
                            }
                        }
                        else { }
                    }
                    else
                    {
                        // save to xml first
                        string DBName = getDBNameFromFileName(RegistryEditor.getConnectionStringFromRegistry());
                        string clientName = getClientNameFromFileName(RegistryEditor.getConnectionStringFromRegistry());
                        string fileName = XMLProcessor.saveXMLFileBeforeSendTLM(DBName, clientName, _treeListTarget, _treeListView, isClient);

                        // transfer the file
                        if (FileTransfer.FileTransferer(fileName, port) == "OK")
                        {
                            MessageBox.Show("Đã gửi báo cáo lên máy chủ", "Thông báo");
                            return;
                        }
                        else
                        {
                            MessageBox.Show("Gửi thất bại, xin thử lại", "Thông báo");
                        }
                    }
                }
            }
        }

        // update targetlist from server into client database
        private void loadTargetListFromXML(string filePath)
        {
            // client does not need to have sql
            if (isClient)
            {
                // save the file path to registry first
                RegistryEditor.setConnectionStringToRegistry(filePath);
                ShowContent(filePath);
                _lblDB.Content = "Bộ chỉ tiêu mặc định: " + System.IO.Path.GetFileNameWithoutExtension(filePath);
            }

            else
            {
                int id = 0;
                List<int> listID = new List<int>();

                DataTable dt = XMLProcessor.getDataTableFromXML(filePath);

                if (dt != null)
                {
                    //if (!DB.isFromOriginalDatabase(dt) && !DB.isDataEmpty())
                    //{
                    //    MessageBox.Show("Bộ chỉ tiêu không thuộc cơ sở dữ liệu hiện tại, xin chọn cơ sở dữ liệu khác", "Thông báo");
                    //    return;
                    //}

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

                        //loadTreeView();
                        loadTreeList();
                        foreach (int i in listID)
                        {
                            targetNames += DB.getNameByID(i, "Target") + Environment.NewLine;
                        }
                        MessageBox.Show("Đã đồng bộ chỉ tiêu với máy chủ, có " + listID.Count + " chỉ tiêu bổ sung: "
                            + Environment.NewLine + targetNames, "Thông báo");
                    }
                    else
                    {
                        MessageBox.Show("Chỉ tiêu này đã được đồng bộ, xin chọn bộ chỉ tiêu khác", "Thông báo");
                        return;
                    }
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
                MessageBoxResult result = MessageBox.Show("Hiển thị bộ chỉ tiêu mặc định lên bảng ?", "Thông báo", MessageBoxButton.OKCancel);
                if (result == MessageBoxResult.OK)
                {
                    loadTreeView();
                }
            }
            else
            {
                MessageBoxResult result = MessageBox.Show("Hiển thị bộ chỉ tiêu mặc định lên bảng ?", "Thông báo", MessageBoxButton.OKCancel);
                if (result == MessageBoxResult.OK)
                {
                    loadTreeList();
                }
            }
        }

        // prevent app resides in system
        private void Window_Closing(object sender, CancelEventArgs e)
        {
            try
            {
                port.Close();
                Application.Current.Shutdown();
            }
            catch (Exception)
            {
            }
        }

        private void _btnCheckSign_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (selectedFileName == string.Empty)
                {
                    MessageBox.Show("Xin chọn 1 tệp trong danh sách", "Thông báo");
                    _dataGridList.Focus();
                    return;
                }

                if (DigitalSignature.verifyXML(selectedFileName) == "OK") // valid signature
                {
                    MessageBox.Show("Xác nhận chữ ký điện tử của tệp "
                        + Environment.NewLine + Environment.NewLine + "" + selectedFileName
                        + Environment.NewLine + Environment.NewLine + " HỢP LỆ", "Thông báo");
                    return;
                }

                else // invalid signature
                {
                    MessageBox.Show("Xác nhận chữ ký điện tử của tệp "
                        + Environment.NewLine + Environment.NewLine + "" + selectedFileName
                        + Environment.NewLine + Environment.NewLine + " KHÔNG HỢP LỆ", "Thông báo");
                    return;
                }
            }
            catch (Exception) { }
        }

        private void _btnList_Click(object sender, RoutedEventArgs e)
        {
            ListFiles();
        }

        // handle case when list has only 1 file then SelectedCell_Changed will not work
        //private void _dataGridList_GotFocus(object sender, RoutedEventArgs e)
        //{
        //    string temp = string.Empty;
        //    try
        //    {
        //        ReportInfo selectedItem = _dataGridList.SelectedItem as ReportInfo;
        //        temp = selectedItem.Path.ToString();
        //    }
        //    catch (Exception)
        //    {
        //        temp = string.Empty;
        //    }

        //    if (temp != selectedFileName || temp !=string.Empty)
        //    {
        //        DisplaySelectedFileInfo();
        //    }
        //    else { }
        //}
    }
}