﻿using System;
using Android.Views;
using Android.Views.Animations;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Widget;
using Peaks360App.AppContext;
using Peaks360App.Utilities;
using static Android.Views.View;

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
        private Action _onTutorialFinished;
        
        public static bool IsInProgress { get; private set; }

        public static void ShowTutorial(Context context, TutorialPart tp, TutorialPage[] tutorialPages, Action onTutorialFinished = null)
        {
            if (AppContextLiveData.Instance.Settings.IsTutorialNeeded(tp))
            {
                IsInProgress = true;
                var dialog = new TutorialDialog(context, tp, tutorialPages, onTutorialFinished);
                dialog.Show();
            }
            else
            {
                onTutorialFinished?.Invoke();
            }
        }

        private TutorialDialog(Context context, TutorialPart tp, TutorialPage[] tutorialPages, Action onTutorialFinished) : base(context)
        {
            _tutorialPart = tp;
            _tutorialPages = tutorialPages;
            _onTutorialFinished = onTutorialFinished;
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

            DisplayPage(_index);
        }

        public void DisplayPage(int index)
        {
            _imageSwitcher.SetImageResource(_tutorialPages[_index].imageResourceId);
            _imageSwitcher.Animation = _imageAnimation;
            _imageSwitcher.Animation.StartNow();

            _hintTextView.SetText(_tutorialPages[_index].textResourceId);

            _checkBoxDontShowAgain.Visibility = (_index == _tutorialPages.Length - 1) 
                ? ViewStates.Visible : ViewStates.Gone;
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
            _index++; 
            if (_index >= _tutorialPages.Length)
            {
                IsInProgress = false;
                Hide();
                Dismiss();
                _onTutorialFinished?.Invoke();
                return;
            }

            DisplayPage(_index);
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