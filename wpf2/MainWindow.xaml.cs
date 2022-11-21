using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Security.Policy;
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
                InitialDirectory = @"C:\Users\kryst\Downloads",
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
            var change = e.Delta * .5;
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

            if (colorShift.R == 0 || colorShift.G == 0 || colorShift.B == 0)
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

            switch (selectedFilter)
            {
                case 0:
                    AveragingFilter();
                    break;
                case 1:
                    MedianFilter();
                    break;
                case 2:
                    SobelFilter();
                    break;
                case 3:
                    HighpassFilter();
                    break;
                case 4:
                    GaussFilter();
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
            foreach (var p in pixels)
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

        private void MedianFilter()
        {
            if (currentBitmap == null) return;

            var copy = new Bitmap(currentBitmap);

            int windowSize = 10; // px

            for (int x = 0; x < currentBitmap.Width; x++)
            {
                for (int y = 0; y < currentBitmap.Height; y++)
                {
                    var neighbours = GetNeighbours(x, y, copy, windowSize);

                    System.Drawing.Color color = GetMedian(neighbours);

                    currentBitmap.SetPixel(x, y, color);

                    neighbours.Clear();
                }
            }

            currentImage.Source = fileLoader.ConvertBitmap(currentBitmap);
        }

        private List<System.Drawing.Color> GetNeighbours(int x, int y, Bitmap copy, int windowSize)
        {
            int bitmapWidth = currentBitmap.Width;
            int bitmapHeight = currentBitmap.Height;

            List<System.Drawing.Color> neighbours = new List<System.Drawing.Color>();

            int windowHalfSize = (int)Math.Floor((double)windowSize / 2);

            for (int i = 0; i < windowSize; i++)
            {
                for (int j = 0; j < windowSize; j++)
                {
                    var a = x + i - windowHalfSize;
                    var b = y + j - windowHalfSize;

                    if (a < 0)
                        a = 0;
                    if (a >= bitmapWidth)
                        a = bitmapWidth - 1;
                    if (b < 0)
                        b = 0;
                    if (b >= bitmapHeight)
                        b = bitmapHeight - 1;

                    var pixel = copy.GetPixel(a, b);
                    neighbours.Add(pixel);
                }
            }

            return neighbours;
        }

        private System.Drawing.Color GetMedian(List<System.Drawing.Color> neighbours)
        {
            int R = (int)neighbours.Median(c => c.R);
            int G = (int)neighbours.Median(c => c.G);
            int B = (int)neighbours.Median(c => c.B);

            return System.Drawing.Color.FromArgb(R, G, B);
        }

        private void SobelFilter()
        {
            if (currentBitmap == null) return;

            var copy = new Bitmap(currentBitmap);

            int[,] sobelX = {{-1, 0, 1},
                                {-2, 0, 2},
                                {-1, 0, 1}};

            int[,] sobelY = {{-1, -2, -1},
                                {0, 0, 0},
                                {1, 2, 1}};

            for (int x = 1; x < currentBitmap.Width - 1; x++)
            {
                for (int y = 1; y < currentBitmap.Height - 1; y++)
                {
                    var aa = copy.GetPixel(x - 1, y - 1).GetGrayScale();
                    var ab = copy.GetPixel(x, y - 1).GetGrayScale();
                    var ac = copy.GetPixel(x + 1, y - 1).GetGrayScale();
                    var ba = copy.GetPixel(x - 1, y).GetGrayScale();
                    var bb = copy.GetPixel(x, y).GetGrayScale();
                    var bc = copy.GetPixel(x + 1, y).GetGrayScale();
                    var ca = copy.GetPixel(x - 1, y + 1).GetGrayScale();
                    var cb = copy.GetPixel(x, y + 1).GetGrayScale();
                    var cc = copy.GetPixel(x + 1, y + 1).GetGrayScale();

                    var pixelX = (sobelX[0,0] * aa) + (sobelX[0,1] * ab) + (sobelX[0,2] * ac) +
                                (sobelX[1,0] * ba) + (sobelX[1,1] * bb) + (sobelX[1,2] * bc) +
                                (sobelX[2,0] * ca) + (sobelX[2,1] * cb) + (sobelX[2,2] * cc);

                    var pixelY = (sobelY[0, 0] * aa) + (sobelY[0, 1] * ab) +  (sobelY[0, 2] * ac) +
                                (sobelY[1, 0] * ba) + (sobelY[1, 1] * bb) + (sobelY[1, 2] * bc) +
                                (sobelY[2, 0] * ca) + (sobelY[2, 1] * cb) + (sobelY[2, 2] * cc);

                    int value = (int)Math.Ceiling(Math.Sqrt((pixelX * pixelX) + (pixelY * pixelY)));
                    value = NormaliseColor(value * 255 / 700);
                    var color = System.Drawing.Color.FromArgb(value, value, value);

                    currentBitmap.SetPixel(x, y, color);
                }
            }

            currentImage.Source = fileLoader.ConvertBitmap(currentBitmap);
        }

        private void HighpassFilter()
        {
            if (currentBitmap == null) return;

            var copy = new Bitmap(currentBitmap);

            int[,] matrix = {{0, -1, 0},
                            {-1, 5, -1},
                            {0, -1, 0}};

            for (int x = 1; x < currentBitmap.Width - 1; x++)
            {
                for (int y = 1; y < currentBitmap.Height - 1; y++)
                {
                    var aa = copy.GetPixel(x - 1, y - 1);
                    var ab = copy.GetPixel(x, y - 1);
                    var ac = copy.GetPixel(x + 1, y - 1);
                    var ba = copy.GetPixel(x - 1, y);
                    var bb = copy.GetPixel(x, y);
                    var bc = copy.GetPixel(x + 1, y);
                    var ca = copy.GetPixel(x - 1, y + 1);
                    var cb = copy.GetPixel(x, y + 1);
                    var cc = copy.GetPixel(x + 1, y + 1);

                    int R = (matrix[0, 0] * aa.R) + (matrix[0, 1] * ab.R) + (matrix[0, 2] * ac.R) +
                            (matrix[1, 0] * ba.R) + (matrix[1, 1] * bb.R) + (matrix[1, 2] * bc.R) +
                            (matrix[2, 0] * ca.R) + (matrix[2, 1] * cb.R) + (matrix[2, 2] * cc.R);

                    int G = (matrix[0, 0] * aa.G) + (matrix[0, 1] * ab.G) + (matrix[0, 2] * ac.G) +
                            (matrix[1, 0] * ba.G) + (matrix[1, 1] * bb.G) + (matrix[1, 2] * bc.G) +
                            (matrix[2, 0] * ca.G) + (matrix[2, 1] * cb.G) + (matrix[2, 2] * cc.G);

                    int B = (matrix[0, 0] * aa.B) + (matrix[0, 1] * ab.B) + (matrix[0, 2] * ac.B) +
                            (matrix[1, 0] * ba.B) + (matrix[1, 1] * bb.B) + (matrix[1, 2] * bc.B) +
                            (matrix[2, 0] * ca.B) + (matrix[2, 1] * cb.B) + (matrix[2, 2] * cc.B);

                    var color = System.Drawing.Color.FromArgb(NormaliseColor(R), NormaliseColor(G), NormaliseColor(B));
                    currentBitmap.SetPixel(x, y, color);
                }
            }

            currentImage.Source = fileLoader.ConvertBitmap(currentBitmap);
        }

        private void GaussFilter()
        {
            if (currentBitmap == null) return;

            var copy = new Bitmap(currentBitmap);

            int windowSize = 10;
            double sigma = 100;

            List<double> gauss = GaussFunction(windowSize, sigma);
            double gaussSum = gauss.Sum(c => c);
            for (int i = 0; i < gauss.Count; i++)
                gauss[i] /= gaussSum;

            for (int x = 0; x < currentBitmap.Width; x++)
            {
                for (int y = 0; y < currentBitmap.Height; y++)
                {
                    List<System.Drawing.Color> neighbours = GetNeighbours(x, y, copy, windowSize);
                    List<RGBColorDouble> gaussedColors = new();
                    for (int i = 0; i < neighbours.Count; i++)
                    {
                        double r = neighbours[i].R * gauss[i];
                        double g = neighbours[i].G * gauss[i];
                        double b = neighbours[i].B * gauss[i];
                        gaussedColors.Add(new RGBColorDouble(r, g, b));
                    }

                    var R = (int)gaussedColors.Sum(c => c.R);
                    var G = (int)gaussedColors.Sum(c => c.G);
                    var B = (int)gaussedColors.Sum(c => c.B);
                    var color = System.Drawing.Color.FromArgb(R, G, B);

                    currentBitmap.SetPixel(x, y, color);
                }
            }

            currentImage.Source = fileLoader.ConvertBitmap(currentBitmap);
        }

        private List<double> GaussFunction(int windowSize, double sigma)
        {
            List<double> list = new();

            int halfWindow = (int)Math.Floor((double)windowSize / 2);

            for (int x = 0 - halfWindow; x < windowSize - halfWindow; x++)
            {
                for (int y = 0 - halfWindow; y < windowSize - halfWindow; y++)
                {
                    list.Add((1 / (2 * Math.PI * sigma * sigma)) * Math.Exp(-((x * x) + (y * y)) / (2 * sigma * sigma)));
                }
            }

            return list;
        }

        private void reset_button_Click(object sender, RoutedEventArgs e)
        {
            if (bitmapBackup != null)
            {
                currentBitmap = new Bitmap(bitmapBackup);
                currentImage.Source = fileLoader.ConvertBitmap(bitmapBackup);
            }
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
