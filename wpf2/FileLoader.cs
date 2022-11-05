using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using wpf2.Enums;
using wpf2.Models;

namespace wpf2
{
    internal class FileLoader
    {
        public Bitmap Bitmap { get; private set; }
        public ImageSource LoadFile(string filePath)
        {
            PPMFileLoader loader;

            try
            {
                using (var fs = File.OpenRead(filePath))
                {
                    string fileExt = Path.GetExtension(filePath).ToLower();

                    if (fileExt == ".jpg" || fileExt == ".jpeg")
                    {
                        Bitmap = new Bitmap(fs);
                        return ConvertBitmap(Bitmap);
                    }
                    else if (fileExt == ".ppm")
                    {
                        loader = new PPMFileLoader(fs);
                        loader.ReadFile();
                        if (!loader.IsFileValid) return null;

                        var writeableBitmap = GenerateBitmap(loader.PPMFileParameters, loader.Pixels);

                        Bitmap = BitmapFromWriteableBitmap(writeableBitmap);
                        return writeableBitmap;
                    }
                    else
                    {
                        return null;
                    }
                }
            }
            catch(Exception ex)
            {
                return null;
            }
        }

        private WriteableBitmap GenerateBitmap(PPMFileParameters parameters, byte[] pixelData)
        {
            int bytesPerPixel = parameters.BitsPerPixel * 3 / 8;
            WriteableBitmap bitmap = new WriteableBitmap(parameters.Width,
                parameters.Height, 96d, 96d, GetPixelFormat(bytesPerPixel), null);

            bitmap.Lock();
            bitmap.WritePixels(new Int32Rect(0, 0, parameters.Width, parameters.Height),
                pixelData.ToArray(), bytesPerPixel * parameters.Width, 0, 0);
            bitmap.Unlock();
            return bitmap;
        }

        private PixelFormat GetPixelFormat(int bytePerPixel)
        {
            switch (bytePerPixel)
            {
                case 3:
                    return PixelFormats.Rgb24;
                case 6:
                    return PixelFormats.Rgb48;
            }
            return PixelFormats.Rgb24;
        }

        public BitmapImage ConvertBitmap(Bitmap bitmap)
        {
            MemoryStream ms = new MemoryStream();
            bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Bmp);
            BitmapImage image = new BitmapImage();
            image.BeginInit();
            ms.Seek(0, SeekOrigin.Begin);
            image.StreamSource = ms;
            image.EndInit();

            return image;
        }

        private System.Drawing.Bitmap BitmapFromWriteableBitmap(WriteableBitmap writeBmp)
        {
            System.Drawing.Bitmap bmp;
            using (MemoryStream outStream = new MemoryStream())
            {
                BitmapEncoder enc = new BmpBitmapEncoder();
                enc.Frames.Add(BitmapFrame.Create((BitmapSource)writeBmp));
                enc.Save(outStream);
                bmp = new System.Drawing.Bitmap(outStream);
            }
            return bmp;
        }
    }
}
