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
using HorizontApp.DataAccess;
using HorizontLib.Domain.Models;
using static Android.Views.View;

namespace HorizontApp.Activities
{
    [Activity(Label = "EditTagDialog")]
    public class EditTagDialog : Dialog, IOnClickListener
    {
        EditText editTagEditText;
        PhotoData photodata;

        private PoiDatabase _database;
        private PoiDatabase Database
        {
            get
            {
                if (_database == null)
                {
                    _database = new PoiDatabase();
                }
                return _database;
            }
        }

        public EditTagDialog(Context context, PhotoData item) : base(context)
        {
            photodata = item;
        }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.EditTagLayout);

            editTagEditText = FindViewById<EditText>(Resource.Id.editTextName);
            editTagEditText.Text = photodata.Tag;

            var buttonSave = FindViewById<Button>(Resource.Id.buttonSave);
            buttonSave.SetOnClickListener(this);
            var buttonClose = FindViewById<Button>(Resource.Id.buttonClose);
            buttonClose.SetOnClickListener(this);
        }

        public async void OnClick(Android.Views.View v)
        {
            try
            {
                switch (v.Id)
                {
                    case Resource.Id.buttonSave:
                        photodata.Tag = editTagEditText.Text;
                        Database.UpdateItem(photodata);
                        Hide();
                        Dismiss();
                        break;
                    case Resource.Id.buttonClose:
                        Hide();
                        Dismiss();
                        break;
                }
            }
            catch { }
        }
    }
}