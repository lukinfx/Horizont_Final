using Android.App;
using Android.OS;
using Android.Widget;
using Android.Content.PM;
using Android.Views;
using static Android.Views.View;
using HorizontApp.Utilities;
using System;
using System.Linq;

namespace HorizontApp.Activities
{



    [Activity(Label = "SettingsActivity", ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation, ScreenOrientation = ScreenOrientation.Landscape)]
    public class SettingsActivity : Activity, IOnClickListener
    {
        CompassViewSettings settings = CompassViewSettings.Instance();
        Switch fullscreenBackground;
        Switch textBackground;
        Spinner appStyle;
        Button back;
        private AppStyles[] _listOfAppStyles = new AppStyles[] { AppStyles.NewStyle, AppStyles.OldStyle };

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            SetContentView(Resource.Layout.SettingsLayout);

            back = FindViewById<Button>(Resource.Id.buttonBack);
            back.SetOnClickListener(this);

            fullscreenBackground = FindViewById<Switch>(Resource.Id.TransparentRectangleFullscreen);
            fullscreenBackground.SetOnClickListener(this);

            appStyle = FindViewById<Spinner>(Resource.Id.spinnerAppStyle);
            //var appStyleDropDown = FindViewById<DropDownListView>(Resource.Id.appStyleDropDown);
            

            textBackground = FindViewById<Switch>(Resource.Id.TransparentRectangleTextBackground);
            textBackground.SetOnClickListener(this);

            appStyle.ItemSelected += new EventHandler<AdapterView.ItemSelectedEventArgs>(appStyle_ItemSelected);
            //var adapter = ArrayAdapter.CreateFromResource(this, Resource.Array.SpinnerItemsArray, Android.Resource.Layout.SimpleSpinnerItem);
            //adapter.SetDropDownViewResource(Android.Resource.Layout.SimpleSpinnerDropDownItem);
            
            var adapter = new ArrayAdapter(this, Android.Resource.Layout.SimpleSpinnerDropDownItem, _listOfAppStyles.ToList());
            appStyle.Adapter = adapter;
            appStyle.SetSelection(_listOfAppStyles.ToList().FindIndex(i => i == CompassViewSettings.Instance().AppStyle));
        }

        private void appStyle_ItemSelected(object sender, AdapterView.ItemSelectedEventArgs e)
        {
            CompassViewSettings.Instance().AppStyle = _listOfAppStyles[e.Position];
        }

        public void OnClick(View v)
        {
           switch (v.Id)
            {
                case Resource.Id.buttonBack:
                    Finish();
                    break;
            }
                

        }
    }
}