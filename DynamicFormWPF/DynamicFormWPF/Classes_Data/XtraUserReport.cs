namespace DynamicFormWPF.Classes_Data
{
    using System;
    using System.Data;
    using System.Drawing;
    using System.Windows.Forms;
    using DevExpress.XtraPrinting;
    using DevExpress.XtraReports.UI;
    using DevExpress.XtraTreeList;

    // http://www.devexpress.com/Support/Center/p/B199826.aspx

    public partial class XtraUserReport : XtraReport
    {
        private static DataTable curdataSource;
        private static DateTime fromDate = new DateTime();
        private static DateTime toDate = new DateTime();
        private TreeList treeListReport = new TreeList();

        public XtraUserReport()
        {
            InitializeComponent();
            treeListReport.SaveLayoutToXml(@"C:\data\temp\11.xml");
        }

        public XtraUserReport(DataTable s, DateTime from, DateTime to)
        {
            curdataSource = s;
            fromDate = from;
            toDate = to;
            InitializeComponent();
            treeListReport.SaveLayoutToXml(@"C:\data\temp\11.xml");
        }

        protected override void OnBeforePrint(System.Drawing.Printing.PrintEventArgs e)
        {
            base.OnBeforePrint(e);
            UpdateTreeList(treeListReport);
        }

        private void UpdateTreeList(TreeList treeList)
        {
            if (treeList != null)
            {
                //DataSource = curdataSource;
                treeList.RestoreLayoutFromXml(@"C:\data\temp\11.xml");

                WinControlContainer wcc = new WinControlContainer();

                //TreeList treeListReport = new TreeList();
                treeListReport.Parent = new Form();

                Detail.Controls.Add(wcc);

                wcc.WinControl = treeList;

                XRLabel a = new XRLabel();
                a.Text = "Báo cáo chỉ tiêu";
                a.Font = new Font("Tahoma", 15, FontStyle.Bold);
                a.WidthF = 650;
                a.TextAlignment = TextAlignment.MiddleCenter;

                XRLabel b = new XRLabel();
                string today = string.Empty;
                if (toDate == DateTime.Today)
                {
                    today = "hôm nay, ";
                }
                if (fromDate == Convert.ToDateTime("1/01/0001"))
                {
                    b.Text = "Từ " + String.Format("{0:dd-MM-yyyy}", DB.getMinDateTime()) + " đến ngày " + today + String.Format("{0:dd-MM-yyyy}", toDate);
                }
                else
                {
                    b.Text = "Từ ngày " + String.Format("{0:dd-MM-yyyy}", fromDate) + " đến ngày " + today + String.Format("{0:dd-MM-yyyy}", toDate);
                }
                b.Font = new Font("Tahoma", 13, FontStyle.Regular);
                b.WidthF = 650;
                b.TextAlignment = TextAlignment.TopCenter;

                ReportHeader.Controls.Add(a);
                PageHeader.Controls.Add(b);

                treeListReport.KeyFieldName = "TargetID";
                treeListReport.ParentFieldName = "ParentID";
                treeListReport.DataSource = curdataSource;
                treeListReport.OptionsPrint.PrintAllNodes = true;

                treeListReport.OptionsView.ShowPreview = true;
                treeListReport.ExpandAll();
                treeListReport.ForceInitialize();

                //PrintingSystem ps = new PrintingSystem();
                //// Create a link that will print a control.
                //PrintableComponentLink link = new PrintableComponentLink(ps);
                //// Specify the control to be printed.
                //link.Component = treeListReport;
                //// Set the paper format.
                //link.PaperKind = System.Drawing.Printing.PaperKind.A4;
                //// Subscribe to the CreateReportHeaderArea event used to generate the report header.
                //link.CreateReportHeaderArea +=
                //  new CreateAreaEventHandler(printableComponentLink1_CreateReportHeaderArea);
                //// Generate the report.
                //link.CreateDocument();
                //// Show the report.
                //link.ShowPreview();
            }
        }

        //private void printableComponentLink1_CreateReportHeaderArea(object sender, CreateAreaEventArgs e)
        //{
        //    string reportHeader = "Báo cáo chỉ tiêu";
        //    e.Graph.StringFormat = new BrickStringFormat(StringAlignment.Center);
        //    e.Graph.Font = new Font("Tahoma", 14, FontStyle.Bold);
        //    RectangleF rec = new RectangleF(0, 0, e.Graph.ClientPageSize.Width, 50);
        //    e.Graph.DrawString(reportHeader, Color.Black, rec, BorderSide.None);
        //}
    }
}