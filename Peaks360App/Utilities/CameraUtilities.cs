using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Android.Graphics;
using Peaks360App.AppContext;
using Android.Hardware.Camera2;
using Android.Util;
using Android.Hardware.Camera2.Params;

namespace Peaks360App.Utilities
{
    public class CameraUtilities
    {
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

        public static Size[] GetCameraResolutions(string cameraId)
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
            return imageSizes;
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
    }
}