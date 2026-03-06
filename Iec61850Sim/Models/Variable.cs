using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Iec61850Sim.Models
{
    internal class Point
    {
        public Point(string name, string address)
        {
            Address = address;
        }

        internal string Name { get; set; }
        internal string Address { get; set; }
        internal int Quality { get; set; } = 192;
        internal DateTime Timestamp { get; set; } = DateTime.Now;
    }

    internal class Digital : Point 
    {
        public Digital(string name, string address) : base(name, address) { }

        internal Int32 Value { get; set; }
    }

    internal class Analogic : Point
    {
        public Analogic(string name, string address) : base(name, address) { }

        internal float Value { get; set; }
    }

}
