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

namespace DynamicFormWPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private static string databasePath = string.Empty;
        private static string selectedFileName = string.Empty;
        private static int selectedRowIndex = 0;
        private static List<string> ls = new List<string>();
        BindingList<ReportInfo> listRecordInfos = new BindingList<ReportInfo>();
        FileInfo[] structuredRecords = null;
        FileInfo[] freeFormRecords = null;

        private RegistryKey appKey;

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                _dataGridList.AutoGenerateColumns = true;
                DataGridTextColumn columnFilePath = new DataGridTextColumn();
                columnFilePath.Header = "File Path";
                columnFilePath.Binding = new Binding("Path");
                columnFilePath.Visibility = Visibility.Hidden;
                DataGridTextColumn columnFileName = new DataGridTextColumn();
                columnFileName.Header = "Tên báo cáo";
                columnFileName.Binding = new Binding("Name");
                columnFileName.Width = _dataGridList.Width;

                _dataGridList.Columns.Add(columnFilePath);
                _dataGridList.Columns.Add(columnFileName);

                checkExistedDB();

                ReloadDataGrid();
                loadTreeView();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        // check if database has been created
        private void checkExistedDB()
        {
            if (!DB.isConnectedToDB())
            {
                MessageBoxResult result = MessageBox.Show("Chưa có CSDL, Tạo mới?", "Thông báo", MessageBoxButton.OKCancel);
                if (result == MessageBoxResult.OK)
                {
                    createNewPhysicalDatabase();
                }
                else {
                    MessageBoxResult result2 = MessageBox.Show("Chọn CSDL có sẵn?", "Thông báo", MessageBoxButton.OKCancel);
                    if (result2 == MessageBoxResult.OK)
                    {
                        getExistedDB();   
                    }
                }
            }
        }

        // create treeview from database
        public void loadTreeView()
        {
            _wrapPanelDragDrop.Children.Clear();

            DataTable parentNodes = DB.getTargetList(-1);

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
                    TextBox textbox = new TextBox();
                    textbox.Name = "_txt" + node["ParentID"].ToString();
                    textbox.Width = 40;
                    textbox.AllowDrop = true;
                    textbox.PreviewTextInput += new TextCompositionEventHandler(this._textBox_PreviewTextInput);
                    textbox.TextChanged += new TextChangedEventHandler(this._textbox_TextChanged);
                    dock.Children.Add(textbox);
                    dock.Children.Add(label);
                    treeNode.Header = dock;
                    flattenedTree[Convert.ToInt32(node["ParentID"])].Items.Add(treeNode);
                }
                else // create new tree with LV1 target as root
                {
                    TreeView _treeViewTarget = new TreeView();
                    _treeViewTarget.Items.Clear();
                    _treeViewTarget.Items.Add(treeNode);
                    _wrapPanelDragDrop.Children.Add(_treeViewTarget);
                }
                treeNode.IsExpanded = true;
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

        //private void GenerateTextBoxesFromDB()
        //{
        //    // create textbox control according to DB
        //    DataTable nameList = DB.getTypeList();
        //    if (nameList != null)
        //    {
        //        foreach (DataRow row in nameList.Rows)
        //        {
        //            GenerateControl(row[0].ToString());
        //        }
        //    }
        //}

        //private void GenerateControl(string controlName)
        //{
            //controlName = controlName.Replace(" ", string.Empty);

            //// button for adding to DB 1 record by 1
            //Button button = new Button();
            //button.Name = "_btn" + controlName;
            //button.Content = controlName;
            //button.ToolTip = DB.getTypeDescription(controlName);
            //button.HorizontalAlignment = HorizontalAlignment.Right;
            //button.VerticalAlignment = VerticalAlignment.Top;
            //button.Width = 80;
            //button.Height = 20;
            //button.Margin = new Thickness(5, 5, 0, 0);
            //// click to add to DB the content in the according textbox
            //button.Click += new RoutedEventHandler(this.ButtonClickEvent);

            //TextBox textBox = new TextBox();
            //textBox.Name = controlName;
            //textBox.ToolTip = DB.getTypeDescription(controlName);
            //textBox.HorizontalAlignment = HorizontalAlignment.Right;
            //textBox.VerticalAlignment = VerticalAlignment.Top;
            //textBox.Width = 80;
            //textBox.Height = 50;
            //textBox.Margin = new Thickness(5, 5, 0, 0);
            //textBox.TextWrapping = TextWrapping.WrapWithOverflow;

            //_wrapPanelDragDrop.Children.Add(button);
            //_wrapPanelDragDrop.Children.Add(textBox);
        //}

        private void ReloadDataGrid()
        {
            DataTable dt = new DataTable();
            try
            {
                dt = DB.getRecordList();
                _dataGrid.SetBinding(ItemsControl.ItemsSourceProperty, new Binding { Source = dt });
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
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

        private DataTable getDataTableFromXML(string filePath)
        {
            DataTable dt;
            try
            {
                // load XML file
                XElement x = XElement.Load(filePath);
                // declare a new DataTable and pass XElement to it
                dt = XMLViewer.XElementToDataTable(x);
            }
            catch (Exception)
            {
                return null;
            }
            return dt;
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

        private void addRecordToDB(TextBox txt)
        {
            string content = txt.Text;
            if (content != string.Empty)
            {
                DB.updateContent(content, txt.Name);
                ReloadDataGrid();
                txt.Clear();
            }
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
                listRecordInfos.Clear();
            }
            catch (Exception) { }
        }

        private void ListFiles()
        {
            ClearReportList();
            string filePath = string.Empty;
            filePath = @"F:\Backups\VS2010 Projects\DynamicFormWPF";
            DirectoryInfo di = new DirectoryInfo(filePath);
            structuredRecords = di.GetFiles("*.xml", SearchOption.TopDirectoryOnly);
            freeFormRecords = di.GetFiles("*.rtf", SearchOption.TopDirectoryOnly);

            foreach (FileInfo fi in structuredRecords)
            {
                listRecordInfos.Add(new ReportInfo(fi.FullName, fi.Name));
            }
            foreach (FileInfo fi in freeFormRecords)
            {
                listRecordInfos.Add(new ReportInfo(fi.FullName, fi.Name));
            }
            //_dataGridList.SetBinding(ItemsControl.ItemsSourceProperty, new Binding { Source = listReportInfos });
            _dataGridList.ItemsSource = listRecordInfos;
            _lblList.Content = _dataGridList.Items.Count - 1 + " Files";
        }

        private void ShowContent(string filePath)
        {
            if (filePath.Contains(".rtf")) // free form
            {
                _UnformattedTab.IsSelected = true;
                _flowDocUnformatted.Document = getFlowDocumentText(filePath);
            }
            if (filePath.Contains(".xml")) // structured
            {
                _FormattedTab.IsSelected = true;
                //string text = getTextFromDataTable(getDataTableFromXML(filePath));
                //Paragraph paragraph = new Paragraph();
                //paragraph.Inlines.Add(text);
                //FlowDocument document = new FlowDocument(paragraph);
                //_flowDocFormatted.Document = document;

                DataTable dt = getDataTableFromXML(filePath);
                //dt.Columns.Add("Check", typeof(bool));
                //dt.Columns.Add(checkColumn);
                _dataGridFormatted.SetBinding(ItemsControl.ItemsSourceProperty, new Binding { Source = dt });
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

        private void _btnBatchInsertFromXML_Click(object sender, RoutedEventArgs e)
        {
            if (_dataGridFormatted.SelectedItems.Count > 0)
            {
                MessageBoxResult result = MessageBox.Show("Nhập " + _dataGridFormatted.SelectedItems.Count + " chỉ tiêu vào cơ sở dữ liệu?", "Thông báo", MessageBoxButton.OKCancel);
                if (result == MessageBoxResult.OK)
                {
                    string info = string.Empty;

                    string targetName = string.Empty;
                    int quantityValue = 0;
                    string clientName = string.Empty;
                    DateTime recordTime;

                    int i = 0;
                    for (i = 0; i < _dataGridFormatted.SelectedItems.Count; i++)
                    {
                        try
                        {
                            DataRowView item = (System.Data.DataRowView)_dataGridFormatted.SelectedItems[i];
                            targetName = item.Row["TargetName"].ToString();
                            quantityValue = Convert.ToInt32(item.Row["QuantityValue"]);
                            clientName = item.Row["ClientName"].ToString();
                            recordTime = Convert.ToDateTime(item.Row["RecordTime"]);
                            info = DB.updateFromXMLtoDB(targetName, quantityValue, clientName, recordTime);
                            if (info != "OK")
                            {
                                MessageBox.Show(info);
                                return;
                            }
                        }
                        catch (Exception)
                        {
                            MessageBox.Show("Hãy chọn ít nhất 1 báo cáo");
                            return;
                        }
                    }
                    MessageBox.Show("Đã nhập thành công " + i + " báo cáo");
                }
            }
            else
            {
                MessageBox.Show("Hãy chọn ít nhất 1 báo cáo");
            }
            ReloadDataGrid();
        }

        private void _btnList_Click(object sender, RoutedEventArgs e)
        {
            ListFiles();
        }

        private void tabControl1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_FormattedTab.IsSelected)
            {
                _FormattedTab.IsEnabled = true;
                _UnformattedTab.IsEnabled = false;
                _wrapPanelDragDrop.IsEnabled = false;
                _btnBatchInsertFromXML.IsEnabled = true;
            }
            if (_UnformattedTab.IsSelected)
            {
                _UnformattedTab.IsEnabled = true;
                _FormattedTab.IsEnabled = false;
                _wrapPanelDragDrop.IsEnabled = true;
                _btnBatchInsertFromXML.IsEnabled = false;
            }
        }

        private void _btnUpdateDB_Click(object sender, RoutedEventArgs e)
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
                            loopThroughNodes(root);
                        }
                    }
                }
            }
            else { }
        }

        // recursive though textbox in each node then continue with the child nodes
        private void loopThroughNodes(TreeViewItem root)
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
                            addRecordToDB((TextBox)control);
                        }
                    }
                    loopThroughNodes(node); // continue looping in LV3 and deeper nodes
                }
            }
        }

        private void button3_Click(object sender, RoutedEventArgs e)
        {
            DB.ClearTable("tblRecord");
            ReloadDataGrid();
        }

        private void Menu_File_Exit_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("Thoát chương trình?", "Thông báo", MessageBoxButton.OKCancel);
            if (result == MessageBoxResult.OK)
            {
                this.Close();
            }
            else { }
        }

        // create crystal report
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
                TargetCreator target = new TargetCreator(this);
                target.ShowDialog();
            }
            else
            {
                MessageBox.Show("Chưa kết nối CSDL", "Thông báo");
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
            string DBPath = string.Empty;
            SaveFileDialog saveDialog = new SaveFileDialog();
            saveDialog.InitialDirectory = Convert.ToString(Environment.SpecialFolder.MyDocuments);
            saveDialog.Filter = "Access Database File (*.accdb)|*.accdb";
            saveDialog.FilterIndex = 1;

            Nullable<bool> result = saveDialog.ShowDialog();

            if (result == true)
            {
                DBPath = saveDialog.FileName;
                MessageBox.Show(DB.createNewDB(DBPath));
            }

            if (DB.isDataEmpty())
            {
                MessageBoxResult result2 = MessageBox.Show("Chưa có chỉ tiêu nào, Tạo mới?", "Thông báo", MessageBoxButton.OKCancel);
                if (result2 == MessageBoxResult.OK)
                {
                    TargetCreator target = new TargetCreator(this);
                    target.ShowDialog();
                }
                else { }
            }
            ReloadDataGrid();
            loadTreeView();
        }

        void getExistedDB() {
            string DBPath = string.Empty;

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

                MessageBox.Show("Đã chọn CSDL tại đường dẫn " + DBPath);
                
                ReloadDataGrid();
                loadTreeView();
            }
        }

        private void _btnSearchCategory_Click(object sender, RoutedEventArgs e)
        {
            //string index = string.Empty;
            //index = _txtSearchCategory.Text;

            //foreach (Control control in _wrapPanelDragDrop.Children)
            //{
            //    if (control.GetType() == typeof(Button))
            //    {
            //        if (control.Name.Contains(index))
            //        {
            //            _wrapPanelSearch.Children.Add(control);
            //        }
            //    }
            //    if (control.GetType() == typeof(TextBox))
            //    {
            //        if (control.Name.Contains(index))
            //        {
            //            _wrapPanelSearch.Children.Add(control);
            //        }
            //    }
            //}
        }

        private void _dataGridList_SelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
        {
            DisplaySelectedFileInfo();
        }
    }
}
