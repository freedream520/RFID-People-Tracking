using Microsoft.Kinect;
using Microsoft.Kinect.VisualGestureBuilder;
using System.Collections.Generic;
using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Controls;
using System.Windows.Input;
using System.Threading;
using ActivityRecognition;
using RFID_Beta_5;
using MathNet.Filtering.Median;
using System.IO;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Configuration;
//ddds
namespace PeopleTrackingGui
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// Tilt angle of Kinect
        /// </summary>
        public static readonly double TILT_ANGLE = Double.Parse(Properties.Resources.TiltAngle); // Degree

        /// <summary>
        /// Kinect sensor reference
        /// </summary>
        private KinectSensor kinectSensor;

        /// <summary>
        /// Reader for multi-source, e.g., depth, color, body
        /// </summary>
        private MultiSourceFrameReader multiSourceFrameReader;

        /// <summary>
        /// Status of Kinect connection
        /// Used for Kinect connection error handling
        /// </summary>
        bool isKinectConnected = false;

        /// <summary>
        /// Body joints raw info from Kinect body frame
        /// </summary>
        private Body[] bodies;

        /// <summary>
        /// Person info for activity recognition
        /// </summary>
        public static Person[] persons;

        /// <summary>
        /// Defined activities
        /// </summary>
        private LinkedList<Activity> activities;

        /// <summary>
        /// Binding source property for depth display
        /// </summary>
        public ImageSource DepthSource { get { return depthSource; } }

        /// <summary>
        /// Private variable for depth frame image
        /// </summary>
        private DrawingImage depthSource;

        /// <summary>
        /// Drawing group for depth display
        /// </summary>
        private DrawingGroup drawingGroup;

        /// <summary>
        /// Stored depth frame pixels
        /// </summary>
        private ushort[] depthFramePixels;

        /// <summary>
        /// Mouse status for activity area selection
        /// </summary>
        private bool isMouseDown = false;

        /// <summary>
        /// Mouse start position for activity area selection
        /// </summary>
        private Point mouseDownPosition;

        /// <summary>
        /// Mouse end position for activity area selection
        /// </summary>
        private Point mouseUpPosition;

        /// <summary>
        /// Status for activity, position, RFID recording
        /// </summary>
        private bool isRecording = false;

        /// <summary>
        /// Turn off this boolean if no RFID system available
        /// Only use Kinect to recognize activity
        /// No object use detection
        /// </summary>
        private bool isRFIDRequired = true;

        ///// <summary>
        ///// A thread for RFID system
        ///// </summary>
        //private Thread rfidThread;

        /// <summary>
        /// RFID reader reference
        /// </summary>
        private RFID rfidReader;





        /// <summary>
        /// Timer for checking Kinect connection when starting the application
        /// </summary>
        System.Timers.Timer kinectConnectionCheck;
        private static readonly double KINECT_CONNECTION_CHECK_INTERVAL = 1000 * 2; // Millisecond

        /// <summary>
        /// Latency for stop recording
        /// </summary>
        System.Timers.Timer recordStop;
        private static readonly double RECORD_STOP_INTERVAL = 1000 * 10; // Millisecond


        /// <summary>
        /// Check if it is in the record stop period
        /// </summary>
        private bool isStoppingRecord = false;



        /// <summary>
        /// Status for starting to find templates
        /// </summary>
        private bool isFindingTemplate;

        /// <summary>
        /// Status for segmentation in depth frame by height segment
        /// </summary>
        public static bool isHeightSegmented;

        /// <summary>
        /// The height segmented depth frame pixels for display
        /// </summary>
        public static ushort[] segmentedDepthFramePixels;

        /// <summary>
        /// The 3D points in camera coordinates system mapped from depth view 
        /// </summary>
        public static CameraSpacePoint[] cameraSpacePoints;

        /// <summary>
        /// Binding source property for top view display
        /// Intensity represents for height
        /// </summary>
        public ImageSource TopViewInHeight { get { return topViewInHeight; } }

        /// <summary>
        /// Private top view image
        /// </summary>
        private DrawingImage topViewInHeight;

        /// <summary>
        /// Drawing group for top view
        /// </summary>
        private DrawingGroup drawingGroup_topView;

        /// <summary>
        /// Thread for RFID
        /// </summary>
        private Thread rfid_Thread;

        /// <summary>
        /// list of all the buttons
        /// </summary>
        //private List<Button> btnList;

        /// <summary>
        /// list of all the windows to show(depth, heightview and roomlayout)
        /// </summary>
        private List<UIElement> windowsList;


        public delegate void ShutDownApplicationHandler();
        //public event ShutDownApplicationHandler shutDown;

        private Point lastClickPosition;

        private bool clicked;

        private List<FrameworkElement> pageMargin;



        private static readonly double PERSON_MATCH_INTERVAL = 1000 * 5; // Millisecond

        public Dictionary<ulong, SkeletonPosition> skeletonList;


        private Dictionary<ulong, double> lastSkeletonDistance;

        private Point kinectPosition;

        private Dictionary<ulong, KeyValuePair<string, double>> optMatch;

        private ObservableCollection<DataXY> _data = new ObservableCollection<DataXY>();
        public ObservableCollection<DataXY> Data { get { return _data; } }

        public delegate void changeGraphHandler(double tagVelocityAnnte2, double velocitySkeleton1, double tagVelocityAnnte1,double velocitySkeleton2);
        public delegate void getSkeletonPositionHandler();

        public int test = 0;

        private Dictionary<ulong, double> skeletonDis;

        public bool kinectStarted = false;

        public Thread match;

        public double distanceThrechhood = 50;

        /// <summary>
        /// 
        /// </summary>
        public readonly double THRESHOlD_STATIC = 50;


        public Dictionary<ulong, Point> skeletonLastPoint;

        public Dictionary<ulong, Point> skeletonPoint;

        private int writtingCount = 0;

        private Point antenna1Position =new Point(120,150);

        public Dictionary<int, Point> antennaPositions = new Dictionary<int, Point>();

        public class DataXY
        {
            public string cat1 { get; set; }
            public double val1 { get; set; }
            public double val2 { get; set; }
            public double val3 { get; set; }
            public double val4 { get; set; }
        }

        public void ShutDownApplication()
        {
            ShutDownApplicationHandler d = ShutDownApp;
            this.Dispatcher.Invoke(d);

        }

        public void ShutDownApp()
        {
            Application.Current.Shutdown();
        }

        private void initAntennaPositions() {

            antennaPositions.Add(1, new Point(120, 150));
            antennaPositions.Add(2, new Point(0, 0));


        }
        /// <summary>
        /// Entry
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            //templateSearchThread = new Thread(new ThreadStart(TemplateDetector.DoInBackgrond));
            initAntennaPositions();

            //ErrorLog.LogSystemEvent("UI initialized");

            kinectSensor = KinectSensor.GetDefault();

            bodies = new Body[kinectSensor.BodyFrameSource.BodyCount];

            persons = new Person[kinectSensor.BodyFrameSource.BodyCount];
            activities = new LinkedList<Activity>();
            drawingGroup = new DrawingGroup();
            depthSource = new DrawingImage(drawingGroup);
            drawingGroup_topView = new DrawingGroup();
            topViewInHeight = new DrawingImage(drawingGroup_topView);
            DataContext = this;

            depthFramePixels = new ushort[kinectSensor.DepthFrameSource.FrameDescription.Width *
                                         kinectSensor.DepthFrameSource.FrameDescription.Height];

            segmentedDepthFramePixels = new ushort[kinectSensor.DepthFrameSource.FrameDescription.Width *
                                         kinectSensor.DepthFrameSource.FrameDescription.Height];


            // Select source type needed, e.g., depth, color, body
            multiSourceFrameReader = kinectSensor.OpenMultiSourceFrameReader(FrameSourceTypes.Body | FrameSourceTypes.Depth);
            multiSourceFrameReader.MultiSourceFrameArrived += MultiSourceFrameReader_MultiSourceFrameArrived;
            kinectSensor.IsAvailableChanged += KinectSensor_IsAvailableChanged;
            kinectSensor.Open();
            //ErrorLog.LogSystemEvent("Try to start Kinect Sensor");

            kinectConnectionCheck = new System.Timers.Timer();
            kinectConnectionCheck.AutoReset = false;
            kinectConnectionCheck.Elapsed += kinectConnectionCheck_Elapsed;
            kinectConnectionCheck.Interval = KINECT_CONNECTION_CHECK_INTERVAL;
            kinectConnectionCheck.Enabled = true;


            windowsList = new List<UIElement>();

            windowsList.Add(RoomLayout);
            windowsList.Add(DepthFrame);


            pageMargin = new List<FrameworkElement>();

            pageMargin.Add(RoomLayout);
            pageMargin.Add(DepthFrame);
            pageMargin.Add(TopView);
            skeletonList = new Dictionary<ulong, SkeletonPosition>();
            lastSkeletonDistance = new Dictionary<ulong, double>();

            kinectPosition = new Point(0, 0);

            optMatch = new Dictionary<ulong, KeyValuePair<string, double>>();
            //DepthButton.Background = Brushes.Yellow;

            skeletonDis = new Dictionary<ulong, double>();

            //match = new Thread(new ThreadStart(this.skeletonMatch));

            this.DataContext = this;
            for (int i = 60; i > 0; i--)
            {
                DateTime dt = new DateTime(DateTime.Now.Ticks - TimeSpan.FromSeconds(i).Ticks);
                _data.Add(new DataXY() { cat1 = dt.ToString("HH:mm:ss"), val1 = 0, val2 = 0, val3 = 0, val4 = 0 });
            }

            skeletonLastPoint = new Dictionary<ulong, Point>();
            skeletonPoint = new Dictionary<ulong, Point>();

        }


        /// <summary>
        /// Check Kinect connection when starting the application
        /// Timer callback
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void kinectConnectionCheck_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (!kinectSensor.IsAvailable)
            {
                //ErrorHandler.ProcessDisconnectError();
                //ErrorLog.LogSystemError("Kinect Connection Error!");
            }

            ((System.Timers.Timer)sender).Close();
        }

        /// <summary>
        /// Handle the processing when Kinect connection status is changed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void KinectSensor_IsAvailableChanged(object sender, IsAvailableChangedEventArgs e)
        {
            if (!isKinectConnected && e.IsAvailable)
            {
                isKinectConnected = true;
                //ErrorHandler.ProcessConnectNotification();
                //ErrorLog.LogSystemError("Kinect Connection Error!");
            }
            else if (isKinectConnected && !e.IsAvailable)
            {
                isKinectConnected = false;

                // Bug: conflict with restarting application
                //ErrorHandler.ProcessDisconnectError();
            }
        }


        /// <summary>
        /// Handle the processing when Kinect frame arrived
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MultiSourceFrameReader_MultiSourceFrameArrived(object sender, MultiSourceFrameArrivedEventArgs e)
        {
            //if (isDownApplication) Application.Current.Shutdown();

            MultiSourceFrame multiSourceFrame = e.FrameReference.AcquireFrame();

            if (multiSourceFrame != null)
            {
                using (DepthFrame depthFrame = multiSourceFrame.DepthFrameReference.AcquireFrame())
                {
                    using (BodyFrame bodyFrame = multiSourceFrame.BodyFrameReference.AcquireFrame())
                    {
                        using (DrawingContext drawingContext = drawingGroup.Open())
                        {
                            if (depthFrame != null && bodyFrame != null)
                            {

                                Canvas_Position_Foreground.Children.Clear();
                                // Find templates
                                if (!isFindingTemplate)
                                {
                                    ushort[] depthFrameData = new ushort[depthFrame.FrameDescription.Height * depthFrame.FrameDescription.Width];
                                    depthFrame.CopyFrameDataToArray(depthFrameData);

                                    cameraSpacePoints = new CameraSpacePoint[depthFrame.FrameDescription.Height * depthFrame.FrameDescription.Width];
                                    kinectSensor.CoordinateMapper.MapDepthFrameToCameraSpace(depthFrameData, cameraSpacePoints);

                                    //templateSearchThread.Start();



                                    isFindingTemplate = false;

                                }

                                // Display depth frame
                                // Uncomment to enable the display for height segmentation result
                                //if (!isHeightSegmented)
                                if (true)
                                {
                                    drawingContext.DrawImage(Transformation.ToBitmap(depthFrame, depthFramePixels, true),
                                                        new Rect(0.0, 0.0, kinectSensor.DepthFrameSource.FrameDescription.Width, kinectSensor.DepthFrameSource.FrameDescription.Height));
                                }
                                else
                                {
                                    drawingContext.DrawImage(Transformation.ToBitmap(depthFrame, segmentedDepthFramePixels, false),
                                                        new Rect(0.0, 0.0, kinectSensor.DepthFrameSource.FrameDescription.Width, kinectSensor.DepthFrameSource.FrameDescription.Height));
                                }



                                // Load raw body joints info from Kinect
                                bodyFrame.GetAndRefreshBodyData(bodies);

                                // Update personal infomation from raw joints
                                for (int i = 0; i < kinectSensor.BodyFrameSource.BodyCount; ++i)
                                {
                                    if (persons[i] == null) persons[i] = new Person();

                                    ulong trackingId = bodies[i].TrackingId;


                                    if (bodies[i].IsTracked)
                                    {
                                        // Update tracking status
                                        persons[i].IsTracked = true;

                                        persons[i].ID = bodies[i].TrackingId;

                                        // Assign color to person in the top view for positioning
                                        persons[i].Color = Plot.BodyColors[i];

                                        // Get person's 3D postion in camera's coordinate system
                                        CameraSpacePoint headPositionCamera = bodies[i].Joints[JointType.Head].Position; // Meter

                                        // Convert to 3D position in horizontal coordinate system
                                        CameraSpacePoint headPositionGournd = Transformation.RotateBackFromTilt(TILT_ANGLE, true, headPositionCamera);

                                        // Convert to 2D top view position on canvas
                                        Transformation.ConvertGroundSpaceToPlane(headPositionGournd, persons[i]);

                                        // Determine body orientation using shoulder joints
                                        CameraSpacePoint leftShoulderPositionGround = Transformation.RotateBackFromTilt(TILT_ANGLE, true, bodies[i].Joints[JointType.ShoulderLeft].Position);
                                        CameraSpacePoint rightShoulderPositionGround = Transformation.RotateBackFromTilt(TILT_ANGLE, true, bodies[i].Joints[JointType.ShoulderRight].Position);
                                        BodyOrientation.DecideOrientation(leftShoulderPositionGround, rightShoulderPositionGround, persons[i],
                                                                         Transformation.CountZeroInRec(depthFramePixels, kinectSensor.CoordinateMapper.MapCameraPointToDepthSpace(headPositionCamera),
                                                                         16, kinectSensor.DepthFrameSource.FrameDescription.Width), Canvas_Position_Foreground);

                                    }
                                    else persons[i].IsTracked = false;
                                }


                                DrawPeopleOnDepth(drawingContext);
                                DrawPeopleOnCanvas();
                                DetermineSystemStatus();
                                DrawSystemStatus();

                                // Recognize and record activities when recording requirements are satisfied
                                if (isRecording)
                                {
                                    if (RFID.IsRFIDOpen)
                                    {
                                        RecordSkeletonPosition();
                                        //RecordSkeletonDisplacement();

                                    }
                                    DrawActivityOnCanvas();
                                    Record();
                                }

                                if (RFID.frameCounter >= 10)
                                {
                                    int droptime = RFID.droptime;
                                    dropLabel.Content = "#drop=" + droptime;
                                    RFID.drawLineTimer.Stop();
                                    RFID.frameCounter = 0;
                                    // AsyncTask
                                    BackgroundWorker worker = new BackgroundWorker();
                                    worker.WorkerReportsProgress = true;
                                    worker.DoWork += skeletonMatch;
                                    worker.RunWorkerAsync();
                                    RFID.droptime = 0;

                                }

                            }
                        }
                    }
                }
                kinectStarted = true;
            }
        }



        private void checkMatch()
        {
            //the list to record the skeletonId that need to be removed because when we iterate the dictionary, we cannot do any operate to it like remove or add it.
            List<ulong> skeletonToRemove = new List<ulong>();
            lock (skeletonList)
            {
                foreach (KeyValuePair<ulong, KeyValuePair<string, double>> match in optMatch)
                {

                    if (skeletonList.ContainsKey(match.Key))
                    {

 
                        double[] skeletonDis1 = generateSequence(skeletonList[match.Key].relDistance, 1);
                        double[] skeletonDis2 = generateSequence(skeletonList[match.Key].relDistance, 2);
                        OnlineMedianFilter filter = new OnlineMedianFilter(5);

                        skeletonDis1 = filter.ProcessSamples(skeletonDis1);
                        skeletonDis2 = filter.ProcessSamples(skeletonDis2);

                        int j = 0;

                        //##############################################################
                        //TODO no hard programming!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
                        //##############################################################
                        double[] rfidVe = new double[RFID.RfidDistanceList[match.Value.Key].distance.Count];
                        double[] rfidVe2= new double[RFID.RfidDistanceList[match.Value.Key].distance.Count];
                        foreach (KeyValuePair<DateTime, Dictionary<int,double>> rfidDis in RFID.RfidDistanceList[match.Value.Key].distance)
                        {
                            rfidVe[j] = rfidDis.Value[1];
                            rfidVe2[j] = rfidDis.Value[2];
                            j++;
                        }

                        //NDtw.Dtw dtw = new NDtw.Dtw(skeletonDis, rfidVe);
                        //Correlation c = new Correlation();
                        //double dtwDis = Math.Abs(c.ComputeCoeff(skeletonDis, rfidVe));
                        //double dtwDis = Math.Abs(dtw.GetCost());
                        //dtwDis = dtwDis / dtw.GetPath().Length;

                        double difference1 = Difference.getDifference(skeletonDis1, rfidVe);
                        double difference2 = Difference.getDifference(skeletonDis2, rfidVe2);

                        double difference = (difference1 + difference2) / 2;

                        //double difference= Math.Sqrt(Math.Pow(difference1, 2) + Math.Pow(difference2, 2));
                        //double difference = difference1 + difference2; 

                        if (difference > distanceThrechhood)
                        {
                            skeletonToRemove.Add(match.Key);
                        }

                    }
                    else {

                        skeletonToRemove.Add(match.Key);

                    }

                }
            }

            //remove the skeletonMatches which have higher distance
            foreach (ulong skeletonDelete in skeletonToRemove)
            {
                optMatch.Remove(skeletonDelete);
            }

            foreach (Person person in persons)
            {
                if (optMatch.ContainsKey(person.ID))
                {
                    person.tagId = ConfigurationManager.AppSettings[optMatch[person.ID].Key];
                    person.dis = optMatch[person.ID].Value;
                }
                else {
                    person.tagId = "unKnown";
                }

            }

            resetDoppler();

        }

        private void skeletonMatch(object sender, DoWorkEventArgs e)
        {

            lock (skeletonList)
            {

                // writeSkeletonAndRfid();
                

                //writeSkeletonAndRfidRaw();

                if (RFID.totalCounter >= 1000)
                {
                    checkMatch();
                    RFID.totalCounter = 0;
                }
                else {

                    List<string> tagStatic = new List<string>();
                    List<ulong> skeletonStatic = new List<ulong>();

                    //seperateStaticSkeleton(skeletonList, skeletonStatic);
                    //seperateStaticTag(RFID.RfidDistanceList, tagStatic);

                    foreach (KeyValuePair<ulong, SkeletonPosition> skeletonEntry in skeletonList)
                    {
                        if (skeletonEntry.Value.relDistance.Count < 9)
                        {
                            Console.WriteLine("id " + skeletonEntry.Key + "not enough data");
                            continue;
                        }
                        if (!optMatch.ContainsKey(skeletonEntry.Key))
                        {

                            double[] skeletonDis = generateSequence(skeletonEntry.Value.relDistance,1);
                            double[] skeletonDis2 = generateSequence(skeletonEntry.Value.relDistance, 2);

                            //Console.WriteLine("deviation of skeleton "+ skeletonEntry.Key+" is " + Correlation.deviation(skeletonDis));

                            //OnlineMedianFilter fileter = new OnlineMedianFilter(5);

                            foreach (KeyValuePair<string, RfidVelocity> rfidVelocityEntry in RFID.RfidDistanceList)
                            {
                                //int j = 0;
                                double[] rfidVe = generateSequence(rfidVelocityEntry.Value.distance,1);
                                double[] rfidVe2 = generateSequence(rfidVelocityEntry.Value.distance, 2);
                                //Correlation c = new Correlation();

                                Console.WriteLine("deviation of tag " + rfidVelocityEntry.Key + " is " + Correlation.deviation(rfidVe));

                                if (tagStatic.Contains(rfidVelocityEntry.Key))
                                {
                                    continue;
                                }

                                ////status of this tag(static or not)
                                //if (Correlation.deviation(rfidVe)<= THRESHOlD_STATIC) {

                                //    if (tagStatic.Contains(rfidVelocityEntry.Key)) {
                                //        tagStatic.Add(rfidVelocityEntry.Key);
                                //    }
                                //    continue;
                                //}

                                //NDtw.Dtw dtw = new NDtw.Dtw(skeletonDis, rfidVe);

                                //double dtwDis = Math.Abs(dtw.GetCost());

                                //dtwDis = dtwDis / dtw.GetPath().Length;

                                double difference1 = Difference.getDifference(skeletonDis, rfidVe);
                                double difference2 = Difference.getDifference(skeletonDis2, rfidVe2);


                                //double difference = Math.Sqrt(Math.Pow(difference1, 2) + Math.Pow(difference2, 2));
                                double difference = (difference1 + difference2) / 2;

                                Console.WriteLine(skeletonEntry.Key + " with" + rfidVelocityEntry.Key + "has " + difference + " distance");

                                if (optMatch.ContainsKey(skeletonEntry.Key))
                                {
                                    if (optMatch[skeletonEntry.Key].Value > difference)
                                    {
                                        optMatch[skeletonEntry.Key] = new KeyValuePair<string, double>(rfidVelocityEntry.Key, difference);
                                    }
                                }
                                else if (difference <= distanceThrechhood)
                                {
                                    optMatch.Add(skeletonEntry.Key, new KeyValuePair<string, double>(rfidVelocityEntry.Key, difference));
                                }


                            }

                            //hasWrittenrfid = true;
                        }
                    }

                    //foreach (ulong skeletonId in skeletonStatic)
                    //{
                    //    double[] skeletonDis = generateSequence(skeletonList[skeletonId].relDistance);

                    //    foreach (string tagId in tagStatic)
                    //    {
                    //        double[] rfidDis = generateSequence(RFID.RfidDistanceList[tagId].distance);

                    //        //double meanDeviation=Correlation.meanDeviation(skeletonDis, rfidDis);

                    //        if (optMatch.ContainsKey(skeletonId))
                    //        {
                    //        }
                    //        else {
                    //            optMatch.Add(skeletonId, new KeyValuePair<string, double>(tagId, 1));
                    //        }
                    //    }

                    //}

                    foreach (Person person in persons)
                    {
                        if (optMatch.ContainsKey(person.ID))
                        {
                            person.tagId = ConfigurationManager.AppSettings[optMatch[person.ID].Key];
                            person.dis = optMatch[person.ID].Value;
                        }
                        else {
                            person.tagId = "unKnown";
                        }

                    }
                    skeletonList.Clear();
                    RFID.RfidDistanceList.Clear();

                    lastSkeletonDistance.Clear();

                }
                RFID.drawLineTimer.Start();

            }



        }

        private void seperateStaticSkeleton(Dictionary<ulong, SkeletonPosition> skeletonList, List<ulong> skeletonStatic)
        {

            foreach (KeyValuePair<ulong, SkeletonPosition> skeletonEntry in skeletonList)
            {
                //double[] skeletonDis = generateSkeletonSequence(skeletonEntry.Value.relDistance);

                //if (Correlation.deviation(skeletonDis) <= THRESHOlD_STATIC)
                //{
                //    skeletonStatic.Add(skeletonEntry.Key);
                //}

            }

            //foreach (ulong id in skeletonStatic) {
            //    skeletonList.Remove(id);
            //}
        }

        private void seperateStaticTag(Dictionary<string, RfidVelocity> RfidDistanceList, List<string> tagStatic)
        {

            foreach (KeyValuePair<string, RfidVelocity> rfidVelocityEntry in RfidDistanceList)
            {
                //int j = 0;
                double[] rfidVe = generateSequence(rfidVelocityEntry.Value.distance,1);
                double[] rfidVe2 = generateSequence(rfidVelocityEntry.Value.distance, 2);
                //status of this tag(static or not)
                if (Correlation.deviation(rfidVe) <= THRESHOlD_STATIC)
                {
                    tagStatic.Add(rfidVelocityEntry.Key);
                }

                //foreach (string id in tagStatic) {
                //    RfidDistanceList.Remove(id);
                //}

            }
        }

        private void writeSkeletonAndRfidRaw()
        {
            writtingCount++;
            test++;
            if (test % 1 == 0)
            {
                lock (skeletonList) lock (RFID.RfidDistanceList)
                    {
                        foreach (KeyValuePair<ulong, SkeletonPosition> skeletonEntry in skeletonList)
                        {
                            double[] skeletonDis = new double[skeletonEntry.Value.relDistance.Count];

                            int i = 0;
                            //##############################################
                            //hard programmed
                            //##############################################
                            foreach (KeyValuePair<DateTime, Dictionary<int,double>> relDis in skeletonEntry.Value.relDistance)
                            {
                                skeletonDis[i] = relDis.Value[1];
                                i++;
                            }

                            //OnlineMedianFilter filter = new OnlineMedianFilter(5);

                            //skeletonDis = filter.ProcessSamples(skeletonDis);

                            for (int k = 0; k < skeletonDis.Length; k++)
                            {
                                using (StreamWriter writer = new StreamWriter(@"C:\Users\Dongyang\Desktop\" + skeletonEntry.Key+" " + writtingCount + "raw.txt", true))
                                {
                                    writer.WriteLine("{0}", skeletonDis[k]);
                                }
                            }
                        }

                        foreach (KeyValuePair<string, RfidVelocity> rfidVelocityEntry in RFID.RfidDistanceList)
                        {
                            int j = 0;
                            //####################################################################
                            //TODO no hard programming!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
                            //####################################################################
                            double[] rfidVe = new double[rfidVelocityEntry.Value.distance.Count];
                            double[] rfidVe2=new double[rfidVelocityEntry.Value.distance.Count];
                            foreach (KeyValuePair<DateTime, Dictionary<int,double>> rfidDis in rfidVelocityEntry.Value.distance)
                            {
                                rfidVe[j] = rfidDis.Value[1];
                                rfidVe2[j]= rfidDis.Value[2];
                                j++;



                            }

                            //OnlineMedianFilter o = new OnlineMedianFilter(5);

                            //rfidVe = o.ProcessSamples(rfidVe);

                            for (int k = 0; k < rfidVe.Length; k++)
                            {
                                using (StreamWriter writer = new StreamWriter(@"C:\Users\Dongyang\Desktop\" + rfidVelocityEntry.Key + " " + writtingCount + "raw.txt", true))
                                {
                                    writer.WriteLine("{0}", rfidVe[k]);
                                }
                            }
                        }


                        foreach (KeyValuePair<string, RfidVelocity> rfidVelocityEntry in RFID.RfidDistanceList)
                        {
                            int j = 0;
                            double[] rfidApiPh = new double[rfidVelocityEntry.Value.api_phase.Count];

                            foreach (KeyValuePair<DateTime, double> rfidApi in rfidVelocityEntry.Value.api_phase)
                            {
                                rfidApiPh[j] = rfidApi.Value;
                                j++;



                            }

                            //OnlineMedianFilter o = new OnlineMedianFilter(5);

                            //rfidVe = o.ProcessSamples(rfidVe);

                            for (int k = 0; k < rfidApiPh.Length; k++)
                            {
                                using (StreamWriter writer = new StreamWriter(@"C:\Users\Dongyang\Desktop\" + rfidVelocityEntry.Key + " " + writtingCount + "api_phase_raw.txt", true))
                                {
                                    writer.WriteLine("{0}", rfidApiPh[k]);
                                }
                            }
                        }


                    }
                return;
            }

        }

        private void writeSkeletonAndRfid()
        {
            test++;
            if (test % 1 == 0)
            {
                lock (skeletonList) lock (RFID.RfidDistanceList)
                    {
                        foreach (KeyValuePair<ulong, SkeletonPosition> skeletonEntry in skeletonList)
                        {
                            double[] skeletonDis = new double[skeletonEntry.Value.relDistance.Count];
                            double[] skeletonDis2 = new double[skeletonEntry.Value.relDistance.Count];

                            int i = 0;
                            //#############################################
                            //hard programmed
                            //#############################################
                            foreach (KeyValuePair<DateTime, Dictionary<int,double>> relDis in skeletonEntry.Value.relDistance)
                            {
                                skeletonDis[i] = relDis.Value[1];
                                skeletonDis2[i] = relDis.Value[2];
                                i++;
                            }

                            //OnlineMedianFilter filter = new OnlineMedianFilter(5);

                            //skeletonDis = filter.ProcessSamples(skeletonDis);
                            //skeletonDis2 = filter.ProcessSamples(skeletonDis2);

                            for (int k = 0; k < skeletonDis.Length; k++)
                            {
                                using (StreamWriter writer = new StreamWriter(@"C:\Users\Dongyang\Desktop\" + skeletonEntry.Key + "_1.txt", true))
                                {
                                    writer.WriteLine("{0}", skeletonDis[k]);
                                }
                            }

                            for (int k = 0; k < skeletonDis2.Length; k++)
                            {
                                using (StreamWriter writer = new StreamWriter(@"C:\Users\Dongyang\Desktop\" + skeletonEntry.Key + "_2.txt", true))
                                {
                                    writer.WriteLine("{0}", skeletonDis2[k]);
                                }
                            }
                        }

                        foreach (KeyValuePair<string, RfidVelocity> rfidVelocityEntry in RFID.RfidDistanceList)
                        {

                            int j = 0;
                            double[] rfidVe = new double[rfidVelocityEntry.Value.distance.Count];
                            double[] rfidVe2 = new double[rfidVelocityEntry.Value.distance.Count];
                            foreach (KeyValuePair<DateTime, Dictionary<int, double>> rfidDis in rfidVelocityEntry.Value.distance)
                            {
                                rfidVe[j] = rfidDis.Value[1];
                                rfidVe2[j] = rfidDis.Value[2];
                                j++;
                            }

                            //OnlineMedianFilter o = new OnlineMedianFilter(5);

                            //rfidVe = o.ProcessSamples(rfidVe);

                            //rfidVe2 = o.ProcessSamples(rfidVe2);

                            for (int k = 0; k < rfidVe.Length; k++)
                            {
                                using (StreamWriter writer = new StreamWriter(@"C:\Users\Dongyang\Desktop\" + rfidVelocityEntry.Key + "_1.txt", true))
                                {
                                    writer.WriteLine("{0}", rfidVe[k]);
                                }
                            }
                            for (int k = 0; k < rfidVe2.Length; k++)
                            {
                                using (StreamWriter writer = new StreamWriter(@"C:\Users\Dongyang\Desktop\" + rfidVelocityEntry.Key + "_2.txt", true))
                                {
                                    writer.WriteLine("{0}", rfidVe2[k]);
                                }
                            }
                        }

                    }
                return;
            }

        }

        private double calcRealDisPlacement(ulong id, Point point)
        {

            if (skeletonLastPoint.ContainsKey(id))
            {
                double dis = Transformation.PointVectorDistance(point, skeletonLastPoint[id]);
                skeletonLastPoint[id] = point;
                return dis;

            }
            else {
                skeletonLastPoint.Add(id, point);
                return 0;
            }


        }

        private void RecordSkeletonDisplacement_(Point antennaPosition,int antennaNum,Point bodyPoint,Body body) {

            Point relativePoint = Transformation.RelativePosition(bodyPoint,antennaPosition);

            double realDis = calcRealDisPlacement(body.TrackingId, relativePoint);

            if (skeletonList.ContainsKey(body.TrackingId))
            {
                
                lock (skeletonDis)
                {

                    if (skeletonDis.ContainsKey(body.TrackingId))
                    {
                        skeletonDis[body.TrackingId] += realDis;
                    }
                    else {
                        skeletonDis.Add(body.TrackingId, realDis);
                    }
                }

            }
            else {
                var s = new SkeletonPosition(body.TrackingId);

                Dictionary<int, double> skeletonDistance = new Dictionary<int, double>();
                skeletonDistance.Add(antennaNum,0);
                s.relDistance.Add(DateTime.Now, skeletonDistance);

                skeletonList.Add(body.TrackingId, s);


            }

        }

        //private void RecordSkeletonDisplacement()
        //{
        //    for (int i = 0; i < kinectSensor.BodyFrameSource.BodyCount; ++i)
        //    {
        //        if (bodies[i].IsTracked)
        //        {
        //            CameraSpacePoint headPositionCamera = Transformation.RotateBackFromTilt(TILT_ANGLE, true, bodies[i].Joints[JointType.Head].Position);
        //            Point bodyPosition = new Point(headPositionCamera.X * 100, headPositionCamera.Z * 100);

        //            RecordSkeletonDisplacement_();




        //        }
        //    }
        //}


        private void RecordSkeletonPosition()
        {
            lock (skeletonPoint)
            {
                CheckTrakckedSkeleton();

                for (int i = 0; i < kinectSensor.BodyFrameSource.BodyCount; ++i)
                {
                    if (bodies[i].IsTracked)
                    {
                        CameraSpacePoint headPositionCamera = Transformation.RotateBackFromTilt(TILT_ANGLE, true, bodies[i].Joints[JointType.Head].Position);

                        Point bodyPosition = new Point(headPositionCamera.X * 100, headPositionCamera.Z * 100);

                        ulong skeletonId = bodies[i].TrackingId;

                        if (skeletonPoint.ContainsKey(skeletonId))
                        {
                            skeletonPoint[skeletonId] = bodyPosition;
                        }
                        else {
                            skeletonPoint.Add(skeletonId, bodyPosition);
                        }

                        //double distance = Transformation.PointDistance(kinectPosition, bodyPosition);

                        //if (skeletonList.ContainsKey(bodies[i].TrackingId) && lastSkeletonDistance.ContainsKey(bodies[i].TrackingId))
                        //{


                        //    //relative distance to last time
                        //    double relDistance = lastSkeletonDistance[bodies[i].TrackingId] - distance;


                        //    //record every skeleton's position
                        //    lock (skeletonDis)
                        //    {

                        //        if (skeletonDis.ContainsKey(bodies[i].TrackingId))
                        //        {
                        //            skeletonDis[bodies[i].TrackingId] += relDistance;
                        //        }
                        //        else {
                        //            skeletonDis.Add(bodies[i].TrackingId, relDistance);
                        //        }
                        //    }


                        //    //skeletonList[bodies[i].TrackingId].relDistance.Add(DateTime.Now, relDistance);

                        //    lastSkeletonDistance[bodies[i].TrackingId] = distance;

                        //}
                        //else {

                        //    var s = new SkeletonPosition(bodies[i].TrackingId);

                        //    s.relDistance.Add(DateTime.Now, 0);

                        //    skeletonList.Add(bodies[i].TrackingId, s);

                        //    lastSkeletonDistance.Add(bodies[i].TrackingId, distance);


                        //}
                    }
                }

            }
        }

        private void CheckTrakckedSkeleton()
        {




        }

        /// <summary>
        /// Record each person's activitie in timeline
        /// Record each person's position in timeline
        /// </summary>
        public void Record()
        {

            //ActivityRecognition.Record.RecordPosition(persons);            
            //ActivityRecognition.Record.RecordJoints(bodies, true);
        }

        /// <summary>
        /// Draw recording system status
        /// </summary>
        private void DrawSystemStatus()
        {
            Plot.DrawSystemStatus(Canvas_Position_Foreground, isRecording);
        }

        /// <summary>
        /// Start to stop recording after a latency
        /// Callback for timer
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RecordStop_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            //applicationRestart.Enabled = false;

            if (isRFIDRequired)
            {
                RFID.IsRFIDOpen = false;
                System.Threading.Thread.Sleep(1000);

                if (rfid_Thread != null)
                {
                    rfid_Thread.Abort();
                    rfid_Thread = null;
                }

                //if the thread exist and the program is recording, then shut it down

            }

            if (rfidReader != null) rfidReader = null;

            isRecording = false;
            isStoppingRecord = false;
        }

        /// <summary>
        /// Determine recording system status with requirements
        /// </summary>
        private void DetermineSystemStatus()
        {
            // Take number of people in the view for the requirements
            // Start recording
            if (Transformation.GetNumberOfPeople(persons) >= 1)
            {
                if (!isRecording)
                {

                    ActivityRecognition.Record.StartTime = DateTime.Now.ToString("M-d-yyyy_HH-mm-ss");

                    //if (objectDetector == null) objectDetector = new ObjectDetector(ListBox_Object, ListView_Object);
                    if (rfidReader == null) rfidReader = new RFID(this);

                    if (isRFIDRequired)
                    {
                        //ObjectDetector.IsOpenRFID = true;
                        RFID.IsRFIDOpen = true;

                        if (rfid_Thread == null)
                        {
                            //ErrorLog.LogSystemEvent("Try to start RFID system");
                            rfid_Thread = new Thread(new ThreadStart(rfidReader.run));
                            rfid_Thread.Start();
                        }

                    }



                }
                else {

                    //restartStarted = true;
                }




                isRecording = true;
            }



            // Stop recording while the moving person < 1
            if (Transformation.GetNumberOfPeople(persons) < 0)
            {


                if (isRecording)
                {

                    if (!isStoppingRecord)
                    {
                        isStoppingRecord = true;

                        recordStop = new System.Timers.Timer();
                        recordStop.AutoReset = false;
                        recordStop.Elapsed += RecordStop_Elapsed;
                        recordStop.Interval = RECORD_STOP_INTERVAL;
                        recordStop.Enabled = true;
                    }
                }
            }
            else
            {


                if (isStoppingRecord)
                {
                    isStoppingRecord = false;
                    recordStop.Enabled = false;
                }
            }
        }



        /// <summary>
        /// Draw person on top view for positioning
        /// </summary>
        private void DrawPeopleOnCanvas()
        {
            foreach (Person person in persons)
            {
                if (person.IsTracked)
                {
                    //StreamWriter writer = new StreamWriter("person_position.txt", true);
                    //writer.WriteLine("{0},{1}",person.Position.X,person.Position.Y);
                    //writer.Close();
                    Plot.DrawPeopleFromKinectOnCanvas(Plot.EllipseDiameter, person.Position.X, person.Position.Y, person.Color, Canvas_Position_Foreground);
                    Plot.DrawPeopleTagId(person, Canvas_Position_Foreground);
                    Plot.DrawOrientationOnCanvas(Plot.EllipseDiameter, Plot.TriangleSideLength, person.Orientation, person.Position.X, person.Position.Y, System.Windows.Media.Brushes.Black, Canvas_Position_Foreground);
                }
            }
        }

        /// <summary>
        /// Draw text of each person's activity on top view for positioning
        /// </summary>
        private void DrawActivityOnCanvas()
        {
            foreach (Person person in persons)
            {
                if (person.IsTracked)
                {
                    Plot.DrawActivitiesOnCanvas(person, Canvas_Position_Foreground);
                }
            }
        }

        /// <summary>
        /// Label each person's head and some joints in depth frame view
        /// </summary>
        /// <param name="drawingContext"></param>
        private void DrawPeopleOnDepth(DrawingContext drawingContext)
        {
            for (int i = 0; i < persons.Length; i++)
            {
                if (persons[i].IsTracked)
                {
                    LinkedList<CameraSpacePoint> jointPoints = new LinkedList<CameraSpacePoint>();

                    // Select the joints needed to display
                    jointPoints.AddLast(bodies[i].Joints[JointType.Head].Position);
                    jointPoints.AddLast(bodies[i].Joints[JointType.ShoulderLeft].Position);
                    jointPoints.AddLast(bodies[i].Joints[JointType.ShoulderRight].Position);

                    Plot.DrawJointsOnDepth(kinectSensor.CoordinateMapper, jointPoints, drawingContext, kinectSensor);
                    Plot.DrawTrackedHead(kinectSensor.CoordinateMapper, persons[i].Color, bodies[i].Joints[JointType.Neck].Position, bodies[i].Joints[JointType.Head].Position, drawingContext, kinectSensor);
                }
            }
        }

        /// <summary>
        /// Dispose unreleased instances when closing the window
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Kinect
            if (multiSourceFrameReader != null) multiSourceFrameReader.Dispose();
            if (kinectSensor != null) kinectSensor.Close();



            if (rfidReader != null)
            {
                if (rfidReader != null)
                {
                    RFID.IsRFIDOpen = false;
                    rfidReader.Stop();
                }
                if (rfid_Thread != null) rfid_Thread.Abort();
            }

        }

        /// <summary>
        /// Get the start mouse position for area when defining activity
        /// Mouse event callback
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Canvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            isMouseDown = true;
            mouseDownPosition = e.GetPosition(Canvas_Position_Foreground);
            Canvas.SetLeft(Rectangle_SelectArea, mouseDownPosition.X);
            Canvas.SetTop(Rectangle_SelectArea, mouseDownPosition.Y);
            Rectangle_SelectArea.Width = 0;
            Rectangle_SelectArea.Height = 0;
        }

        /// <summary>
        /// Change the rectangle - selected area when mose is moving
        /// Mouse event callback
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {

        }

        /// <summary>
        /// Get the end mouse position for area when defining activity
        /// Mouse event callback
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Canvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            isMouseDown = false;
            mouseUpPosition = e.GetPosition(Canvas_Position_Foreground);
        }

        /// <summary>
        /// Add a defined activity
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_AddActivity(object sender, RoutedEventArgs e)
        {


        }

        /// <summary>
        /// Pop up the window to add activity
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_PopupActivity(object sender, RoutedEventArgs e)
        {
            Popup_AddActivity.IsOpen = true;
        }

        /// <summary>
        /// Pop up the window to save or clear defined activities
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_PopupSetting(object sender, RoutedEventArgs e)
        {
            Popup_Settings.IsOpen = true;
        }

        /// <summary>
        /// Save defined activities
        /// Load automatically when next start
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_SaveSettings(object sender, RoutedEventArgs e)
        {


            Popup_Settings.IsOpen = false;
        }

        /// <summary>
        /// Clear defined activities
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_ClearSettings(object sender, RoutedEventArgs e)
        {
            activities.Clear();

            Popup_Settings.IsOpen = false;
        }



        private void MainMouseDown(object sender, MouseButtonEventArgs e)
        {
            lastClickPosition = e.GetPosition(MW);
            clicked = true;
        }

        private void MainMouseMove(object sender, MouseEventArgs e)
        {

            if (clicked)
            {

                double moveDistance = e.GetPosition(MW).X - lastClickPosition.X;

                //var point = viewPage.TranslatePoint(new Point(), MW);

                double left = viewPage.Margin.Left;
                double top = viewPage.Margin.Top;
                double right = viewPage.Margin.Right;
                double bottom = viewPage.Margin.Bottom;

                viewPage.Margin = new Thickness(left + moveDistance, top, right, bottom);

                lastClickPosition = e.GetPosition(MW);

            }


        }

        private void MainMouseLeave(object sender, MouseEventArgs e)
        {
            clicked = false;
            updatePageView();
        }



        private void MainMouseUp(object sender, MouseButtonEventArgs e)
        {

            clicked = false;
            updatePageView();
        }

        private void updatePageView()
        {

            double left = viewPage.Margin.Left;
            double nearest = pageMargin[0].Margin.Left;
            FrameworkElement nearstpage = pageMargin[0];

            foreach (FrameworkElement i in pageMargin)
            {
                if (Math.Abs((-left - i.Margin.Left)) < Math.Abs((-left - nearest)))
                {
                    nearest = i.Margin.Left;
                    nearstpage = i;
                }
            }

            double top = viewPage.Margin.Top;
            double right = viewPage.Margin.Right;
            double bottom = viewPage.Margin.Bottom;

            viewPage.Margin = new Thickness(-nearest, top, right, bottom);

            this.s1.Text = nearstpage.Name;

        }

        public void showGraph(double tagVelocityAnnte2, double velocitySkeleton1, double tagVelocityAnnte1,double velocitySkeleton2)
        {
            try
            {
                changeGraphHandler d = updateGraph;
                this.Dispatcher.Invoke(d, new Object[] { tagVelocityAnnte2, velocitySkeleton1, tagVelocityAnnte1, velocitySkeleton2 });
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

        }

        public void updateGraph(double tagVelocity, double tag2Velocity,double tagVelocityAnnte, double velocitySkeleton2)
        {

            _data.Add(new DataXY() { cat1 = DateTime.Now.ToString("HH:mm:ss"), val1 = tagVelocity, val2 = tag2Velocity, val3 = tagVelocityAnnte, val4 = velocitySkeleton2 });
            if (_data.Count > 60)
            {
                _data.RemoveAt(0);
            }

        }

        public Dictionary<ulong, Point> getSkeletonPosition()
        {



            return null;
        }

        public Dictionary<ulong, Point> searchSkeletonPosition()
        {
            return skeletonPoint;
            //return skeletonDis;
        }

        /// <summary>
        /// After check the already match, reset the skeletonList, rfidDistanceList, etc. 
        /// </summary>
        private void resetDoppler()
        {
            lock (skeletonList) lock (RFID.RfidDistanceList)
                {
                    skeletonList.Clear();
                    RFID.RfidDistanceList.Clear();

                    //RFID.tagLastTime.Clear();

                    lastSkeletonDistance.Clear();
                    RFID.totalCounter = 0;

                    kinectStarted = false;
                    RFID.hasTagReported = false;
                }
        }

        //generate the sequence 
        private double[] generateSequence(Dictionary<DateTime, Dictionary<int,double>> timeSequence,int antennaNum)
        {

            lock (RFID.RfidDistanceList)
            {

                double[] sequence = new double[timeSequence.Count];
                int i = 0;
                foreach (KeyValuePair<DateTime, Dictionary<int, double>> pair in timeSequence)
                {
                    sequence[i] = pair.Value[antennaNum];
                    i++;
                }

                return sequence;

            }

        }

        private double[] generateSkeletonSequence(Dictionary<DateTime, double> timeSequence)
        {
            lock (RFID.RfidDistanceList)
            {

                double[] sequence = new double[timeSequence.Count];
                int i = 0;
                foreach (KeyValuePair<DateTime, double> pair in timeSequence)
                {
                    sequence[i] = pair.Value;
                    i++;
                }

                return sequence;

            }
        }


    }
}
