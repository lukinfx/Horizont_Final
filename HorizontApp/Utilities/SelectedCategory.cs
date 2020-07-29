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

namespace HorizontApp.Utilities
{
    public class SelectedCategory
    {
        public bool VisiblePeaks = true;
        public bool VisibleMountains = true;
        public bool VisibleLakes = true;
        public bool VisibleCastles = true;
        public bool VisiblePalaces = true;
        public bool VisibleRuins = true;
        public bool VisibleViewTowers = true;
        public bool VisibleTransmitters = true;
        public bool VisibleChurches = true;
        public bool VisibleTest = true;

        private SelectedCategory()
        {
            
        }

        private static SelectedCategory _selectedCategory;
    
        public static SelectedCategory Instance()
        {
            if (_selectedCategory == null)
            {
                _selectedCategory = new SelectedCategory();
                return _selectedCategory;
            } 
            else
            {
                return _selectedCategory;
            }
        }
    }

    
}