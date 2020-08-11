using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Widget;
using HorizontApp.Domain.Enums;
using HorizontApp.Utilities;
using System.Collections.Generic;
using static Android.Views.View;

namespace HorizontApp.Activities
{
    [Activity(Label = "SelectCategoryActivity", ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation, ScreenOrientation = ScreenOrientation.Landscape)]
    public class SelectCategoryActivity : Activity, IOnClickListener
    {
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

            var instance = CompassViewSettings.Instance();

            back = FindViewById<Button>(Resource.Id.buttonBack);
            back.SetOnClickListener(this);

            checkBoxMountains = FindViewById<CheckBox>(Resource.Id.checkBoxMountains);
            checkBoxMountains.Checked = instance.Categories.Contains(PoiCategory.Mountains);

            checkBoxLakes = FindViewById<CheckBox>(Resource.Id.checkBoxLakes);
            checkBoxLakes.Checked = instance.Categories.Contains(PoiCategory.Lakes);

            checkBoxCastles = FindViewById<CheckBox>(Resource.Id.checkBoxCastles);
            checkBoxCastles.Checked = instance.Categories.Contains(PoiCategory.Castles);

            checkBoxPalaces = FindViewById<CheckBox>(Resource.Id.checkBoxPalaces);
            checkBoxPalaces.Checked = instance.Categories.Contains(PoiCategory.Palaces);

            checkBoxRuins = FindViewById<CheckBox>(Resource.Id.checkBoxRuins);
            checkBoxRuins.Checked = instance.Categories.Contains(PoiCategory.Ruins);

            checkBoxViewTowers = FindViewById<CheckBox>(Resource.Id.checkBoxViewTowers);
            checkBoxViewTowers.Checked = instance.Categories.Contains(PoiCategory.ViewTowers);

            checkBoxViewTransmitters = FindViewById<CheckBox>(Resource.Id.checkBoxTransmitters);
            checkBoxViewTransmitters.Checked = instance.Categories.Contains(PoiCategory.Transmitters);

            checkBoxChurches = FindViewById<CheckBox>(Resource.Id.checkBoxChurches);
            checkBoxChurches.Checked = instance.Categories.Contains(PoiCategory.Churches);

            checkBoxTest = FindViewById<CheckBox>(Resource.Id.checkBoxTest);
            checkBoxTest.Checked = instance.Categories.Contains(PoiCategory.Test);

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
            var selectedCategories = new List<PoiCategory>();
            if (checkBoxMountains.Checked) selectedCategories.Add(PoiCategory.Mountains);
            if (checkBoxLakes.Checked) selectedCategories.Add(PoiCategory.Lakes);
            if (checkBoxCastles.Checked) selectedCategories.Add(PoiCategory.Castles);
            if (checkBoxPalaces.Checked) selectedCategories.Add(PoiCategory.Palaces);
            if (checkBoxRuins.Checked) selectedCategories.Add(PoiCategory.Ruins);
            if (checkBoxViewTowers.Checked) selectedCategories.Add(PoiCategory.ViewTowers);
            if (checkBoxViewTransmitters.Checked) selectedCategories.Add(PoiCategory.Transmitters);
            if (checkBoxChurches.Checked) selectedCategories.Add(PoiCategory.Churches);
            if (checkBoxTest.Checked) selectedCategories.Add(PoiCategory.Test);

            var instance = CompassViewSettings.Instance();
            instance.Categories = selectedCategories;
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