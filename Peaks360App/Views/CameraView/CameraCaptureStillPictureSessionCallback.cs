using Android.Hardware.Camera2;
using Android.Util;

namespace Peaks360App.Views.Camera
{
    public class CameraCaptureStillPictureSessionCallback : CameraCaptureSession.CaptureCallback
    {
        private static readonly string TAG = "CameraCaptureStillPictureSessionCallback";

        private readonly CameraFragment owner;

        public CameraCaptureStillPictureSessionCallback(CameraFragment owner)
        {
            if (owner == null)
                throw new System.ArgumentNullException("owner");
            this.owner = owner;
        }

        public override void OnCaptureCompleted(CameraCaptureSession session, CaptureRequest request, TotalCaptureResult result)
        {
            // If something goes wrong with the save (or the handler isn't even 
            // registered, this code will toast a success message regardless...)
            owner.ShowToast("Saved: " + "new photo");
            Log.Debug(TAG, "new photo");
            owner.UnlockFocus();
        }
    }
}
