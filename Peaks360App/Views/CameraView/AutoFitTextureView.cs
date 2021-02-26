using System;
using Android.Content;
using Android.Util;
using Android.Views;
using Peaks360App.AppContext;

namespace Peaks360App.Views.Camera
{
    public class AutoFitTextureView : TextureView
    {
        private int mImageWidth = 0;
        private int mImageHeight = 0;

        public AutoFitTextureView(Context context)
            : this(context, null)
        {
        }

        public AutoFitTextureView(Context context, IAttributeSet attrs)
            : this(context, attrs, 0)
        {
        }

        public AutoFitTextureView(Context context, IAttributeSet attrs, int defStyle)
            : base(context, attrs, defStyle)
        {
        }

        public void SetAspectRatio(int width, int height)
        {
            if (width == 0 || height == 0)
                throw new ArgumentException("Size cannot be negative.");
            mImageWidth = width;
            mImageHeight = height;

            RequestLayout();
        }

        protected override void OnMeasure(int widthMeasureSpec, int heightMeasureSpec)
        {
            base.OnMeasure(widthMeasureSpec, heightMeasureSpec);

            int width = MeasureSpec.GetSize(widthMeasureSpec);
            int height = MeasureSpec.GetSize(heightMeasureSpec);

            if (0 == mImageWidth || 0 == mImageHeight)
            {
                SetMeasuredDimension(width, height);
            }
            else
            {
                AdjustImageToCropDisplay(width, height, mImageWidth, mImageHeight);
                //AdjustImageToFitInDisplay(width, height, mImageWidth, mImageHeight);
            }
        }

        private void AdjustImageToFitInDisplay(int displayWidth, int displayHeight, int imageWidth, int imageHeight)
        {
            if (displayWidth < (float)displayHeight * imageWidth / (float)imageHeight)
            {
                SetMeasuredDimension(displayWidth, displayWidth * imageHeight / imageWidth);
            }
            else
            {
                SetMeasuredDimension(displayHeight * imageWidth / imageHeight, displayHeight);
            }
        }

        private void AdjustImageToCropDisplay(int displayWidth, int displayHeight, int imageWidth, int imageHeight)
        {
            if (AppContextLiveData.Instance.IsPortrait)
            {
                var displayRatio = displayWidth / (float)displayHeight; //0.5
                var imageRatio = imageWidth / (float)imageHeight; //0.75

                var displayWidthRequired = displayHeight * imageRatio; //1500
                var diff = displayWidth - displayWidthRequired; //-500

                TranslationX = diff / 2f;
                ScaleX = imageRatio / displayRatio;
                ScaleY = 1;
            }
            else
            {
                var displayRatio = displayWidth / (float)displayHeight; //2
                var imageRatio = imageWidth / (float)imageHeight; //1.33

                var displayHeightRequired = displayWidth / imageRatio; //1500
                var diff = displayHeight - displayHeightRequired; //-500

                //It's strange that we dont need to do any adjustments in landspace mode.
                //TranslationY = diff / 2f;
                //ScaleX = 1;
                //ScaleY = displayRatio / imageRatio;
            }
        }
    }
}

