using System;
using Android.Graphics;

namespace Peaks360App.Utilities
{
    public class ColorMatrixBuilder
    {
        private float _contrast = 1f; //0..10 default value is 1
        private float _brightness = 0f; //-255..255 default value is 0
        private float _alpha = 1f; //0..1 default value is 1
        private float _hue = 0; //-180..+180 ???, default value is 0
        private float _saturation = 1;

        public ColorMatrixBuilder()
        {
        }

        /// <summary>
        /// Change contrast
        /// </summary>
        /// <param name="contrast">0..10 default value is 1</param>
        /// <returns></returns>
        public ColorMatrixBuilder Contrast(float contrast)
        {
            _contrast = contrast;
            return this;
        }

        /// <summary>
        /// Change brightness
        /// </summary>
        /// <param name="brightness">-255..255 default value is 0</param>
        /// <returns></returns>
        public ColorMatrixBuilder Brightness(float brightness)
        {
            _brightness = brightness;
            return this;
        }

        /// <summary>
        /// Change alpha
        /// </summary>
        /// <param name="alpha">0..1 default value is 1</param>
        /// <returns></returns>
        public ColorMatrixBuilder Alpha(float alpha)
        {
            _alpha = alpha;
            return this;
        }

        /// <summary>
        /// Change hue
        /// -180:violet
        /// -100:pink
        /// -85:red
        /// -30:gold
        /// -20:yellow
        /// 0:noChange
        /// +50:green
        /// +100:blue
        /// +180:violet
        /// </summary>
        /// <param name="hue">-180..+180 ???, default value is 0</param>
        /// <returns></returns>
        public ColorMatrixBuilder Hue(float hue)
        {
            _hue = hue;
            return this;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="saturation">Range 0..1. Default value 1.</param>
        /// <returns></returns>
        public ColorMatrixBuilder Saturation(float saturation)
        {
            _saturation = saturation;
            return this;
        }

        public ColorMatrix Create()
        {
            ColorMatrix cm = new ColorMatrix(new float[]
            {
                _contrast, 0, 0, 0, _brightness,
                0, _contrast, 0, 0, _brightness,
                0, 0, _contrast, 0, _brightness,
                0, 0, 0, _alpha, 0
            });
            AdjustHue(cm, _hue);
            //if (_saturation < 0.99f)
            {
                var cm2 = new ColorMatrix();
                cm2.SetSaturation(_saturation);
                cm.PostConcat(cm2);
                //cm.SetSaturation(_saturation);
            }

            return cm;
        }

        protected float CleanValue(float p_val, float p_limit)
        {
            return Math.Min(p_limit, Math.Max(-p_limit, p_val));
        }

        protected void AdjustHue(ColorMatrix cm, float value)
        {
            value = CleanValue(value, 180f) / 180f * (float)Math.PI;
            if (value == 0)
            {
                return;
            }
            float cosVal = (float)Math.Cos(value);
            float sinVal = (float)Math.Sin(value);
            float lumR = 0.213f;
            float lumG = 0.715f;
            float lumB = 0.072f;
            float[] mat = new float[]
            {
                lumR + cosVal * (1 - lumR) + sinVal * (-lumR), lumG + cosVal * (-lumG) + sinVal * (-lumG), lumB + cosVal * (-lumB) + sinVal * (1 - lumB), 0, 0,
                lumR + cosVal * (-lumR) + sinVal * (0.143f), lumG + cosVal * (1 - lumG) + sinVal * (0.140f), lumB + cosVal * (-lumB) + sinVal * (-0.283f), 0, 0,
                lumR + cosVal * (-lumR) + sinVal * (-(1 - lumR)), lumG + cosVal * (-lumG) + sinVal * (lumG), lumB + cosVal * (1 - lumB) + sinVal * (lumB), 0, 0,
                0f, 0f, 0f, 1f, 0f,
                0f, 0f, 0f, 0f, 1f };
            cm.PostConcat(new ColorMatrix(mat));
        }
    }
}