using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Net;
using System.Net.Cache;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Interactivity;

namespace OnedrawHelper.Behaviors
{
    class LazyImageBehavior : Behavior<Image>
    {
        #region LazySource 添付プロパティ
        public Uri LazySource
        {
            get { return (Uri)GetValue(LazySourceProperty); }
            set { SetValue(LazySourceProperty, value); }
        }

        public static readonly DependencyProperty LazySourceProperty =
            DependencyProperty.Register("LazySource", typeof(Uri), typeof(LazyImageBehavior), new PropertyMetadata(null, LazySource_Changed));
        #endregion

        private static async void LazySource_Changed(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var element = sender as LazyImageBehavior;
            if (element == null) return;

            var image = await LazyImageExtension.GetImage(e.NewValue as Uri);
            if (image != null)
                element.AssociatedObject.Source = image;
        }
    }

    static class LazyImageExtension
    {
        public static Task<BitmapImage> GetImage(Uri uri)
        {
            return Task.Run(() =>
            {
                var wc = new WebClient() { CachePolicy = new RequestCachePolicy(RequestCacheLevel.CacheIfAvailable) };
                try
                {
                    var image = new BitmapImage();
                    image.BeginInit();
                    image.StreamSource = new MemoryStream(wc.DownloadData(uri));
                    image.EndInit();
                    image.Freeze();
                    return image;
                }
                catch (WebException)
                {
                    return null;
                }
                catch (IOException)
                {
                    return null;
                }
                catch (InvalidOperationException)
                {
                    return null;
                }
                finally
                {
                    wc.Dispose();
                }
            });
        }
    }
}
