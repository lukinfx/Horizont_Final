using System;
using Android.Content;
using Android.Graphics;
using Android.Util;
using Android.Views;
using Android.Widget;
using Peaks360Lib.Domain.ViewModel;

namespace Peaks360App.Views.ScaleImage
{
    public class ScaleImageView : ImageView
    {
        private const int HOLDER_SIZE = 30;
        public const int MIN_IMAGE_SIZE = 1024;
        private const int MIN_CROP_DISTANCE = 100;

        private Context m_Context;

        private float m_MaxScale;
        private float m_MinScale; 
        private float m_StdScale;
        private float m_Scale;

        public Matrix m_Matrix;
        private float[] m_MatrixValues = new float[9];
        private int m_Width;
        private int m_Height;
        private int m_IntrinsicWidth;
        private int m_IntrinsicHeight;
        private int m_InitialPaddingWidth;
        private int m_InitialPaddingHeight;
        private Rect croppingRectangle;
        private Paint croppingPaint;
        private Paint croppingPaintWarning;
        private Paint croppingHandlePaint;



        public Rect CroppingRectangle {
            get
            {
                return croppingRectangle;
            }
            set
            {
                croppingRectangle = value;
                if (croppingRectangle != null)
                {
                    ValidateCroppingRectangle();
                }

                Invalidate();
            }
        }

        public RectF CroppingRectangleOnDisplay
        {
            get
            {
                if (CroppingRectangle == null)
                    return null;

                var dst = new RectF();
                var src = new RectF(CroppingRectangle);
                m_Matrix.MapRect(dst, src); 
                return dst;
            }
        }

        public float MinScale { get { return m_MinScale; } }
        public float MaxScale { get { return m_MaxScale; } }
        public float StdScale { get { return m_StdScale; } }
        public float MiddleScale { get { return (m_MaxScale + m_MinScale) * 0.2f; } }

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

            croppingPaint = new Paint();
            croppingPaint.SetARGB(150, 0, 0, 0);
            croppingPaint.SetStyle(Paint.Style.Fill);
            croppingPaint.StrokeWidth = 0;

            croppingPaintWarning = new Paint();
            croppingPaintWarning.SetARGB(50, 255, 0, 0);
            croppingPaintWarning.SetStyle(Paint.Style.Fill);
            croppingPaintWarning.StrokeWidth = 0;

            croppingHandlePaint = new Paint();
            croppingHandlePaint.SetARGB(255, 200, 200, 200);
            croppingHandlePaint.SetStyle(Paint.Style.FillAndStroke);
            croppingHandlePaint.StrokeWidth = 4;
            
        }

        protected override bool SetFrame(int l, int t, int r, int b)
        {
            m_Width = r - l;
            m_Height = b - t;

            if (Drawable == null)
            {
                return base.SetFrame(l, t, r, b);
            }

            bool resetScale = false;
            if (m_IntrinsicWidth != Drawable.IntrinsicWidth || m_IntrinsicHeight != Drawable.IntrinsicHeight)
            {
                m_IntrinsicWidth = Drawable.IntrinsicWidth;
                m_IntrinsicHeight = Drawable.IntrinsicHeight;
                resetScale = true;
            }

            if (Width != m_Width || Height != m_Height)
            {
                resetScale = true;
            }

            if (resetScale)
            {
                InitializeTransformationMatrix();
            }

            return base.SetFrame(l, t, r, b);
        }

        public void InitializeTransformationMatrix()
        {
            m_Matrix.Reset();

            //Calculate scale
            m_MinScale = CalculateMinScale(m_Width, m_Height);
            m_MaxScale = 30 * m_MinScale;
            m_StdScale = (m_Width) / (float) m_IntrinsicWidth;
            m_Scale = m_StdScale;

            //Calculate initial padding
            int scaledImageHeight = (int) (m_Scale * m_IntrinsicHeight);
            m_InitialPaddingHeight = (scaledImageHeight - m_Height) / 2;
            m_InitialPaddingWidth = 0;

            //Set image transformation matrix
            m_Matrix.PostScale(m_Scale, m_Scale);
            m_Matrix.PostTranslate(-m_InitialPaddingWidth, -m_InitialPaddingHeight);
            ImageMatrix = m_Matrix;
        }

        private float CalculateMinScale(int width, int height)
        {
            var minScale = (float)width / (float)m_IntrinsicWidth;

            if (minScale * m_IntrinsicHeight > height)
            {
                minScale = (float)height / (float)m_IntrinsicHeight;
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

        private (float x, float y) GetCroppingHandle(CroppingHandle handle)
        {
            var cr = CroppingRectangleOnDisplay;
            
            switch (handle)
            {
                case CroppingHandle.Left:
                    return (cr.Left, (cr.Top + cr.Bottom) / 2);
                case CroppingHandle.Right:
                    return (cr.Right, (cr.Top + cr.Bottom) / 2);
                case CroppingHandle.Top:
                    return ((cr.Right + cr.Left) / 2, cr.Top);
                case CroppingHandle.Bottom:
                    return ((cr.Right + cr.Left) / 2, cr.Bottom);
                default:
                    throw new SystemException("Unsupported handle type");
            }
        }

        public bool IsCropAllowed(CroppingHandle handle, float distX, float distY)
        {
            var oldCRDisp = CroppingRectangleOnDisplay;

            //Check if it is possible?
            switch (handle)
            {
                case CroppingHandle.Left:
                    return oldCRDisp.Right - (oldCRDisp.Left + distX) > MIN_CROP_DISTANCE;
                case CroppingHandle.Right:
                    return (oldCRDisp.Right + distX) - oldCRDisp.Left > MIN_CROP_DISTANCE;
                case CroppingHandle.Top:
                    return oldCRDisp.Bottom - (oldCRDisp.Top + distY) > MIN_CROP_DISTANCE;
                case CroppingHandle.Bottom:
                    return (oldCRDisp.Bottom + distY) - oldCRDisp.Top > MIN_CROP_DISTANCE;
            }

            return false;
        }

        public bool IsCroppedImageTooSmall()
        {
            return CroppingRectangle.Width() < MIN_IMAGE_SIZE || CroppingRectangle.Height() < MIN_IMAGE_SIZE;
        }

        private void ValidateCroppingRectangle()
        {
            if (croppingRectangle.Left < 0) 
                croppingRectangle.Left = 0;

            if (croppingRectangle.Right > m_IntrinsicWidth) 
                croppingRectangle.Right = m_IntrinsicWidth;

            if (croppingRectangle.Top < 0) 
                croppingRectangle.Top = 0;

            if (croppingRectangle.Bottom > m_IntrinsicHeight) 
                croppingRectangle.Bottom = m_IntrinsicHeight;
        }

        protected override void OnDraw(Canvas canvas)
        {
            base.OnDraw(canvas);

            if (CroppingRectangleOnDisplay != null)
            {
                Rect r = new Rect((int)CroppingRectangleOnDisplay.Left, (int)CroppingRectangleOnDisplay.Top, (int)CroppingRectangleOnDisplay.Right, (int)CroppingRectangleOnDisplay.Bottom);

                if (IsCroppedImageTooSmall())
                {
                    canvas.DrawRect(r, croppingPaintWarning);
                }

                //canvas.DrawRect(CroppingRectangleOnDisplay, croppingPaint);
                canvas.DrawRect(0, 0, r.Left, Height, croppingPaint);
                canvas.DrawRect(r.Right, 0, Width, Height, croppingPaint);
                canvas.DrawRect(r.Left, 0, r.Right, r.Top, croppingPaint);
                canvas.DrawRect(r.Left, r.Bottom, r.Right, Height, croppingPaint);

                foreach (CroppingHandle handle in (CroppingHandle[])Enum.GetValues(typeof(CroppingHandle)))
                {
                    var (x, y) = GetCroppingHandle(handle);
                    canvas.DrawCircle(x, y, HOLDER_SIZE, croppingHandlePaint);
                }
            }
        }

        public CroppingHandle? GetCroppingHandle(float x, float y)
        {
            (x, y) = ToLocationOnScreen(x, y);

            foreach (CroppingHandle handle in (CroppingHandle[])Enum.GetValues(typeof(CroppingHandle)))
            {
                var (handleX, handleY) = GetCroppingHandle(handle);
                var distX = handleX - x;
                var distY = handleY - y;
                if (FloatMath.Sqrt(distX * distX + distY * distY) < HOLDER_SIZE*2)
                {
                    return handle;
                }
            }

            return null;
        }

        private (float x, float y) ToLocationOnScreen(float x, float y)
        {
            var windowLocationOnScreen = new int[2];
            GetLocationInWindow(windowLocationOnScreen);
            x = x - windowLocationOnScreen[0];
            y = y - windowLocationOnScreen[1];
            return (x, y);
        }
    }
}