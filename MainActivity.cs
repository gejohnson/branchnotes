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
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true, Icon = "@drawable/icon")]
    public class MainActivity : AppCompatActivity
    {
        Button btnRecord, btnStopRecord, btnStart, btnStop, btnSaveNote, btnSearch;
        string pathFolder = Android.OS.Environment.ExternalStorageDirectory.AbsolutePath.ToString() + "/BranchNotes";
        string fileTemp = Android.OS.Environment.ExternalStorageDirectory.AbsolutePath.ToString() + "/BranchNotes" + "/" + "$temp.3gp";
        MediaRecorder mediaRecorder;
        MediaPlayer mediaPlayer;
        List<note> notesList;
        //MediaController mediaController;
        ListView notesListView;
        NotesListAdapter adapter;
        private const int REQUEST_PERMISSION_CODE = 1000;
        private bool isGrantedPermission = false;

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            switch(requestCode)
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
        }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.activity_main);

            RequestPermissions();

            TableAccess.Create(TableAccess.path);

            if (!Directory.Exists(pathFolder))
                Directory.CreateDirectory(pathFolder);

            btnStart = FindViewById<Button>(Resource.Id.btnPlay);
            btnStop = FindViewById<Button>(Resource.Id.btnStop);
            btnRecord = FindViewById<Button>(Resource.Id.btnRecord);
            btnStopRecord = FindViewById<Button>(Resource.Id.btnStopRecord);
            btnSaveNote = FindViewById<Button>(Resource.Id.btnSaveNote);
            btnSearch = FindViewById<Button>(Resource.Id.btnSearch);
            notesListView = FindViewById<ListView>(Resource.Id.notesListView);

            notesList = TableAccess.GetAllNotes();
            foreach (note n in notesList.ToArray())
            {
                if (n.ParentId != -1)
                    notesList.Remove(n);
            }
            adapter = new NotesListAdapter(this, notesList);
            notesListView.Adapter = adapter;
            notesListView.ItemClick += NotesListView_ItemClick;
            RegisterForContextMenu(notesListView);

            btnStop.Enabled = false;
            btnStart.Enabled = false;
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
            btnStart.Click += delegate
            {
                StartLastRecord();
            };
            btnStop.Click += delegate
            {
                StopLastRecord();
            };
            btnSaveNote.Click += delegate
            {
                SaveNote();
            };
            btnSearch.Click += delegate
            {
                Search();
            };
        }

        private void Search()
        {
                var intent = new Intent(this, typeof(SearchActivity));
                StartActivity(intent);
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
                for(int i=0; i<menuItems.Length; i++)
                {
                    menu.Add(Menu.None, i, i, menuItems[i]);
                }
            }
        }
        public override bool OnContextItemSelected(IMenuItem item)
        {
            var info = (AdapterView.AdapterContextMenuInfo)item.MenuInfo;
            var index = item.ItemId;
            var menuItems = Resources.GetStringArray(Resource.Array.menu);
            var menuItemName = menuItems[index];
            var noteTitle = notesList[info.Position].Title;

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
            btnStop.Enabled = false;
            btnStopRecord.Enabled = false;
            btnStart.Enabled = true;
            btnRecord.Enabled = true;

            if(mediaPlayer != null)
            {
                mediaPlayer.Stop();
                mediaPlayer.Release();
                SetUpMediaRecorder();
            }
        }
        private void StartLastRecord()
        {
            btnStopRecord.Enabled = false;
            btnStop.Enabled = true;
            btnRecord.Enabled = false;

            mediaPlayer = new MediaPlayer();
            try
            {
                mediaPlayer.SetDataSource(fileTemp);
                mediaPlayer.Prepare();
            } catch (Exception ex)
            {
                Log.Debug("DEBUG", ex.Message);
            }
            mediaPlayer.Start();
            Toast.MakeText(this, "Playing Recording", ToastLength.Short).Show();
        }
        private void StopRecorder()
        {
            mediaRecorder.Stop();
            btnStart.Enabled = true;
            btnStop.Enabled = false;
            btnStopRecord.Enabled = false;
            btnRecord.Enabled = true;
            btnSaveNote.Enabled = true;

            Toast.MakeText(this, "Stop Recording...", ToastLength.Short).Show();
        }
        private void RecordAudio()
        {
            if(isGrantedPermission)
            {
                SetUpMediaRecorder();
                try
                {
                    mediaRecorder.Prepare();
                    mediaRecorder.Start();

                    btnStart.Enabled = false;
                    btnStopRecord.Enabled = true;
                }
                catch(Exception ex)
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
                    TableAccess.AddNewNote(title);
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
        private void RequestPermissions()
        {
            if (CheckSelfPermission(Android.Manifest.Permission.WriteExternalStorage) != Android.Content.PM.Permission.Granted
            && (CheckSelfPermission(Android.Manifest.Permission.RecordAudio) != Android.Content.PM.Permission.Granted)
            && (CheckSelfPermission(Android.Manifest.Permission.ReadExternalStorage) != Android.Content.PM.Permission.Granted))
            {
                ActivityCompat.RequestPermissions(this, new string[] {
                    Android.Manifest.Permission.WriteExternalStorage,
                    Android.Manifest.Permission.RecordAudio,
                    Android.Manifest.Permission.ReadExternalStorage
                }, REQUEST_PERMISSION_CODE);
            }
            else
                isGrantedPermission = true;
        }
    }
}
