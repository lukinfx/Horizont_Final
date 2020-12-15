﻿using System;
using System.Collections.Generic;
using Android;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Util;
using Android.Views;
using Android.Widget;
using Android.Hardware.Camera2;
using Android.Graphics;
using Android.Hardware.Camera2.Params;
using Android.Media;
using Java.IO;
using Java.Lang;
using Java.Util;
using Java.Util.Concurrent;
using Boolean = Java.Lang.Boolean;
using Math = Java.Lang.Math;
using Orientation = Android.Content.Res.Orientation;
using HorizontApp.AppContext;
using HorizontLib.Domain.Models;
using HorizontApp.Utilities;

namespace HorizontApp.Views.Camera
{
    public class CameraFragment : Fragment//, FragmentCompat.IOnRequestPermissionsResultCallback
    {
        private static readonly SparseIntArray ORIENTATIONS = new SparseIntArray();
        private static readonly string FRAGMENT_DIALOG = "dialog";

        // Tag for the {@link Log}.
        private static readonly string TAG = "CameraFragment";

        // Camera state: Showing camera preview.
        public const int STATE_PREVIEW = 0;

        // Camera state: Waiting for the focus to be locked.
        public const int STATE_WAITING_LOCK = 1;

        // Camera state: Waiting for the exposure to be precapture state.
        public const int STATE_WAITING_PRECAPTURE = 2;

        //Camera state: Waiting for the exposure state to be something other than precapture.
        public const int STATE_WAITING_NON_PRECAPTURE = 3;

        // Camera state: Picture was taken.
        public const int STATE_PICTURE_TAKEN = 4;

        // Max preview width that is guaranteed by Camera2 API
        private static readonly int MAX_PREVIEW_WIDTH = 1920;

        // Max preview height that is guaranteed by Camera2 API
        private static readonly int MAX_PREVIEW_HEIGHT = 1080;

        // TextureView.ISurfaceTextureListener handles several lifecycle events on a TextureView
        private CameraSurfaceTextureListener mSurfaceTextureListener;

        // ID of the current {@link CameraDevice}.
        private string mCameraId;

        // An AutoFitTextureView for camera preview
        private AutoFitTextureView mTextureView;

        // A {@link CameraCaptureSession } for camera preview.
        public CameraCaptureSession mCaptureSession;

        // A reference to the opened CameraDevice
        public CameraDevice mCameraDevice;

        private Surface mSurface;
        // The size of the camera preview
        private Size mPreviewSize;

        // CameraDevice.StateListener is called when a CameraDevice changes its state
        private CameraStateListener mStateCallback;

        // An additional thread for running tasks that shouldn't block the UI.
        private HandlerThread mBackgroundThread;

        // A {@link Handler} for running tasks in the background.
        public Handler mBackgroundHandler;

        // An {@link ImageReader} that handles still image capture.
        private ImageReader mImageReader;

        // This is the output file for our picture.
        public File mFile;

        // This a callback object for the {@link ImageReader}. "onImageAvailable" will be called when a
        // still image is ready to be saved.
        private ImageAvailableListener mOnImageAvailableListener;

        //{@link CaptureRequest.Builder} for the camera preview
        public CaptureRequest.Builder mPreviewRequestBuilder;

        // {@link CaptureRequest} generated by {@link #mPreviewRequestBuilder}
        public CaptureRequest mPreviewRequest;

        // The current state of camera state for taking pictures.
        public int mState = STATE_PREVIEW;

        // A {@link Semaphore} to prevent the app from exiting before closing the camera.
        public Semaphore mCameraOpenCloseLock = new Semaphore(1);

        // Whether the current camera device supports Flash or not.
        private bool mFlashSupported;

        // Orientation of the camera sensor
        private int mSensorOrientation;

        // A {@link CameraCaptureSession.CaptureCallback} that handles events related to JPEG capture.
        public CameraCaptureListener mCaptureCallback;
        private IAppContext _context { get { return AppContextLiveData.Instance; } }

        // Shows a {@link Toast} on the UI thread.
        public void ShowToast(string text)
        {
            if (Activity != null)
            {
                Activity.RunOnUiThread(new ShowToastRunnable(Activity.ApplicationContext, text));
            }
        }

        private class ShowToastRunnable : Java.Lang.Object, IRunnable
        {
            private string text;
            private Context context;

            public ShowToastRunnable(Context context, string text)
            {
                this.context = context;
                this.text = text;
            }

            public void Run()
            {
                Toast.MakeText(context, text, ToastLength.Short).Show();
            }
        }

        private static Size ChooseOptimalSize(Size[] choices, int textureViewWidth,
            int textureViewHeight, int maxWidth, int maxHeight, Size aspectRatio)
        {
            //return new Size(textureViewWidth, textureViewHeight);

            // Collect the supported resolutions that are at least as big as the preview Surface
            var bigEnough = new List<Size>();
            // Collect the supported resolutions that are smaller than the preview Surface
            var notBigEnough = new List<Size>();
            int w = aspectRatio.Width;
            int h = aspectRatio.Height;

            for (var i = 0; i < choices.Length; i++)
            {
                Size option = choices[i];
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
                Log.Error(TAG, "Couldn't find any suitable preview size");
                return choices[0];
            }
        }

        public static CameraFragment NewInstance()
        {
            return new CameraFragment();
        }

        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            mStateCallback = new CameraStateListener(this);
            mSurfaceTextureListener = new CameraSurfaceTextureListener(this);

            // fill ORIENTATIONS list
            ORIENTATIONS.Append((int)SurfaceOrientation.Rotation0, 90);
            ORIENTATIONS.Append((int)SurfaceOrientation.Rotation90, 0);
            ORIENTATIONS.Append((int)SurfaceOrientation.Rotation180, 270);
            ORIENTATIONS.Append((int)SurfaceOrientation.Rotation270, 180);
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            return inflater.Inflate(Resource.Layout.fragment_camera2_basic, container, false);
        }

        public override void OnViewCreated(View view, Bundle savedInstanceState)
        {
            mTextureView = (AutoFitTextureView)view.FindViewById(Resource.Id.texture);
        }

        public override void OnActivityCreated(Bundle savedInstanceState)
        {
            base.OnActivityCreated(savedInstanceState);
            //mFile = new File(Activity.GetExternalFilesDir(null), "pic.jpg");
            mCaptureCallback = new CameraCaptureListener(this);
            mOnImageAvailableListener = new ImageAvailableListener(this);
        }

        public override void OnResume()
        {
            base.OnResume();
            StartBackgroundThread();

            // When the screen is turned off and turned back on, the SurfaceTexture is already
            // available, and "onSurfaceTextureAvailable" will not be called. In that case, we can open
            // a camera and start preview from here (otherwise, we wait until the surface is ready in
            // the SurfaceTextureListener).
            if (mTextureView.IsAvailable)
            {
                OpenCamera(mTextureView.Width, mTextureView.Height);
            }
            else
            {
                mTextureView.SurfaceTextureListener = mSurfaceTextureListener;
            }
        }

        public void StopPreview()
        {
            //mSurface.Freeze();
            //mTextureView.SurfaceTextureListener = null;
            //mPreviewRequestBuilder.RemoveTarget(mSurface);
            mCaptureSession.StopRepeating();
        }

        public void StartPreview()
        {
            //mSurface.Unfreeze();
            //mTextureView.SurfaceTextureListener = mSurfaceTextureListener;
            //mPreviewRequestBuilder.AddTarget(mSurface);
            mCaptureSession.SetRepeatingRequest(mPreviewRequest, mCaptureCallback, mBackgroundHandler);
        }


        public override void OnPause()
        {
            CloseCamera();
            StopBackgroundThread();
            base.OnPause();
        }

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
                    var facing = (Integer)characteristics.Get(CameraCharacteristics.LensFacing);
                    if (facing != null && facing == (Integer.ValueOf((int)LensFacing.Front)))
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

        // Sets up member variables related to camera.
        private void SetUpCameraOutputs(int width, int height)
        {
            var activity = Activity;
            var manager = (CameraManager)activity.GetSystemService(Context.CameraService);
            try
            {
                //var cameraId = manager.GetCameraIdList()[i];
                var cameraId = _context.Settings.CameraId;
                CameraCharacteristics characteristics = manager.GetCameraCharacteristics(cameraId);

                var map = (StreamConfigurationMap)characteristics.Get(CameraCharacteristics.ScalerStreamConfigurationMap);

                mImageReader = ImageReader.NewInstance(_context.Settings.cameraResolutionSelected.Width, _context.Settings.cameraResolutionSelected.Height, ImageFormatType.Jpeg, /*maxImages*/2);
                mImageReader.SetOnImageAvailableListener(mOnImageAvailableListener, mBackgroundHandler);

                // Find out if we need to swap dimension to get the preview size relative to sensor
                // coordinate.
                var displayRotation = activity.WindowManager.DefaultDisplay.Rotation;
                //noinspection ConstantConditions
                mSensorOrientation = (int)characteristics.Get(CameraCharacteristics.SensorOrientation);
                bool swappedDimensions = false;
                switch (displayRotation)
                {
                    case SurfaceOrientation.Rotation0:
                    case SurfaceOrientation.Rotation180:
                        if (mSensorOrientation == 90 || mSensorOrientation == 270)
                        {
                            swappedDimensions = true;
                        }
                        break;
                    case SurfaceOrientation.Rotation90:
                    case SurfaceOrientation.Rotation270:
                        if (mSensorOrientation == 0 || mSensorOrientation == 180)
                        {
                            swappedDimensions = true;
                        }
                        break;
                    default:
                        Log.Error(TAG, "Display rotation is invalid: " + displayRotation);
                        break;
                }

                Point displaySize = new Point();
                activity.WindowManager.DefaultDisplay.GetSize(displaySize);
                var rotatedPreviewWidth = width;
                var rotatedPreviewHeight = height;
                var maxPreviewWidth = displaySize.X;
                var maxPreviewHeight = displaySize.Y;

                if (swappedDimensions)
                {
                    rotatedPreviewWidth = height;
                    rotatedPreviewHeight = width;
                    maxPreviewWidth = displaySize.Y;
                    maxPreviewHeight = displaySize.X;
                }

                if (maxPreviewWidth > MAX_PREVIEW_WIDTH)
                {
                    maxPreviewWidth = MAX_PREVIEW_WIDTH;
                }

                if (maxPreviewHeight > MAX_PREVIEW_HEIGHT)
                {
                    maxPreviewHeight = MAX_PREVIEW_HEIGHT;
                }

                // Danger, W.R.! Attempting to use too large a preview size could  exceed the camera
                // bus' bandwidth limitation, resulting in gorgeous previews but the storage of
                // garbage capture data.
                mPreviewSize = ChooseOptimalSize(map?.GetOutputSizes(Class.FromType(typeof(SurfaceTexture))),
                     rotatedPreviewWidth, rotatedPreviewHeight,
                     maxPreviewWidth, maxPreviewHeight,
                     _context.Settings.cameraResolutionSelected);
                
                // We fit the aspect ratio of TextureView to the size of preview we picked.
                var orientation = Resources.Configuration.Orientation;
                if (orientation == Orientation.Landscape)
                {
                    mTextureView.SetAspectRatio(mPreviewSize.Width, mPreviewSize.Height);
                }
                else
                {
                    mTextureView.SetAspectRatio(mPreviewSize.Height, mPreviewSize.Width);
                }

                // Check if the flash is supported.
                var available = (Boolean)characteristics.Get(CameraCharacteristics.FlashInfoAvailable);
                if (available == null)
                {
                    mFlashSupported = false;
                }
                else
                {
                    mFlashSupported = (bool)available;
                }

                FetchCameraViewAngle(cameraId);

                mCameraId = cameraId;
                return;
            }
            catch (CameraAccessException e)
            {
                e.PrintStackTrace();
            }
            catch (NullPointerException e)
            {
                //###
                // Currently an NPE is thrown when the Camera2API is used but not supported on the
                // device this code runs.
                ErrorDialog.NewInstance("Camera error").Show(ChildFragmentManager, FRAGMENT_DIALOG);
            }
        }

        // Opens the camera specified by {@link CameraFragment#mCameraId}.
        public void OpenCamera(int width, int height)
        {
            /*if (ContextCompat.CheckSelfPermission(Activity, Manifest.Permission.Camera) != Permission.Granted)
            {
                RequestCameraPermission();
                return;
            }*/
            SetUpCameraOutputs(width, height);
            ConfigureTransform(width, height);
            var activity = Activity;
            var manager = (CameraManager)activity.GetSystemService(Context.CameraService);
            try
            {
                if (!mCameraOpenCloseLock.TryAcquire(2500, TimeUnit.Milliseconds))
                {
                    throw new RuntimeException("Time out waiting to lock camera opening.");
                }
                manager.OpenCamera(mCameraId, mStateCallback, mBackgroundHandler);
            }
            catch (CameraAccessException e)
            {
                e.PrintStackTrace();
            }
            catch (InterruptedException e)
            {
                throw new RuntimeException("Interrupted while trying to lock camera opening.", e);
            }
        }

        // Closes the current {@link CameraDevice}.
        private void CloseCamera()
        {
            try
            {
                mCameraOpenCloseLock.Acquire();
                if (null != mCaptureSession)
                {
                    mCaptureSession.Close();
                    mCaptureSession = null;
                }
                if (null != mCameraDevice)
                {
                    mCameraDevice.Close();
                    mCameraDevice = null;
                }
                if (null != mImageReader)
                {
                    mImageReader.Close();
                    mImageReader = null;
                }
            }
            catch (InterruptedException e)
            {
                throw new RuntimeException("Interrupted while trying to lock camera closing.", e);
            }
            finally
            {
                mCameraOpenCloseLock.Release();
            }
        }

        // Starts a background thread and its {@link Handler}.
        private void StartBackgroundThread()
        {
            mBackgroundThread = new HandlerThread("CameraBackground");
            mBackgroundThread.Start();
            mBackgroundHandler = new Handler(mBackgroundThread.Looper);
        }

        // Stops the background thread and its {@link Handler}.
        private void StopBackgroundThread()
        {
            mBackgroundThread.QuitSafely();
            try
            {
                mBackgroundThread.Join();
                mBackgroundThread = null;
                mBackgroundHandler = null;
            }
            catch (InterruptedException e)
            {
                e.PrintStackTrace();
            }
        }

        // Creates a new {@link CameraCaptureSession} for camera preview.
        public void CreateCameraPreviewSession()
        {
            try
            {
                SurfaceTexture texture = mTextureView.SurfaceTexture;
                if (texture == null)
                {
                    throw new IllegalStateException("texture is null");
                }

                // We configure the size of default buffer to be the size of camera preview we want.
                texture.SetDefaultBufferSize(mPreviewSize.Width, mPreviewSize.Height);

                // This is the output Surface we need to start preview.
                mSurface = new Surface(texture);
                
                // We set up a CaptureRequest.Builder with the output Surface.
                mPreviewRequestBuilder = mCameraDevice.CreateCaptureRequest(CameraTemplate.Preview);
                DisableFlash(mPreviewRequestBuilder);
                mPreviewRequestBuilder.AddTarget(mSurface);

                // Here, we create a CameraCaptureSession for camera preview.
                List<Surface> surfaces = new List<Surface>();
                surfaces.Add(mSurface);
                surfaces.Add(mImageReader.Surface);
                mCameraDevice.CreateCaptureSession(surfaces, new CameraCaptureSessionCallback(this), null);

            }
            catch (CameraAccessException e)
            {
                e.PrintStackTrace();
            }
        }

        public static T Cast<T>(Java.Lang.Object obj) where T : class
        {
            var propertyInfo = obj.GetType().GetProperty("Instance");
            return propertyInfo == null ? null : propertyInfo.GetValue(obj, null) as T;
        }

        // Configures the necessary {@link android.graphics.Matrix}
        // transformation to `mTextureView`.
        // This method should be called after the camera preview size is determined in
        // setUpCameraOutputs and also the size of `mTextureView` is fixed.

        public void ConfigureTransform(int viewWidth, int viewHeight)
        {
            Activity activity = Activity;
            if (null == mTextureView || null == mPreviewSize || null == activity)
            {
                return;
            }
            var rotation = (int)activity.WindowManager.DefaultDisplay.Rotation;
            Matrix matrix = new Matrix();
            RectF viewRect = new RectF(0, 0, viewWidth, viewHeight);
            RectF bufferRect = new RectF(0, 0, mPreviewSize.Height, mPreviewSize.Width);
            float centerX = viewRect.CenterX();
            float centerY = viewRect.CenterY();
            if ((int)SurfaceOrientation.Rotation90 == rotation || (int)SurfaceOrientation.Rotation270 == rotation)
            {
                bufferRect.Offset(centerX - bufferRect.CenterX(), centerY - bufferRect.CenterY());
                matrix.SetRectToRect(viewRect, bufferRect, Matrix.ScaleToFit.Fill);
                float scale = Math.Max((float)viewHeight / mPreviewSize.Height, (float)viewWidth / mPreviewSize.Width);
                matrix.PostScale(scale, scale, centerX, centerY);
                matrix.PostRotate(90 * (rotation - 2), centerX, centerY);
            }
            else if ((int)SurfaceOrientation.Rotation180 == rotation)
            {
                matrix.PostRotate(180, centerX, centerY);
            }
            mTextureView.SetTransform(matrix);
        }

        // Initiate a still image capture.
        public void TakePicture(IAppContext context)
        {
            mOnImageAvailableListener.SetContext(context);
            LockFocus();
        }

        // Lock the focus as the first step for a still image capture.
        private void LockFocus()
        {
            try
            {
                // This is how to tell the camera to lock focus.

                mPreviewRequestBuilder.Set(CaptureRequest.ControlAfTrigger, (int)ControlAFTrigger.Start);
                // Tell #mCaptureCallback to wait for the lock.
                mState = STATE_WAITING_LOCK;
                mCaptureSession.Capture(mPreviewRequestBuilder.Build(), mCaptureCallback,
                        mBackgroundHandler);
            }
            catch (CameraAccessException e)
            {
                e.PrintStackTrace();
            }
        }

        // Run the precapture sequence for capturing a still image. This method should be called when
        // we get a response in {@link #mCaptureCallback} from {@link #lockFocus()}.
        public void RunPrecaptureSequence()
        {
            try
            {
                // This is how to tell the camera to trigger.
                mPreviewRequestBuilder.Set(CaptureRequest.ControlAePrecaptureTrigger, (int)ControlAEPrecaptureTrigger.Start);
                // Tell #mCaptureCallback to wait for the precapture sequence to be set.
                mState = STATE_WAITING_PRECAPTURE;
                mCaptureSession.Capture(mPreviewRequestBuilder.Build(), mCaptureCallback, mBackgroundHandler);
            }
            catch (CameraAccessException e)
            {
                e.PrintStackTrace();
            }
        }

        // Capture a still picture. This method should be called when we get a response in
        // {@link #mCaptureCallback} from both {@link #lockFocus()}.
        public void CaptureStillPicture()
        {
            try
            {
                var activity = Activity;
                if (null == activity || null == mCameraDevice)
                {
                    return;
                }

                // This is the CaptureRequest.Builder that we use to take a picture.
                var stillCaptureBuilder = mCameraDevice.CreateCaptureRequest(CameraTemplate.StillCapture);

                stillCaptureBuilder.AddTarget(mImageReader.Surface);

                // Use the same AE and AF modes as the preview.
                stillCaptureBuilder.Set(CaptureRequest.ControlAfMode, (int) ControlAFMode.ContinuousPicture);
                
                //Disable flash
                DisableFlash(stillCaptureBuilder);

                // Orientation
                int rotation = (int) activity.WindowManager.DefaultDisplay.Rotation;
                stillCaptureBuilder.Set(CaptureRequest.JpegOrientation, GetOrientation(rotation));

                mCaptureSession.StopRepeating();
                mCaptureSession.Capture(stillCaptureBuilder.Build(), new CameraCaptureStillPictureSessionCallback(this), null);
            }
            catch (CameraAccessException ex)
            {
                ex.PrintStackTrace();
            }
            catch (Java.Lang.Exception ex)
            {
                ex.PrintStackTrace();
            }
            catch (System.Exception ex)
            {
                //TODO: Handle exception
            }
        }

        // Retrieves the JPEG orientation from the specified screen rotation.
        private int GetOrientation(int rotation)
        {
            // Sensor orientation is 90 for most devices, or 270 for some devices (eg. Nexus 5X)
            // We have to take that into account and rotate JPEG properly.
            // For devices with orientation of 90, we simply return our mapping from ORIENTATIONS.
            // For devices with orientation of 270, we need to rotate the JPEG 180 degrees.
            return (ORIENTATIONS.Get(rotation) + mSensorOrientation + 270) % 360;
        }

        // Unlock the focus. This method should be called when still image capture sequence is
        // finished.
        public void UnlockFocus()
        {
            try
            {
                // Reset the auto-focus trigger
                mPreviewRequestBuilder.Set(CaptureRequest.ControlAfTrigger, (int)ControlAFTrigger.Cancel);

                mCaptureSession.Capture(mPreviewRequestBuilder.Build(), mCaptureCallback,
                        mBackgroundHandler);
                // After this, the camera will go back to the normal state of preview.
                mState = STATE_PREVIEW;
                mCaptureSession.SetRepeatingRequest(mPreviewRequest, mCaptureCallback,
                        mBackgroundHandler);
            }
            catch (CameraAccessException e)
            {
                e.PrintStackTrace();
            }
        }

        public void DisableFlash(CaptureRequest.Builder requestBuilder)
        {
            if (mFlashSupported)
            {
                requestBuilder.Set(CaptureRequest.FlashMode, (int)FlashMode.Off);
            }
        }

        void FetchCameraViewAngle(string cameraId)
        {
            try
            {
                var camera = Android.Hardware.Camera.Open(Int32.Parse(cameraId));
                AppContextLiveData.Instance.Settings.SetCameraParameters(
                    camera.GetParameters().HorizontalViewAngle,
                    camera.GetParameters().VerticalViewAngle,
                    camera.GetParameters().PictureSize.Width, camera.GetParameters().PictureSize.Height);
            }
            catch
            {
                //Default values
                AppContextLiveData.Instance.Settings.SetCameraParameters(60, 40, 1920, 1080);
            }
        }
    }
}

