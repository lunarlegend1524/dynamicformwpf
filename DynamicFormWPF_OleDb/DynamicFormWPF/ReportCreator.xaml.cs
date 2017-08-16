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
    using DevExpress.XtraReports;
    using DevExpress.Xpf.Grid;
    using DevExpress.Data;
    using DevExpress.XtraTreeList;
    using DevExpress.Xpf.Core;
    using Itenso.TimePeriod;
    /// <summary>

    /// Interaction logic for ReportCreator.xaml
    /// </summary>
    public partial class ReportCreator : Window
    {
        public ReportCreator()
        {
            InitializeComponent();
        }

        private static DateTime fromDate = new DateTime();
        private static DateTime toDate = new DateTime();
        private static List<int> listTargetIDs = new List<int>();
        private static DataTable dtTree = new DataTable("recordTreeTable");

        private static int currentYear = 0;

        // reference http://www.codeproject.com/Articles/168662/Time-Period-Library-for-NET

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            DataTable dt = DB.getTargetList(-1);
            if (dt == null)
            {
                MessageBox.Show("Chưa chọn tệp chứa bộ chỉ tiêu", "Thông báo");
                return;
            }
            ThemeManager.SetThemeName(_treeListTarget, "Seven");
            InitializeControls();
            loadTreeList();
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
            dtTree = DB.getReportDataTable(fromDate, toDate);
            treeListReport.DataSource = dtTree;
            treeListReport.KeyFieldName = "TargetID";
            treeListReport.ParentFieldName = "ParentID";
            treeListReport.ExpandAll();

            // pass the treelist as report's datasource
            XtraUserReport xup = new XtraUserReport(treeListReport.DataSource as DataTable, fromDate, toDate);
            xup.ShowPreviewDialog(); // .ShowPreview works only in WinForm
        }

        private void loadTreeList()
        {
            DataTable dt = DB.getReportDataTable2(fromDate, toDate);
            _treeListTarget.ItemsSource = dt;
            _treeListView.ExpandAllNodes();

            updatePeriodInfo();
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

            _rbAll.IsChecked = true;
            _rbMode_Checked(null, null);
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

                TimeCalendar calendar = new TimeCalendar();
                Quarter quarter = new Quarter();

                if (_rbQuarter1.IsChecked == true)
                {
                    calendar = new TimeCalendar(new TimeCalendarConfig { YearBaseMonth = YearMonth.January });
                    quarter = new Quarter(Convert.ToDateTime("1/1/" + currentYear), calendar);
                    fromDate = quarter.FirstDayStart;
                    toDate = quarter.LastDayStart;
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
                loadTreeList();
            }
        }

        private void _cbbQuaterYear_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            try
            {
                currentYear = Convert.ToInt32(_cbbQuaterYear.SelectedValue);
            }
            catch (Exception)
            {
                currentYear = 2012;
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
                Year year = new Year(currentYear);
                fromDate = year.FirstDayStart;
                toDate = year.LastDayStart;
            }
            catch (Exception)
            {
                currentYear = 2012;
                Year year = new Year();
                fromDate = year.FirstDayStart;
                toDate = year.LastDayStart;
            }
            finally
            {
                loadTreeList();
            }
        }

        private void _rbQuarter_Checked(object sender, RoutedEventArgs e)
        {
            TimeCalendar calendar = new TimeCalendar();
            Quarter quarter = new Quarter();

            if (_rbQuarter1.IsChecked == true)
            {
                calendar = new TimeCalendar(new TimeCalendarConfig { YearBaseMonth = YearMonth.January });
                quarter = new Quarter(Convert.ToDateTime("1/1/" + currentYear), calendar);
                fromDate = quarter.FirstDayStart;
                toDate = quarter.LastDayStart;
            }

            else if (_rbQuarter2.IsChecked == true)
            {
                calendar = new TimeCalendar(new TimeCalendarConfig { YearBaseMonth = YearMonth.January });
                quarter = new Quarter(Convert.ToDateTime("1/4/" + currentYear), calendar);
                fromDate = quarter.FirstDayStart;
                toDate = quarter.LastDayStart;
            }

            else if (_rbQuarter3.IsChecked == true)
            {
                calendar = new TimeCalendar(new TimeCalendarConfig { YearBaseMonth = YearMonth.January });
                quarter = new Quarter(Convert.ToDateTime("1/7/" + currentYear), calendar);
                fromDate = quarter.FirstDayStart;
                toDate = quarter.LastDayStart;
            }

            else if (_rbQuarter4.IsChecked == true)
            {
                calendar = new TimeCalendar(new TimeCalendarConfig { YearBaseMonth = YearMonth.January });
                quarter = new Quarter(Convert.ToDateTime("1/10/" + currentYear), calendar);
                fromDate = quarter.FirstDayStart;
                toDate = quarter.LastDayStart;
            }

            loadTreeList();
        }

        private void updatePeriodInfo() 
        {
            _lblPeriod.Content = fromDate.ToShortDateString() + " - " + toDate.ToShortDateString();
        }
    }
}
