using Emgu.CV;
using Emgu.CV.Ocl;
using Emgu.CV.Reg;
using Emgu.CV.Structure;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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

        public Bitmap CurrentBitmap 
        {
            get => _currentBitmap;
            set
            {
                _currentBitmap = value;
                UpdateHistogram();
            }
        }


        private Bitmap _currentBitmap;
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

            var normalisedPositionX = (int)Math.Floor(positionInImageX / currentImage.Width * CurrentBitmap.Width);
            var normalisedPositionY = (int)Math.Floor(positionInImageY / currentImage.Height * CurrentBitmap.Height);

            var pixel = CurrentBitmap.GetPixel(normalisedPositionX, normalisedPositionY);

            var toolTip = new ToolTip();
            toolTip.IsOpen = true;
            toolTip.StaysOpen = false;
            toolTip.Content = $"R: {pixel.R}, G: {pixel.G}, B: {pixel.B}";
        }

        private void load_file_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                InitialDirectory = @"C:\Downloads",
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

                CurrentBitmap = fileLoader.Bitmap;
                bitmapBackup = new Bitmap(fileLoader.Bitmap);
                currentImage.Source = source;

     
                var scale1 = main_canvas.ActualWidth / CurrentBitmap.Width;
                var scale2 = main_canvas.ActualHeight / CurrentBitmap.Height;

                var scale = scale1 < scale2 ? scale1 : scale2;
                currentImage.Height = CurrentBitmap.Height * scale;
                currentImage.Width = CurrentBitmap.Width * scale;
                Canvas.SetTop(currentImage, 0);
                Canvas.SetLeft(currentImage, 0);
            }
        }

        private void UpdateHistogram()
        {
            histogram.ClearHistogram();
            histogram.GenerateHistograms(CurrentBitmap.ToImage<Bgr, byte>(), 256);
            histogram.Refresh();
        }
        private int NormaliseColor(int color)
        {
            if (color < 0) return 0;
            if (color > 255) return 255;
            return color;
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

        private void bw1_button_Click(object sender, RoutedEventArgs e)
        {
            System.Drawing.Color c;
            int newColor;

            for (int x = 0; x < CurrentBitmap.Width; x++)
            {
                for (int y = 0; y < CurrentBitmap.Height; y++)
                {
                    c = CurrentBitmap.GetPixel(x, y);

                    newColor = (int)((c.R + c.G + c.B) / 3);

                    CurrentBitmap.SetPixel(x, y, System.Drawing.Color.FromArgb(0, NormaliseColor(newColor), NormaliseColor(newColor),
                        NormaliseColor(newColor)));
                }
            }

            currentImage.Source = fileLoader.ConvertBitmap(CurrentBitmap);
        }

        private void bw2_button_Click(object sender, RoutedEventArgs e)
        {
            System.Drawing.Color c;
            int newColor;

            for (int x = 0; x < CurrentBitmap.Width; x++)
            {
                for (int y = 0; y < CurrentBitmap.Height; y++)
                {
                    c = CurrentBitmap.GetPixel(x, y);

                    newColor = (int)(0.3 * c.R + 0.59 * c.G + 0.11 * c.B);

                    CurrentBitmap.SetPixel(x, y, System.Drawing.Color.FromArgb(0, NormaliseColor(newColor), NormaliseColor(newColor),
                        NormaliseColor(newColor)));
                }
            }

            currentImage.Source = fileLoader.ConvertBitmap(CurrentBitmap);
        }

        private void AveragingFilter()
        {
            if (CurrentBitmap == null) return;

            var copy = new Bitmap(CurrentBitmap);

            List<System.Drawing.Color> c = new List<System.Drawing.Color>();

            RGBColor average;

            for (int x = 0; x < CurrentBitmap.Width; x++)
            {
                for (int y = 0; y < CurrentBitmap.Height; y++)
                {
                    if (x - 1 >= 0 && y + 1 < CurrentBitmap.Height) c.Add(copy.GetPixel(x - 1, y + 1));
                    if (y + 1 < CurrentBitmap.Height) c.Add(copy.GetPixel(x, y + 1));
                    if (x + 1 < CurrentBitmap.Width && y + 1 < CurrentBitmap.Height) c.Add(copy.GetPixel(x + 1, y + 1));
                    if (x - 1 >= 0) c.Add(copy.GetPixel(x - 1, y));
                    c.Add(copy.GetPixel(x, y));
                    if (x + 1 < CurrentBitmap.Width) c.Add(copy.GetPixel(x + 1, y));
                    if (x - 1 >= 0 && y - 1 >= 0) c.Add(copy.GetPixel(x - 1, y - 1));
                    if (y - 1 >= 0) c.Add(CurrentBitmap.GetPixel(x, y - 1));
                    if (x + 1 < CurrentBitmap.Width && y - 1 >= 0) c.Add(CurrentBitmap.GetPixel(x + 1, y - 1));

                    average = CalculateAverage(c);

                    CurrentBitmap.SetPixel(x, y, System.Drawing.Color.FromArgb(0, NormaliseColor(average.R), NormaliseColor(average.G),
                        NormaliseColor(average.B)));

                    c.Clear();
                }
            }

            currentImage.Source = fileLoader.ConvertBitmap(CurrentBitmap);
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

        private void stretch_histogram_Click(object sender, RoutedEventArgs e)
        {
            RGBColor min = new RGBColor(), max = new RGBColor();
            min.R = min.G = min.B = 255;
            max.R = max.G = max.B = 0;

            System.Drawing.Color c;

            for (int x = 0; x < CurrentBitmap.Width; x++)
            {
                for (int y = 0; y < CurrentBitmap.Height; y++)
                {
                    c = CurrentBitmap.GetPixel(x, y);

                    if (c.R > max.R) max.R = c.R;
                    if (c.G > max.G) max.G = c.G;
                    if (c.B > max.B) max.B = c.B;
                    if (c.R < min.R) min.R = c.R;
                    if (c.G < min.G) min.G = c.G;
                    if (c.B < min.B) min.B = c.B;
                }
            }

            for (int x = 0; x < CurrentBitmap.Width; x++)
            {
                for (int y = 0; y < CurrentBitmap.Height; y++)
                {
                    c = CurrentBitmap.GetPixel(x, y);

                    CurrentBitmap.SetPixel(x, y, System.Drawing.Color.FromArgb(0, StretchColor(min.R, max.R, c.R),
                        StretchColor(min.G, max.G, c.G), StretchColor(min.B, max.B, c.B)));
                }
            }

            currentImage.Source = fileLoader.ConvertBitmap(CurrentBitmap);
            UpdateHistogram();
        }

        private int StretchColor(int min, int max, byte color)
        {
            return NormaliseColor((int)((255 / (double)(max - min)) * (color - min)));
        }

        private void equalize_histogram_Click(object sender, RoutedEventArgs e)
        {
            double sumR = 0, sumG = 0, sumB = 0;

            var r = new int[256];
            var g = new int[256];
            var b = new int[256];

            var Dr = new double[256];
            var Dg = new double[256];
            var Db = new double[256];

            System.Drawing.Color c;

            for (int x = 0; x < CurrentBitmap.Width; x++)
            {
                for (int y = 0; y < CurrentBitmap.Height; y++)
                {
                    c = CurrentBitmap.GetPixel(x, y);
                    r[c.R]++;
                    g[c.G]++;
                    b[c.B]++;
                }
            }

            var numberOfPixels = CurrentBitmap.Width * CurrentBitmap.Height;
            
            for(int i = 0; i < 256; i++)
            {
                sumR += (double)r[i]/numberOfPixels;
                sumB += (double)b[i]/numberOfPixels;
                sumG += (double)g[i]/numberOfPixels;

                Dr[i] += sumR;
                Dg[i] += sumG;
                Db[i] += sumB;
            }

            var LUTr = LUTEqualization(Dr);
            var LUTg = LUTEqualization(Dg);
            var LUTb = LUTEqualization(Db);

            for (int x = 0; x < CurrentBitmap.Width; x++)
            {
                for (int y = 0; y < CurrentBitmap.Height; y++)
                {
                    c = CurrentBitmap.GetPixel(x, y);

                    CurrentBitmap.SetPixel(x, y, System.Drawing.Color.FromArgb(0, NormaliseColor(LUTr[c.R]), 
                        NormaliseColor(LUTg[c.G]), NormaliseColor(LUTb[c.B])));
                }
            }

            currentImage.Source = fileLoader.ConvertBitmap(CurrentBitmap);
            UpdateHistogram();
        }

        private int[] LUTEqualization(double[] Dcolor)
        {
            var LUT = new int[256];
            double D0;

            int i = 0;
            while (Dcolor[i] == 0) i++;
            D0 = Dcolor[i];

            for (i = 0; i < 256; i++)
            {
                LUT[i] = (int)(((Dcolor[i] - D0) / (1 - D0)) * (256 - 1));
            }

            return LUT;
        }

        private void apply_Click(object sender, RoutedEventArgs e)
        {
            var selectedOption = filter_selection.SelectedIndex;
            var threshold = string.IsNullOrWhiteSpace(threshold_input.Text) ? "100" : threshold_input.Text;

            switch (selectedOption)
            {
                case 0:
                    BinarizationManual(int.Parse(threshold));
                    break;
                case 1:
                    BinarizationBlackPercentage(int.Parse(threshold));
                    break;
                case 2:
                    BinarizationMeanSelection();
                    break;
            }

            UpdateHistogram();
        }

        private void Binarize(Bitmap bitmap, int threshold)
        {
            for (int x = 0; x < bitmap.Width; x++)
            {
                for (int y = 0; y < bitmap.Height; y++)
                {
                    var color = bitmap.GetPixel(x, y).GetGrayScaleColor();
                    if (color.R < threshold)
                        color = Color.FromArgb(0, 0, 0);
                    else
                        color = Color.FromArgb(255, 255, 255);

                    bitmap.SetPixel(x, y, color);
                }
            }
        }

        private void BinarizationManual(int threshold)
        {
            Binarize(CurrentBitmap, threshold);

            currentImage.Source = fileLoader.ConvertBitmap(CurrentBitmap);
        }

        private void BinarizationBlackPercentage(int percentage)
        {
            List<int> grayScaleArray = new(256);
            for (int i = 0; i < 256; i++)
            {
                grayScaleArray.Add(0);
            }

            for (int x = 0; x < CurrentBitmap.Width; x++)
            {
                for (int y = 0; y < CurrentBitmap.Height; y++)
                {
                    var colorIndex = CurrentBitmap.GetPixel(x, y).GetGrayScale();
                    grayScaleArray[colorIndex]++;
                }
            }

            var pixelsCount = CurrentBitmap.Width * CurrentBitmap.Height;
            var sum = 0;

            var threshold = 0;

            for (int i = 0; i < 256; i++)
            {
                if (sum >= (pixelsCount * ((float)percentage / 100)))
                {
                    threshold = i;
                    break;
                }

                sum += grayScaleArray[i];
            }

            Binarize(CurrentBitmap, threshold);

            currentImage.Source = fileLoader.ConvertBitmap(CurrentBitmap);
        }

        private void BinarizationMeanSelection()
        {
            int threshold = 100;
            var meanOne = threshold;
            var meanTwo = threshold;
            var done = false;
            while (!done)
            {
                var memOneCount = 0;
                var memTwoCount = 0;
                var memOneSum = 0;
                var memTwoSum = 0;

                for (var x = 0; x < CurrentBitmap.Width; x++)
                {
                    for (var y = 0; y < CurrentBitmap.Height; y++)
                    {
                        var colorGrayScale = CurrentBitmap.GetPixel(x, y).GetGrayScale();
                        if (colorGrayScale < threshold)
                        {
                            memOneCount++;
                            memOneSum += colorGrayScale;
                        }
                        else
                        {
                            memTwoCount++;
                            memTwoSum += colorGrayScale;
                        }
                    }
                }

                var firstMean = memOneSum / memOneCount;
                var secondMean = memTwoSum / memTwoCount;

                if (meanOne == firstMean && meanTwo == secondMean)
                {
                    done = true;
                    break;
                }
                else
                {
                    meanOne = firstMean;
                    meanTwo = secondMean;
                    threshold = (meanOne + meanTwo) / 2;
                }
            }

            Binarize(CurrentBitmap, threshold);

            currentImage.Source = fileLoader.ConvertBitmap(CurrentBitmap);
        }

        private void reset_Click(object sender, RoutedEventArgs e)
        {
            if (bitmapBackup != null)
            {
                currentImage.Source = fileLoader.ConvertBitmap(bitmapBackup);
                CurrentBitmap = new Bitmap(bitmapBackup);
            }
                
        }


    }
}
