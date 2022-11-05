using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using wpf2.Models;

namespace wpf2
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private System.Windows.Controls.Image currentImage;
        private FileLoader fileLoader = new FileLoader();
        private double imageX, imageY;
        private double mouseX, mouseY;
        private bool isMovingViaMouse;
        private Bitmap currentBitmap;
        private Bitmap bitmapBackup;

        public MainWindow()
        {
            InitializeComponent();
            currentImage = new System.Windows.Controls.Image();
            main_canvas.Children.Add(currentImage);
            currentImage.MouseMove += image_MouseMove;
            currentImage.MouseRightButtonDown += image_MouseRightButtonDown;
            Canvas.SetTop(currentImage, 0);
            Canvas.SetLeft(currentImage, 0);
        }

        private void image_MouseRightButtonDown(object sender, MouseEventArgs e)
        {
            var mouseCurrentPoint = Mouse.GetPosition(main_canvas);

            var positionInImageX = mouseCurrentPoint.X - Canvas.GetLeft(currentImage);
            var positionInImageY = mouseCurrentPoint.Y - Canvas.GetTop(currentImage);

            var normalisedPositionX = (int)Math.Floor(positionInImageX / currentImage.Width * currentBitmap.Width);
            var normalisedPositionY = (int)Math.Floor(positionInImageY / currentImage.Height * currentBitmap.Height);

            var pixel = currentBitmap.GetPixel(normalisedPositionX, normalisedPositionY);

            var toolTip = new ToolTip();
            toolTip.IsOpen = true;
            toolTip.StaysOpen = false;
            toolTip.Content = $"R: {pixel.R}, G: {pixel.G}, B: {pixel.B}";
        }

        private void load_file_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                InitialDirectory = @"D:\Downloads\ppm-obrazy-testowe\ppm-obrazy-testowe",
                Filter = "Images|*.ppm;*.jpg;*.jpeg",
                RestoreDirectory = true,
                Title = "Wybierz plik",
                DefaultExt = "txt",
                CheckFileExists = true,
                CheckPathExists = true,
            };

            if (openFileDialog.ShowDialog() == true)
            {
                var source = fileLoader.LoadFile(openFileDialog.FileName);
                if (source == null)
                {
                    MessageBox.Show("File corupted!");
                    return;
                }

                currentBitmap = fileLoader.Bitmap;
                bitmapBackup = new Bitmap(fileLoader.Bitmap);
                currentImage.Source = source;

     
                var scale1 = main_canvas.ActualWidth / currentBitmap.Width;
                var scale2 = main_canvas.ActualHeight / currentBitmap.Height;

                var scale = scale1 < scale2 ? scale1 : scale2;
                currentImage.Height = currentBitmap.Height * scale;
                currentImage.Width = currentBitmap.Width * scale;
                Canvas.SetTop(currentImage, 0);
                Canvas.SetLeft(currentImage, 0);
            }
        }

        private int NormaliseColor(int color)
        {
            if (color < 0) return 0;
            if (color > 255) return 255;
            return color;
        }

        private void save_button_Click(object sender, RoutedEventArgs e)
        {
            int jpgQuality;
            if (!int.TryParse(jpg_quality.Text, out jpgQuality) || jpgQuality < 0 || jpgQuality > 100)
            {
                MessageBox.Show("Enter correct parameters!");
                return;
            }

            var saveDialog = new SaveFileDialog();

            saveDialog.FileName = "result";
            saveDialog.DefaultExt = "jpg";
            saveDialog.Filter = "JPG images (*.jpg)|*.jpg";

            ImageCodecInfo jpgEncoder = GetEncoder(ImageFormat.Jpeg);

            System.Drawing.Imaging.Encoder myEncoder =
                System.Drawing.Imaging.Encoder.Quality;


            EncoderParameters myEncoderParameters = new EncoderParameters(1);
            
            EncoderParameter myEncoderParameter = new EncoderParameter(myEncoder, jpgQuality);
            myEncoderParameters.Param[0] = myEncoderParameter;
            if (saveDialog.ShowDialog() == true)
            {
                var fileName = saveDialog.FileName;
                if (!System.IO.Path.HasExtension(fileName) || System.IO.Path.GetExtension(fileName) != "jpg")
                    fileName = fileName + ".jpg";

                currentBitmap.Save(fileName, jpgEncoder, myEncoderParameters);
            }
        }

        private void image_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                var mouseCurrentPoint = Mouse.GetPosition(main_canvas);
                if (isMovingViaMouse == false)
                {
                    imageX = Canvas.GetLeft(currentImage);
                    imageY = Canvas.GetTop(currentImage);
                    mouseX = mouseCurrentPoint.X;
                    mouseY = mouseCurrentPoint.Y;
                    isMovingViaMouse = true;
                }
                Canvas.SetTop(currentImage, imageY + (mouseCurrentPoint.Y - mouseY));
                Canvas.SetLeft(currentImage, imageX + (mouseCurrentPoint.X - mouseX));
            }
            else isMovingViaMouse = false;
        }

        private void main_canvas_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            var change = e.Delta * .2;
            currentImage.Height += change;
            currentImage.Width += change;
        }

        private void add_colors_button_Click(object sender, RoutedEventArgs e)
        {
            if (currentBitmap == null) return;

            var colorShift = ParseRGBFromInput();

            if (colorShift == null) return;

            System.Drawing.Color c;

            for (int x = 0; x < currentBitmap.Width; x++)
            {
                for (int y = 0; y < currentBitmap.Height; y++)
                {
                    c = currentBitmap.GetPixel(x, y);
                    currentBitmap.SetPixel(x, y, System.Drawing.Color.FromArgb(0, NormaliseColor(c.R + colorShift.R), NormaliseColor(c.G + colorShift.G),
                        NormaliseColor(c.B + colorShift.B)));
                }
            }

            currentImage.Source = fileLoader.ConvertBitmap(currentBitmap);
        }

        private void multiply_colors_button_Click(object sender, RoutedEventArgs e)
        {
            if (currentBitmap == null) return;

            var colorShift = ParseRGBFromInput();

            if (colorShift == null) return;

            System.Drawing.Color c;

            for (int x = 0; x < currentBitmap.Width; x++)
            {
                for (int y = 0; y < currentBitmap.Height; y++)
                {
                    c = currentBitmap.GetPixel(x, y);
                    currentBitmap.SetPixel(x, y, System.Drawing.Color.FromArgb(0, NormaliseColor(c.R * colorShift.R), NormaliseColor(c.G * colorShift.G),
                        NormaliseColor(c.B * colorShift.B)));
                }
            }

            currentImage.Source = fileLoader.ConvertBitmap(currentBitmap);
        }

        private void divide_button_Click(object sender, RoutedEventArgs e)
        {
            if (currentBitmap == null) return;

            var colorShift = ParseRGBFromInput();

            if (colorShift == null) return;

            if(colorShift.R == 0 || colorShift.G == 0 || colorShift.B == 0)
            {
                MessageBox.Show("Value can't be 0!");
                return;
            }

            System.Drawing.Color c;

            for (int x = 0; x < currentBitmap.Width; x++)
            {
                for (int y = 0; y < currentBitmap.Height; y++)
                {
                    c = currentBitmap.GetPixel(x, y);
                    currentBitmap.SetPixel(x, y, System.Drawing.Color.FromArgb(0, NormaliseColor(c.R / colorShift.R), NormaliseColor(c.G / colorShift.G),
                        NormaliseColor(c.B / colorShift.B)));
                }
            }

            currentImage.Source = fileLoader.ConvertBitmap(currentBitmap);
        }

        private void bw1_button_Click(object sender, RoutedEventArgs e)
        {
            System.Drawing.Color c;
            int newColor;

            for (int x = 0; x < currentBitmap.Width; x++)
            {
                for (int y = 0; y < currentBitmap.Height; y++)
                {
                    c = currentBitmap.GetPixel(x, y);

                    newColor = (int)((c.R + c.G + c.B) / 3);

                    currentBitmap.SetPixel(x, y, System.Drawing.Color.FromArgb(0, NormaliseColor(newColor), NormaliseColor(newColor),
                        NormaliseColor(newColor)));
                }
            }

            currentImage.Source = fileLoader.ConvertBitmap(currentBitmap);
        }

        private void bw2_button_Click(object sender, RoutedEventArgs e)
        {
            System.Drawing.Color c;
            int newColor;

            for (int x = 0; x < currentBitmap.Width; x++)
            {
                for (int y = 0; y < currentBitmap.Height; y++)
                {
                    c = currentBitmap.GetPixel(x, y);

                    newColor = (int)(0.3 * c.R + 0.59 * c.G + 0.11 * c.B);

                    currentBitmap.SetPixel(x, y, System.Drawing.Color.FromArgb(0, NormaliseColor(newColor), NormaliseColor(newColor),
                        NormaliseColor(newColor)));
                }
            }

            currentImage.Source = fileLoader.ConvertBitmap(currentBitmap);
        }

        private void brightness_button_Click(object sender, RoutedEventArgs e)
        {
            if (currentBitmap == null) return;

            var brightnessShift = (int)brightness_slider.Value;

            System.Drawing.Color c;

            for (int x = 0; x < currentBitmap.Width; x++)
            {
                for (int y = 0; y < currentBitmap.Height; y++)
                {
                    c = currentBitmap.GetPixel(x, y);
                    currentBitmap.SetPixel(x, y, System.Drawing.Color.FromArgb(0, NormaliseColor(c.R + brightnessShift), NormaliseColor(c.G + brightnessShift),
                        NormaliseColor(c.B + brightnessShift)));
                }
            }

            currentImage.Source = fileLoader.ConvertBitmap(currentBitmap);
        }

        private void filter_button_Click(object sender, RoutedEventArgs e)
        {
            var selectedFilter = filter_selection.SelectedIndex;

            switch(selectedFilter)
            {
                case 0:
                    AveragingFilter();
                    break;
                case 1:
                    break;
                case 2:
                    break;
                case 3:
                    break;
                case 4:
                    break;
            }
        }

        private void AveragingFilter()
        {
            if (currentBitmap == null) return;

            var copy = new Bitmap(currentBitmap);

            List<System.Drawing.Color> c = new List<System.Drawing.Color>();

            RGBColor average;

            for (int x = 0; x < currentBitmap.Width; x++)
            {
                for (int y = 0; y < currentBitmap.Height; y++)
                {
                    if (x - 1 >= 0 && y + 1 < currentBitmap.Height) c.Add(copy.GetPixel(x - 1, y + 1));
                    if (y + 1 < currentBitmap.Height) c.Add(copy.GetPixel(x, y + 1));
                    if (x + 1 < currentBitmap.Width && y + 1 < currentBitmap.Height) c.Add(copy.GetPixel(x + 1, y + 1));
                    if (x - 1 >= 0) c.Add(copy.GetPixel(x - 1, y));
                    c.Add(copy.GetPixel(x, y));
                    if (x + 1 < currentBitmap.Width) c.Add(copy.GetPixel(x + 1, y));
                    if (x - 1 >= 0 && y - 1 >= 0) c.Add(copy.GetPixel(x - 1, y - 1));
                    if (y - 1 >= 0) c.Add(currentBitmap.GetPixel(x, y - 1));
                    if (x + 1 < currentBitmap.Width && y - 1 >= 0) c.Add(currentBitmap.GetPixel(x + 1, y - 1));

                    average = CalculateAverage(c);

                    currentBitmap.SetPixel(x, y, System.Drawing.Color.FromArgb(0, NormaliseColor(average.R), NormaliseColor(average.G),
                        NormaliseColor(average.B)));

                    c.Clear();
                }
            }

            currentImage.Source = fileLoader.ConvertBitmap(currentBitmap);
        }

        private RGBColor CalculateAverage(List<System.Drawing.Color> pixels)
        {
            RGBColor result = new RGBColor();
            foreach(var p in pixels)
            {
                result.R += p.R;
                result.G += p.G;
                result.B += p.B;
            }
            result.R /= pixels.Count;
            result.G /= pixels.Count;
            result.B /= pixels.Count;
            return result;
        }

        private void reset_button_Click(object sender, RoutedEventArgs e)
        {
            if(bitmapBackup != null)
                currentImage.Source = fileLoader.ConvertBitmap(bitmapBackup);
        }

        private RGBColor ParseRGBFromInput()
        {
            RGBColor color = new RGBColor();

            List<bool> canBeParsed = new List<bool>();
            int output;

            canBeParsed.Add(Int32.TryParse(R_input.Text, out output));
            color.R = output;

            canBeParsed.Add(Int32.TryParse(G_input.Text, out output));
            color.G = output;

            canBeParsed.Add(Int32.TryParse(B_input.Text, out output));
            color.B = output;

            if (canBeParsed.Any(p => p == false))
            {
                MessageBox.Show("Enter correct parameters!");
                return null;
            }
            return color;
        }

        private ImageCodecInfo GetEncoder(ImageFormat format)
        {
            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageEncoders();

            foreach (ImageCodecInfo codec in codecs)
            {
                if (codec.FormatID == format.Guid)
                {
                    return codec;
                }
            }

            return null;
        }


    }
}
