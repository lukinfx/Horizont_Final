using Android.Media;
//using Java.IO;
using Java.Lang;
using Java.Nio;
using Java.Nio.FileNio;
using System;
using System.IO;
//using System.IO;

namespace HorizontApp.Views.Camera
{
    public class ImageAvailableListener : Java.Lang.Object, ImageReader.IOnImageAvailableListener
    {
        public ImageAvailableListener(CameraFragment fragment)
        {
            if (fragment == null)
                throw new System.ArgumentNullException("fragment");
            
            owner = fragment;
        }

        private readonly Java.IO.File file;
        private readonly CameraFragment owner;

        //public File File { get; private set; }
        //public CameraFragment Owner { get; private set; }

        public void OnImageAvailable(ImageReader reader)
        {
            owner.mBackgroundHandler.Post(new ImageSaver(reader.AcquireNextImage()));
        }

        // Saves a JPEG {@link Image} into the specified {@link File}.
        private class ImageSaver : Java.Lang.Object, IRunnable
        {
            private static string SAVED_PICTURES_FOLDER = "HorizonPhotos";

            // The JPEG image
            private Image mImage;

            // The file we save the image into.
            //private File mFile;

            public ImageSaver(Image image)
            {
                if (image == null)
                    throw new System.ArgumentNullException("image");
                mImage = image;
            }

            private static string GetElevationFileName(DateTime? dt = null)
            {
                if (!dt.HasValue)
                    dt = DateTime.Now;

                string mName = $"{DateTime.Now.Year}_{DateTime.Now.Month}_{DateTime.Now.Day}_{DateTime.Now.Hour}:{DateTime.Now.Minute}:{DateTime.Now.Second}_Horizont.jpg";
                return mName;
            }

            private static string GetPhootsFileFolder()
            {
                string path = System.IO.Path.Combine(Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData), SAVED_PICTURES_FOLDER);

                if (File.Exists(path))
                {
                    return path;
                }
                else
                {
                    Directory.CreateDirectory(path);
                }
                return path;
                

            }

            private static string GetPhotosFilePath(DateTime? dt = null)
            {
                return System.IO.Path.Combine(GetPhootsFileFolder(), GetElevationFileName(dt));
            }

            public void Run()
            {
                ByteBuffer buffer = mImage.GetPlanes()[0].Buffer;
                byte[] bytes = new byte[buffer.Remaining()];
                buffer.Get(bytes);

                var file = new Java.IO.File(GetPhotosFilePath());
                

                using (var output = new Java.IO.FileOutputStream(file))
                {
                    try
                    {
                        output.Write(bytes);
                    }
                    catch (Java.IO.IOException e)
                    {
                        e.PrintStackTrace();
                    }
                    finally
                    {
                        mImage.Close();
                    }
                }
            }
        }
    }
}