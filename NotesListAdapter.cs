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

namespace recorder_stub
{
    class NotesListAdapter : BaseAdapter<note>
    {
        List<note> notes;
        Activity context;

        public NotesListAdapter(Activity _context, List<note> _notes) : base()
        {
            this.notes = _notes;
            this.context = _context;
        }

        public override long GetItemId(int position)
        {
            return position;
        }

        public override note this[int position]
        {
            get
            {
                return notes[position];
            }
        }

        public override int Count
        {
            get
            {
                return notes.Count;
            }
        }

        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            View view = convertView;
            if(view == null)
            {
                view = context.LayoutInflater.Inflate(Android.Resource.Layout.SimpleSelectableListItem, null);
            }
            view.FindViewById<TextView>(Android.Resource.Id.Text1).Text = notes[position].ToString();
            return view;
        }
    }
}
