using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Widget;
using HorizontApp.Utilities;
using static Android.Views.View;

namespace HorizontApp.Activities
{
    [Activity(Label = "SelectCategoryActivity", ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation, ScreenOrientation = ScreenOrientation.Landscape)]
    public class SelectCategoryActivity : Activity, IOnClickListener
    {
        CheckBox checkBoxPeaks;
        CheckBox checkBoxMountains;
        CheckBox checkBoxLakes;
        CheckBox checkBoxCastles;
        CheckBox checkBoxPalaces;
        CheckBox checkBoxRuins;
        CheckBox checkBoxViewTowers;
        CheckBox checkBoxViewTransmitters;
        CheckBox checkBoxChurches;
        CheckBox checkBoxTest;
        Button back;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.SelectCategoryActivity);
            // Create your application here

            var instance = SelectedCategory.Instance();

            back = FindViewById<Button>(Resource.Id.buttonBack);
            back.SetOnClickListener(this);

            checkBoxPeaks = FindViewById<CheckBox>(Resource.Id.checkBoxPeaks);
            checkBoxPeaks.Checked = instance.VisiblePeaks;

            checkBoxMountains = FindViewById<CheckBox>(Resource.Id.checkBoxMountains);
            checkBoxMountains.Checked = instance.VisibleMountains;

            checkBoxLakes = FindViewById<CheckBox>(Resource.Id.checkBoxLakes);
            checkBoxLakes.Checked = instance.VisibleLakes;

            checkBoxCastles = FindViewById<CheckBox>(Resource.Id.checkBoxCastles);
            checkBoxCastles.Checked = instance.VisibleCastles;

            checkBoxPalaces = FindViewById<CheckBox>(Resource.Id.checkBoxPalaces);
            checkBoxPalaces.Checked = instance.VisiblePalaces;

            checkBoxRuins = FindViewById<CheckBox>(Resource.Id.checkBoxRuins);
            checkBoxRuins.Checked = instance.VisibleRuins;

            checkBoxViewTowers = FindViewById<CheckBox>(Resource.Id.checkBoxViewTowers);
            checkBoxViewTowers.Checked = instance.VisibleViewTowers;

            checkBoxViewTransmitters = FindViewById<CheckBox>(Resource.Id.checkBoxTransmitters);
            checkBoxViewTransmitters.Checked = instance.VisibleTransmitters;

            checkBoxChurches = FindViewById<CheckBox>(Resource.Id.checkBoxChurches);
            checkBoxChurches.Checked = instance.VisibleChurches;

            checkBoxTest = FindViewById<CheckBox>(Resource.Id.checkBoxTest);
            checkBoxTest.Checked = instance.VisibleTest;


            checkBoxPeaks.CheckedChange += CheckedChange;
            checkBoxMountains.CheckedChange += CheckedChange;
            checkBoxLakes.CheckedChange += CheckedChange;
            checkBoxCastles.CheckedChange += CheckedChange;
            checkBoxPalaces.CheckedChange += CheckedChange;
            checkBoxRuins.CheckedChange += CheckedChange;
            checkBoxViewTowers.CheckedChange += CheckedChange;
            checkBoxViewTransmitters.CheckedChange += CheckedChange;
            checkBoxChurches.CheckedChange += CheckedChange;
            checkBoxTest.CheckedChange += CheckedChange;
        }

        private void CheckedChange(object sender, CompoundButton.CheckedChangeEventArgs e)
        {
            var instance = SelectedCategory.Instance();
            instance.VisiblePeaks = checkBoxPeaks.Checked;
            instance.VisibleMountains = checkBoxMountains.Checked;
            instance.VisibleLakes = checkBoxLakes.Checked;
            instance.VisibleCastles = checkBoxCastles.Checked;
            instance.VisiblePalaces = checkBoxPalaces.Checked;
            instance.VisibleRuins = checkBoxRuins.Checked;
            instance.VisibleViewTowers = checkBoxViewTowers.Checked;
            instance.VisibleTransmitters = checkBoxViewTransmitters.Checked; 
            instance.VisibleChurches = checkBoxChurches.Checked;
            instance.VisibleTest = checkBoxTest.Checked;
        }

        public async void OnClick(Android.Views.View v)
        {
            switch (v.Id)
            {
                case Resource.Id.buttonBack:
                    {
                        Finish();
                        break;
                    }
            }
        }
    }
}