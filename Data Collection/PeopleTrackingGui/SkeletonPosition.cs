using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace PeopleTrackingGui
{
    public class SkeletonPosition
    {
        public ulong skeletonId;

        public Dictionary<DateTime, Dictionary<int,double>> relDistance;

        public SkeletonPosition(ulong skeletonId)
        {
            this.skeletonId = skeletonId;
            relDistance = new Dictionary<DateTime, Dictionary<int, double>>();
        }


    }
}
