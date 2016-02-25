//------------------------------------------------------------------------------
// <summary>
// Description of a person
// </summary>
// <author> Dongyang Yao (dongyang.yao@rutgers.edu) </author>
//------------------------------------------------------------------------------

using System.Collections.Generic;

namespace ActivityRecognition
{
    public class Person
    {
        /// <summary>
        /// If it is tracked
        /// </summary>
        public bool IsTracked;

        /// <summary>
        /// Person ID
        /// </summary>
        public ulong ID;

        /// <summary>
        /// 2D top view position
        /// </summary>
        public System.Windows.Point Position; // Centimeter

        /// <summary>
        /// Original 2D top view position
        /// </summary>
        public System.Windows.Point OldPosition; // Centimeter

        /// <summary>
        /// Body orientation of the person
        /// </summary>
        public BodyOrientation.Orientations Orientation;

        /// <summary>
        /// Person color on foreground of top view
        /// </summary>
        public System.Windows.Media.Brush Color;

        /// <summary>
        /// The activities the person is performing
        /// </summary>
        public string Activities;

        public double height;

        public bool matched;

        public string tagId="unKnown";

        public double tagSkeletonDis=-1;

        public double dis = -1;



        /// <summary>
        /// Constructor
        /// </summary>
        public Person()
        {
            IsTracked = false;
            ID = 0;
            Position = new System.Windows.Point(0.0, 0.0);
            Orientation = BodyOrientation.Orientations.Front;
            Color = System.Windows.Media.Brushes.Red;
            Activities = "";
           
        }
    }
}
