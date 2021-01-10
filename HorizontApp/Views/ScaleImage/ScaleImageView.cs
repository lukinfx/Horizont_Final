﻿using System;
using Android.Content;
using Android.Graphics;
using Android.Util;
using Android.Views;
using Android.Widget;

namespace HorizontApp.Views.ScaleImage
{
    public class ScaleImageViewGestureDetector : GestureDetector.SimpleOnGestureListener
    {
        private readonly ScaleImageView m_ScaleImageView;
        public ScaleImageViewGestureDetector(ScaleImageView imageView)
        {
            m_ScaleImageView = imageView;
        }
        public override bool OnDown(MotionEvent e)
        {
            return true;
        }
        public override bool OnDoubleTap(MotionEvent e)
        {
            m_ScaleImageView.MaxZoomTo((int)e.GetX(), (int)e.GetY());
            m_ScaleImageView.Cutting();
            return true;
        }
    }

    public class ScaleImageView : ImageView
    {
        private Context m_Context;
        private float m_MaxScale = 10.0f;
        private float m_MinScale; 
        private float m_StdScale;
        private float m_Scale;

        private Matrix m_Matrix;
        private float[] m_MatrixValues = new float[9];
        private int m_Width;
        private int m_Height;
        private int m_IntrinsicWidth;
        private int m_IntrinsicHeight;
        private int m_InitialPaddingWidth;
        private int m_InitialPaddingHeight;
        
        

        public float MinScale { get { return m_MinScale; } }

        //private GestureDetector m_GestureDetector;
        public ScaleImageView(Context context, IAttributeSet attrs) :
        base(context, attrs)
        {
            m_Context = context;
            Initialize();
        }
        public ScaleImageView(Context context, IAttributeSet attrs, int defStyle) 
            : base(context, attrs, defStyle)
        {
            m_Context = context;
            Initialize();
        }

        public override void SetImageBitmap(Bitmap bm)
        {
            base.SetImageBitmap(bm);
            this.Initialize();
        }
        public override void SetImageResource(int resId)
        {
            base.SetImageResource(resId);
            this.Initialize();
        }
        private void Initialize()
        {
            this.SetScaleType(ScaleType.Matrix);
            m_Matrix = new Matrix();
            if (Drawable != null)
            {
                m_IntrinsicWidth = Drawable.IntrinsicWidth;
                m_IntrinsicHeight = Drawable.IntrinsicHeight;
            }
        }

        protected override bool SetFrame(int l, int t, int r, int b)
        {

            m_Width = r - l;
            m_Height = b - t;
            m_Matrix.Reset();

            //Calculate scale
            m_MinScale = CalculateMinScale(l, t, r, b);
            m_StdScale = (r-l) / (float)m_IntrinsicWidth;
            m_Scale = m_StdScale;

            //Calculate initial padding
            int scaledImageHeight = (int)(m_Scale * m_IntrinsicHeight);
            m_InitialPaddingHeight = (scaledImageHeight - m_Height) / 2;
            m_InitialPaddingWidth = 0;

            //Set image transformation matrix
            m_Matrix.PostScale(m_Scale, m_Scale);
            m_Matrix.PostTranslate(-m_InitialPaddingWidth, -m_InitialPaddingHeight);
            ImageMatrix = m_Matrix;

            return base.SetFrame(l, t, r, b);
        }

        private float CalculateMinScale(int l, int t, int r, int b)
        {
            var Height = b - t;
            var Width = r - l;
            var minScale = (float)Width / (float)m_IntrinsicWidth;

            if (minScale * m_IntrinsicHeight > Height)
            {
                minScale = (float)Height / (float)m_IntrinsicHeight;
            }

            return minScale;
        }

        private float GetValue(Matrix matrix, int whichValue)
        {
            matrix.GetValues(m_MatrixValues);
            return m_MatrixValues[whichValue];
        }
        
        /// <summary>
        /// Scale in display units (1 = full screen width)
        /// </summary>
        public float DisplayScale
        {
            get { return Scale * m_IntrinsicWidth / Width; }
        }
        /// <summary>
        /// X Translation in display pixels
        /// </summary>
        public float DisplayTranslateX
        {
            get
            {
                return ((DisplayScale * Width) - Width )/2 + TranslateX + m_InitialPaddingWidth * DisplayScale;
            }
        }
        /// <summary>
        /// Y Translation in display pixels
        /// </summary>
        public float DisplayTranslateY
        {
            get
            {
                return ((DisplayScale * Height) - Height)/2 + TranslateY + m_InitialPaddingHeight * DisplayScale;
            }
        }

        public float Scale
        {
            get { return this.GetValue(m_Matrix, Matrix.MscaleX); }
        }
        public float TranslateX
        {
            get { return this.GetValue(m_Matrix, Matrix.MtransX); }
        }
        public float TranslateY
        {
            get { return this.GetValue(m_Matrix, Matrix.MtransY); }
        }

        public void MaxZoomTo(int x, int y)
        {
            if (this.m_MinScale != this.Scale && (Scale - m_MinScale) > 0.1f)
            {
                var scale = m_MinScale / Scale;
                ZoomTo(scale, x, y);
            }
            else
            {
                var scale = m_MaxScale / Scale;
                ZoomTo(scale, x, y);
            }
        }
        public void ZoomTo(float zoomFactor, int x, int y)
        {
            if (Scale * zoomFactor < m_MinScale)
            {
                zoomFactor = m_MinScale / Scale;
            }
            else
            {
                if (zoomFactor >= 1 && Scale * zoomFactor > m_MaxScale)
                {
                    zoomFactor = m_MaxScale / Scale;
                }
            }
            m_Matrix.PostScale(zoomFactor, zoomFactor);
            //move to center
            m_Matrix.PostTranslate(-(m_Width * zoomFactor - m_Width) / 2, -(m_Height * zoomFactor - m_Height) / 2);
            //move x and y distance
            m_Matrix.PostTranslate(-(x - (m_Width / 2)) * zoomFactor, 0);
            m_Matrix.PostTranslate(0, -(y - (m_Height / 2)) * zoomFactor);
            ImageMatrix = m_Matrix;
        }
        internal void MoveTo(int distanceX, int distanceY)
        {
            m_Matrix.PostTranslate(distanceX, distanceY);
        }

        public void Cutting()
        {
            var width = (int)(m_IntrinsicWidth * Scale);
            var height = (int)(m_IntrinsicHeight * Scale);
            if (TranslateX < -(width - m_Width))
            {
                m_Matrix.PostTranslate(-(TranslateX + width - m_Width), 0);
            }
            if (TranslateX > 0)
            {
                m_Matrix.PostTranslate(-TranslateX, 0);
            }
            if (TranslateY < -(height - m_Height))
            {
                m_Matrix.PostTranslate(0, -(TranslateY + height - m_Height));
            }
            if (TranslateY > 0)
            {
                m_Matrix.PostTranslate(0, -TranslateY);
            }
            if (width < m_Width)
            {
                m_Matrix.PostTranslate((m_Width - width) / 2, 0);
            }
            if (height < m_Height)
            {
                m_Matrix.PostTranslate(0, (m_Height - height) / 2);
            }
            ImageMatrix = m_Matrix;
        }
        public float Distance(float x0, float x1, float y0, float y1)
        {
            var x = x0 - x1;
            var y = y0 - y1;
            return FloatMath.Sqrt(x * x + y * y);
        }
        public float DispDistance()
        {
            return FloatMath.Sqrt(m_Width * m_Width + m_Height * m_Height);
        }
        
        public bool OnTouch(View v, MotionEvent e)
        {
            return OnTouchEvent(e);
        }

    }
}