using System;
using Android.Content;
using Android.Views;
using Android.Util;
using Android.Graphics;

namespace CameraViewControl.Droid.Camera2Api
{
    public class AutoFitTextureView : TextureView
    {
        private int _ratioWidth = 0;
        private int _ratioHeight = 0;

        public AutoFitTextureView(Context context)
            : this(context, null)
        {
        }

        public AutoFitTextureView(Context context, IAttributeSet attrs)
            : this(context, attrs, 0)
        {
        }

        public AutoFitTextureView(Context context, IAttributeSet attrs, int defStyle)
            : base(context, attrs, defStyle)
        {
            
        }

        /**
        * Sets the aspect ratio for this view. The size of the view will be measured based on the ratio
        * calculated from the parameters. Note that the actual sizes of parameters don't matter, that
        * is, calling SetAspectRatio(2, 3) and SetAspectRatio(4, 6) make the same result.
        */
        public void SetAspectRatio(int width, int height)
        {
            if (width < 0 || height < 0)
            {
                throw new ArgumentException("Size cannot be negative.");
            }
            _ratioWidth = width;
            _ratioHeight = height;
            RequestLayout();
        }

        protected override void OnMeasure(int widthMeasureSpec, int heightMeasureSpec)
        {
            base.OnMeasure(widthMeasureSpec, heightMeasureSpec);
            int width = MeasureSpec.GetSize(widthMeasureSpec);
            int height = MeasureSpec.GetSize(heightMeasureSpec);

            if (0 == _ratioWidth || 0 == _ratioHeight)
            {
                SetMeasuredDimension(width, height);
            }
            else
            {
                if (width < (float)height * _ratioWidth / _ratioHeight)
                {
                    var w = width;
                    var h = height;
                    SetMeasuredDimension(w, h);
                    ConfigureTransform(w, h, _ratioWidth, _ratioHeight);
                }
                else
                {
                    var w = width;
                    var h = height;
                    SetMeasuredDimension(w, h);
                    ConfigureTransform(w, h, _ratioWidth, _ratioHeight);
                }
            }
        }

        private void ConfigureTransform(int viewWidth, int viewHeight, int previewWidth, int previewHeight)
        {
            Matrix matrix = new Matrix();
            RectF viewRect = new RectF(0, 0, viewWidth, viewHeight);
            RectF bufferRect = new RectF(0, 0, previewWidth, previewHeight);
            float centerX = viewRect.CenterX();
            float centerY = viewRect.CenterY();

            bufferRect.Offset(centerX - bufferRect.CenterX(), centerY - bufferRect.CenterY());
            matrix.SetRectToRect(viewRect, bufferRect, Matrix.ScaleToFit.Fill);
            float verticalScale = (float)viewHeight / previewHeight;

            matrix.PostScale(verticalScale, verticalScale, centerX, centerY);

            SetTransform(matrix);
        }
    }
}