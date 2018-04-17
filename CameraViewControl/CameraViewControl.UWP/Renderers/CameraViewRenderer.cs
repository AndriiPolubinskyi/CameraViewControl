using CameraViewControl.Controls;
using CameraViewControl.UWP.Renderers;
using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.Sensors;
using Windows.Foundation;
using Windows.Graphics.Display;
using Windows.Graphics.Imaging;
using Windows.Media;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Streams;
using Windows.System.Display;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Xamarin.Forms.Platform.UWP;

[assembly: ExportRenderer(typeof(CameraView), typeof(CameraViewRenderer))]
namespace CameraViewControl.UWP.Renderers
{
    class CameraViewRenderer: ViewRenderer<CameraView, CaptureElement>
    {
        readonly DisplayInformation displayInformation = DisplayInformation.GetForCurrentView();
        readonly SimpleOrientationSensor orientationSensor = SimpleOrientationSensor.GetDefault();
        readonly DisplayRequest displayRequest = new DisplayRequest();
        SimpleOrientation deviceOrientation = SimpleOrientation.NotRotated;
        DisplayOrientations displayOrientation = DisplayOrientations.Portrait;

        // Rotation metadata to apply to preview stream (https://msdn.microsoft.com/en-us/library/windows/apps/xaml/hh868174.aspx)
        static readonly Guid RotationKey = new Guid("C380465D-2271-428C-9B83-ECEA3B4A85C1"); // (MF_MT_VIDEO_ROTATION)

        readonly SystemMediaTransportControls systemMediaControls = SystemMediaTransportControls.GetForCurrentView();

        MediaCapture mediaCapture;
        CaptureElement captureElement;
        bool isInitialized;
        bool isPreviewing;
        bool externalCamera;
        bool mirroringPreview;

        /// <summary>
        /// Occurs when photo.
        /// </summary>
        public event EventHandler<byte[]> Photo;

        protected override void OnElementChanged(ElementChangedEventArgs<CameraView> e)
        {
            base.OnElementChanged(e);

            if (Control == null)
            {
				captureElement = new CaptureElement();
                captureElement.Stretch = Stretch.UniformToFill;

                SetNativeControl(captureElement);
            }

            if (e.OldElement != null)
            {
                // Unsubscribe
                Tapped -= OnCameraPreviewTapped;
                this.Photo -= e.NewElement.NotifyPhoto;

                e.OldElement.OpenCamera -= HandleCameraInitialisation;
                e.OldElement.Shutter -= HandleShutter;
            }

            if (e.NewElement != null)
            {
                // Subscribe
                Tapped += OnCameraPreviewTapped;
                this.Photo += e.NewElement.NotifyPhoto;

                e.NewElement.OpenCamera += HandleCameraInitialisation;
                e.NewElement.Shutter += HandleShutter;
            }
        }

        async void SetupCamera()
        {
            SetupUI();
            await InitializeCameraAsync();
        }

        #region Media Capture

        async Task InitializeCameraAsync()
        {
            if (mediaCapture == null)
            {
                var devices = await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture);
                var cameraDevice = devices.FirstOrDefault(c => c.EnclosureLocation != null && c.EnclosureLocation.Panel == Windows.Devices.Enumeration.Panel.Back);
                // Get any camera if there isn't one on the back panel
                cameraDevice = cameraDevice ?? devices.FirstOrDefault();

                if (cameraDevice == null)
                {
                    Debug.WriteLine("No camera found");
                    return;
                }

                mediaCapture = new MediaCapture();


                var mediaCaptureInitSettings = this.CreateInitializationSettings(cameraDevice.Id);
                await mediaCapture.InitializeAsync(mediaCaptureInitSettings);

                try
                {

                    // Prevent the device from sleeping while the preview is running
                    displayRequest.RequestActive();

                    // Setup preview source in UI and mirror if required
                    captureElement.Source = mediaCapture;
                    captureElement.FlowDirection = mirroringPreview ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;

                    // Start preview
                    await mediaCapture.StartPreviewAsync();

                    isInitialized = true;
                }

                catch (UnauthorizedAccessException)
                {
                    Debug.WriteLine("Camera access denied");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Exception initializing MediaCapture - {0}: {1}", cameraDevice.Id, ex.ToString());
                }

                if (isInitialized)
                {
                    if (cameraDevice.EnclosureLocation == null || cameraDevice.EnclosureLocation.Panel == Windows.Devices.Enumeration.Panel.Unknown)
                    {
                        externalCamera = true;
                    }
                    else
                    {
                        // Camera is on device
                        externalCamera = false;

                        // Mirror preview if camera is on front panel
                        mirroringPreview = (cameraDevice.EnclosureLocation.Panel == Windows.Devices.Enumeration.Panel.Front);
                    }
                    await StartPreviewAsync();
                }
            }
        }

        private MediaCaptureInitializationSettings CreateInitializationSettings(string cameraId)
        {
            return new MediaCaptureInitializationSettings
            {
                VideoDeviceId = cameraId
            };
        }


        async Task StartPreviewAsync()
        {
            isPreviewing = true;

            if (isPreviewing)
            {
                await SetPreviewRotationAsync();
            }
        }

        async Task StopPreviewAsync()
        {
            isPreviewing = false;
            await mediaCapture.StopPreviewAsync();

            // Use dispatcher because sometimes this method is called from non-UI threads
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                // UI cleanup
                captureElement.Source = null;

                // Allow device screen to sleep now preview is stopped
                displayRequest.RequestRelease();
            });
        }

        async Task SetPreviewRotationAsync()
        {
            // Only update the orientation if the camera is mounted on the device
            if (externalCamera)
            {
                return;
            }

            // Derive the preview rotation
            int rotation = ConvertDisplayOrientationToDegrees(displayOrientation);

            // Invert if mirroring
            if (mirroringPreview)
            {
                rotation = (360 - rotation) % 360;
            }

            // Add rotation metadata to preview stream
            var props = mediaCapture.VideoDeviceController.GetMediaStreamProperties(MediaStreamType.VideoPreview);
            props.Properties.Add(RotationKey, rotation);
            await mediaCapture.SetEncodingPropertiesAsync(MediaStreamType.VideoPreview, props, null);
        }

        async Task TakePhotoAsync()
        {

            var stream = new InMemoryRandomAccessStream();

            await mediaCapture.CapturePhotoToStreamAsync(ImageEncodingProperties.CreateJpeg(), stream);
            await StopPreviewAsync();

            BitmapDecoder decoder = await BitmapDecoder.CreateAsync(stream);
            SoftwareBitmap softwareBitmap = await decoder.GetSoftwareBitmapAsync();


            var data = await EncodedBytes(softwareBitmap, BitmapEncoder.JpegEncoderId);

            Photo?.Invoke(this, data.ToArray());
        }

        private async Task<byte[]> EncodedBytes(SoftwareBitmap soft, Guid encoderId)
        {
            byte[] array = null;

            // First: Use an encoder to copy from SoftwareBitmap to an in-mem stream (FlushAsync)
            // Next:  Use ReadAsync on the in-mem stream to get byte[] array

            using (var ms = new InMemoryRandomAccessStream())
            {
                BitmapEncoder encoder = await BitmapEncoder.CreateAsync(encoderId, ms);
                encoder.SetSoftwareBitmap(soft);

                try
                {
                    await encoder.FlushAsync();
                }
                catch
                {
                    return new byte[0];
                }

                array = new byte[ms.Size];
                await ms.ReadAsync(array.AsBuffer(), (uint)ms.Size, InputStreamOptions.None);
            }
            return array;
        }

        #endregion

        #region Helpers

        void SetupUI()
        {
            // Lock page to landscape to prevent the capture element from rotating
            DisplayInformation.AutoRotationPreferences = DisplayOrientations.Landscape;

            displayOrientation = displayInformation.CurrentOrientation;
            if (orientationSensor != null)
            {
                deviceOrientation = orientationSensor.GetCurrentOrientation();
            }

            RegisterEventHandlers();


        }

        void CleanupUI()
        {
            UnregisterEventHandlers();

            // Revert orientation preferences
            DisplayInformation.AutoRotationPreferences = DisplayOrientations.None;
        }

        void RegisterEventHandlers()
        {
            if (orientationSensor != null)
            {
                orientationSensor.OrientationChanged += OnOrientationSensorOrientationChanged;
            }
            displayInformation.OrientationChanged += OnDisplayInformationOrientationChanged;
        }

        void UnregisterEventHandlers()
        {
            if (orientationSensor != null)
            {
                orientationSensor.OrientationChanged -= OnOrientationSensorOrientationChanged;
            }
            displayInformation.OrientationChanged -= OnDisplayInformationOrientationChanged;
        }

        #endregion

        #region Rotation

        SimpleOrientation GetCameraOrientation()
        {
            if (externalCamera)
            {
                // Cameras that aren't attached to the device do not rotate along with it
                return SimpleOrientation.NotRotated;
            }

            var result = deviceOrientation;

            // On portrait-first devices, the camera sensor is mounted at a 90 degree offset to the native orientation
            if (displayInformation.NativeOrientation == DisplayOrientations.Portrait)
            {
                switch (result)
                {
                    case SimpleOrientation.Rotated90DegreesCounterclockwise:
                        result = SimpleOrientation.NotRotated;
                        break;
                    case SimpleOrientation.Rotated180DegreesCounterclockwise:
                        result = SimpleOrientation.Rotated90DegreesCounterclockwise;
                        break;
                    case SimpleOrientation.Rotated270DegreesCounterclockwise:
                        result = SimpleOrientation.Rotated180DegreesCounterclockwise;
                        break;
                    case SimpleOrientation.NotRotated:
                        result = SimpleOrientation.Rotated270DegreesCounterclockwise;
                        break;
                }
            }

            // If the preview is mirrored for a front-facing camera, invert the rotation
            if (mirroringPreview)
            {
                // Rotating 0 and 180 ddegrees is the same clockwise and anti-clockwise
                switch (result)
                {
                    case SimpleOrientation.Rotated90DegreesCounterclockwise:
                        return SimpleOrientation.Rotated270DegreesCounterclockwise;
                    case SimpleOrientation.Rotated270DegreesCounterclockwise:
                        return SimpleOrientation.Rotated90DegreesCounterclockwise;
                }
            }

            return result;
        }

        static int ConvertDeviceOrientationToDegrees(SimpleOrientation orientation)
        {
            switch (orientation)
            {
                case SimpleOrientation.Rotated90DegreesCounterclockwise:
                    return 90;
                case SimpleOrientation.Rotated180DegreesCounterclockwise:
                    return 180;
                case SimpleOrientation.Rotated270DegreesCounterclockwise:
                    return 270;
                case SimpleOrientation.NotRotated:
                default:
                    return 0;
            }
        }

        static int ConvertDisplayOrientationToDegrees(DisplayOrientations orientation)
        {
            switch (orientation)
            {
                case DisplayOrientations.Portrait:
                    return 90;
                case DisplayOrientations.LandscapeFlipped:
                    return 180;
                case DisplayOrientations.PortraitFlipped:
                    return 270;
                case DisplayOrientations.Landscape:
                default:
                    return 0;
            }
        }

        static PhotoOrientation ConvertOrientationToPhotoOrientation(SimpleOrientation orientation)
        {
            switch (orientation)
            {
                case SimpleOrientation.Rotated90DegreesCounterclockwise:
                    return PhotoOrientation.Rotate90;
                case SimpleOrientation.Rotated180DegreesCounterclockwise:
                    return PhotoOrientation.Rotate180;
                case SimpleOrientation.Rotated270DegreesCounterclockwise:
                    return PhotoOrientation.Rotate270;
                case SimpleOrientation.NotRotated:
                default:
                    return PhotoOrientation.Normal;
            }
        }

        #endregion

        #region Event Handlers

        async void OnCameraPreviewTapped(object sender, TappedRoutedEventArgs e)
        {
            if (isPreviewing)
            {
                await StopPreviewAsync();
            }
            else
            {
                await StartPreviewAsync();
            }
        }

        /// <summary>
        /// Handles the shutter.
        /// </summary>
        /// <param name="sender">Sender.</param>
        /// <param name="e">E.</param>
        private async void HandleShutter(object sender, EventArgs e)
        {
            await TakePhotoAsync();
        }

        /// <summary>
        /// Handles the camera initialisation.
        /// </summary>
        /// <param name="sender">Sender.</param>
        /// <param name="args">If set to <c>true</c> arguments.</param>
        private void HandleCameraInitialisation(object sender, bool args)
        {
            SetupCamera();
        }

        void OnOrientationSensorOrientationChanged(SimpleOrientationSensor sender, SimpleOrientationSensorOrientationChangedEventArgs args)
        {
            // Only update orientatino if the device is not parallel to the ground
            if (args.Orientation != SimpleOrientation.Faceup && args.Orientation != SimpleOrientation.Facedown)
            {
                deviceOrientation = args.Orientation;
            }
        }

        async void OnDisplayInformationOrientationChanged(DisplayInformation sender, object args)
        {
            displayOrientation = sender.CurrentOrientation;

            if (isPreviewing)
            {
                await SetPreviewRotationAsync();
            }
        }

        //async void OnHardwareCameraButtonPressed(object sender, CameraEventArgs e)
        //{
        //    await TakePhotoAsync();
        //}

        #endregion
    }
}
