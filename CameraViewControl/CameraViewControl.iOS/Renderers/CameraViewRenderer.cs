using CameraViewControl.Controls;
using CameraViewControl.Enums;
using CameraViewControl.iOS.Camera;
using CameraViewControl.iOS.Renderers;
using CameraViewControl.iOS.Utils;
using System;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;

[assembly: ExportRenderer(typeof(CameraView), typeof(CameraViewRenderer))]
namespace CameraViewControl.iOS.Renderers
{
    public class CameraViewRenderer : ViewRenderer<CameraView, CameraIOS>
    {
        #region Private Properties

        /// <summary>
        /// The bodyshop camera IO.
        /// </summary>
        private CameraIOS _bodyshopCameraIOS;

        #endregion

        #region Protected Methods

        /// <summary>
        /// Raises the element changed event.
        /// </summary>
        /// <param name="e">E.</param>
        protected override void OnElementChanged(ElementChangedEventArgs<CameraView> e)
        {
            base.OnElementChanged(e);

            if (Control == null)
            {
                _bodyshopCameraIOS = new CameraIOS();

                SetNativeControl(_bodyshopCameraIOS);
            }

            if (e.OldElement != null)
            {
                e.OldElement.Flash -= HandleFlash;
                e.OldElement.OpenCamera -= HandleCameraInitialisation;
                e.OldElement.OnFocused -= HandleFocus;
                e.OldElement.Shutter -= HandleShutter;
                e.OldElement.Widths -= HandleWidths;

                _bodyshopCameraIOS.Busy -= e.OldElement.NotifyBusy;
                _bodyshopCameraIOS.Available -= e.OldElement.NotifyAvailability;
                _bodyshopCameraIOS.Photo -= e.OldElement.NotifyPhoto;
            }

            if (e.NewElement != null)
            {
                e.NewElement.Flash += HandleFlash;
                e.NewElement.OpenCamera += HandleCameraInitialisation;
                e.NewElement.OnFocused += HandleFocus;
                e.NewElement.Shutter += HandleShutter;
                e.NewElement.Widths += HandleWidths;

                _bodyshopCameraIOS.Busy += e.NewElement.NotifyBusy;
                _bodyshopCameraIOS.Available += e.NewElement.NotifyAvailability;
                _bodyshopCameraIOS.Photo += e.NewElement.NotifyPhoto;
                VolumeButtonHandler.Instance.Start();
                VolumeButtonHandler.Instance.ButtonClicked += Instance_ButtonClicked;
            }
        }

        /// <summary>
        /// Dispose the specified disposing.
        /// </summary>
        /// <param name="disposing">If set to <c>true</c> disposing.</param>
        protected override void Dispose(bool disposing)
        {
            if (_bodyshopCameraIOS != null)
            {
                // stop output session and dispose camera elements before popping page
                _bodyshopCameraIOS.StopAndDispose();
                _bodyshopCameraIOS.Dispose();
            }
            VolumeButtonHandler.Instance.Stop();
            base.Dispose(disposing);
        }

        /// <summary>
        /// Raises the element property changed event.
        /// </summary>
        /// <param name="sender">Sender.</param>
        /// <param name="e">E.</param>
        protected override void OnElementPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            base.OnElementPropertyChanged(sender, e);

            if (Element != null && _bodyshopCameraIOS != null)
            {
                if (e.PropertyName == VisualElement.HeightProperty.PropertyName ||
                    e.PropertyName == VisualElement.WidthProperty.PropertyName)
                {
                    _bodyshopCameraIOS.SetBounds((nint)Element.Width, (nint)Element.Height);
                }
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Handles the widths.
        /// </summary>
        /// <param name="sender">Sender.</param>
        /// <param name="e">E.</param>
        private void HandleWidths(object sender, float e)
        {
            _bodyshopCameraIOS.SetWidths(e);
        }

        /// <summary>
        /// Handles the shutter.
        /// </summary>
        /// <param name="sender">Sender.</param>
        /// <param name="e">E.</param>
        private async void HandleShutter(object sender, EventArgs e)
        {
            await _bodyshopCameraIOS.TakePhoto();
        }

        /// <summary>
        /// Handles the orientation change.
        /// </summary>
        /// <param name="sender">Sender.</param>
        /// <param name="e">E.</param>
        private void HandleOrientationChange(object sender, Orientation e)
        {
            _bodyshopCameraIOS.HandleOrientationChange(e);
        }

        /// <summary>
        /// Handles the focus.
        /// </summary>
        /// <param name="sender">Sender.</param>
        /// <param name="e">E.</param>
        private void HandleFocus(object sender, Point e)
        {
            _bodyshopCameraIOS.ChangeFocusPoint(e);
        }

        /// <summary>
        /// Handles the camera initialisation.
        /// </summary>
        /// <param name="sender">Sender.</param>
        /// <param name="args">If set to <c>true</c> arguments.</param>
        private void HandleCameraInitialisation(object sender, bool args)
        {
            _bodyshopCameraIOS.InitializeCamera();

            Element.OrientationChange += HandleOrientationChange;
        }

        /// <summary>
        /// Handles the flash.
        /// </summary>
        /// <param name="sender">Sender.</param>
        /// <param name="args">If set to <c>true</c> arguments.</param>
        private void HandleFlash(object sender, bool args)
        {
            _bodyshopCameraIOS.SwitchFlash(args);
        }

        /// <summary>
        /// Handles the focus change.
        /// </summary>
        /// <param name="sender">Sender.</param>
        /// <param name="args">Arguments.</param>
        private void HandleFocusChange(object sender, Point args)
        {
            _bodyshopCameraIOS.ChangeFocusPoint(args);
        }

        private async void Instance_ButtonClicked(object sender, EventArgs e)
        {
            await _bodyshopCameraIOS.TakePhoto();
        }

        #endregion
    }
}
