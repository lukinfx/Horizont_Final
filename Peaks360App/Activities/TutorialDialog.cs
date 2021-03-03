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
using System;
using Android.Content.Res;
using Java.Interop;
using Android.Views;
using Android.Views.Animations;

namespace Peaks360App.Activities
{
    public class TutorialPage
    {
        public int imageResourceId;
        public int textResourceId;
    }

    public class TutorialDialog : Dialog, IOnClickListener, ViewSwitcher.IViewFactory
    {
        private ImageSwitcher _imageSwitcher;
        private TextView _hintTextView;
        private CheckBox _checkBoxDontShowAgain;
        private Animation _imageAnimation;
        private int _index = 0;
        private TutorialPart _tutorialPart;
        private TutorialPage[] _tutorialPages;

        public static void ShowTutorial(Context context, TutorialPart tp, TutorialPage[] tutorialPages)
        {
            if (AppContextLiveData.Instance.Settings.IsTutorialNeeded(tp))
            {
                var dialog = new TutorialDialog(context, tp, tutorialPages);
                dialog.Show();
            }
        }

        private TutorialDialog(Context context, TutorialPart tp, TutorialPage[] tutorialPages) : base(context)
        {
            _tutorialPages = tutorialPages;
        }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            
            SetCancelable(false);
            SetContentView(Resource.Layout.Tutorial);

            _imageAnimation = AnimationUtils.LoadAnimation(Context, Android.Resource.Animation.FadeIn); // load an animation
            _imageAnimation.Duration = 1000;

            _imageSwitcher = (ImageSwitcher)FindViewById(Resource.Id.tutorialImageSwitcher);
            _imageSwitcher.SetOnClickListener(this);
            _imageSwitcher.SetFactory(this);

            _checkBoxDontShowAgain = (CheckBox)FindViewById(Resource.Id.checkBoxDontShowAgain);
            _checkBoxDontShowAgain.SetOnClickListener(this);

            _hintTextView = (TextView)FindViewById(Resource.Id.tutorialHintText);

            ShowNext();
        }

        public async void OnClick(Android.Views.View v)
        {
            switch (v.Id)
            {
                case Resource.Id.tutorialImageSwitcher:
                    ShowNext();
                    break;
                case Resource.Id.checkBoxDontShowAgain:
                    AppContextLiveData.Instance.Settings.SetTutorialNeeded(_tutorialPart, false);
                    AppContextLiveData.Instance.Settings.SaveData();
                    break;
            }
        }

        private void ShowNext()
        {
            try
            {
                if (_index == _tutorialPages.Length)
                {
                    _hintTextView.Visibility = ViewStates.Gone;
                    _checkBoxDontShowAgain.Visibility = ViewStates.Visible;
                    return;
                }

                if (_index > _tutorialPages.Length)
                {
                    Hide();
                    Dismiss();
                    return;
                }

                _imageSwitcher.SetImageResource(_tutorialPages[_index].imageResourceId);
                _imageSwitcher.Animation = _imageAnimation;
                _imageSwitcher.Animation.StartNow();

                _hintTextView.SetText(_tutorialPages[_index].textResourceId);

                _checkBoxDontShowAgain.Visibility = ViewStates.Gone;
            }
            finally
            {
                _index++;
            }
        }

        public View MakeView()
        {
            ImageView image = new ImageView(this.Context);
            image.SetScaleType(ImageView.ScaleType.FitEnd);
            image.SetAdjustViewBounds(true);
            return image;
        }
    }
}