using Android.Media;
using Java.Nio;
using System;

namespace CameraViewControl.Droid.Camera2Api
{
    public class ImageAvailableListener : Java.Lang.Object, ImageReader.IOnImageAvailableListener
    {
        public event EventHandler<byte[]> ImageProcessingCompleted;

        public void OnImageAvailable(ImageReader reader)
        {
            Image image = null;
            try
            {
                image = reader.AcquireLatestImage();
                ByteBuffer buffer = image.GetPlanes()[0].Buffer;
                byte[] bytes = new byte[buffer.Capacity()];
                buffer.Get(bytes);
                ImageProcessingCompleted?.Invoke(this, bytes);
            }

            finally
            {
                if (image != null)
                {
                    image.Close();
                }
            }
        }
    }
}