using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using wpf2.Enums;

namespace wpf2.Models
{
    internal class PPMFileParameters
    {
        public FileType Type { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public int BitsPerPixel { get; set; }
    }
}
