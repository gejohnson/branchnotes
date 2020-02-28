using System;
using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using Android.Text;
using Java.Lang;
using Android;
using System.Collections.Generic;

namespace recorder_stub
{
    [Activity(Label = "Search Notes", MainLauncher = false)]
    public class SearchActivity : Activity, Android.Text.ITextWatcher
    {
        ArrayAdapter adapter;
        List<note> data = new List<note>();

        public void AfterTextChanged(IEditable s)
        { }
        public void BeforeTextChanged(ICharSequence s, int start, int count, int after)
        { }
        public void OnTextChanged(ICharSequence s, int start, int before, int count)
        {
            adapter.Filter.InvokeFilter(s);
        }
        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            SetContentView(Resource.Layout.activity_search);

            List<note> allNotes = TableAccess.GetAllNotes();
            foreach (note n in allNotes)
                data.Add(n);

            var edtSearch = FindViewById<EditText>(Resource.Id.edtSearch);
            var lstSearch = FindViewById<ListView>(Resource.Id.lstSearch);

            adapter = new ArrayAdapter(this, Resource.Layout.listItem, Resource.Id.textView1, data);
            lstSearch.Adapter = adapter;

            lstSearch.ItemClick += LstSearch_ItemClick;

            edtSearch.AddTextChangedListener(this);
        }

        private void LstSearch_ItemClick(object sender, AdapterView.ItemClickEventArgs e)
        {
            note selectedNote = data[e.Position];
            IList<string> noteExtra = new List<string>();
            noteExtra.Add(selectedNote.Title);
            noteExtra.Add(selectedNote.Id.ToString());
            var intent = new Intent(this, typeof(NodeActivity));
            intent.PutStringArrayListExtra("Title and ID", noteExtra);
            StartActivity(intent);
        }
    }
}

