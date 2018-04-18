using System;
using Xamarin.Forms;

namespace CameraViewControl.Controls
{
    public class CustomImage : View
    {
        /// <summary>
        /// The tint color string property.
        /// </summary>
        /// <summary>
        /// The tint on property.
        /// </summary>
        public static readonly BindableProperty TintColorStringProperty = 
            BindableProperty.Create("TintColorString", typeof(string), typeof(CustomImage), null, BindingMode.OneWay, null, TintColorStringChanged);

        /// <summary>
        /// Gets or sets the tint color string.
        /// </summary>
        /// <value>The tint color string.</value>
        public string TintColorString
        {
            get
            {
                return (string)GetValue(TintColorStringProperty);
            }
            set
            {
                SetValue(TintColorStringProperty, value);
            }
        }

        public static void TintColorStringChanged(BindableObject bindable, object oldValue, object newValue)
        {
            ((CustomImage)bindable).CustomPropertyChanged?.Invoke(bindable, TintColorStringProperty.PropertyName);
        }

        /// <summary>
        /// The tint on property.
        /// </summary>
        public static readonly BindableProperty TintOnProperty = BindableProperty.Create((CustomImage o) => o.TintOn, default(bool),
            propertyChanged: (bindable, oldvalue, newValue) =>
            {
                ((CustomImage)bindable).CustomPropertyChanged?.Invoke(bindable, TintOnProperty.PropertyName);
            });

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="Camera.XamForms.Controls.CustomImage"/> tint on.
        /// </summary>
        /// <value><c>true</c> if tint on; otherwise, <c>false</c>.</value>
        public bool TintOn
        {
            get
            {
                return (bool)GetValue(TintOnProperty);
            }
            set
            {
                SetValue(TintOnProperty, value);
            }
        }

        /// <summary>
        /// The path property.
        /// </summary>
        public static readonly BindableProperty PathProperty = BindableProperty.Create((CustomImage o) => o.Path, default(string),
            propertyChanged: (bindable, oldvalue, newValue) =>
            {
                ((CustomImage)bindable).CustomPropertyChanged?.Invoke(bindable, PathProperty.PropertyName);
            });

        /// <summary>
        /// Gets or sets the path.
        /// </summary>
        /// <value>The path.</value>
        public string Path
        {
            get
            {
                return (string)GetValue(PathProperty);
            }
            set
            {
                SetValue(PathProperty, value);
            }
        }

        /// <summary>
        /// The aspect property.
        /// </summary>
        public static readonly BindableProperty AspectProperty = BindableProperty.Create((CustomImage o) => o.Aspect, default(Aspect),
            propertyChanged: (bindable, oldvalue, newValue) =>
            {
                ((CustomImage)bindable).CustomPropertyChanged?.Invoke(bindable, AspectProperty.PropertyName);
            });

        /// <summary>
        /// Gets or sets the aspect.
        /// </summary>
        /// <value>The aspect.</value>
        public Aspect Aspect
        {
            get
            {
                return (Aspect)GetValue(AspectProperty);
            }
            set
            {
                SetValue(AspectProperty, value);
            }
        }

        /// <summary>
        /// Occurs when custom property changed.
        /// </summary>
        public event EventHandler<string> CustomPropertyChanged;

        /// <param name="propertyName">The name of the property that changed.</param>
        /// <summary>
        /// Call this method from a child class to notify that a change happened on a property.
        /// </summary>
        protected override void OnPropertyChanged(string propertyName)
        {
            base.OnPropertyChanged(propertyName);

            if (propertyName == CustomImage.TintColorStringProperty.PropertyName ||
                propertyName == CustomImage.TintOnProperty.PropertyName ||
                propertyName == CustomImage.AspectProperty.PropertyName)
            {
                if (CustomPropertyChanged != null)
                {
                    this.CustomPropertyChanged(this, propertyName);
                }
            }
        }
    }
}
