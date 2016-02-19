using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace PeopleTrackingGui
{
    class SkeletonPosition
    {
        public ulong skeletonId;

        public Dictionary<DateTime, double> relDistance;

        public SkeletonPosition(ulong skeletonId)
        {
            this.skeletonId = skeletonId;
            relDistance = new Dictionary<DateTime, double>();
        }


    }
}
