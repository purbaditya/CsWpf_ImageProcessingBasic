// using System.DirectoryServices;
using System.Drawing;
// using System.Security.Cryptography;
// using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
// using System.Windows.Navigation;
// using System.Windows.Shapes;
// using System.Windows.Xps.Packaging;
using Microsoft.Win32;
using OpenCvSharp;
// using OpenCvSharp.Extensions;
using OpenCvSharp.WpfExtensions;
using System.IO;
// using System.Collections.ObjectModel;
// using Aspose.Imaging;
// using Aspose.Imaging.FileFormats.Dicom;
using Aspose.Imaging.ImageOptions;

using WpfImage = System.Windows.Controls.Image;
using DrawImage = System.Drawing.Image;
using AsposeImage = Aspose.Imaging.Image;
using cv2Size = OpenCvSharp.Size;

using Brushes = System.Windows.Media.Brushes;
using Rectangle = System.Windows.Shapes.Rectangle;
using DrawRect = System.Drawing.Rectangle;
using Point = System.Windows.Point;
using CommunityToolkit.HighPerformance;

namespace BasicImageProcessingInterface
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : System.Windows.Window
    {
        private Rectangle Bbox = null;
        private Point StartPoint, LastPoint;

        public MainWindow()
        {
            InitializeComponent();
            string[] strEdgeDetection = new string[] { "Canny", "Sobel", "Laplacian", "Scharr" };
            IPAlgorithms_ED.ItemsSource = strEdgeDetection;
            IPAlgorithms_ED.SelectedIndex = 0;

            string[] str2DFilter = new string[] { "Gaussian", "Median", "Bilateral", "Edge Preserving" };
            IPAlgorithms_2DF.ItemsSource = str2DFilter;
            IPAlgorithms_2DF.SelectedIndex = 0;
        }

        // Methods
        private static WpfImage ConvertDrawImageToWpfImage(DrawImage image)
        {
            if (image == null)
                throw new ArgumentNullException("image", "Image should not be null.");
            using (System.Drawing.Bitmap dImg = new System.Drawing.Bitmap(image))
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    dImg.Save(ms, System.Drawing.Imaging.ImageFormat.Bmp);
                    System.Windows.Media.Imaging.BitmapImage bImg = new System.Windows.Media.Imaging.BitmapImage();

                    bImg.BeginInit();
                    bImg.StreamSource = new MemoryStream(ms.ToArray());
                    bImg.EndInit();

                    System.Windows.Controls.Image img = new System.Windows.Controls.Image();
                    img.Source = bImg;
                    return img;
                }
            }
        }
        private static DrawImage ConvertWpfImageToDrawImage(WpfImage image)
        {
            if (image.Source == null)
                throw new ArgumentNullException("image", "Image should not be null.");

            BmpBitmapEncoder encoder = new BmpBitmapEncoder();
            MemoryStream ms = new MemoryStream();
            encoder.Frames.Add(BitmapFrame.Create((BitmapSource)image.Source));
            encoder.Save(ms);
            DrawImage img = DrawImage.FromStream(ms);
            return img;
        }
        private static DrawImage ConvertCroppedBitmapToDrawImage(CroppedBitmap image)
        {
            BmpBitmapEncoder encoder = new BmpBitmapEncoder();
            MemoryStream ms = new MemoryStream();
            encoder.Frames.Add(BitmapFrame.Create(image));
            encoder.Save(ms);
            DrawImage img = DrawImage.FromStream(ms);
            return img;
        }
        private void canDraw_MouseMove(object sender, MouseEventArgs e)
        {
            LastPoint = Mouse.GetPosition(canDraw);
            Bbox.Width = (int)Math.Abs(LastPoint.X - StartPoint.X);
            Bbox.Height = (int)Math.Abs(LastPoint.Y - StartPoint.Y);
            Canvas.SetLeft(Bbox, Math.Min(LastPoint.X, StartPoint.X));
            Canvas.SetTop(Bbox, Math.Min(LastPoint.Y, StartPoint.Y));
        }
        private void canDraw_MouseRelease(object sender, MouseButtonEventArgs e)
        {
            canDraw.ReleaseMouseCapture();
            canDraw.MouseMove -= canDraw_MouseMove;
            canDraw.MouseUp -= canDraw_MouseRelease;
            canDraw.Children.Remove(Bbox);

            if (LastPoint.X < 0) LastPoint.X = 0;
            if (LastPoint.X >= canDraw.Width) LastPoint.X = (int)canDraw.Width - 1;
            if (LastPoint.Y < 0) LastPoint.Y = 0;
            if (LastPoint.Y >= canDraw.Height) LastPoint.Y = (int)canDraw.Height - 1;

            int x = (int)Math.Min(LastPoint.X, StartPoint.X);
            int y = (int)Math.Min(LastPoint.Y, StartPoint.Y);
            int width = (int)Math.Abs(LastPoint.X - StartPoint.X) + 1;
            int height = (int)Math.Abs(LastPoint.Y - StartPoint.Y) + 1;

            BitmapSource bms = (BitmapSource)InputImage.Source;
            x = x - (int)(canDraw.ActualWidth - bms.Width) / 2;
            y = y - (int)(canDraw.ActualHeight - bms.Height) / 2;
            CroppedBitmap cropped_bitmap = new CroppedBitmap(bms, new Int32Rect(x, y, width, height));
            DrawImage dimg = ConvertCroppedBitmapToDrawImage(cropped_bitmap);
            var bmp = new Bitmap(dimg);
            using var src = OpenCvSharp.Extensions.BitmapConverter.ToMat(bmp);
            Cv2.CvtColor(src, src, ColorConversionCodes.BGRA2GRAY);
            InputImage.Source = src.ToWriteableBitmap(PixelFormats.Gray8); //cropped_bitmap;
            canDraw.IsEnabled = false;
            Bbox = null;
        }
        private void canDraw_MouseClick(object sender, MouseButtonEventArgs e)
        {
            StartPoint = Mouse.GetPosition(canDraw);
            LastPoint = StartPoint;
            Bbox = new Rectangle();
            Bbox.Width = 1;
            Bbox.Height = 1;
            Bbox.Stroke = Brushes.Red;
            Bbox.Cursor = Cursors.Cross;

            canDraw.Children.Add(Bbox);
            Canvas.SetLeft(Bbox, StartPoint.X);
            Canvas.SetTop(Bbox, StartPoint.Y);

            canDraw.MouseMove += canDraw_MouseMove;
            canDraw.MouseUp += canDraw_MouseRelease;
            canDraw.CaptureMouse();
        }
        private void ProcessED_Click(object sender, RoutedEventArgs e)
        {
            WpfImage img = InputImage;
            if (img != null && img.Source != null)
            {
                DrawImage dimg = ConvertWpfImageToDrawImage(img);
                var bmp = new Bitmap(dimg);
                using var src = OpenCvSharp.Extensions.BitmapConverter.ToMat(bmp);
                Cv2.CvtColor(src, src, ColorConversionCodes.BGRA2GRAY);
                Cv2.EqualizeHist(src, src);
                using var dst = new Mat();
                using var dx = new Mat();
                using var dy = new Mat();
                switch (IPAlgorithms_ED.SelectedIndex)
                {
                    case 0:
                        Cv2.Canny(src, dst, 100, 140);
                        break;
                    case 1:
                        Cv2.Sobel(src, dst, MatType.CV_8UC1, 2, 2);
                        break;
                    case 2:
                        Cv2.Laplacian(src, dst, -1);
                        break;
                    case 3:
                        Cv2.Scharr(src, dx, MatType.CV_8UC1, 1, 0);
                        Cv2.Scharr(src, dy, MatType.CV_8UC1, 0, 1);
                        Cv2.Add(dx, dy, dst);
                        break;
                    default:
                        break;
                }
                int ch = dst.Type().Channels;
                OutputImage.Source = dst.ToWriteableBitmap(PixelFormats.Gray8);

            }
        }
        private void UploadImage_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog fd = new OpenFileDialog();
            if (fd.ShowDialog() == true)
            {
                string imagePath = fd.FileName;

                // If dicom, encode and save dicom image or pixel data as jpeg
                if (imagePath.Contains(".dcm"))
                {
                    var image = AsposeImage.Load(imagePath);
                    image.Save("../../../DICOM2JPEG.jpg", new JpegOptions());
                    using var src = new Mat("../../../DICOM2JPEG.jpg", ImreadModes.Grayscale);
                    Cv2.Resize(src, src, new cv2Size(512, 512));
                    InputImage.Source = src.ToWriteableBitmap(PixelFormats.Gray8);
                }
                else
                {
                    using var src = new Mat(imagePath, ImreadModes.Grayscale);
                    Cv2.Resize(src, src, new cv2Size(512, 512));
                    InputImage.Source = src.ToWriteableBitmap(PixelFormats.Gray8);
                }
                if (InputImage.Source != null)
                {
                    Addnoise.IsChecked = false;
                    Addnoise.IsEnabled = true;
                    Equalize.IsChecked = false;
                    Equalize.IsEnabled = true;
                }
                canDraw.IsEnabled = true;
            }
        }

        private void ClearImage_Click(object sender, RoutedEventArgs e)
        {
            InputImage.Source = null;
            OutputImage.Source = null;
            Addnoise.IsEnabled = true;
            Addnoise.IsChecked = false;
            Equalize.IsEnabled = true;
            Equalize.IsChecked = false;
            canDraw.IsEnabled = false;
            File.Delete("../../../DICOM2JPEG.jpg");
        }

        private void Process2DF_Click(object sender, RoutedEventArgs e)
        {
            WpfImage img = InputImage;
            if (img != null && img.Source != null)
            {
                DrawImage dimg = ConvertWpfImageToDrawImage(img);
                var bmp = new Bitmap(dimg);
                using var src = OpenCvSharp.Extensions.BitmapConverter.ToMat(bmp);
                using var dst = new Mat();
                Cv2.CvtColor(src, src, ColorConversionCodes.BGRA2GRAY);

                switch (IPAlgorithms_2DF.SelectedIndex)
                {
                    case 0:
                        Cv2.GaussianBlur(src, dst, new cv2Size(7, 7), 0);
                        break;
                    case 1:
                        Cv2.MedianBlur(src, dst, 5);
                        break;
                    case 2:
                        Cv2.BilateralFilter(src, dst, 5, 30, 30);
                        break;
                    case 3:
                        Cv2.EdgePreservingFilter(src, dst, EdgePreservingMethods.RecursFilter, 1, 5);
                        break;
                    default:
                        break;
                }
                int ch = dst.Type().Channels;
                OutputImage.Source = dst.ToWriteableBitmap(PixelFormats.Gray8);
            }
        }

        private void AddNoise_Checked(object sender, RoutedEventArgs e)
        {
            WpfImage img = InputImage;
            if (img.Source != null || Addnoise.IsChecked == false)
            {
                DrawImage dimg = ConvertWpfImageToDrawImage(img);
                var bmp = new Bitmap(dimg);
                using var src = OpenCvSharp.Extensions.BitmapConverter.ToMat(bmp);
                using var ns = new Mat(src.Rows, src.Cols, MatType.CV_8UC1);
                using var dst = new Mat();
                Cv2.CvtColor(src, src, ColorConversionCodes.BGRA2GRAY);
                Cv2.Randn(ns, 0, 30);
                Cv2.Add(src, ns, src);
                int ch = src.Type().Channels;
                InputImage.Source = src.ToWriteableBitmap(PixelFormats.Gray8);
                Addnoise.IsEnabled = false;
            }
            else
            {
                Addnoise.IsChecked = false;
            }
        }

        private void Algorithms_SelectionChanged_2DF(object sender, SelectionChangedEventArgs e)
        {
            // TODO
        }

        private void Equalize_Checked(object sender, RoutedEventArgs e)
        {
            WpfImage img = InputImage;
            if (img.Source != null || Equalize.IsChecked == false)
            {
                DrawImage dimg = ConvertWpfImageToDrawImage(img);
                var bmp = new Bitmap(dimg);
                using var src = OpenCvSharp.Extensions.BitmapConverter.ToMat(bmp);
                using var ns = new Mat(src.Rows, src.Cols, MatType.CV_8UC3);
                using var dst = new Mat();
                Cv2.CvtColor(src, src, ColorConversionCodes.BGRA2GRAY);
                Cv2.EqualizeHist(src, src);
                int ch = src.Type().Channels;
                InputImage.Source = src.ToWriteableBitmap(PixelFormats.Gray8);
                Equalize.IsEnabled = false;
            }
            else
            {
                Equalize.IsChecked = false;
            }
        }

        private void Algorithms_SelectionChanged_ED(object sender, SelectionChangedEventArgs e)
        {
            // TODO
        }
    }
}