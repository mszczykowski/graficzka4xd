using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using wpf2.Enums;
using wpf2.Helpers;
using wpf2.Models;

namespace wpf2
{
    internal sealed class PPMFileLoader
    {
        public PPMFileParameters PPMFileParameters => ppmFileParameters;
        public byte[] Pixels => pixels.ToArray();
        public bool IsFileValid => isFileValid;

        private PPMFileParameters ppmFileParameters;
        private List<byte> pixels;
        private FileStream file;
        private int iteration;
        private bool isFileValid;
        private static Regex regex = new(@"\s+");
        public PPMFileLoader(FileStream file)
        {
            ppmFileParameters = new PPMFileParameters();
            iteration = 0;
            isFileValid = true;
            pixels = new List<byte>();
            this.file = file;
        }
        
        public void ReadFile()
        {
            string line;
            int skip = 0;

            using (StreamReader reader = new StreamReader(file, Encoding.UTF8, false, 1024))
            {
                while ((line = reader.ReadLine()) != null)
                {
                    if (iteration < 4)
                    {
                        skip += line.Count();
                    }

                    line = RemoveWhiteSpace(line);

                    var inputs = line.Split(" ");

                    foreach (var input in inputs)
                    {
                        if (IsComent(input)) break;
                        if (string.IsNullOrEmpty(input)) continue;

                        if (iteration < 4) PopulateFileParameters(input);

                        else
                        {
                            if (ppmFileParameters.Type == FileType.P3) PopulatePixelDataText(input);
                            else
                            {
                                PopulatePixelDataBinary(skip);
                                return;
                            }
                        }
                    }
                    skip++;
                    if (!isFileValid) break;
                }
            }
        }

        public void PopulatePixelDataBinary(int skip)
        {
            file.Position = skip;
            byte[] buffor = new byte[129];
            while(file.Read(buffor, 0, buffor.Length) > 0)
            {
                pixels.AddRange(buffor);
            }
        }

        private void PopulateFileParameters(string input)
        {
            int numericValue = 0;
            if(iteration != 0)
            {
                if (!Int32.TryParse(input, out numericValue))
                {
                    isFileValid = false;
                    return;
                }
            }

            switch (iteration)
            {
                case 0:
                    if (input == "P3")
                    {
                        ppmFileParameters.Type = FileType.P3;
                    }
                    else if (input == "P6")
                    {
                        ppmFileParameters.Type = FileType.P6;
                    }
                    else isFileValid = false;
                    iteration++;
                    break;
                case 1:
                    ppmFileParameters.Width = numericValue;
                    iteration++;
                    break;
                case 2:
                    ppmFileParameters.Height = numericValue;
                    iteration++;
                    break;
                case 3:
                    ppmFileParameters.BitsPerPixel = (int)Math.Log(numericValue, 2) + 1; ;
                    iteration++;
                    break;
            };
        }

        private void PopulatePixelDataText(string input)
        {
            int colorComponent = 0;
            if (!int.TryParse(input, out colorComponent))
            {
                isFileValid = false;
                return;
            }

            var bytes = BitConverter.GetBytes(colorComponent);

            for (int i = 0; i < ppmFileParameters.BitsPerPixel/8; i++)
            {
                pixels.Add(bytes[i]);
            }
        }

        private bool IsComent(string input)
        {
            if (string.IsNullOrEmpty(input)) return false;

            return input == "#" || input[0] == '#';
        }


        private string RemoveWhiteSpace(string input)
        {
            if (!string.IsNullOrEmpty(input)) 
                return string.Join(" ", input.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries));

            return input;
        }
    }
}
