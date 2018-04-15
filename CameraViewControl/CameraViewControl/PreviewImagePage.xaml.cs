using System.IO;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace CameraViewControl
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class PreviewImagePage : ContentPage
    {
        public PreviewImagePage(byte[] image)
        {
            InitializeComponent();
            imgPreview.Source = ImageSource.FromStream(() => new MemoryStream(image));
        }
    }
}