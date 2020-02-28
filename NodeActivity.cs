using Android.App;
using Android.OS;
using Android.Support.V7.App;
using Android.Runtime;
using Android.Widget;
using Android.Media;
using Android.Support.V4.App;
using Android.Util;
using Android.Views;
using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using Android.Content;

namespace recorder_stub
{
    [Activity(Label = "Node Activity", Theme = "@style/AppTheme", MainLauncher = true)]
    public class NodeActivity : AppCompatActivity
    {
        Button btnRecord, btnStopRecord, btnPlayParent, btnStopParent, btnPlayChild, btnStopChild, btnSaveNote;
        string pathFolder = Android.OS.Environment.ExternalStorageDirectory.AbsolutePath.ToString() + "/BranchNotes";
        string fileTemp = "";
        TextView textViewHeader;
        MediaRecorder mediaRecorder;
        MediaPlayer mediaPlayer;
        List<note> notesList;
        //MediaController mediaController;
        ListView notesListView;
        NotesListAdapter adapter;
        IList<string> parentExtra;
        string parentTitle;
        int parentId;
        string parentFile;

        private const int REQUEST_PERMISSION_CODE = 1000;

        private bool isGrantedPermission = false;

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            switch (requestCode)
            {
                case REQUEST_PERMISSION_CODE:
                    {
                        if (grantResults.Length > 0 && grantResults[0] == Android.Content.PM.Permission.Granted)
                        {
                            Toast.MakeText(this, "Granted,", ToastLength.Short).Show();
                            isGrantedPermission = true;
                        }
                        else
                        {
                            Toast.MakeText(this, "Denied,", ToastLength.Short).Show();
                            isGrantedPermission = false;
                        }
                        break;
                    }
            }
            //Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);
            //base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            SetContentView(Resource.Layout.node_activity);

            isGrantedPermission = true;

            fileTemp = pathFolder + "/" + "$temp.3gp";

            btnPlayParent = FindViewById<Button>(Resource.Id.btnPlayParent);
            btnStopParent = FindViewById<Button>(Resource.Id.btnStopParent);
            btnRecord = FindViewById<Button>(Resource.Id.btnRecord);
            btnStopRecord = FindViewById<Button>(Resource.Id.btnStopRecord);
            btnSaveNote = FindViewById<Button>(Resource.Id.btnSaveNote);
            notesListView = FindViewById<ListView>(Resource.Id.notesListView);
            btnStopChild = FindViewById<Button>(Resource.Id.btnStopChild);
            btnPlayChild = FindViewById<Button>(Resource.Id.btnPlayChild);
            textViewHeader = FindViewById<TextView>(Resource.Id.textViewHeader);




            parentExtra = Intent.GetStringArrayListExtra("Title and ID");
            parentTitle = parentExtra[0];
            parentId = Int32.Parse(parentExtra[1]);
            parentFile = pathFolder + "/" + parentTitle + ".3gp";

            textViewHeader.Text = parentTitle;

            notesList = TableAccess.GetAllNotes();

            foreach (note n in notesList.ToArray())
            {
                if (n.ParentId != parentId)
                    notesList.Remove(n);
            }

            adapter = new NotesListAdapter(this, notesList);

            notesListView.Adapter = adapter;

            notesListView.ItemClick += NotesListView_ItemClick;

            RegisterForContextMenu(notesListView);

            btnStopChild.Enabled = false;
            btnPlayChild.Enabled = false;
            btnStopRecord.Enabled = false;
            btnSaveNote.Enabled = false;

            btnRecord.Click += delegate
            {
                RecordAudio();
            };

            btnStopRecord.Click += delegate
            {
                StopRecorder();
            };

            btnPlayChild.Click += delegate
            {
                StartLastRecord();
            };

            btnStopChild.Click += delegate
            {
                StopLastRecord();
            };

            btnSaveNote.Click += delegate
            {
                SaveNote();
            };
            btnPlayParent.Click += delegate
            {
                StartParentRecord();
            };
        }
        private void StartParentRecord()
        {
            //TODO: Figure out button enable/disable behavior

            mediaPlayer = new MediaPlayer();
            try
            {
                mediaPlayer.SetDataSource(parentFile);
                mediaPlayer.Prepare();
            }
            catch (Exception ex)
            {
                Log.Debug("DEBUG", ex.Message);
            }
            mediaPlayer.Start();
            Toast.MakeText(this, "Playing Parent", ToastLength.Short).Show();
        }

        private void NotesListView_ItemClick(object sender, AdapterView.ItemClickEventArgs e)
        {
            note selectedNote = notesList[e.Position];
            IList<string> noteExtra = new List<string>();
            noteExtra.Add(selectedNote.Title);
            noteExtra.Add(selectedNote.Id.ToString());
            var intent = new Intent(this, typeof(NodeActivity));
            intent.PutStringArrayListExtra("Title and ID", noteExtra);
            StartActivity(intent);
        }

        public override void OnCreateContextMenu(IContextMenu menu, View v, IContextMenuContextMenuInfo menuInfo)
        {
            if (v.Id == Resource.Id.notesListView)
            {
                var info = (AdapterView.AdapterContextMenuInfo)menuInfo;
                menu.SetHeaderTitle("Edit Note");
                var menuItems = Resources.GetStringArray(Resource.Array.menu);
                for (int i = 0; i < menuItems.Length; i++)
                {
                    menu.Add(Menu.None, i, i, menuItems[i]);
                }
            }
        }
        public override bool OnContextItemSelected(IMenuItem item)
        {
            //This is information about the context menu itself.
            var info = (AdapterView.AdapterContextMenuInfo)item.MenuInfo;

            //This is the index of the selected menu item; "Rename" being 0 and "Delete" being 1.
            var index = item.ItemId;

            //This is the parent array containing both of said menu items. 
            var menuItems = Resources.GetStringArray(Resource.Array.menu);

            //This is the name of the selected menu item, derived by applying the selected index to the parent array.
            var menuItemName = menuItems[index];

            //This is the actual title of the note that was long-pressed on, gotten by using the passed-in application context of the pressed position.
            var noteTitle = notesList[info.Position].Title;

            //Toast.MakeText(this, string.Format("Selected {0} for item {1}", menuItemName, noteTitle), ToastLength.Short).Show();

            if (menuItemName.Equals("Rename"))
            {
                string newTitle = noteTitle;
                List<string> titles = TableAccess.GetAllTitles();
                Android.App.AlertDialog.Builder adb = new Android.App.AlertDialog.Builder(this);
                Android.App.AlertDialog ad;

                EditText et = new EditText(this);
                et.SetSingleLine();
                et.Text = noteTitle;

                adb.SetTitle("Rename Note");
                adb.SetMessage("Type a title and hit enter:");
                adb.SetView(et);
                adb.SetPositiveButton("Rename", (senderAlert, args) =>
                {
                    try
                    {
                        notesList.RemoveAt(info.Position);
                        TableAccess.EditName(noteTitle, newTitle);
                        notesList.Add(TableAccess.GetNoteFromTitle(newTitle));
                    }
                    catch (Exception ex)
                    {
                        Toast.MakeText(this, "Could not save file: " + ex, ToastLength.Short).Show();
                    }
                    adb.Dispose();
                });

                adb.SetNegativeButton("Cancel", (senderAlert, args) =>
                {
                    adb.Dispose();
                });

                ad = adb.Show();
                ad.GetButton(-1).Enabled = false;

                et.KeyPress += (object sender, View.KeyEventArgs e) =>
                {
                    if ((e.KeyCode == Keycode.Enter))
                    {
                        newTitle = StripIllegalChars(et.Text);
                        if (string.IsNullOrEmpty(newTitle))
                        {
                            Toast.MakeText(this, "Please enter a valid name", ToastLength.Short).Show();
                            ad.GetButton(-1).Enabled = false;
                        }
                        else if (titles.Contains(newTitle))
                        {
                            Toast.MakeText(this, "Name already exists, please choose another.", ToastLength.Short).Show();
                            ad.GetButton(-1).Enabled = false;
                        }
                        else
                            ad.GetButton(-1).Enabled = true;
                    }
                    else
                        ad.GetButton(-1).Enabled = false;
                };
                return true;
            }
            else if (menuItemName.Equals("Delete"))
            {
                Android.App.AlertDialog.Builder adb = new Android.App.AlertDialog.Builder(this);
                Android.App.AlertDialog ad;

                adb.SetTitle("Delete Note");
                adb.SetMessage("Are you sure you want to delete this note and all of its replies?");
                adb.SetPositiveButton("Delete", (senderAlert, args) =>
                {
                    try
                    {
                        notesList.RemoveAt(info.Position);
                        TableAccess.DeleteRecursive(noteTitle);
                        adapter = new NotesListAdapter(this, notesList);
                        notesListView.Adapter = adapter;
                    }
                    catch (Exception ex)
                    {
                        Toast.MakeText(this, "Could not delete note: " + ex, ToastLength.Short).Show();
                    }
                    adb.Dispose();
                });
                adb.SetNegativeButton("Cancel", (senderAlert, args) =>
                {
                    adb.Dispose();
                });

                ad = adb.Show();

                return true;
            };
            return false;
        }
        private void StopLastRecord()
        {
            btnStopChild.Enabled = false;
            btnStopRecord.Enabled = false;
            btnPlayChild.Enabled = true;
            btnRecord.Enabled = true;

            if (mediaPlayer != null)
            {
                mediaPlayer.Stop();
                mediaPlayer.Release();
                SetUpMediaRecorder();
            }
        }
        private void StartLastRecord()
        {
            btnStopRecord.Enabled = false;
            btnPlayChild.Enabled = true;
            btnRecord.Enabled = false;

            mediaPlayer = new MediaPlayer();
            try
            {
                mediaPlayer.SetDataSource(fileTemp);
                mediaPlayer.Prepare();
            }
            catch (Exception ex)
            {
                Log.Debug("DEBUG", ex.Message);
            }
            mediaPlayer.Start();
            Toast.MakeText(this, "Playing Recording", ToastLength.Short).Show();
        }
        private void StopRecorder()
        {
            mediaRecorder.Stop();
            btnPlayChild.Enabled = true;
            btnStopChild.Enabled = false;
            btnStopRecord.Enabled = false;
            btnRecord.Enabled = true;
            btnSaveNote.Enabled = true;

            Toast.MakeText(this, "Stop Recording...", ToastLength.Short).Show();
        }
        private void RecordAudio()
        {
            if (isGrantedPermission)
            {
                SetUpMediaRecorder();
                try
                {
                    mediaRecorder.Prepare();
                    mediaRecorder.Start();

                    btnPlayChild.Enabled = false;
                    btnStopRecord.Enabled = true;
                }
                catch (Exception ex)
                {
                    Log.Debug("DEBUG", ex.Message);
                }

                Toast.MakeText(this, "Recording...", ToastLength.Short).Show();
            }
        }
        private void SaveNote()
        {
            string title = string.Empty;
            List<string> titles = TableAccess.GetAllTitles();
            Android.App.AlertDialog.Builder adb = new Android.App.AlertDialog.Builder(this);
            Android.App.AlertDialog ad;

            EditText et = new EditText(this);
            et.SetSingleLine();

            adb.SetTitle("Save Note");
            adb.SetMessage("Type a title and hit enter:");
            adb.SetView(et);
            adb.SetPositiveButton("Save", (senderAlert, args) =>
            {
                try
                {
                    System.IO.File.Copy(fileTemp, pathFolder + "/" + title + ".3gp");
                    TableAccess.AddNewNote(title, parentId);
                    notesList.Add(TableAccess.GetNoteFromTitle(title));
                }
                catch (Exception ex)
                {
                    Toast.MakeText(this, "Could not save file: " + ex, ToastLength.Short).Show();
                }
                adb.Dispose();
            });

            adb.SetNegativeButton("Cancel", (senderAlert, args) =>
            {
                adb.Dispose();
            });

            ad = adb.Show();
            ad.GetButton(-1).Enabled = false;

            et.KeyPress += (object sender, View.KeyEventArgs e) =>
            {
                if ((e.KeyCode == Keycode.Enter))
                {
                    title = StripIllegalChars(et.Text);
                    if (string.IsNullOrEmpty(title))
                    {
                        Toast.MakeText(this, "Please enter a valid name", ToastLength.Short).Show();
                        ad.GetButton(-1).Enabled = false;
                    }
                    else if (titles.Contains(title))
                    {
                        Toast.MakeText(this, "Name already exists, please choose another.", ToastLength.Short).Show();
                        ad.GetButton(-1).Enabled = false;
                    }
                    else
                        ad.GetButton(-1).Enabled = true;
                }
                else
                    ad.GetButton(-1).Enabled = false;
            };

        }
        private void SetUpMediaRecorder()
        {
            mediaRecorder = new MediaRecorder();
            mediaRecorder.SetAudioSource(AudioSource.Mic);
            mediaRecorder.SetOutputFormat(OutputFormat.ThreeGpp);
            mediaRecorder.SetAudioEncoder(AudioEncoder.AmrNb);
            mediaRecorder.SetOutputFile(fileTemp);
        }
        private string StripIllegalChars(string input)
        {
            try
            {
                return Regex.Replace(input, @"[^\w-\s]", "");
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
