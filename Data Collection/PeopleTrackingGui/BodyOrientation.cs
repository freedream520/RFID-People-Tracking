//------------------------------------------------------------------------------
// <summary>
// Enum for body orientation
// Methods for body orientation determination
// </summary>
// <author> Dongyang Yao (dongyang.yao@rutgers.edu) </author>
//------------------------------------------------------------------------------

using Microsoft.Kinect;
using System;

namespace ActivityRecognition
{
    public class BodyOrientation
    {
        /// <summary>
        /// General distance of two shoulders for person
        /// </summary>
        public static readonly double ShoulderDistance = 0.4064; // Meter

        /// <summary>
        /// Boundary between (front, back) and (left, right) orientation
        /// </summary>
        public static readonly double OrientationBoundary = 2;

        /// <summary>
        /// The distance of two shoulders when people is facing left or right
        /// </summary>
        public static double MaxShoulderZDifference = 0.274; // Defined

        /// <summary>
        /// Representation for each orientation
        /// </summary>
        public enum Orientations
        {
            Front = 0x01,  // 0001
            Left = 0x02,   // 0010
            Right = 0x04,  // 0100
            Back = 0x08    // 1000
        };

        /// <summary>
        /// Convert int representation to enum representation
        /// </summary>
        /// <param name="number"></param>
        /// <returns></returns>
        public static Orientations ConvertIntToOrientations(int number)
        {
            Orientations orientations = 0;
            if ((number & (int)Orientations.Front) != 0) orientations |= Orientations.Front;
            if ((number & (int)Orientations.Left) != 0) orientations |= Orientations.Left;
            if ((number & (int)Orientations.Right) != 0) orientations |= Orientations.Right;
            if ((number & (int)Orientations.Back) != 0) orientations |= Orientations.Back;
            return orientations;
        }

        /// <summary>
        /// Decide body orientation with body joints
        /// </summary>
        /// <param name="leftShoulder"></param>
        /// <param name="rightShoulder"></param>
        /// <param name="person"></param>
        /// <param name="zeroCount"></param>
        /// <param name="canvas"></param>
        public static void DecideOrientation(CameraSpacePoint leftShoulder, CameraSpacePoint rightShoulder, Person person, int zeroCount, System.Windows.Controls.Canvas canvas)
        {
            // Uncomment to automatically find the max distance of two shoulders
            //if (Math.Abs(leftShoulder.Z - rightShoulder.Z) > MaxShoulderZDifference) MaxShoulderZDifference = Math.Abs(leftShoulder.Z - rightShoulder.Z);
            if (Math.Abs(leftShoulder.Z - rightShoulder.Z) < (MaxShoulderZDifference / OrientationBoundary))
            {
                double y = person.Position.Y;
                double length = (canvas.Height - Plot.MinReliableDistance) / 4;
                if (y >= 0 && y <= length + Plot.MinReliableDistance)
                {
                    person.Orientation = zeroCount >= 1 ? Orientations.Back : Orientations.Front;
                }
                if (y >= length + Plot.MinReliableDistance && y <= length * 2 + Plot.MinReliableDistance)
                {
                    person.Orientation = zeroCount >= 2 ? Orientations.Back : Orientations.Front;
                }
                if (y >= length * 2 + Plot.MinReliableDistance && y <= length * 3 + Plot.MinReliableDistance)
                {
                    person.Orientation = zeroCount >= 30 ? Orientations.Back : Orientations.Front;
                }
                if (y >= length * 3 + Plot.MinReliableDistance && y <= length * 4 + Plot.MinReliableDistance)
                {
                    person.Orientation = zeroCount >= 55 ? Orientations.Back : Orientations.Front;
                }
            }
            else person.Orientation = leftShoulder.Z > rightShoulder.Z ? Orientations.Right : Orientations.Left;
        }
    }
}
