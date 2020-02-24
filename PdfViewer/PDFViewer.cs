using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Resources;
using System.Windows.Threading;
using Windows.Data.Pdf;
using Windows.Storage.Streams;

namespace PdfViewer
{
    class PDFViewer : Control
    {
        public static readonly DependencyProperty PagesProperty = DependencyProperty.Register("Pages", typeof(ObservableCollection<PdfPage>), typeof(PDFViewer), new PropertyMetadata(null));
        public ObservableCollection<PdfPage> Pages
        {
            get => (ObservableCollection<PdfPage>)this.GetValue(PagesProperty);
            private set => this.SetValue(PagesProperty, value);
        }
        public PDFViewer()
        {
            ScrollerTimer = new DispatcherTimer();
            ScrollerTimer.Interval = TimeSpan.FromMilliseconds(250);
            ScrollerTimer.Tick += ScrollerTimer_Tick;

            ThumbnailScrollerTimer = new DispatcherTimer();
            ThumbnailScrollerTimer.Interval = TimeSpan.FromMilliseconds(250);
            ThumbnailScrollerTimer.Tick += ThumbnailScrollerTimer_Tick; ;

            this.Loaded += (s, e) => Test();
        }

        

        private DispatcherTimer ScrollerTimer;
        private DispatcherTimer ThumbnailScrollerTimer;

        ScrollViewer thumbnailViewer;
        ScrollViewer Scroller;
        public override void OnApplyTemplate()
        {
            thumbnailViewer = this.GetTemplateChild("ThumbnailScroller") as ScrollViewer;
            thumbnailViewer.ScrollChanged += ThumbnailViewer_ScrollChanged;

            Scroller = this.GetTemplateChild("Scroller") as ScrollViewer;
            Scroller.ScrollChanged += Scroller_ScrollChanged; ;

            base.OnApplyTemplate();
        }

        private void Scroller_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            ScrollerTimer.Stop();
            ScrollerTimer.Start();
        }

        private void ScrollerTimer_Tick(object sender, EventArgs e)
        {
            ScrollerTimer.Stop();

            if (Pages == null)
                return;

            double viewOffset = Scroller.VerticalOffset;
            double viewHeight = Scroller.ActualHeight;
            double position = 0;

            foreach (var page in Pages)
            {
                double pageOffset = position;
                if ((pageOffset >= viewOffset && (pageOffset + page.ImageSize.Height) <= (viewOffset + viewHeight)) |
                    (pageOffset <= viewOffset && (pageOffset + page.ImageSize.Height > viewOffset)) |
                    (pageOffset >= viewOffset && pageOffset <= (viewOffset + viewHeight)))
                    page.CreateImage();
                else
                    page.DeleteImage();

                position += page.ImageSize.Height + 40;
            }
        }

        private void ThumbnailViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            ThumbnailScrollerTimer.Stop();
            ThumbnailScrollerTimer.Start();
        }

        private void ThumbnailScrollerTimer_Tick(object sender, EventArgs e)
        {
            if (Pages == null)
                return;

            double viewOffset = thumbnailViewer.VerticalOffset;
            double viewHeight = thumbnailViewer.ActualHeight;
            double position = 0;

            foreach (var page in Pages)
            {
                double pageOffset = position;
                if ((pageOffset >= viewOffset && (pageOffset + page.ThumbnailSize.Height) <= (viewOffset + viewHeight)) |
                    (pageOffset <= viewOffset && (pageOffset + page.ThumbnailSize.Height > viewOffset)) |
                    (pageOffset >= viewOffset && pageOffset <= (viewOffset + viewHeight)))
                    page.CreateThumbnail();
                else
                    page.DeleteThumbnail();

                position += page.ThumbnailSize.Height + 40;
            }
        }

        async void Test()
        {
            Pages = new ObservableCollection<PdfPage>();

            string uri = "/PdfViewer;component/Manuals/OXYBABY_6_DE.pdf";
            byte[] pdfData = null;
            StreamResourceInfo streamInfo = Application.GetResourceStream(
                new Uri(uri, UriKind.RelativeOrAbsolute));
            if (streamInfo != null)
            {
                using (Stream stream = streamInfo.Stream)
                {
                    using (MemoryStream MS = new MemoryStream())
                    {
                        stream.CopyTo(MS);
                        pdfData = MS.ToArray();
                    }
                }
            }

            PdfDocument doc = await PdfDocument.LoadFromStreamAsync(await ConvertTo(pdfData));
            for (int i = 0; i < doc.PageCount; i++)
            {
                Pages.Add(new PdfPage(i, doc));
            }

        }

        static PDFViewer()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(PDFViewer),
                new FrameworkPropertyMetadata(typeof(PDFViewer)));
        }

        internal async Task<IRandomAccessStream> ConvertTo(byte[] arr)
        {
            InMemoryRandomAccessStream randomAccessStream = new InMemoryRandomAccessStream();
            await randomAccessStream.WriteAsync(arr.AsBuffer());
            randomAccessStream.Seek(0);
            return (IRandomAccessStream)randomAccessStream;
        }
    }
}
