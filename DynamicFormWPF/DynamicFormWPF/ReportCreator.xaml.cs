namespace DynamicFormWPF
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Data;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Data;
    using System.Windows.Documents;
    using System.Windows.Media;
    using DevExpress.Xpf.Core;
    using DevExpress.XtraTreeList;
    using DynamicFormWPF.Classes_Data;

    /// <summary>

    /// Interaction logic for ReportCreator.xaml
    /// </summary>
    public partial class ReportCreator : Window
    {
        private static int clientIDofCheckBox = 0;
        private static DateTime fromDate = new DateTime();
        private static DateTime toDate = new DateTime();
        private static List<int> listTargetIDs = new List<int>();
        private static DataTable dtTree = new DataTable("recordTreeTable");
        private static int currentYear = 0;

        public ReportCreator()
        {
            InitializeComponent();
        }

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
            InitializeControls();
            loadTreeList();
            loadTreeViewClient();
        }

        private void _dateFrom_SelectedDateChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            fromDate = Convert.ToDateTime(_fromDate.SelectedDate);

            if (fromDate == Convert.ToDateTime("1/01/0001"))
            {
                fromDate = DB.getMinDateTime();
            }

            loadTreeList();
        }

        private void _dateTo_SelectedDateChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            toDate = Convert.ToDateTime(_toDate.SelectedDate);

            if (toDate == Convert.ToDateTime("1/01/0001"))
            {
                toDate = DB.getMaxDateTime();
            }

            loadTreeList();
        }

        private void _btnCreateReport_Click(object sender, RoutedEventArgs e)
        {
            if (toDate == Convert.ToDateTime("1/01/0001"))
            {
                toDate = DateTime.Today;
            }

            TreeList treeListReport = new TreeList();
            dtTree = DB.getReportDataTable(fromDate, toDate, listTargetIDs, true); // with Vietnamese header
            dtTree.Columns.Remove("ClientID");
            treeListReport.DataSource = dtTree;
            treeListReport.KeyFieldName = "TargetID";
            treeListReport.ParentFieldName = "ParentID";
            treeListReport.ExpandAll();

            // pass the treelist as report's datasource
            XtraUserReport xup = new XtraUserReport(treeListReport.DataSource as DataTable, fromDate, toDate);

            //xup.ShowPreviewDialog(); // .ShowPreview works only in WinForm
        }

        private void loadTreeList()
        {
            DataTable dt = DB.getReportDataTable(fromDate, toDate, listTargetIDs, false); // without Vietnamese header
            _treeListTarget.ItemsSource = dt;
            _treeListView.ExpandAllNodes();

            updatePeriodInfo();
        }

        public void loadTreeViewClient()
        {
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
                    DockPanel dock = new DockPanel();
                    Label label = new Label();
                    label.Content = node["ClientName"].ToString();
                    CheckBox checkBox = new CheckBox();
                    checkBox.Name = "_txt" + node["ClientID"].ToString();
                    checkBox.Checked += new RoutedEventHandler(checkBox_CheckedChanged);
                    checkBox.Unchecked += new RoutedEventHandler(checkBox_CheckedChanged);
                    dock.Children.Add(checkBox);
                    dock.Children.Add(label);
                    treeNode.Header = dock;
                    flattenedTree[Convert.ToInt32(node["ParentID"])].Items.Add(treeNode);
                }
                else // create new parent node
                {
                    DockPanel dock = new DockPanel();
                    Label label = new Label();
                    label.Content = node["ClientName"].ToString();
                    CheckBox checkBox = new CheckBox();
                    checkBox.Name = "_txt" + node["ClientID"].ToString();
                    checkBox.Checked += new RoutedEventHandler(checkBox_CheckedChanged);
                    checkBox.Unchecked += new RoutedEventHandler(checkBox_CheckedChanged);
                    dock.Children.Add(checkBox);
                    dock.Children.Add(label);
                    treeNode.Header = dock;
                    _treeViewClient.Items.Add(treeNode);
                }
                treeNode.IsExpanded = true;
            }
        }

        // count number of targets
        //private void CreateTotal(string fieldName, DevExpress.Data.SummaryItemType summaryType)
        //{
        //    TreeListSummaryItem total = new TreeListSummaryItem()
        //    {
        //        FieldName = fieldName,
        //        SummaryType = summaryType,
        //        ShowInColumn = fieldName
        //    };
        //    _treeListTarget.TotalSummary.Add(total);
        //}

        private void InitializeControls()
        {
            DataTable yearList = DB.getYearList();
            _cbbYear.SetBinding(ItemsControl.ItemsSourceProperty, new Binding { Source = yearList });
            _cbbYear.DisplayMemberPath = "YearList";
            _cbbYear.SelectedValuePath = "YearList";
            _cbbYear.SelectedIndex = 0;

            _cbbQuaterYear.SetBinding(ItemsControl.ItemsSourceProperty, new Binding { Source = yearList });
            _cbbQuaterYear.DisplayMemberPath = "YearList";
            _cbbQuaterYear.SelectedValuePath = "YearList";
            _cbbQuaterYear.SelectedIndex = 0;

            _rbDay.Checked += new RoutedEventHandler(_rbMode_Checked);
            _rbQuarter.Checked += new RoutedEventHandler(_rbMode_Checked);
            _rbYear.Checked += new RoutedEventHandler(_rbMode_Checked);
            _rbAll.Checked += new RoutedEventHandler(_rbMode_Checked);

            _rbQuarter1.Checked += new RoutedEventHandler(_rbQuarter_Checked);
            _rbQuarter2.Checked += new RoutedEventHandler(_rbQuarter_Checked);
            _rbQuarter3.Checked += new RoutedEventHandler(_rbQuarter_Checked);
            _rbQuarter4.Checked += new RoutedEventHandler(_rbQuarter_Checked);

            // set initial mode after load: all times
            _rbAll.IsChecked = true;
            _rbMode_Checked(null, null);

            // reset selection data for treelist
            listTargetIDs = new List<int>();
            dtTree = new DataTable("recordTreeTable");
            currentYear = 0;
        }

        private void _rbMode_Checked(object sender, RoutedEventArgs e)
        {
            if (_rbDay.IsChecked == true)
            {
                _gridQuarter.Visibility = Visibility.Hidden;
                _gridYear.Visibility = Visibility.Hidden;
                _gridDay.Visibility = Visibility.Visible;

                _dateFrom_SelectedDateChanged(null, null);
                _dateTo_SelectedDateChanged(null, null);
            }

            else if (_rbQuarter.IsChecked == true)
            {
                _gridDay.Visibility = Visibility.Hidden;
                _gridYear.Visibility = Visibility.Hidden;
                _gridQuarter.Visibility = Visibility.Visible;
                _cbbQuaterYear_SelectionChanged(null, null);

                if (_rbQuarter1.IsChecked == true)
                {
                    fromDate = Convert.ToDateTime("1/1/" + currentYear);
                    toDate = Convert.ToDateTime("31/3/" + currentYear);
                }
            }

            else if (_rbYear.IsChecked == true)
            {
                _gridDay.Visibility = Visibility.Hidden;
                _gridQuarter.Visibility = Visibility.Hidden;
                _gridYear.Visibility = Visibility.Visible;

                _cbbYear_SelectionChanged(null, null);
            }

            else if (_rbAll.IsChecked == true)
            {
                _gridDay.Visibility = Visibility.Hidden;
                _gridQuarter.Visibility = Visibility.Hidden;
                _gridYear.Visibility = Visibility.Hidden;

                fromDate = DB.getMinDateTime();
                toDate = DB.getMaxDateTime();
            }
            loadTreeList();
        }

        private void _cbbQuaterYear_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            try
            {
                currentYear = Convert.ToInt32(_cbbQuaterYear.SelectedValue);
            }
            catch (Exception)
            {
                currentYear = DateTime.Now.Year;
            }
            finally
            {
                _rbQuarter_Checked(null, null);
                loadTreeList();
            }
        }

        private void _cbbYear_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            try
            {
                currentYear = Convert.ToInt32(_cbbYear.SelectedValue);
            }
            catch (Exception)
            {
                currentYear = DateTime.Now.Year;
            }
            finally
            {
                fromDate = Convert.ToDateTime("1/1/" + currentYear);
                toDate = Convert.ToDateTime("31/12/" + currentYear);
                loadTreeList();
            }
        }

        private void _rbQuarter_Checked(object sender, RoutedEventArgs e)
        {
            if (_rbQuarter1.IsChecked == true)
            {
                fromDate = Convert.ToDateTime("1/1/" + currentYear);
                toDate = Convert.ToDateTime("31/3/" + currentYear);
            }

            else if (_rbQuarter2.IsChecked == true)
            {
                fromDate = Convert.ToDateTime("1/4/" + currentYear);
                toDate = Convert.ToDateTime("30/6/" + currentYear);
            }

            else if (_rbQuarter3.IsChecked == true)
            {
                fromDate = Convert.ToDateTime("1/7/" + currentYear);
                toDate = Convert.ToDateTime("30/9/" + currentYear);
            }

            else if (_rbQuarter4.IsChecked == true)
            {
                fromDate = Convert.ToDateTime("1/10/" + currentYear);
                toDate = Convert.ToDateTime("31/12/" + currentYear);
            }

            loadTreeList();
        }

        private void updatePeriodInfo()
        {
            _lblPeriod.Content = fromDate.ToShortDateString() + " - " + toDate.ToShortDateString();
        }

        private void checkBox_CheckedChanged(object sender, RoutedEventArgs e)
        {
            clientIDofCheckBox = Convert.ToInt32(((CheckBox)sender).Name.Substring(4));
            List<int> IDs = DB.getClientIDList(clientIDofCheckBox);

            if (((CheckBox)sender).IsChecked == true)
            {
                if (DB.getParentID(clientIDofCheckBox, "Client") == 0) // LV1 Client is checked
                {
                    foreach (int ID in IDs)
                    {
                        listTargetIDs.Add(ID); // add all LV2 Clients
                    }

                    // check all LV2 clients checkboxes
                    foreach (TreeViewItem root in _treeViewClient.Items) // loop through LV1 nodes
                    {
                        if (Convert.ToInt32(root.Tag) == clientIDofCheckBox) // loop only selected node
                        {
                            checkBoxesByLoopingThroughNodes(root, IDs, true); // loop through LV2 nodes
                        }
                    }
                }

                else
                {
                    listTargetIDs.Add(clientIDofCheckBox); // add LV2 Client
                }
            }

            else if (((CheckBox)sender).IsChecked == false) // LV1 Client is unchecked
            {
                if (DB.getParentID(clientIDofCheckBox, "Client") == 0)
                {
                    foreach (int ID in IDs)
                    {
                        listTargetIDs.Remove(ID); // remove all LV2 Clients
                    }

                    // uncheck all LV2 clients checkboxes
                    foreach (TreeViewItem root in _treeViewClient.Items) // loop through LV1 nodes
                    {
                        if (Convert.ToInt32(root.Tag) == clientIDofCheckBox) // loop only selected node
                        {
                            checkBoxesByLoopingThroughNodes(root, IDs, false); // loop through LV2 nodes
                        }
                    }
                }

                else
                {
                    listTargetIDs.Remove(clientIDofCheckBox); // remove LV2 Client
                }
            }
            loadTreeList();
        }

        private void checkBoxesByLoopingThroughNodes(TreeViewItem root, List<int> IDs, bool isCheck)
        {
            foreach (TreeViewItem node in ((TreeViewItem)root).Items) // loop through LV2 nodes
            {
                if (node != null)
                {
                    DockPanel dock = node.Header as DockPanel;
                    foreach (Control control in dock.Children)
                    {
                        if (control.GetType() == typeof(CheckBox))
                        {
                            // check which checkboxes are children of selected client
                            foreach (int ID in IDs)
                            {
                                if (Convert.ToInt32(((CheckBox)control).Name.Substring(4)) == ID)
                                {
                                    ((CheckBox)control).IsChecked = isCheck;
                                }
                            }
                        }
                    }

                    //checkBoxesByLoopingThroughNodes(node); // continue looping in LV3 and deeper nodes
                }
            }
        }

        private void _expClient_Expanded(object sender, RoutedEventArgs e)
        {
            _expClient.Height = 172;
        }

        private void _expClient_Collapsed(object sender, RoutedEventArgs e)
        {
            _expClient.Height = 38; // to prevent Expander overlaps other controls
        }
    }
}