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
        private DateTime _creationTime;

        public ReportInfo() { }
        public ReportInfo(string Path, string Name, DateTime CreationTime)
        {
            this._path = Path;
            this._name = Name;
            this._creationTime = CreationTime;
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

        public DateTime CreationTime
        {
            get { return _creationTime; }
            set { _creationTime = value; }
        }
    }
}
