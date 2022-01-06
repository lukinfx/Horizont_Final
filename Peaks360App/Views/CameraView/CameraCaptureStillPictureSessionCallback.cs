using Android.Hardware.Camera2;
using Android.Util;
using Peaks360App.Utilities;

namespace Peaks360App.Views.Camera
{
    public class CameraCaptureStillPictureSessionCallback : CameraCaptureSession.CaptureCallback
    {
        //private static readonly string TAG = "CameraCaptureStillPictureSessionCallback";

        private readonly CameraFragment owner;

        public CameraCaptureStillPictureSessionCallback(CameraFragment owner)
        {
            if (owner == null)
                throw new System.ArgumentNullException("owner");
            this.owner = owner;
        }

        public override void OnCaptureCompleted(CameraCaptureSession session, CaptureRequest request, TotalCaptureResult result)
        {
            owner.UnlockFocus();

            // If something goes wrong with the save (or the handler isn't even 
            // registered, this code will toast a success message regardless...)
            //PopupHelper.Toast(Peaks360Application.Context, Resource.String.PhotoShow_PhotoSaved);
        }
    }
}
