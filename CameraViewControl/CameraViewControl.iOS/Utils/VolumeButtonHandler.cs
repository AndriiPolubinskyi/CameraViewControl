using AVFoundation;
using CoreFoundation;
using Foundation;
using MediaPlayer;
using System;
using UIKit;

namespace CameraViewControl.iOS.Utils
{
    public class VolumeButtonHandler : NSObject
    {
        private float _initialVolume;
        private bool _initialized = false;
        private MPVolumeView _volumeView;
        private AVAudioSession _session;
        private bool _isActive;

        private const float maxVolume = 0.95f;
        private const float minVolume = 0.05f;
        private const string VOLUMECHANGE_KEYPATH = "outputVolume";

        private static readonly VolumeButtonHandler _instance = new VolumeButtonHandler();

        /// <summary>
        /// Will be raised when any volume button was clicked
        /// </summary>
        public event EventHandler ButtonClicked;

        /// <summary>
        /// Get the singleton instance.
        /// </summary>
        /// <value>The instance.</value>
        public static VolumeButtonHandler Instance
        {
            get { return _instance; }
        }

        /// <summary>
        /// Private constructor to ensure the singleton
        /// </summary>
        private VolumeButtonHandler()
        {
        }

        /// <summary>
        /// Initializes the handler
        /// </summary>
        public void Start()
        {
            if (_initialized)
                return;

            NSNotificationCenter.DefaultCenter.AddObserver(UIApplication.WillResignActiveNotification, onApplicationDidChangeActive);
            NSNotificationCenter.DefaultCenter.AddObserver(UIApplication.DidBecomeActiveNotification, onApplicationDidChangeActive);

            initAudioSession();
            disableVolumeHUD();

            // Wait for the volume view to be ready before setting the volume to avoid showing the HUD
            DispatchQueue.DefaultGlobalQueue.DispatchAfter(new DispatchTime((Int64)0.1f * 1000000), () => { setInitialVolume(); });

            _initialized = true;
            _isActive = true;
        }

        /// <summary>
        /// Deinitializes the handler
        /// </summary>
        public void Stop()
        {
            if (!_initialized)
                return;

            _session.RemoveObserver(this, VOLUMECHANGE_KEYPATH);
            NSNotificationCenter.DefaultCenter.RemoveObserver(this);
            _initialized = false;
            _isActive = false;
        }

        #region private methods
        /// <summary>
        /// Sets the system volume.
        /// </summary>
        /// <param name="value">The new volume value</param>
        private void setSystemVolume(float value)
        {
            MPMusicPlayerController.ApplicationMusicPlayer.Volume = value;
        }

        /// <summary>
        /// Disables the volume HUD.
        /// </summary>
        /// <remarks>>Creates a new MPVolumeView and adds it to the mainwindow in the "off-screen" area. Adding this view suppresses showing the volume HUD</remarks>
        private void disableVolumeHUD()
        {
            _volumeView = new MPVolumeView(new CoreGraphics.CGRect(float.MaxValue, float.MaxValue, 0, 0));
            UIApplication.SharedApplication.Windows[0].AddSubview(_volumeView);
        }

        /// <summary>
        /// Will be called when the system volume has been chanfed
        /// </summary>
        /// <param name="newValue">New value.</param>
        private void OnVolumeChanged(float newValue)
        {
            if (_initialized && _isActive)
            {
                if (newValue == _initialVolume)
                {
                    // Resetting volume, skip blocks
                    return;
                }

                ButtonClicked?.Invoke(this, EventArgs.Empty);

                // Reset volume
                setSystemVolume(_initialVolume);
            }
        }

        /// <summary>
        /// Inits the AVAudioSession.
        /// </summary>
        private void initAudioSession()
        {
            _session = AVFoundation.AVAudioSession.SharedInstance();
            var errorOrNull = _session.SetCategory(AVAudioSessionCategory.Ambient);
            if (errorOrNull != null)
            {
                throw new Exception(errorOrNull.Description);
            }

            errorOrNull = _session.SetActive(true);
            if (errorOrNull != null)
            {
                throw new Exception(errorOrNull.Description);
            }

            _session.AddObserver(this, VOLUMECHANGE_KEYPATH, NSKeyValueObservingOptions.New, IntPtr.Zero);

            // Audio session is interrupted when you send the app to the background,
            // and needs to be set to active again when it goes to app goes back to the foreground
            NSNotificationCenter.DefaultCenter.AddObserver(AVAudioSession.InterruptionNotification, onAudioSessionInterrupted);
        }

        public override void ObserveValue(NSString keyPath, NSObject ofObject, NSDictionary change, IntPtr context)
        {
            if (keyPath == VOLUMECHANGE_KEYPATH)
            {
                var volume = (float)(change["new"] as NSNumber);

                OnVolumeChanged(volume);
            }
            else
                base.ObserveValue(keyPath, ofObject, change, context);
        }

        /// <summary>
        /// Will be called for AVAudioSession.InterruptionNotification
        /// </summary>
        /// <param name="notification">Notification.</param>
        private void onAudioSessionInterrupted(NSNotification notification)
        {
            NSDictionary changeInfo = notification.UserInfo;
            Int32 interuptionType = (Int32)(changeInfo["AVAudioSessionInterruptionTypeKey"] as NSNumber);
            if (interuptionType == 0)
            { //AVAudioSession.InterruptionTypeEnded:
                var errorOrNull = _session.SetActive(true);
                if (errorOrNull != null)
                {
                    throw new Exception(errorOrNull.Description);
                }
            }
        }

        /// <summary>
        /// Will be called for UIApplication.WillResignActiveNotification or UIApplication.DidBecomeActiveNotification
        /// </summary>
        /// <param name="notification">Notification.</param>
        private void onApplicationDidChangeActive(NSNotification notification)
        {
            _isActive = notification.Name == UIApplication.DidBecomeActiveNotification;
        }

        /// <summary>
        /// retrieves and initializes the current volume
        /// </summary>
        private void setInitialVolume()
        {
            _initialVolume = _session.OutputVolume;
            if (_initialVolume > maxVolume)
            {
                _initialVolume = maxVolume;
                setSystemVolume(_initialVolume);
            }
            else if (_initialVolume < minVolume)
            {
                _initialVolume = minVolume;
                setSystemVolume(_initialVolume);
            }
        }
        #endregion
    }
}
