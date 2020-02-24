using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using Windows.Data.Pdf;
using Windows.Storage.Streams;

namespace PdfViewer
{
    class PdfPage : INotifyPropertyChanged
    {
        #region Private Deklaration
        private BitmapImage PropThumbnailImage;
        private BitmapImage PropRenderedImage;
        #endregion

        #region Public Deklaration
        public int Index { get; }
        public Size ThumbnailSize { get; }
        public Size ImageSize { get; }
        public PdfDocument Document { get; }

        public BitmapImage ThumbnailImage
        {
            get => PropThumbnailImage;
            set
            {
                PropThumbnailImage = value;
                OnPropertyChanged("ThumbnailImage");
            }
        }
        public BitmapImage RenderedImage
        {
            get => PropRenderedImage;
            set
            {
                PropRenderedImage = value;
                OnPropertyChanged("RenderedImage");
            }
        }

        public PdfPage(int index, PdfDocument document)
        {
            Index = index;
            Document = document;

            Windows.Data.Pdf.PdfPage page = Document.GetPage((uint)Index);
            double aspectRatio = page.Size.Width / page.Size.Height;
            ThumbnailSize = new Size(200, 200 / aspectRatio);
            ImageSize = new Size(page.Size.Width, page.Size.Height);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string property)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }
        #endregion

        #region Constructor<

        #endregion

        public async void CreateThumbnail()
        {
            if(ThumbnailImage == null)
                ThumbnailImage = await RenderPage(200, double.NaN);
        }

        public void DeleteThumbnail()
        {
            ThumbnailImage = null;
        }

        public async void CreateImage()
        {
            if(RenderedImage == null)
                RenderedImage = await RenderPage(ImageSize.Width, ImageSize.Height);
        }

        public void DeleteImage()
        {
            RenderedImage = null;
        }




        private async Task<BitmapImage> RenderPage(double width, double height)
        {
            Windows.Data.Pdf.PdfPage page = Document.GetPage((uint)Index);

            double aspectRatio = page.Size.Width / page.Size.Height;
            Size renderedSize = new Size(width, height);
            if (double.IsNaN(width) && !double.IsNaN(height))
            {
                renderedSize.Width = height * aspectRatio;
            }
            else if (double.IsNaN(height) && !double.IsNaN(width))
            {
                renderedSize.Height = width / aspectRatio;
            }

            BitmapImage image = new BitmapImage();
            using (var stream = new InMemoryRandomAccessStream())
            {
                PdfPageRenderOptions opt = new PdfPageRenderOptions();
                opt.DestinationHeight = Convert.ToUInt32(renderedSize.Height);
                opt.DestinationWidth = Convert.ToUInt32(renderedSize.Width);
                await page.RenderToStreamAsync(stream, opt);

                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.StreamSource = stream.AsStream();
                image.DecodePixelHeight = Convert.ToInt32(renderedSize.Height);
                image.DecodePixelWidth = Convert.ToInt32(renderedSize.Width);
                image.EndInit();
            }

            image.Freeze();
            return image;
        }
    }
}
