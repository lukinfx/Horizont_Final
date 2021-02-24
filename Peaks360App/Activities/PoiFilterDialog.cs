using System.Collections.Generic;
using System.Linq;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Widget;
using Peaks360Lib.Domain.Enums;
using Peaks360App.AppContext;
using Peaks360App.Utilities;
using static Android.Views.View;

namespace Peaks360App.Activities
{
    public class PoiFilterDialog : Dialog, IOnClickListener
    {
        private PoiCategory[] supportedCategories = new PoiCategory[] { PoiCategory.Historic, PoiCategory.Mountains, PoiCategory.Churches, PoiCategory.Cities, PoiCategory.Lakes, PoiCategory.Other, PoiCategory.Transmitters, PoiCategory.ViewTowers };
        private Dictionary<PoiCategory, ImageButton> _imageButtonCategoryFilter = new Dictionary<PoiCategory, ImageButton>();
        private IAppContext _context;
        public PoiFilterDialog(Context context, IAppContext Context) : base(context)
        {
            var listOfCategories = Context.Settings.Categories;
            _context = Context;
        }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            SetContentView(Resource.Layout.MainActivityPoiFilter);
            InitializeCategoryFilterButtons();
        }

        private void InitializeCategoryFilterButton(int resourceId)
        {
            var category = PoiCategoryHelper.GetCategory(resourceId);
            var imageButton = FindViewById<ImageButton>(resourceId);

            imageButton.SetOnClickListener(this);
            bool enabled = _context.Settings.Categories.Contains(category);

            imageButton.SetImageResource(PoiCategoryHelper.GetImage(category, enabled));

            _imageButtonCategoryFilter.Add(category, imageButton);
        }

        private void InitializeCategoryFilterButtons()
        {
            InitializeCategoryFilterButton(Resource.Id.imageButtonSelectMountain);
            InitializeCategoryFilterButton(Resource.Id.imageButtonSelectLake);
            InitializeCategoryFilterButton(Resource.Id.imageButtonSelectCity);
            InitializeCategoryFilterButton(Resource.Id.imageButtonSelectOther);
            InitializeCategoryFilterButton(Resource.Id.imageButtonSelectTransmitter);
            InitializeCategoryFilterButton(Resource.Id.imageButtonSelectHistoric);
            InitializeCategoryFilterButton(Resource.Id.imageButtonSelectViewtower);
            InitializeCategoryFilterButton(Resource.Id.imageButtonSelectChurch);


            var buttonSave = FindViewById<Button>(Resource.Id.buttonSavePoiFilter);
            buttonSave.SetOnClickListener(this);
            var buttonSelectAll = FindViewById<Button>(Resource.Id.buttonSelectAll);
            buttonSelectAll.SetOnClickListener(this);
            var buttonSelectNone = FindViewById<Button>(Resource.Id.buttonSelectNone);
            buttonSelectNone.SetOnClickListener(this);
        }

        public async void OnClick(Android.Views.View v)
        {
            try
            {
                switch (v.Id)
                {
                    case Resource.Id.imageButtonSelectMountain:
                    case Resource.Id.imageButtonSelectLake:
                    case Resource.Id.imageButtonSelectCity:
                    case Resource.Id.imageButtonSelectOther:
                    case Resource.Id.imageButtonSelectTransmitter:
                    case Resource.Id.imageButtonSelectHistoric:
                    case Resource.Id.imageButtonSelectViewtower:
                    case Resource.Id.imageButtonSelectChurch:
                        OnCategoryFilterChanged(v.Id);
                        break;
                    case Resource.Id.buttonSavePoiFilter:
                        Hide();
                        Dismiss();
                        break;
                    case Resource.Id.buttonSelectAll:
                        OnCategoryFilterSelectAll();
                        break;
                    case Resource.Id.buttonSelectNone:
                        OnCategoryFilterSelectNone();
                        break;
                }
            }
            catch { }
        }
        private void OnCategoryFilterChanged(int resourceId)
        {
            var poiCategory = PoiCategoryHelper.GetCategory(resourceId);
            var imageButton = _imageButtonCategoryFilter[poiCategory];

            if (_context.Settings.Categories.Contains(poiCategory))
            {
                _context.Settings.Categories.Remove(poiCategory);
                imageButton.SetImageResource(PoiCategoryHelper.GetImage(poiCategory, false));
            }
            else
            {
                _context.Settings.Categories.Add(poiCategory);
                imageButton.SetImageResource(PoiCategoryHelper.GetImage(poiCategory, true));
            }

            _context.Settings.NotifySettingsChanged(ChangedData.PoiFilterSettings);
        }

        private void OnCategoryFilterSelectAll()
        {
            foreach (var category in supportedCategories)
            {
                if (_context.Settings.Categories.Contains(category))
                {
                    continue;
                }
                else
                {
                    var imageButton = _imageButtonCategoryFilter[category];
                    _context.Settings.Categories.Add(category);
                    imageButton.SetImageResource(PoiCategoryHelper.GetImage(category, true));
                }
            }
            _context.Settings.NotifySettingsChanged(ChangedData.PoiFilterSettings);
        }

        private void OnCategoryFilterSelectNone()
        {
            foreach (var category in _context.Settings.Categories)
            {
                if (supportedCategories.Contains(category))
                {
                    var imageButton = _imageButtonCategoryFilter[category];
                    imageButton.SetImageResource(PoiCategoryHelper.GetImage(category, false));
                }
            }

            _context.Settings.Categories.Clear();

            _context.Settings.NotifySettingsChanged(ChangedData.PoiFilterSettings);
        }
    }
}