//------------------------------------------------------------------------------
// <summary>
// Collection of plot methods
// </summary>
// <author> Dongyang Yao (dongyang.yao@rutgers.edu) </author>
//------------------------------------------------------------------------------

using System.Windows.Shapes;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows;
using System;
using System.Collections.Generic;
using Microsoft.Kinect;


namespace ActivityRecognition
{
    public class Plot
    {
        /// <summary>
        /// Width of Kinect label on top view canvas
        /// </summary>
        public static readonly float KinectWidth = 40;

        /// <summary>
        /// Height of Kinect label on top view canvas
        /// </summary>
        public static readonly float KinectHeight = 16;

        /// <summary>
        /// Ellipse diameter for person on top view canvas
        /// </summary>
        public static readonly double EllipseDiameter = 20;

        /// <summary>
        /// Text size on top view canvas
        /// </summary>
        public static readonly double FontSize = 20;

        /// <summary>
        /// Length for body orientation shape on top view canvas
        /// </summary>
        public static readonly double TriangleSideLength = 10;

        /// <summary>
        /// Drawing thickness for joint
        /// </summary>
        public static readonly double JointThickness = 3;

        /// <summary>
        /// Drawing thickness for head rectangle
        /// </summary>
        public static readonly double HeadRectThickness = 2;

        /// <summary>
        /// Max reliable distance from Kinect on depth frame
        /// </summary>
        public static readonly float MaxReliableDistance = 450; // Centimeter

        /// <summary>
        /// Min reliable distance from Kinect on depth frame
        /// </summary>
        public static readonly float MinReliableDistance = 50; // Centimeter

        /// <summary>
        /// Corrected Z position
        /// </summary>
        public static readonly float InferredZPositionClamp = 0.1f;

        /// <summary>
        /// Colors for six persons
        /// </summary>
        public static readonly Brush[] BodyColors = {
                                              Brushes.Red,
                                              Brushes.Orange,
                                              Brushes.Green,
                                              Brushes.Blue,
                                              Brushes.Pink,
                                              Brushes.Purple
                                          };

        /// <summary>
        /// Draw rectangle around head on depth view
        /// </summary>
        /// <param name="coordinateMapper"></param>
        /// <param name="color"></param>
        /// <param name="neckJointPosition"></param>
        /// <param name="headJointPosition"></param>
        /// <param name="drawingContext"></param>
        /// <param name="kinectSensor"></param>
        public static void DrawTrackedHead(CoordinateMapper coordinateMapper, Brush color, CameraSpacePoint neckJointPosition, CameraSpacePoint headJointPosition, DrawingContext drawingContext, KinectSensor kinectSensor)
        {
            DepthSpacePoint neckDepthPoint = CameraSpacePointToDepthSpace(coordinateMapper, neckJointPosition);
            DepthSpacePoint headDepthPoint = CameraSpacePointToDepthSpace(coordinateMapper, headJointPosition);
            double size = Math.Abs(headDepthPoint.Y - neckDepthPoint.Y) * 2;

            if (headDepthPoint.X >= 0 && headDepthPoint.X < kinectSensor.DepthFrameSource.FrameDescription.Width
                    && headDepthPoint.Y >= 0 && headDepthPoint.Y <= kinectSensor.DepthFrameSource.FrameDescription.Height
                    && size <= kinectSensor.DepthFrameSource.FrameDescription.Height)
            {
                Rect rect = new Rect(new Point(headDepthPoint.X - size / 2, headDepthPoint.Y - size / 2), new Size(size, size));
                drawingContext.DrawRoundedRectangle(null, new Pen(color, HeadRectThickness), rect, JointThickness, JointThickness);
            }
        }

        /// <summary>
        /// Draw system status label on top view canvas
        /// </summary>
        /// <param name="canvas"></param>
        /// <param name="isSystemOn"></param>
        public static void DrawSystemStatus(Canvas canvas, bool isSystemOn)
        {
            DrawEllipse(30, 30, 0, 0, isSystemOn ? Brushes.Green : Brushes.Red, canvas);
        }

        /// <summary>
        /// Draw persons' activities on top view canvas
        /// </summary>
        /// <param name="person"></param>
        /// <param name="canvas"></param>
        public static void DrawActivitiesOnCanvas(Person person, Canvas canvas)
        {
            Point point = Transformation.ConvertGroundPlaneToCanvas(new Point(person.Position.X + EllipseDiameter / 2, person.Position.Y - EllipseDiameter / 2), canvas);
            DrawText(person.Activities, FontSize, point.X, point.Y, Brushes.Black, canvas);
        }

        /// <summary>
        /// Draw posture text on top view canvas
        /// </summary>
        /// <param name="person"></param>
        /// <param name="gestures"></param>
        /// <param name="canvas"></param>
        public static void DrawGesturesOnCanvas(Person person, Dictionary<string, float> gestures, Canvas canvas)
        {
            Point point = Transformation.ConvertGroundPlaneToCanvas(new Point(person.Position.X + EllipseDiameter / 2, person.Position.Y - EllipseDiameter / 2), canvas);

            string text = null;
            foreach (var gesture in gestures)
            {
                text += gesture.Key + "(" + gesture.Value + ")\n";
            }

            // Bug: gesture text will cover the activity text
            DrawText(text, FontSize, point.X, point.Y, Brushes.Black, canvas);
        }

        /// <summary>
        /// Convert camera space point to depth space
        /// </summary>
        /// <param name="coordinateMapper"></param>
        /// <param name="cameraSpacePoint"></param>
        /// <returns></returns>
        public static DepthSpacePoint CameraSpacePointToDepthSpace(CoordinateMapper coordinateMapper, CameraSpacePoint cameraSpacePoint)
        {
            DepthSpacePoint depthPoint;

            if (cameraSpacePoint.Z < 0)
            {
                CameraSpacePoint inferredPoint = new CameraSpacePoint();
                inferredPoint.X = cameraSpacePoint.X;
                inferredPoint.Y = cameraSpacePoint.Y;
                inferredPoint.Z = InferredZPositionClamp;
                depthPoint = coordinateMapper.MapCameraPointToDepthSpace(inferredPoint);
            }
            else
            {
                depthPoint = coordinateMapper.MapCameraPointToDepthSpace(cameraSpacePoint);
            }

            return depthPoint;
        }

        /// <summary>
        /// Draw joints on depth view
        /// </summary>
        /// <param name="coordinateMapper"></param>
        /// <param name="jointPoints"></param>
        /// <param name="drawingContext"></param>
        /// <param name="kinectSensor"></param>
        public static void DrawJointsOnDepth(CoordinateMapper coordinateMapper, LinkedList<CameraSpacePoint> jointPoints, DrawingContext drawingContext, KinectSensor kinectSensor)
        {
            foreach (CameraSpacePoint point in jointPoints)
            {
                DepthSpacePoint depthPoint = CameraSpacePointToDepthSpace(coordinateMapper, point);

                if (depthPoint.X >= 0 && depthPoint.X < kinectSensor.DepthFrameSource.FrameDescription.Width
                    && depthPoint.Y >= 0 && depthPoint.Y <= kinectSensor.DepthFrameSource.FrameDescription.Height)
                    drawingContext.DrawEllipse(Brushes.Gold, null, new Point(depthPoint.X, depthPoint.Y), JointThickness, JointThickness);
            }
        }

        /// <summary>
        /// Draw people on top view canvas
        /// </summary>
        /// <param name="diameter"></param>
        /// <param name="headPositionGroundX"></param>
        /// <param name="headPositionGroundY"></param>
        /// <param name="brush"></param>
        /// <param name="canvas"></param>
        public static void DrawPeopleFromKinectOnCanvas(double diameter, double headPositionGroundX, double headPositionGroundY, Brush brush, Canvas canvas)
        {
            Point point = Transformation.ConvertGroundPlaneToCanvas(new Point(headPositionGroundX - diameter / 2, headPositionGroundY + diameter / 2), canvas);  // Centimeter
            DrawEllipse(diameter, diameter, point.X, point.Y, brush, canvas);
        }

        /// <summary>
        /// Draw body orientation on top view canvas
        /// </summary>
        /// <param name="diameter"></param>
        /// <param name="size"></param>
        /// <param name="orientation"></param>
        /// <param name="headPositionGroundX"></param>
        /// <param name="headPositionGroundY"></param>
        /// <param name="brush"></param>
        /// <param name="canvas"></param>
        public static void DrawOrientationOnCanvas(double diameter, double size, BodyOrientation.Orientations orientation, double headPositionGroundX, double headPositionGroundY, Brush brush, Canvas canvas)
        {
            if (orientation == BodyOrientation.Orientations.Front)
            {
                Point point1 = Transformation.ConvertGroundPlaneToCanvas(new Point(headPositionGroundX + size / 2, headPositionGroundY - diameter / 2), canvas);
                Point point2 = Transformation.ConvertGroundPlaneToCanvas(new Point(headPositionGroundX - size / 2, headPositionGroundY - diameter / 2), canvas);
                Point point3 = Transformation.ConvertGroundPlaneToCanvas(new Point(headPositionGroundX, headPositionGroundY - diameter / 2 - Math.Cos(30 / 180 * Math.PI) * size), canvas);
                DrawTriangle(point1, point2, point3, brush, canvas);
            }
            else if (orientation == BodyOrientation.Orientations.Left)
            {
                Point point1 = Transformation.ConvertGroundPlaneToCanvas(new Point(headPositionGroundX - diameter / 2, headPositionGroundY + size / 2), canvas);
                Point point2 = Transformation.ConvertGroundPlaneToCanvas(new Point(headPositionGroundX - diameter / 2, headPositionGroundY - size / 2), canvas);
                Point point3 = Transformation.ConvertGroundPlaneToCanvas(new Point(headPositionGroundX - diameter / 2 - Math.Cos(30 / 180 * Math.PI) * size, headPositionGroundY), canvas);
                DrawTriangle(point1, point2, point3, brush, canvas);
            }
            else if (orientation == BodyOrientation.Orientations.Right)
            {
                Point point1 = Transformation.ConvertGroundPlaneToCanvas(new Point(headPositionGroundX + diameter / 2, headPositionGroundY + size / 2), canvas);
                Point point2 = Transformation.ConvertGroundPlaneToCanvas(new Point(headPositionGroundX + diameter / 2, headPositionGroundY - size / 2), canvas);
                Point point3 = Transformation.ConvertGroundPlaneToCanvas(new Point(headPositionGroundX + diameter / 2 + Math.Cos(30 / 180 * Math.PI) * size, headPositionGroundY), canvas);
                DrawTriangle(point1, point2, point3, brush, canvas);
            }
            else
            {
                Point point1 = Transformation.ConvertGroundPlaneToCanvas(new Point(headPositionGroundX + size / 2, headPositionGroundY + diameter / 2), canvas);
                Point point2 = Transformation.ConvertGroundPlaneToCanvas(new Point(headPositionGroundX - size / 2, headPositionGroundY + diameter / 2), canvas);
                Point point3 = Transformation.ConvertGroundPlaneToCanvas(new Point(headPositionGroundX, headPositionGroundY + diameter / 2 + Math.Cos(30 / 180 * Math.PI) * size), canvas);
                DrawTriangle(point1, point2, point3, brush, canvas);
            }
        }

        /// <summary>
        /// Draw body orientation's shaple represented
        /// </summary>
        /// <param name="point1"></param>
        /// <param name="point2"></param>
        /// <param name="point3"></param>
        /// <param name="brush"></param>
        /// <param name="canvas"></param>
        public static void DrawTriangle(Point point1, Point point2, Point point3, Brush brush, Canvas canvas)
        {
            Polygon triangle = new Polygon();
            PointCollection points = new PointCollection();
            points.Add(point1);
            points.Add(point2);
            points.Add(point3);
            triangle.Points = points;
            triangle.Fill = brush;
            canvas.Children.Add(triangle);
        }

        /// <summary>
        /// Draw text
        /// </summary>
        /// <param name="content"></param>
        /// <param name="size"></param>
        /// <param name="left"></param>
        /// <param name="top"></param>
        /// <param name="brush"></param>
        /// <param name="canvas"></param>
        public static void DrawText(string content, double size, double left, double top, Brush brush, Canvas canvas)
        {
            TextBlock tb = new TextBlock();
            tb.Text = content;
            tb.FontSize = size;
            tb.Foreground = brush;
            Canvas.SetTop(tb, top);
            Canvas.SetLeft(tb, left);
            canvas.Children.Add(tb);
        }

        /// <summary>
        /// Draw background - no refresh
        /// On top view canvas
        /// </summary>
        /// <param name="canvas"></param>
        public static void InitBackgroundCanvas(Canvas canvas)
        {
            // Draw Kinect
            DrawRectangle(KinectWidth, KinectHeight, (canvas.Width - KinectWidth) / 2, canvas.Height, Brushes.Black, canvas);
            DrawText("Kinect", 12, (canvas.Width - KinectWidth) / 2 + 3, canvas.Height, Brushes.Gold, canvas);
        }


        /// <summary>
        /// Draw line
        /// </summary>
        public static void DrawLine()
        {

        }

        /// <summary>
        /// Draw rectangle
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="left"></param>
        /// <param name="top"></param>
        /// <param name="brush"></param>
        /// <param name="canvas"></param>
        public static void DrawRectangle(double width, double height, double left, double top, Brush brush, Canvas canvas)
        {
            Rectangle rect = new Rectangle();
            rect.Fill = brush;
            rect.Width = width;
            rect.Height = height;
            Canvas.SetLeft(rect, left);
            Canvas.SetTop(rect, top);
            canvas.Children.Add(rect);
        }

        

        /// <summary>
        /// Draw ellipse
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="left"></param>
        /// <param name="top"></param>
        /// <param name="brush"></param>
        /// <param name="canvas"></param>
        public static void DrawEllipse(double width, double height, double left, double top, Brush brush, Canvas canvas)
        {
            Ellipse el = new Ellipse();
            el.Width = width;
            el.Height = height;
            el.Fill = brush;
            Canvas.SetTop(el, top);
            Canvas.SetLeft(el, left);
            canvas.Children.Add(el);
        }

        public static void DrawPeopleTagId(Person person,Canvas canvas) {
            Point point = Transformation.ConvertGroundPlaneToCanvas(new Point(person.Position.X + EllipseDiameter / 2, person.Position.Y - EllipseDiameter / 2), canvas);
            DrawText(person.tagId, FontSize, point.X, point.Y, Brushes.Black, canvas);

            Point point2 = Transformation.ConvertGroundPlaneToCanvas(new Point(person.Position.X + EllipseDiameter / 2, person.Position.Y - EllipseDiameter / 2+20), canvas);
            DrawText(person.dis.ToString(), FontSize, point2.X, point2.Y, Brushes.Black, canvas);
        }
    }
}
