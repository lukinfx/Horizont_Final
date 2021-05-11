using System;
using System.Collections.Generic;
using System.Linq;
using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Hardware.Camera2;
using Android.Util;
using Android.Hardware.Camera2.Params;
using Java.Util;
using Peaks360App.Views.Camera;

namespace Peaks360App.Utilities
{
    public class CameraUtilities
    {
        private const int MAX_CAMERA_RESOLUTION = 5000 * 4000;
        public static List<string> GetCameras()
        {
            var ctx = Application.Context;
            var manager = (CameraManager)ctx.GetSystemService(Context.CameraService);

            var result = new List<string>();

            try
            {
                for (var i = 0; i < manager.GetCameraIdList().Length; i++)
                {
                    var cameraId = manager.GetCameraIdList()[i];
                    CameraCharacteristics characteristics = manager.GetCameraCharacteristics(cameraId);

                    // We don't use a front facing camera in this sample.
                    var facing = (Java.Lang.Integer)characteristics.Get(CameraCharacteristics.LensFacing);
                    if (facing != null && facing == (Java.Lang.Integer.ValueOf((int)LensFacing.Front)))
                    {
                        continue;
                    }

                    result.Add(cameraId);
                }
            }
            catch (System.Exception ex)
            {
            }

            return result;
        }

        public static IEnumerable<Size> GetCameraResolutions(string cameraId)
        {
            var ctx = Application.Context;

            var manager = (CameraManager)ctx.GetSystemService(Context.CameraService);
            CameraCharacteristics characteristics = manager.GetCameraCharacteristics(cameraId);

            var map = (StreamConfigurationMap)characteristics.Get(CameraCharacteristics.ScalerStreamConfigurationMap);
            if (map == null)
            {
                return new List<Size>().ToArray();
            }

            var imageSizes = map.GetOutputSizes((int)ImageFormatType.Jpeg);

            return imageSizes
                .Where(x => x.Height * x.Width <= MAX_CAMERA_RESOLUTION)
                .OrderByDescending(x => x.Height * x.Width);
        }

        public static (float, float) FetchCameraViewAngle(string cameraId)
        {
            try
            {
                var camera = Android.Hardware.Camera.Open(Int32.Parse(cameraId));
                return (camera.GetParameters().HorizontalViewAngle, camera.GetParameters().VerticalViewAngle);
                //camera.GetParameters().PictureSize.Width, camera.GetParameters().PictureSize.Height);
            }
            catch (System.Exception ex)
            {
                //Default values
                return (60, 40); //, 1920, 1080);
            }
        }

        public static (int, int) FetchCameraResolution(string cameraId)
        {
            try
            {
                var camera = Android.Hardware.Camera.Open(Int32.Parse(cameraId));
                return (camera.GetParameters().PictureSize.Width, camera.GetParameters().PictureSize.Height);
            }
            catch (System.Exception ex)
            {
                //Default values
                return (1920, 1080);
            }
        }

        public static string GetDefaultCamera()
        {
            return GetCameras().First();
        }

        public static Size GetDefaultCameraResolution(string cameraId)
        {
            var listOfSizes = CameraUtilities.GetCameraResolutions(cameraId);
            return listOfSizes.First();
        }

        public static Size ChooseOptimalSize(Size[] choices,
            int textureViewWidth, int textureViewHeight, 
            int maxWidth, int maxHeight, 
            Size aspectRatio)
        {
            
            // Collect the supported resolutions that are at least as big as the preview Surface
            var bigEnough = new List<Size>();
            // Collect the supported resolutions that are smaller than the preview Surface
            var notBigEnough = new List<Size>();
            int w = aspectRatio.Width;
            int h = aspectRatio.Height;

            foreach (var option in choices)
            {
                if ((option.Width <= maxWidth) && (option.Height <= maxHeight)
                                               && option.Height == option.Width * h / w)
                {
                    if (option.Width >= textureViewWidth &&
                        option.Height >= textureViewHeight)
                    {
                        bigEnough.Add(option);
                    }
                    else
                    {
                        notBigEnough.Add(option);
                    }
                }
            }

            // Pick the smallest of those big enough. If there is no one big enough, pick the
            // largest of those not big enough.
            if (bigEnough.Count > 0)
            {
                return (Size)Collections.Min(bigEnough, new CompareSizesByArea());
            }
            else if (notBigEnough.Count > 0)
            {
                return (Size)Collections.Max(notBigEnough, new CompareSizesByArea());
            }
            else
            {
                //Log.Error(TAG, "Couldn't find any suitable preview size");
                return choices.First();
            }
        }

        public static bool IsResolutionSupported(string cameraId, Size resolution)
        {
            var supportedResolutions = GetCameraResolutions(cameraId);
            return supportedResolutions.Any(x => x.Width == resolution.Width && x.Height == resolution.Height);
        }
    }
}