//------------------------------------------------------------------------------
// <summary>
// Record positions, postures, joints, RSSIs
// </summary>
// <author> Dongyang Yao (dongyang.yao@rutgers.edu) </author>
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Kinect;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Windows;
using System.Drawing;
using PeopleTrackingGui;

namespace ActivityRecognition
{
    class Record
    {
        /// <summary>
        /// The file name
        /// </summary>
        public static string StartTime = DateTime.Now.ToString("M-d-yyyy_HH-mm-ss");

        

        /// <summary>
        /// Record positions of each person
        /// </summary>
        /// <param name="persons"></param>
        public static void RecordPosition(Person[] persons)
        {
            string dir = @"" + PeopleTrackingGui.Properties.Resources.DirectoryPosition;

            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            string fileName = @"" + dir + StartTime + ".txt";
            string time = DateTime.Now.ToString(@"yyyy-MM-dd HH:mm:ss:fff");

            using (StreamWriter writer = new StreamWriter(fileName, true))
            {
                foreach (Person person in persons)
                {
                    if (person.IsTracked)
                        writer.WriteLine("{0}, {1}, {2}, {3}", person.ID, time, person.Position.X+450, person.Position.Y);
                }
            }
        }

        /// <summary>
        /// Record joints of each person
        /// </summary>
        /// <param name="bodies"></param>
        /// <param name="isPositiveJoints"></param>
        public static void RecordJoints(Body[] bodies, bool isPositiveJoints)
        {
            string dir = @"" + PeopleTrackingGui.Properties.Resources.DirectoryJoint;

            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            string fileName = @"" + dir + StartTime + ".txt";
            string time = DateTime.Now.ToString(@"HH:mm:ss");

            using (StreamWriter writer = new StreamWriter(fileName, true))
            {
                foreach (Body body in bodies)
                {
                    if (body.IsTracked)
                    {
                        writer.WriteLine("{0} {1} {2} {3} {4} {5} {6} {7} {8} {9} {10} {11} {12} {13} {14} {15} {16} {17} {18} {19} {20} {21} {22} {23} {24} {25} {26} {27} {28} {29} {30} {31} {32} {33} {34} {35} {36} {37} {38} {39} {40} {41} {42} {43} {44} {45} {46} {47} {48} {49} {50} {51} {52} {53} {54} {55} {56} {57} {58} {59} {60} {61} {62} {63} {64} {65} {66} {67} {68} {69} {70} {71} {72} {73} {74} {75} {76} {77}",
                            isPositiveJoints ? 1 : 0, MainWindow.TILT_ANGLE, body.TrackingId,
                            body.Joints[JointType.AnkleLeft].Position.X, body.Joints[JointType.AnkleLeft].Position.Y, body.Joints[JointType.AnkleLeft].Position.Z,
                            body.Joints[JointType.AnkleRight].Position.X, body.Joints[JointType.AnkleRight].Position.Y, body.Joints[JointType.AnkleRight].Position.Z,
                            body.Joints[JointType.ElbowLeft].Position.X, body.Joints[JointType.ElbowLeft].Position.Y, body.Joints[JointType.ElbowLeft].Position.Z,
                            body.Joints[JointType.ElbowRight].Position.X, body.Joints[JointType.ElbowRight].Position.Y, body.Joints[JointType.ElbowRight].Position.Z,
                            body.Joints[JointType.FootLeft].Position.X, body.Joints[JointType.FootLeft].Position.Y, body.Joints[JointType.FootLeft].Position.Z,
                            body.Joints[JointType.FootRight].Position.X, body.Joints[JointType.FootRight].Position.Y, body.Joints[JointType.FootRight].Position.Z,
                            body.Joints[JointType.HandLeft].Position.X, body.Joints[JointType.HandLeft].Position.Y, body.Joints[JointType.HandLeft].Position.Z,
                            body.Joints[JointType.HandRight].Position.X, body.Joints[JointType.HandRight].Position.Y, body.Joints[JointType.HandRight].Position.Z,
                            body.Joints[JointType.HandTipLeft].Position.X, body.Joints[JointType.HandTipLeft].Position.Y, body.Joints[JointType.HandTipLeft].Position.Z,
                            body.Joints[JointType.HandTipRight].Position.X, body.Joints[JointType.HandTipRight].Position.Y, body.Joints[JointType.HandTipRight].Position.Z,
                            body.Joints[JointType.Head].Position.X, body.Joints[JointType.Head].Position.Y, body.Joints[JointType.Head].Position.Z,
                            body.Joints[JointType.HipLeft].Position.X, body.Joints[JointType.HipLeft].Position.Y, body.Joints[JointType.HipLeft].Position.Z,
                            body.Joints[JointType.HipRight].Position.X, body.Joints[JointType.HipRight].Position.Y, body.Joints[JointType.HipRight].Position.Z,
                            body.Joints[JointType.KneeLeft].Position.X, body.Joints[JointType.KneeLeft].Position.Y, body.Joints[JointType.KneeLeft].Position.Z,
                            body.Joints[JointType.KneeRight].Position.X, body.Joints[JointType.KneeRight].Position.Y, body.Joints[JointType.KneeRight].Position.Z,
                            body.Joints[JointType.Neck].Position.X, body.Joints[JointType.Neck].Position.Y, body.Joints[JointType.Neck].Position.Z,
                            body.Joints[JointType.ShoulderLeft].Position.X, body.Joints[JointType.ShoulderLeft].Position.Y, body.Joints[JointType.ShoulderLeft].Position.Z,
                            body.Joints[JointType.ShoulderRight].Position.X, body.Joints[JointType.ShoulderRight].Position.Y, body.Joints[JointType.ShoulderRight].Position.Z,
                            body.Joints[JointType.SpineBase].Position.X, body.Joints[JointType.SpineBase].Position.Y, body.Joints[JointType.SpineBase].Position.Z,
                            body.Joints[JointType.SpineMid].Position.X, body.Joints[JointType.SpineMid].Position.Y, body.Joints[JointType.SpineMid].Position.Z,
                            body.Joints[JointType.SpineShoulder].Position.X, body.Joints[JointType.SpineShoulder].Position.Y, body.Joints[JointType.SpineShoulder].Position.Z,
                            body.Joints[JointType.ThumbLeft].Position.X, body.Joints[JointType.ThumbLeft].Position.Y, body.Joints[JointType.ThumbLeft].Position.Z,
                            body.Joints[JointType.ThumbRight].Position.X, body.Joints[JointType.ThumbRight].Position.Y, body.Joints[JointType.ThumbRight].Position.Z,
                            body.Joints[JointType.WristLeft].Position.X, body.Joints[JointType.WristLeft].Position.Y, body.Joints[JointType.WristLeft].Position.Z,
                            body.Joints[JointType.WristRight].Position.X, body.Joints[JointType.WristRight].Position.Y, body.Joints[JointType.WristRight].Position.Z);
                    }
                }
            }
        }

        

        
    }
}
