using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PeopleTrackingGui
{
    class RfidVelocity
    {
        public Dictionary<DateTime, double> velocity;

        public RfidVelocity() {
            velocity = new Dictionary<DateTime, double>();
        }
    }
}
