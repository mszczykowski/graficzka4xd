using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace wpf2.Models
{
    internal sealed class RGBColorDouble
    {
        public double R { get; set; }
        public double G { get; set; }
        public double B { get; set; }

        public RGBColorDouble(double r, double g, double b)
        {
            R = r;
            G = g;
            B = b;
        }
    }
}
