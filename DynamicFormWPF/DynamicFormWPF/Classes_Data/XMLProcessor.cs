namespace DynamicFormWPF.Classes_Data
{
    using System;
    using System.Data;
    using System.IO;
    using System.Linq;
    using System.Xml.Linq;
    using DevExpress.Xpf.Grid;

    // create XML file from TreeList's Rows' Values
    public class XMLProcessor
    {
        // save Rows value in TreeList to XML file
        public static string saveXMLFileBeforeSendTLM(string DBName, string clientName, TreeListControl _treeListTarget, TreeListView _treeListView, bool isClient)
        {
            int i = 0;
            int number = 0;
            int targetID = 0;
            int parentID = 0;
            string targetName = string.Empty;
            string quantity = string.Empty;
            DataTable dt = new DataTable();
            DateTime date = new DateTime();
            TreeListNodeIterator nodeIterator = new TreeListNodeIterator(_treeListView.Nodes, true);

            // save inputted records to xml
            dt = refreshDataTableXML(dt);
            while (nodeIterator.MoveNext())
            {
                i = nodeIterator.Current.RowHandle;
                targetID = Convert.ToInt32(_treeListTarget.GetCellValue(i, "TargetID"));
                parentID = Convert.ToInt32(_treeListTarget.GetCellValue(i, "ParentID"));
                targetName = _treeListTarget.GetCellValue(i, "TargetName").ToString();
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

                // insert empty datetime to xml if no quantity is inputted
                if (quantity == string.Empty)
                {
                    dt.Rows.Add(targetID, parentID, targetName, quantity, 1, string.Empty);
                }

                else if (quantity != string.Empty)
                {
                    if (date == Convert.ToDateTime("1/01/0001"))
                    {
                        date = DateTime.Today;
                    }

                    dt.Rows.Add(targetID, parentID, targetName, quantity, 1, String.Format("{0:dd/MM/yyyy}", date));
                    number++;
                }
            }

            string fileName = string.Empty;

            if (isClient)
            {
                fileName = @"C:\data\temp\" + "[" + DBName + "]"
                    + "_[" + clientName + "]"
                    + "_[Report]"
                    + "_[" + DateTime.Today.Day + "-" + DateTime.Today.Month + "-" + DateTime.Today.Year + "].xml";
            }

            else
            {
                // file name must have specific client info
                fileName = @"C:\data\temp\" + "[" + DB.getDBNameFromConnectionString() + "]"
                   + "_[" + clientName + "]"
                   + "_[Report]"
                   + "_[" + DateTime.Today.Day + "-" + DateTime.Today.Month + "-" + DateTime.Today.Year + "].xml";
            }

            string tempFile = @"C:\data\temp\tempData.xml";
            dt.WriteXml(tempFile);

            string info = string.Empty;

            // sign the XML document with Digital Signature
            info = DigitalSignature.signXML(tempFile);

            if (info != "OK")
            {
                return "";
            }

            // encrypt the signed XML
            info = DataEncryption.EncryptFile(tempFile, fileName);

            if (info != "OK")
            {
                return "";
            }

            return fileName;
        }

        // save xml file to harddisk
        public static string saveXMLFileTLM(string path, string DBName, string clientName, TreeListControl _treeListTarget, TreeListView _treeListView, bool isClient)
        {
            int i = 0;

            //int number = 0;
            int targetID = 0;
            int parentID = 0;
            string targetName = string.Empty;
            string quantity = string.Empty;
            DataTable dt = new DataTable();
            DateTime date = new DateTime();
            TreeListNodeIterator nodeIterator = new TreeListNodeIterator(_treeListView.Nodes, true);

            // save inputted records to xml
            dt = refreshDataTableXML(dt);
            while (nodeIterator.MoveNext())
            {
                i = nodeIterator.Current.RowHandle;
                targetID = Convert.ToInt32(_treeListTarget.GetCellValue(i, "TargetID"));
                parentID = Convert.ToInt32(_treeListTarget.GetCellValue(i, "ParentID"));
                targetName = _treeListTarget.GetCellValue(i, "TargetName").ToString();
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

                // insert empty datetime to xml if no quantity is inputted
                if (quantity == string.Empty)
                {
                    dt.Rows.Add(targetID, parentID, targetName, quantity, 1, string.Empty);
                }

                else if (quantity != string.Empty)
                {
                    if (date == Convert.ToDateTime("1/01/0001"))
                    {
                        date = DateTime.Today;
                    }

                    dt.Rows.Add(targetID, parentID, targetName, quantity, 1, String.Format("{0:dd/MM/yyyy}", date));

                    //number++;
                }
            }

            string fileName = string.Empty;

            if (isClient)
            {
                fileName = "[" + DBName + "]"
                   + "_[" + clientName + "]"
                   + "_[Report]"
                   + "_" + System.IO.Path.GetFileName(path);
            }

            else
            {
                // create file name with DB name prefix [DB]_[ClientName]_[Type]_fileName.xml
                fileName = "[" + DB.getDBNameFromConnectionString() + "]"
                   + "_[" + clientName + "]"
                   + "_[Report]"
                   + "_" + System.IO.Path.GetFileName(path);
            }

            string filePath = System.IO.Path.GetDirectoryName(path);
            string fileName2 = filePath + "\\" + fileName;
            dt.WriteXml(fileName2);
            return fileName2;
        }

        public static DataTable refreshDataTableXML(DataTable dt)
        {
            dt = new DataTable("Record");
            dt.Columns.Add("TargetID", typeof(int));
            dt.Columns.Add("ParentID", typeof(int));
            dt.Columns.Add("TargetName", typeof(string));
            dt.Columns.Add("QuantityValue", typeof(string));
            dt.Columns.Add("ClientID", typeof(int));
            dt.Columns.Add("RecordTime", typeof(string));

            return dt;
        }

        public static DataTable XElementToDataTable(XElement x)
        {
            DataTable dt = new DataTable();

            XElement setup = (from p in x.Descendants() select p).First();
            foreach (XElement xe in setup.Descendants()) // build your DataTable
            {
                dt.Columns.Add(new DataColumn(xe.Name.ToString(), typeof(string)));
            } // add columns to your dt

            var all = from p in x.Descendants(setup.Name.ToString()) select p;
            foreach (XElement xe in all)
            {
                DataRow dr = dt.NewRow();
                foreach (XElement xe2 in xe.Descendants())
                {
                    dr[xe2.Name.ToString()] = xe2.Value; //add in the values
                }
                dt.Rows.Add(dr);
            }
            return dt;
        }

        public static DataTable getDataTableFromXML(string filePath)
        {
            DataTable dt;
            try
            {
                // load XML file
                XElement x = XElement.Load(filePath);

                // declare a new DataTable and pass XElement to it
                dt = XElementToDataTable(x);
            }
            catch (Exception)
            {
                return null;
            }
            return dt;
        }

        public static string readStringFromText(string filePath)
        {
            string info = string.Empty;
            try
            {
                info = File.ReadAllText(filePath);
            }
            catch (FileNotFoundException)
            {
                info = "NotOK";
            }
            return info;
        }
    }
}