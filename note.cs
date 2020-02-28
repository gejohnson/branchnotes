using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using SQLite;
using System.IO;

namespace recorder_stub
{
    [Table("note")]
    public class note
    {
        private static readonly string pathFolder = Android.OS.Environment.ExternalStorageDirectory.AbsolutePath.ToString() + "/BranchNotes";
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        [MaxLength(250), Unique]
        public string Title { get; set; }
        public int ParentId { get; set; }
        public DateTime createDate { get; set; }
        public override string ToString()
        {
            return Title + " — created " + createDate;
        }
    }
}
