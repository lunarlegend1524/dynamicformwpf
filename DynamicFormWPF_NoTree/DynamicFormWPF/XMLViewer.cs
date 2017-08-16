// -----------------------------------------------------------------------
// <copyright file="XMLViewer.cs" company="">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

namespace DynamicFormWPF
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Data;
    using System.Xml.Linq;
    using System.IO;

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public class XMLViewer
    {
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

        public static string readStringFromText(string filePath){
            string info = string.Empty;
            info = File.ReadAllText(filePath);
            return info;
        }
    }
}
