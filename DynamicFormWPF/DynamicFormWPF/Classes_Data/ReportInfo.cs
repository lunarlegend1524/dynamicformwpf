// -----------------------------------------------------------------------
// <copyright file="ReportInfo.cs" company="">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

namespace DynamicFormWPF.Classes_Data
{
    using System;

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public class ReportInfo
    {
        private string _path;
        private string _name;
        private DateTime _creationTime;
        private bool _isSaved;

        public ReportInfo()
        {
        }

        public ReportInfo(string Path, string Name, DateTime CreationTime, bool IsSaved)
        {
            this._path = Path;
            this._name = Name;
            this._creationTime = CreationTime;
            this._isSaved = IsSaved;
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

        public bool IsSaved
        {
            get { return _isSaved; }
            set { _isSaved = value; }
        }
    }
}