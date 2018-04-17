using System;
using System.IO;
using Xamarin.Forms;

namespace CameraViewControl
{
    public partial class MainPage : ContentPage
    {
        static bool IsPortrait(Page p) { return p.Width < p.Height; }

        public MainPage()
        {
            InitializeComponent();
            CameraView.Photo += CameraView_Photo;
        }

        private void CameraView_Photo(object sender, byte[] e)
        {
            Device.BeginInvokeOnMainThread(() =>
                 Navigation.PushModalAsync(new PreviewImagePage(e))
            );
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            CameraView.NotifyOpenCamera(true);
        }

        private void Button_Clicked(object sender, EventArgs e)
        {
            CameraView.NotifyShutter();
        }

    }
}
