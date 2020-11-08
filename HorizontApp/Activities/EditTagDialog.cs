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
using HorizontApp.Utilities;
using HorizontLib.Domain.Models;
using static Android.Views.View;

namespace HorizontApp.Activities
{
    [Activity(Label = "EditTagDialog")]
    public class EditTagDialog : Dialog, IOnClickListener
    {
        private EditText _editTagEditText;
        private PhotoData _photodata;
        private Action<Result> _onFinished;
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
            _photodata = item;
        }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.EditTagLayout);

            _editTagEditText = FindViewById<EditText>(Resource.Id.editTextName);
            _editTagEditText.Text = _photodata.Tag;

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
                        _photodata.Tag = _editTagEditText.Text;
                        Database.UpdateItem(_photodata);
                        _onFinished?.Invoke(Result.Ok);
                        Hide();
                        Dismiss();
                        break;
                    case Resource.Id.buttonClose:
                        _onFinished?.Invoke(Result.Canceled);
                        Hide();
                        Dismiss();
                        break;
                }
            }
            catch (Exception ex)
            {
                PopupHelper.ErrorDialog(Context, "Error", ex.Message);
            }
        }

        public void Show(Action<Result> onFinished)
        {
            _onFinished = onFinished;
            Show();
        }
    }
}