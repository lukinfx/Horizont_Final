using System.Collections.Generic;
using System.Linq;
using Android.App;
using Android.Views;
using Android.Widget;
using Android.Transitions;
using AndroidX.CardView.Widget;

namespace Peaks360App.Activities
{
    public class CardBaseActivity : Activity, View.IOnClickListener
    {
        private class CardItem
        {
            public CardItem(int CardId, int IconId, int LayoutId, int ContentId)
            {
                this.CardId = CardId;
                this.IconId = IconId;
                this.LayoutId = LayoutId;
                this.ContentId = ContentId;
            }
            public int CardId;
            public int IconId;
            public int LayoutId;
            public int ContentId;
        }

        private List<CardItem> _cardItems = new List<CardItem>();

        protected void AddCard(int cardId, int iconId, int layoutId, int contentId)
        {
            _cardItems.Add(new CardItem(cardId, iconId, layoutId, contentId));
            FindViewById<LinearLayout>(layoutId).SetOnClickListener(this);
        }

        private void OnSelectViewCard(int layoutResId)
        {
            foreach (var cardItem in _cardItems)
            {
                var icon = FindViewById<ImageView>(cardItem.IconId);
                var cardView = FindViewById<CardView>(cardItem.CardId);
                var hiddenView = FindViewById<LinearLayout>(cardItem.ContentId);

                if (layoutResId != cardItem.LayoutId && hiddenView.Visibility == ViewStates.Visible)
                {
                    hiddenView.Visibility = ViewStates.Gone;
                    icon.SetImageResource(Resource.Drawable.baseline_expand_more_black_24dp);
                }
            }

            {
                var selectedCardItem = _cardItems.Single(x => x.LayoutId == layoutResId);
                var icon = FindViewById<ImageView>(selectedCardItem.IconId);
                var cardView = FindViewById<CardView>(selectedCardItem.CardId);
                var hiddenView = FindViewById<LinearLayout>(selectedCardItem.ContentId);

                if (hiddenView.Visibility == ViewStates.Visible)
                {
                    TransitionManager.BeginDelayedTransition(cardView, new AutoTransition());
                    hiddenView.Visibility = ViewStates.Gone;
                    icon.SetImageResource(Resource.Drawable.baseline_expand_more_black_24dp);
                }
                else
                {
                    TransitionManager.BeginDelayedTransition(cardView, new AutoTransition());
                    hiddenView.Visibility = ViewStates.Visible;
                    icon.SetImageResource(Resource.Drawable.baseline_expand_less_black_24dp);
                }
            }
        }

        public void OnClick(View v)
        {
            if (_cardItems.Exists(x => x.LayoutId == v.Id))
            {
                OnSelectViewCard(v.Id);
            }
        }
    }
}