using System;
using System.Linq;
using System.Collections.Generic;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Widget;
using static Android.Views.View;
using Peaks360App.Utilities;
using Peaks360Lib.Domain.Enums;

namespace Peaks360App.Activities
{
    [Activity(Label = "CountryDropDownDialog")]
    public class CountryDropDownDialog : Dialog, IOnClickListener, SearchView.IOnQueryTextListener
    {
        private Action<Result, PoiCountry?> _onFinished;
        private List<PoiCountry> _countries;
        private DownloadCountryAdapter _countryAdapter;
        private ListView _downloadCountryListView;
        private SearchView _editTextSearch;

        public CountryDropDownDialog(Context context, List<PoiCountry> countries) : base(context)
        {
            _countries = countries;
        }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.CountryDropDown);

            _editTextSearch = FindViewById<SearchView>(Resource.Id.editTextSearch);
            _editTextSearch.SetOnQueryTextListener(this);
            _downloadCountryListView = FindViewById<ListView>(Resource.Id.DownloadCountryListView);
            _countryAdapter = new DownloadCountryAdapter(LayoutInflater, true);
            _countryAdapter.SetItems(_countries);
            _downloadCountryListView = FindViewById<ListView>(Resource.Id.DownloadCountryListView);
            _downloadCountryListView.Adapter = _countryAdapter;
            _downloadCountryListView.ItemClick += OnCountryListItemClicked;

            var buttonClose = FindViewById<Button>(Resource.Id.buttonClose);
            buttonClose.SetOnClickListener(this);
        }

        private void OnCountryListItemClicked(object sender, AdapterView.ItemClickEventArgs e)
        {
            PoiCountry country = _countryAdapter[e.Position];
            _onFinished(Result.Ok, country);
            Hide();
            Dismiss();
        }

        public async void OnClick(Android.Views.View v)
        {
            try
            {
                switch (v.Id)
                {
                    case Resource.Id.buttonClose:
                        _onFinished?.Invoke(Result.Canceled, null);
                        Hide();
                        Dismiss();
                        break;
                }
            }
            catch (Exception ex)
            {
                PopupHelper.ErrorDialog(Context, ex.Message);
            }
        }

        public void Show(Action<Result, PoiCountry?> onFinished)
        {
            _onFinished = onFinished;
            Show();
        }

        public bool OnQueryTextChange(string query)
        {
            FilterCountries(query);
            return true;
        }

        public bool OnQueryTextSubmit(string query)
        {
            _editTextSearch.ClearFocus();
            return true;
        }

        private void FilterCountries(string filterText)
        {
            if (_editTextSearch.Query.Length == 0)
            {
                _countryAdapter.SetItems(_countries);
                return;
            }

            var filteredCountries = _countries.Where(x => PoiCountryHelper.GetCountryName(x).StartsWith(filterText, StringComparison.OrdinalIgnoreCase));
            _countryAdapter.SetItems(filteredCountries);
        }
    }
}