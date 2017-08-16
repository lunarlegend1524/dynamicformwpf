// -----------------------------------------------------------------------
// <copyright file="ReportInfo.cs" company="">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

namespace DynamicFormWPF
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public class ReportInfo
    {
        private string _path;
        private string _name;

        public ReportInfo() { }
        public ReportInfo(string Path, string Name)
        {
            this._path = Path;
            this._name = Name;
        }
        public string Path
        {
            get { return _path; }
            set { _path = value; }
        }

        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }
    }
}
