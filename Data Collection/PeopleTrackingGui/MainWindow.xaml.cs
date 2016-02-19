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

        /// <summary>
        /// A thread for RFID system
        /// </summary>
        private Thread rfidThread;

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
        /// Timer for restarting the application
        /// </summary>
        System.Timers.Timer applicationRestart;

        System.Timers.Timer updateBackground;

        private static readonly double APPLICATION_RESTART_INTERVAL = 1000 * 60 * 90; // Millisecond

        private static readonly double UPDATE_BACKGROUND_INTERVAL = 1000 * 60 * 0.1; // Millisecond

        /// <summary>
        /// Check if it is in the record stop period
        /// </summary>
        private bool isStoppingRecord = false;

        /// <summary>
        /// Used for shuting down application in UI thread from a background thread
        /// </summary>
        private bool isDownApplication = false;

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
        /// Timer for reset template position found
        /// </summary>
        private System.Timers.Timer templateSearch;
        private static readonly double TEMPLATE_SEARCH_INTERVAL = 1000 * 2; // Millisecond

        /// <summary>
        /// Thread for RFID
        /// </summary>
        private Thread rfid_Thread;


        private bool restartStarted;

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

        private System.Timers.Timer personMatch;

        private static readonly double PERSON_MATCH_INTERVAL = 1000 * 5; // Millisecond

        private Dictionary<ulong, SkeletonPosition> skeletonList;

        private Dictionary<ulong, double> lastSkeletonDistance;

        private Point kinectPosition;

        private Dictionary<ulong, KeyValuePair<string, double>> optMatch;



        public void ShutDownApplication()
        {
            ShutDownApplicationHandler d = ShutDownApp;
            this.Dispatcher.Invoke(d);

        }

        public void ShutDownApp()
        {
            Application.Current.Shutdown();
        }

        /// <summary>
        /// Entry
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            //templateSearchThread = new Thread(new ThreadStart(TemplateDetector.DoInBackgrond));


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

            skeletonList = new Dictionary<ulong, SkeletonPosition>();
            lastSkeletonDistance = new Dictionary<ulong, double>();

            kinectPosition = new Point(0, 0);

            optMatch = new Dictionary<ulong, KeyValuePair<string, double>>();
            //DepthButton.Background = Brushes.Yellow;
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
                                if (isFindingTemplate)
                                {
                                    ushort[] depthFrameData = new ushort[depthFrame.FrameDescription.Height * depthFrame.FrameDescription.Width];
                                    depthFrame.CopyFrameDataToArray(depthFrameData);

                                    cameraSpacePoints = new CameraSpacePoint[depthFrame.FrameDescription.Height * depthFrame.FrameDescription.Width];
                                    kinectSensor.CoordinateMapper.MapDepthFrameToCameraSpace(depthFrameData, cameraSpacePoints);

                                    //templateSearchThread.Start();



                                    isFindingTemplate = false;
                                    templateSearch.Start();

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

                                    }
                                    DrawActivityOnCanvas();
                                    Record();
                                }

                                if (RFID.frameCounter >= 300)
                                {

                                    //foreach (KeyValuePair<ulong, SkeletonPosition> entry in skeletonList)
                                    //{
                                    //    entry.Value.relDistance.Add(RFID.lastTagTime, 0);
                                    //}

                                    skeletonMatch();

                                    RFID.frameCounter = 0;


                                    //skeletonList.Clear();
                                    //RFID.RfidVelocityList.Clear();

                                    //RFID.tagLastTime.Clear();

                                    //lastSkeletonDistance.Clear();
                                }

                                if (RFID.totalCounter >= 1000)
                                {

                                    checkMatch();

                                }
                            }
                        }
                    }
                }
            }
        }

        private void checkMatch()
        {
            //the list to record the skeletonId that need to be removed because when we iterate the dictionary, we cannot do any operate to it like remove or add it.
            List<ulong> skeletonToRemove = new List<ulong>();

            foreach (KeyValuePair<ulong, KeyValuePair<string, double>> match in optMatch)
            {

                if (skeletonList.ContainsKey(match.Key))
                {

                    double[] skeletonDis = new double[skeletonList[match.Key].relDistance.Count];

                    int i = 0;
                    foreach (KeyValuePair<DateTime, double> relDis in skeletonList[match.Key].relDistance)
                    {
                        skeletonDis[i] = relDis.Value;
                        i++;
                    }
                    OnlineMedianFilter filter = new OnlineMedianFilter(5);

                    skeletonDis = filter.ProcessSamples(skeletonDis);


                    int j = 0;
                    double[] rfidVe = new double[RFID.RfidVelocityList[match.Value.Key].velocity.Count];
                    foreach (KeyValuePair<DateTime, double> rfidDis in RFID.RfidVelocityList[match.Value.Key].velocity)
                    {
                        rfidVe[j] = rfidDis.Value;
                        j++;
                    }

                    DTW.SimpleDTW dtw = new DTW.SimpleDTW(skeletonDis, rfidVe);

                    dtw.computeFForward();

                    double dtwDis = dtw.computeFForward();

                    dtwDis = dtwDis / Math.Sqrt(skeletonDis.Length * skeletonDis.Length + rfidVe.Length * rfidVe.Length);

                    if (dtwDis > 0.5)
                    {
                        skeletonToRemove.Add(match.Key);
                    }

                }
                else {

                    skeletonToRemove.Add(match.Key);

                }


            }

            //remove the skeletonMatches which have higher distance
            foreach (ulong skeletonDelete in skeletonToRemove)
            {
                optMatch.Remove(skeletonDelete);
            }

            skeletonList.Clear();
            RFID.RfidVelocityList.Clear();

            RFID.tagLastTime.Clear();

            lastSkeletonDistance.Clear();
            RFID.totalCounter = 0;

        }

        private void skeletonMatch()
        {



            bool hasWrittenrfid = false;
            foreach (KeyValuePair<ulong, SkeletonPosition> skeletonEntry in skeletonList)
            {
                if (!optMatch.ContainsKey(skeletonEntry.Key))
                {

                    double[] skeletonDis = new double[skeletonEntry.Value.relDistance.Count];

                    int i = 0;
                    foreach (KeyValuePair<DateTime, double> relDis in skeletonEntry.Value.relDistance)
                    {
                        skeletonDis[i] = relDis.Value;
                        i++;
                    }

                    OnlineMedianFilter filter = new OnlineMedianFilter(5);

                    skeletonDis = filter.ProcessSamples(skeletonDis);

                    for (int k = 0; k < skeletonDis.Length; k++)
                    {
                        using (StreamWriter writer = new StreamWriter(@"C:\Users\Dongyang\Desktop\" + skeletonEntry.Key + ".txt", true))
                        {
                            writer.WriteLine("{0}", skeletonDis[k]);
                        }
                    }


                    foreach (KeyValuePair<string, RfidVelocity> rfidVelocityEntry in RFID.RfidVelocityList)
                    {
                        int j = 0;
                        double[] rfidVe = new double[rfidVelocityEntry.Value.velocity.Count];

                        foreach (KeyValuePair<DateTime, double> rfidDis in rfidVelocityEntry.Value.velocity)
                        {
                            rfidVe[j] = rfidDis.Value;
                            j++;



                        }

                        OnlineMedianFilter o = new OnlineMedianFilter(5);

                        rfidVe = o.ProcessSamples(rfidVe);

                        if (!hasWrittenrfid)
                        {
                            for (int k = 0; k < rfidVe.Length; k++)
                            {
                                using (StreamWriter writer = new StreamWriter(@"C:\Users\Dongyang\Desktop\" + rfidVelocityEntry.Key + ".txt", true))
                                {
                                    writer.WriteLine("{0}", rfidVe[k]);
                                }
                            }

                        }






                        DTW.SimpleDTW dtw = new DTW.SimpleDTW(skeletonDis, rfidVe);

                        //double[,] matrix = dtw.getFMatrix();

                        //int sequenceLength=findSequenceLength(matrix, -1);

                        double dtwDis = dtw.computeFForward();

                        dtwDis = dtwDis / Math.Sqrt(skeletonDis.Length * skeletonDis.Length + rfidVe.Length * rfidVe.Length);

                        Console.WriteLine(skeletonEntry.Key + " with" + rfidVelocityEntry.Key + "has " + dtwDis + " distance");

                        if (optMatch.ContainsKey(skeletonEntry.Key))
                        {
                            if (optMatch[skeletonEntry.Key].Value > dtwDis)
                            {
                                optMatch[skeletonEntry.Key] = new KeyValuePair<string, double>(rfidVelocityEntry.Key, dtwDis);
                            }
                        }
                        else if (dtwDis <= 0.5)
                        {
                            optMatch.Add(skeletonEntry.Key, new KeyValuePair<string, double>(rfidVelocityEntry.Key, dtwDis));
                        }


                    }

                    hasWrittenrfid = true;
                }
            }

            foreach (Person person in persons)
            {
                if (optMatch.ContainsKey(person.ID))
                {
                    person.tagId = optMatch[person.ID].Key;
                    person.dis = optMatch[person.ID].Value;
                }
                else {
                    person.tagId = "xxx";
                }
                //if (opt.Key == person.ID)
                //{
                //    person.tagId = opt.Value.Key;
                //    break;
                //}
            }

        }

        //private int findSequenceLength(double[,] a,int n) {
        //    int count = 0;

        //    for (int i = a.GetLowerBound(0); i < a.GetUpperBound(0); i++) {

        //        for (int j = a.GetLowerBound(1); j < a.GetUpperBound(1); j++) {
        //            if (a[i, j] == -1) {
        //                count++;
        //            }
        //        }
        //    }

        //    return count;
        //}

        private void RecordSkeletonPosition()
        {

            CheckTrakckedSkeleton();

            for (int i = 0; i < kinectSensor.BodyFrameSource.BodyCount; ++i)
            {
                if (bodies[i].IsTracked)
                {
                    CameraSpacePoint headPositionCamera = Transformation.RotateBackFromTilt(TILT_ANGLE, true, bodies[i].Joints[JointType.Head].Position);

                    Point bodyPosition = new Point(headPositionCamera.X * 100, headPositionCamera.Z * 100);

                    double distance = Transformation.PointDistance(kinectPosition, bodyPosition);

                    if (skeletonList.ContainsKey(bodies[i].TrackingId) && lastSkeletonDistance.ContainsKey(bodies[i].TrackingId))
                    {


                        //relative distance to last time
                        double relDistance = lastSkeletonDistance[bodies[i].TrackingId] - distance;

                        if (relDistance >= 3 || relDistance <= -3)
                        {
                            relDistance = 0;
                        }

                        skeletonList[bodies[i].TrackingId].relDistance.Add(DateTime.Now, relDistance);

                        lastSkeletonDistance[bodies[i].TrackingId] = distance;

                    }
                    else {

                        var s = new SkeletonPosition(bodies[i].TrackingId);

                        s.relDistance.Add(DateTime.Now, 0);

                        skeletonList.Add(bodies[i].TrackingId, s);

                        lastSkeletonDistance.Add(bodies[i].TrackingId, distance);


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

            ActivityRecognition.Record.RecordPosition(persons);
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
                    if (rfidReader == null) rfidReader = new RFID();

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

                    //if (!restartStarted)
                    //{

                    //    applicationRestart.AutoReset = false;
                    //    applicationRestart.Elapsed += ApplicationRestart_Elapsed;
                    //    applicationRestart.Interval = APPLICATION_RESTART_INTERVAL;
                    //    applicationRestart.Enabled = true;
                    //    restartStarted = true;

                    //}
                    //else {
                    //    applicationRestart.Stop();
                    //    applicationRestart.Start();
                    //}

                }
                else {

                    restartStarted = true;
                }




                isRecording = true;
            }



            // Stop recording while the moving person < 1
            if (Transformation.GetNumberOfPeople(persons) < 1)
            {


                if (isRecording)
                {

                    if (!isStoppingRecord)
                    {
                        isStoppingRecord = true;
                        //ErrorLog.LogSystemEvent("Try to stop RFID system because nobody is in the room");
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

            // Object detector - RFID
            //if (objectDetector != null)
            //{
            //    if (objectDetector.Reader != null)
            //    {
            //        if (objectDetector.Reader.IsConnected) objectDetector.Stop();
            //    }
            //    if (rfidThread != null) rfidThread.Abort();
            //}

            if (rfidReader != null)
            {
                //ErrorLog.LogSystemEvent("Try to stop RFID system because main window is closed by user");
                if (rfidReader != null)
                {
                    RFID.IsRFIDOpen = false;
                    rfidReader.Stop();
                }
                if (rfid_Thread != null) rfid_Thread.Abort();
            }




            //ErrorLog.LogSystemEvent("System is now shutting down");
            //ErrorLog.EndLog();
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
            //if (isMouseDown)
            //{
            //    Point currentMousePosition = e.GetPosition(Canvas_Position_Foreground);

            //    double width = currentMousePosition.X - mouseDownPosition.X;
            //    double height = currentMousePosition.Y - mouseDownPosition.Y;

            //    if (width > 0) Rectangle_SelectArea.Width = width;
            //    else
            //    {
            //        Rectangle_SelectArea.Width = -width;
            //        Canvas.SetLeft(Rectangle_SelectArea, currentMousePosition.X);
            //    }

            //    if (height > 0) Rectangle_SelectArea.Height = height;
            //    else
            //    {
            //        Rectangle_SelectArea.Height = -height;
            //        Canvas.SetTop(Rectangle_SelectArea, currentMousePosition.Y);
            //    }
            //}
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

        ///// <summary>
        ///// Popup the view to display the top view in height
        ///// For template detection
        ///// </summary>
        ///// <param name="sender"></param>
        ///// <param name="e"></param>
        //private void Button_ShowHeight(object sender, RoutedEventArgs e)
        //{
        //    //Popup_Area.IsOpen = true;

        //    UpdateButtonColor(HeightViewButton);
        //    UpdateWindowStatement(TopView);
        //}

        //private void Button_ShowDepth(object sender, RoutedEventArgs e)
        //{
        //    //RootCanvas.Visibility = Visibility.Hidden;
        //    UpdateButtonColor(DepthButton);
        //    UpdateWindowStatement(DepthFrame);
        //}

        //private void Button_ShowRoomLayout(object sender, RoutedEventArgs e)
        //{
        //    UpdateButtonColor(RoomLayoutButton);
        //    UpdateWindowStatement(MotherRootCanvas);
        //}

        //private void UpdateButtonColor(Button btnClicked) {
        //    foreach (Button button in btnList)
        //    {
        //        if (button != btnClicked)
        //        {
        //            button.Background = Brushes.White;
        //        }
        //        else {
        //            button.Background = Brushes.Yellow;
        //        }
        //    }
        //}

        //private void UpdateWindowStatement(UIElement showUI) {

        //    foreach (UIElement ui in windowsList) {

        //        if (ui != showUI)
        //        {
        //            ui.Visibility = Visibility.Hidden;
        //        }
        //        else {
        //            ui.Visibility = Visibility.Visible;
        //        }
        //    }

        //}

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








        //private void ListView_Object_SelectionChanged(object sender, SelectionChangedEventArgs e)
        //{

        //}
    }
}
