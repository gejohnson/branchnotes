using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SQLite;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using System.IO;

namespace recorder_stub
{
    public static class TableAccess
    {
        public static string folder = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal);
        public static string path = System.IO.Path.Combine(folder, "db.db3");
        public static string pathFolder = Android.OS.Environment.ExternalStorageDirectory.AbsolutePath.ToString() + "/BranchNotes";
        public static SQLiteConnection conn = new SQLiteConnection(path);
        public static void Create(string path)
        {
            try
            {
                conn.CreateTable<note>();
                Toast.MakeText(Application.Context, "Table created.", ToastLength.Short).Show();
            }
            catch (Exception ex)
            {
                Toast.MakeText(Application.Context, "Table not created: " + ex.Message, ToastLength.Short).Show();
            }
        }
        public static void AddNewNote(string title, int parentID)
        {
            try
            {
                conn.Insert(new note { Title = title, ParentId = parentID, createDate = DateTime.Now });
                Toast.MakeText(Application.Context, "Note added: " + title, ToastLength.Short).Show();
            }
            catch (Exception ex)
            {
                Toast.MakeText(Application.Context, "Note not added: " + ex.Message, ToastLength.Short).Show();
            }
        }
        public static void AddNewNote(string title)
        {
            try
            {
                conn.Insert(new note { Title = title, ParentId = -1, createDate = DateTime.Now });
                Toast.MakeText(Application.Context, "Note added: " + title, ToastLength.Short).Show();
            }
            catch (Exception ex)
            {
                Toast.MakeText(Application.Context, "Note not added: " + ex.Message, ToastLength.Short).Show();
            }
        }
        public static List<note> GetAllNotes()
        {
            try
            {
                return conn.Table<note>().ToList();
            }
            catch (Exception ex)
            {
                Toast.MakeText(Application.Context, "Couldn't find notes: " + ex.Message, ToastLength.Short);
            }
            return new List<note>();
        }
        public static List<string> GetAllTitles()
        {
            List<note> notes = conn.Table<note>().ToList();
            List<string> titles = new List<string>();
            foreach (note n in notes)
            {
                titles.Add(n.Title);
            }
            return titles;
        }
        public static note GetNoteFromTitle(string title)
        {
                note note = conn.Table<note>().Where(x => x.Title == title).FirstOrDefault();
                return note;
        }
        public static void EditName(string title, string newTitle)
        {
            try
            {
                TableQuery<note> table = conn.Table<note>();
                var toUpdate = table.Where(x => x.Title == title).FirstOrDefault();
                toUpdate.Title = newTitle;
                conn.Update(toUpdate);
            }
            catch (Exception ex)
            {
                Toast.MakeText(Application.Context, "Could not rename note: " + ex, ToastLength.Long).Show();
            }
            try
            {
                System.IO.File.Copy(pathFolder + "/" + title + ".3gp", pathFolder + "/" + newTitle + ".3gp");
                System.IO.File.Delete(pathFolder + "/" + title + ".3gp");
            }
            catch (Exception ex)
            {
                Toast.MakeText(Application.Context, "Could not rename audio file file: " + ex, ToastLength.Long).Show();
            }
        }
        public static void DeleteNote(string title)
        {
            try
            {
                TableQuery<note> table = conn.Table<note>();
                var toDelete = table.Where(x => x.Title == title).FirstOrDefault();
                if (toDelete.Title != null)
                {
                    conn.Delete(toDelete);
                }
            }
            catch (Exception ex)
            {
                Toast.MakeText(Application.Context, "Note data not deleted: " + ex, ToastLength.Long).Show();
            }
            try
            {
                System.IO.File.Delete(pathFolder + "/" + title + ".3gp");
            }
            catch (Exception ex)
            {
                Toast.MakeText(Application.Context, "Audio file not deleted: " + ex, ToastLength.Long).Show();
            }
        }
        public static void DeleteRecursive(string title)
        {
            note target = GetNoteFromTitle(title);
            List<note> notesList = GetAllNotes();
            foreach (note n in notesList.ToArray())
            {
                if (n.ParentId == target.Id)
                    DeleteRecursive(n.Title);
            }
            DeleteNote(title);
        }
        public static int GetNumChildren(note parent)
        {
            int children = 0;

            foreach (note n in conn.Table<note>().ToList())
            {
                if (n.ParentId == parent.Id)
                    children++;
            }
            return children;
        }
    }
}

