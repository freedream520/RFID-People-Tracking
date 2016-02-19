//------------------------------------------------------------------------------
// <summary>
// Collection of transformation methods
// </summary>
// <author> Dongyang Yao, Yanyi Zhang </author>
//------------------------------------------------------------------------------

using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Kinect;
using System.Windows.Controls;
using System.IO;
using System.Runtime.InteropServices;
using System.Drawing.Imaging;
using System.Runtime.Serialization.Formatters.Binary;

namespace ActivityRecognition
{
    public static class Transformation
    {

        /// <summary>
        /// minimun displacement of person's position that make the position on graph change 
        /// </summary>
        public readonly static double MIN_DISPLACEMENT=10;

        /// <summary>
        /// Convert from Kinect Gound coordinate to canvas coordinate
        /// </summary>
        /// <param name="point"></param>
        /// <param name="canvas"></param>
        /// <returns></returns>
        public static Point ConvertGroundPlaneToCanvas(Point point, System.Windows.Controls.Canvas canvas)
        {
            return new Point(point.X + canvas.Width / 2, canvas.Height - point.Y);
        }

        /// <summary>
        /// Convert from Kinect Gound coordinate to canvas coordinate
        /// </summary>
        /// <param name="point"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public static Point ConvertGroundPlaneToCanvas(Point point, double width, double height)
        {
            return new Point(point.X + width / 2, height - point.Y);
        }

        /// <summary>
        /// Convert location in a 2D bitmap to index in a array
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public static int Convert2DToArray(int width, int height, float x, float y)
        {
            if (x < 0 || x >= width || y < 0 || y >= height) return -1;
            return (int)(x + 0.5f) + (int)(y + 0.5f) * width;
        }

        /// <summary>
        /// Convert point from ground space to ground plane - centimeter
        /// </summary>
        /// <param name="headPositionGround"></param>
        /// <param name="person"></param>
        public static void ConvertGroundSpaceToPlane(CameraSpacePoint headPositionGround, Person person)
        {
            Point newPoint = new Point(-headPositionGround.X * 100, headPositionGround.Z * 100);
            if (person.OldPosition == null || PointDistance(person.OldPosition, newPoint) >= MIN_DISPLACEMENT) {
                person.Position.X = newPoint.X;
                person.Position.Y = newPoint.Y;
                
                if (person.OldPosition == null)
                {
                    person.OldPosition = new Point(person.Position.X, person.Position.Y);
                }
                else {
                    person.OldPosition.X = person.Position.X;
                    person.OldPosition.Y = person.Position.Y;
                }
            }

            person.height = headPositionGround.Y * 100 + 230;

        }

        /// <summary>
        /// Convert point from ground space to ground plane - centimeter
        /// </summary>
        /// <param name="cameraPoint"></param>
        /// <returns></returns>
        public static Point ConvertGroundSpaceToPlane(CameraSpacePoint cameraPoint)
        {
            Point point = new Point();
            point.X = -cameraPoint.X * 100;
            point.Y = cameraPoint.Z * 100;
            return point;
        }

        /// <summary>
        /// Convert point from camera space to ground space
        /// </summary>
        /// <param name="tiltAngleDegree"></param>
        /// <param name="tiltDown"></param>
        /// <param name="headPositionCamera"></param>
        /// <returns></returns>
        public static CameraSpacePoint RotateBackFromTilt(double tiltAngleDegree, bool tiltDown, CameraSpacePoint headPositionCamera)
        {
            tiltAngleDegree = tiltDown ? -tiltAngleDegree : tiltAngleDegree;
            double tiltAngle = tiltAngleDegree / 180 * Math.PI;
            CameraSpacePoint headPositionGround = new CameraSpacePoint();
            headPositionGround.X = headPositionCamera.X;
            headPositionGround.Y = (float)(headPositionCamera.Z * Math.Sin(tiltAngle) + headPositionCamera.Y * Math.Cos(tiltAngle));
            headPositionGround.Z = (float)(headPositionCamera.Z * Math.Cos(tiltAngle) - headPositionCamera.Y * Math.Sin(tiltAngle));
            return headPositionGround;
        }

        /// <summary>
        /// Convert rotation quaternion to Euler angles in degrees
        /// </summary>
        /// <param name="rotQuaternion"></param>
        /// <param name="pitch"></param>
        /// <param name="yaw"></param>
        /// <param name="roll"></param>
        public static void QuaternionToRotationMatrix(Vector4 rotQuaternion, out double pitch, out double yaw, out double roll)
        {
            double x = rotQuaternion.X;
            double y = rotQuaternion.Y;
            double z = rotQuaternion.Z;
            double w = rotQuaternion.W;

            pitch = Math.Atan2(2 * ((y * z) + (w * x)), (w * w) - (x * x) - (y * y) + (z * z)) / Math.PI * 180.0;
            yaw = Math.Asin(2 * ((w * y) - (x * z))) / Math.PI * 180.0;
            roll = Math.Atan2(2 * ((x * y) + (w * z)), (w * w) + (x * x) - (y * y) - (z * z)) / Math.PI * 180.0;
        }

        

        /*
        public static System.Drawing.Bitmap GenerateDilatedBitmap(BitmapSource source) {

            System.Drawing.Bitmap bitmap = BitmapFromSource(source);

            System.Drawing.Rectangle cloneRect = new System.Drawing.Rectangle(0, 0, Convert.ToInt32(source.Width), Convert.ToInt32(source.Height));
            //System.Drawing.Bitmap bit = BytesToBmp(pixels, new System.Drawing.Size(width, height));

            //System.Drawing.Bitmap cloned=bitmap.Clone(cloneRect, System.Drawing.Imaging.PixelFormat.Format24bppRgb);

            //bitmap.Save(@"C:\Users\Dongyang\Desktop\cc.png");

            AForge.Imaging.Filters.Dilatation filter = new AForge.Imaging.Filters.Dilatation();

            return filter.Apply(bitmap.Clone(cloneRect, System.Drawing.Imaging.PixelFormat.Format24bppRgb));
        }
        */
        /// <summary>
        /// Convert depth frame to image source
        /// </summary>
        /// <param name="frame"></param>
        /// <param name="depthPixels"></param>
        /// <param name="doCopy"></param>
        /// <returns></returns>
        public static ImageSource ToBitmap(DepthFrame frame, ushort[] depthPixels, bool doCopy)
        {
            int width = frame.FrameDescription.Width;
            int height = frame.FrameDescription.Height;
            System.Windows.Media.PixelFormat format = PixelFormats.Bgr32;

            ushort minDepth = frame.DepthMinReliableDistance;
            ushort maxDepth = ushort.MaxValue;

            byte[] pixels = new byte[width * height * (format.BitsPerPixel + 7) / 8];

            if (doCopy) frame.CopyFrameDataToArray(depthPixels);

            int colorIndex = 0;
            for (int depthIndex = 0; depthIndex < depthPixels.Length; ++depthIndex)
            {
                ushort depth = depthPixels[depthIndex];

                //****************************************************************** depth/(8000/256) ??
                byte intensity = (byte)(depth >= minDepth && depth <= maxDepth ? (depth / (8000 / 256)) : 0);

                pixels[colorIndex++] = intensity;
                pixels[colorIndex++] = intensity;
                pixels[colorIndex++] = intensity;

                ++colorIndex;
            }

            int stride = width * format.BitsPerPixel / 8;

            //BitmapSource source = BitmapSource.Create(width, height, 96, 96, format, null, pixels, stride);

            //System.Drawing.Bitmap bitmap=BitmapSourceToBitmap(source);

            return BitmapSource.Create(width, height, 96, 96, format, null, pixels, stride); 
        }

        public static byte[] BmpToBytes(System.Drawing.Bitmap bmp)
        {
            MemoryStream ms = new MemoryStream();
            // Save to memory using the Jpeg format 
            bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Bmp);


            // read to end 
            byte[] bmpBytes = ms.GetBuffer();
            bmp.Dispose();
            ms.Close();


            return bmpBytes;
        }

        public static BitmapSource ConvertBitmapToSource(System.Drawing.Bitmap bitmap)
        {
            var bitmapData = bitmap.LockBits(
                new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height),
                System.Drawing.Imaging.ImageLockMode.ReadOnly, bitmap.PixelFormat);

            var bitmapSource = BitmapSource.Create(
                bitmapData.Width, bitmapData.Height, 96, 96, PixelFormats.Bgr24, null,
                bitmapData.Scan0, bitmapData.Stride * bitmapData.Height, bitmapData.Stride);

            bitmap.UnlockBits(bitmapData);
            return bitmapSource;
        }

        private static System.Drawing.Bitmap BytesToBmp(byte[] bmpBytes, System.Drawing.Size imageSize)
        {
            System.Drawing.Bitmap bmp = new System.Drawing.Bitmap(imageSize.Width, imageSize.Height);


            //BitmapData bData = new BitmapData();

            BitmapData bmpData = bmp.LockBits(new System.Drawing.Rectangle(0, 0, bmp.Width, bmp.Height),
                        ImageLockMode.ReadWrite,
                        System.Drawing.Imaging.PixelFormat.Format32bppRgb);
            //bmp.UnlockBits(bData);
            // Copy the bytes to the bitmap object 
            IntPtr ptr = bmpData.Scan0;

            // Declare an array to hold the bytes of the bitmap.
            //int bytes = Math.Abs(bmpData.Stride) * bmp.Height;
            byte[] rgbValues = new byte[bmpBytes.Length];

            // Copy the RGB values into the array.
            //System.Runtime.InteropServices.Marshal.Copy(ptr, rgbValues, 0, bytes);

            // Set every third value to 255. A 24bpp bitmap will look red.  
            for (int counter = 2; counter < rgbValues.Length; counter += 3)
                rgbValues[counter] = bmpBytes[counter];

            // Copy the RGB values back to the bitmap
            System.Runtime.InteropServices.Marshal.Copy(rgbValues, 0, ptr, rgbValues.Length);

            // Unlock the bits.
            bmp.UnlockBits(bmpData);

            //return bmpData;
            return bmp;
        }




        /// <summary>
        /// Count number of undefined points in a rectangle
        /// </summary>
        /// <param name="depthPixels"></param>
        /// <param name="center"></param>
        /// <param name="sideLength"></param>
        /// <param name="displayWidth"></param>
        /// <returns></returns>
        public static int CountZeroInRec(ushort[] depthPixels, DepthSpacePoint center, int sideLength, int displayWidth)
        {
            int zeroCount = 0;
            for (int row = (int)(center.Y - sideLength / 2); row < (int)(center.Y + sideLength / 2); ++row)
            {
                for (int col = (int)(center.X - sideLength / 2); col < (int)(center.X + sideLength / 2); ++col)
                {
                    int index = col + row * displayWidth;

                    if (index >= 0 && index < depthPixels.Length)
                    {
                        if ((int)depthPixels[col + row * displayWidth] == 0) zeroCount++;
                    }
                }
            }
            return zeroCount;
        }

        /// <summary>
        /// Get number of tracked person
        /// </summary>
        /// <param name="persons"></param>
        /// <returns></returns>
        public static int GetNumberOfPeople(Person[] persons)
        {
            int num = 0;
            foreach (Person person in persons)
            {
                if (person.IsTracked) num++;
            } 
            return num;
        }

        public static System.Drawing.Bitmap BitmapFromSource(BitmapSource bitmapsource)
        {
            System.Drawing.Bitmap bitmap;
            var outStream = new MemoryStream();
            BitmapEncoder enc = new BmpBitmapEncoder();

            enc.Frames.Add(BitmapFrame.Create(bitmapsource));
            enc.Save(outStream);

            using (var localBitmap = new System.Drawing.Bitmap(outStream))
            {
                bitmap = localBitmap.Clone(new System.Drawing.Rectangle(0, 0, localBitmap.Width, localBitmap.Height),
                       System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            }

            
            //bitmap = new System.Drawing.Bitmap(outStream);

            return bitmap;
        }

        
        /// <summary>
        /// change bitmap to 24rgb pixel format
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static System.Drawing.Bitmap ChangeBitmapTo24Rgb(System.Drawing.Bitmap source) {

            return source.Clone(new System.Drawing.Rectangle(0,0,source.Width,source.Height),System.Drawing.Imaging.PixelFormat.Format24bppRgb);
        }

        public static JpegBitmapEncoder transferDrawingImageToBitmap(UIElement source)
        {

            // var image = new Image { Source = drawingImage };
            //image.Height = drawingImage.Height;
            //image.Width = drawingImage.Width;
            Size size = source.RenderSize;


            var bitmap = new RenderTargetBitmap(Convert.ToInt32(size.Width), Convert.ToInt32(size.Height), 96, 96, PixelFormats.Pbgra32);

            //image.Measure(size);
            //image.Arrange(new Rect(size));

            bitmap.Render(source);

            //var encoder = new PngBitmapEncoder();
            //encoder.Frames.Add(BitmapFrame.Create(bitmap));

            JpegBitmapEncoder jpg = new JpegBitmapEncoder();
            jpg.Frames.Add(BitmapFrame.Create(bitmap));

            //string time = DateTime.Now.ToString(@"HH_mm_ss");

            //using (Stream stm = File.Create(@"C:\Users\Dongyang\Desktop\TEMP\"+ time + ".jpg"))
            //{
            //    jpg.Save(stm);
            //    stm.Close();
            //}

            return jpg;
        }

        /// <summary>
        /// rotate any angles
        /// </summary>
        /// <param name="bmp">original Bitmap</param>
        /// <param name="angle">angle to rotate</param>
        ///
        /// <returns>output Bitmap</returns>
        public static System.Drawing.Bitmap KiRotate(System.Drawing.Bitmap bmp, float angle)
        {
            int w = bmp.Width + 2;
            int h = bmp.Height + 2;

            System.Drawing.Bitmap tmp = new System.Drawing.Bitmap(w, h);
            System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(tmp);
            g.DrawImageUnscaled(bmp, 1, 1);
            g.Dispose();

            System.Drawing.Drawing2D.GraphicsPath path = new System.Drawing.Drawing2D.GraphicsPath();
            path.AddRectangle(new System.Drawing.RectangleF(0f, 0f, w, h));
            System.Drawing.Drawing2D.Matrix mtrx = new System.Drawing.Drawing2D.Matrix();
            mtrx.Rotate(angle);
            System.Drawing.RectangleF rct = path.GetBounds(mtrx);

            System.Drawing.Bitmap dst = new System.Drawing.Bitmap((int)rct.Width, (int)rct.Height);
            g = System.Drawing.Graphics.FromImage(dst);
            g.TranslateTransform(-rct.X, -rct.Y);
            g.RotateTransform(angle);
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBilinear;
            g.DrawImageUnscaled(tmp, 0, 0);
            g.Dispose();

            tmp.Dispose();

            return dst;
        }


        public static double PointDistance(Point p1, Point p2) {

            return Math.Sqrt(Math.Pow((p1.X-p2.X),2)+ Math.Pow((p1.Y - p2.Y), 2));
        }
         

    }
}
