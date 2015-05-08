using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerPinger
{
    public class WeightedRequest
    {
        public int Weight { get; set; }
        public Uri Uri { get; set; }
    }
}
