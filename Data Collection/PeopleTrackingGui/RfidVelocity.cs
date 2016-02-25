using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PeopleTrackingGui
{
    public class RfidVelocity
    {
        public Dictionary<DateTime, double> distance;

        public RfidVelocity() {
            distance = new Dictionary<DateTime, double>();
        }
    }
}
