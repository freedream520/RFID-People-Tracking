using System;
using System.IO;
using System.Text;
using System.Collections;
using System.Timers;
using Impinj.OctaneSdk;
using System.Windows;
using System.Reflection;
using System.Collections.Generic;
using NodaTime;
using PeopleTrackingGui;
using System.Collections.ObjectModel;
using ActivityRecognition;

namespace RFID_Beta_5
{
    class RFID
    {
        public static bool IsRFIDOpen = false;

        public readonly static long TIME_DIFFERENCE = 4 * 3600 * 1000 + 18 * 60 * 1000 + 16 * 1000;

        public static System.Timers.Timer drawLineTimer;

        private Dictionary<string, double> anglesToDraw;

        // Create an instance of the ImpinjReader class.
        static ImpinjReader reader = new ImpinjReader();
        public static int[] tags = new int[4];
        Boolean START_TIME_FLAG = false;
        static double startTime;

        Timer RFID_Update_Timer = new Timer();
        static double Ant_1 = 0;
        static double Ant_2 = 0;
        static double Ant_3 = 0;
        static double Ant_4 = 0;

        static double Ant_count_1 = 0;
        static double Ant_count_2 = 0;
        static double Ant_count_3 = 0;
        static double Ant_count_4 = 0;

        // Double for previous phase angle
        static double phaseAngle_1_Past = 0;
        //static double phaseAngle_2_Past = 0;
        static double phaseAngle_3_Past = 0;
        static double phaseAngle_4_Past = 0;
        private Dictionary<string, double> phaseAngle_2_Past;

        // double for previous timestamp
        static double timestamp_1_Past = 0;
        //static double timestamp_2_Past = 0;
        private Dictionary<string, double> timestamp_2_Past;
        static double timestamp_3_Past = 0;
        static double timestamp_4_Past = 0;

        // double for default API value
        static double API_DFS_1 = 0;
        static double API_DFS_2 = 0;
        static double API_DFS_3 = 0;
        static double API_DFS_4 = 0;

        // double for default API value
        static double channel_1_Past = 0;
        //static double channel_2_Past = 0;
        private Dictionary<string, double> channel_2_Past;
        static double channel_3_Past = 0;
        static double channel_4_Past = 0;

        //for test
        static double delta_Pahse_Angle_1_tmp; static double delta_Pahse_Angle_2_tmp; static double delta_Pahse_Angle_3_tmp; static double delta_Pahse_Angle_4_tmp;
        static double delta_Timestamp_1_tmp; static double delta_Timestamp_2_tmp; static double delta_Timestamp_3_tmp; static double delta_Timestamp_4_tmp;
        static double tmp_phase;
        static double timeWriteFile;

        static double Ant_1_previous;
        //static double Ant_2_previous;
        private Dictionary<string, double> Ant_2_previous;
        static double Ant_3_previous;
        static double Ant_4_previous;


        static int Row_count = 1;

        public static string StartTime;
        public static string fileName;

        public static Dictionary<string, RfidVelocity> RfidDistanceList;

        //public static Dictionary<string, DateTime> tagLastTime;

        private Dictionary<string, double> velocityPast;



        public static int frameCounter = 0;

        public static int totalCounter = 0;

        public static DateTime lastTagTime;

        public PeopleTrackingGui.MainWindow m;

        public Dictionary<string, Queue<double>> tagAngleBuffer;

        public readonly double DRAW_LINE_INTERVAL = 1000 * 0.2;

        public static bool hasTagReported = false;

        private Dictionary<string, int> tagLeave;

        public Dictionary<ulong, Point> skeletonLastPoint;

        public Dictionary<string, double> apiPhasePast;



        public RFID(MainWindow m)
        {
            string dir = @"" + PeopleTrackingGui.Properties.Resources.DirectoryRFID;
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            StartTime = DateTime.Now.ToString("M-d-yyyy_HH-mm-ss");
            fileName = @dir + "RFID_" + StartTime + ".txt";

            RfidDistanceList = new Dictionary<string, RfidVelocity>();
            //tagLastTime = new Dictionary<string, DateTime>();
            this.m = m;
            tagAngleBuffer = new Dictionary<string, Queue<double>>();



            anglesToDraw = new Dictionary<string, double>();

            drawLineTimer = new System.Timers.Timer();
            drawLineTimer.AutoReset = true;
            drawLineTimer.Interval = DRAW_LINE_INTERVAL;
            drawLineTimer.Elapsed += updateGraph_Elapse;
            drawLineTimer.Enabled = true;



            phaseAngle_2_Past = new Dictionary<string, double>();
            Ant_2_previous = new Dictionary<string, double>();
            channel_2_Past = new Dictionary<string, double>();
            timestamp_2_Past = new Dictionary<string, double>();
            velocityPast = new Dictionary<string, double>();
            tagLeave = new Dictionary<string, int>();

            skeletonLastPoint = new Dictionary<ulong, Point>();
            apiPhasePast = new Dictionary<string, double>();

        }

        private void resetTagLeave(string tagId)
        {

            if (tagLeave.ContainsKey(tagId))
            {

                tagLeave[tagId] = 0;
            }
        }

        private bool checkTagLeave(String tagId)
        {

            if (tagLeave.ContainsKey(tagId))
            {

                if (tagLeave[tagId] >= 5)
                {

                    return true;
                }
                else
                {

                    tagLeave[tagId] += 1;
                }

            }
            //else {
            //    tagLeave.Add(tagId, 1);
            //}

            return false;

        }

        private double getskeletonVelocity(ulong skeletonId, Point position)
        {
            if (skeletonLastPoint.ContainsKey(skeletonId))
            {
                double distance = Transformation.PointVectorDistance(position, skeletonLastPoint[skeletonId]);
                skeletonLastPoint[skeletonId] = position;
                return distance;
            }
            else {
                skeletonLastPoint.Add(skeletonId, position);
                return 0;
            }

        }

        private void updateGraph_Elapse(object sender, ElapsedEventArgs e)
        {
            if (hasTagReported && m.kinectStarted)
            {
                double angle1 = 0, angle2 = 0;
                double dis1 = 0, velocitySkeleton = 0;
                lock (anglesToDraw)
                {
                    foreach (KeyValuePair<string, double> angle in anglesToDraw)
                    {

                        //if (angle.Key == "0908 2014 9630 0000 0000 6668")
                        //{
                        //    angle1 = angle.Value;
                        //}
                        //else {
                        //    angle2 = angle.Value;
                        //}
                    }
                }


                Dictionary<ulong, Point> skeletonPoint = m.searchSkeletonPosition();
                lock (skeletonPoint)
                {

                    foreach (KeyValuePair<ulong, Point> position in skeletonPoint)
                    {

                        double distance = getskeletonVelocity(position.Key, position.Value);

                        double velocity = distance * 1000 / DRAW_LINE_INTERVAL;

                        if (velocity > 200)
                        {
                            Console.WriteLine("exception");
                        }

                        if (m.skeletonList.ContainsKey(position.Key))
                        {
                            if (!m.skeletonList[position.Key].relDistance.ContainsKey(DateTime.Now))
                                try
                                {
                                    velocitySkeleton = velocity;
                                    m.skeletonList[position.Key].relDistance.Add(DateTime.Now, velocity);
                                }
                                catch (Exception ex)
                                {

                                }

                        }
                        else {
                            var s = new SkeletonPosition(position.Key);

                            s.relDistance.Add(DateTime.Now, velocity);

                            m.skeletonList.Add(position.Key, s);
                        }

                    }



                }

                lock (RfidDistanceList)
                {
                    List<string> recordedTags = new List<string>();
                    ///TODO revise velocity of rfid
                    Dictionary<string, double> copyVelocityPast;
                    lock (velocityPast)
                    {
                        copyVelocityPast = new Dictionary<string, double>(velocityPast);
                    }


                    foreach (KeyValuePair<string, double> velocity in copyVelocityPast)
                    {
                        if (velocity.Value == -1000)
                        {
                            if (checkTagLeave(velocity.Key))
                            {
                                Console.WriteLine(velocity.Key + " move away!!");
                                velocityPast.Remove(velocity.Key);
                            }
                            Console.WriteLine("throw away because of " + velocity.Key);

                            m.searchSkeletonPosition().Clear();
                            return;
                        }
                        else {
                            resetTagLeave(velocity.Key);
                        }
                        double distance = velocity.Value * 100;
                        double apiDis = apiPhasePast[velocity.Key] * 100;

                        if (RfidDistanceList.ContainsKey(velocity.Key))
                        {
                            if (!RfidDistanceList[velocity.Key].distance.ContainsKey(DateTime.Now))
                            {
                                try
                                {
                                    RfidDistanceList[velocity.Key].distance.Add(DateTime.Now, distance);
                                    RfidDistanceList[velocity.Key].api_phase.Add(DateTime.Now, apiDis);
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine(ex.ToString());
                                }

                            }
                        }
                        else
                        {
                            RfidDistanceList.Add(velocity.Key, new RfidVelocity());
                            RfidDistanceList[velocity.Key].distance.Add(DateTime.Now, distance);
                        }

                        if (velocity.Key == "1111 1111 1111 1111 1111 0003")
                        {
                            dis1 = distance;
                        }
                        //else {
                        //    dis2 = distance;
                        //}
                        recordedTags.Add(velocity.Key);
                    }

                    lock (velocityPast)
                    {
                        var keys = velocityPast.Keys;

                        foreach (string key in recordedTags)
                        {

                            velocityPast[key] = -1000;

                        }
                    }







                }





                m.showGraph(dis1, velocitySkeleton);
                frameCounter++;

                totalCounter++;
            }
        }



        public void run()
        {
            try
            {
                reader.Connect(SolutionConstants.ReaderHostname);

                FeatureSet features = reader.QueryFeatureSet();
                Settings settings = reader.QueryDefaultSettings();
                settings.Report.IncludeAntennaPortNumber = true;
                settings.Report.IncludePeakRssi = true;
                settings.Report.IncludeDopplerFrequency = true;
                settings.Report.IncludePhaseAngle = true;
                settings.Report.IncludeLastSeenTime = true;
                settings.Report.IncludeChannel = true;

                settings.ReaderMode = ReaderMode.MaxMiller;
                settings.SearchMode = SearchMode.DualTarget;

                //settings.Filters.TagFilter1.TagMask = "1111";
                //settings.Filters.TagFilter1.MemoryBank = MemoryBank.Epc;
                //settings.Filters.TagFilter1.BitPointer = BitPointers.Epc;
                //settings.Filters.TagFilter1.BitCount = 16;
                //settings.Filters.Mode = TagFilterMode.OnlyFilter1;

                settings.Session = 2;
                settings.TagPopulationEstimate = 20;
                settings.Antennas.DisableAll();
                settings.Antennas.GetAntenna(1).IsEnabled = false;
                settings.Antennas.GetAntenna(2).IsEnabled = true;
                settings.Antennas.GetAntenna(3).IsEnabled = false;
                settings.Antennas.GetAntenna(4).IsEnabled = false;

                //settings.Antennas.GetAntenna(1).MaxTxPower = true;
                //settings.Antennas.GetAntenna(1).MaxRxSensitivity = true;
                settings.Antennas.GetAntenna(2).MaxTxPower = true;
                settings.Antennas.GetAntenna(2).MaxRxSensitivity = true;
                //settings.Antennas.GetAntenna(3).MaxTxPower = true;
                //settings.Antennas.GetAntenna(3).MaxRxSensitivity = true;
                //settings.Antennas.GetAntenna(4).MaxTxPower = true;
                //settings.Antennas.GetAntenna(4).MaxRxSensitivity = true;

                reader.ApplySettings(settings);
                reader.TagsReported += OnTagsReported;
                reader.Start();

                // Start timer
                //RFID_Update_Timer.Elapsed += new ElapsedEventHandler(Update_RFID_Status);
                //RFID_Update_Timer.Interval = 0.5 * 1000;
                //RFID_Update_Timer.Enabled = true;

                // Wait for the user to press enter.
                Console.WriteLine("Press enter to exit.");
                Console.ReadLine();

                //reader.Stop();

            }
            catch (OctaneSdkException e)
            {
                // Handle Octane SDK errors.
                Console.WriteLine("Octane SDK exception: {0}", e.Message);
            }
            catch (Exception e)
            {
                // Handle other .NET errors.
                Console.WriteLine("Exception : {0}", e.Message);
            }
        }


        // Update Excel file
        private void Update_RFID_Status(object source, ElapsedEventArgs e)
        {
            if (Ant_count_1 == 0) Ant_1 = 0;
            else Ant_1 = Ant_1 / Ant_count_1;

            if (Ant_count_2 == 0) Ant_2 = 0;
            else Ant_2 = Ant_2 / Ant_count_2;

            if (Ant_count_3 == 0) Ant_3 = 0;
            else Ant_3 = Ant_3 / Ant_count_3;

            if (Ant_count_4 == 0) Ant_4 = 0;
            else Ant_4 = Ant_4 / Ant_count_4;

            API_DFS_1 = API_DFS_1 / (Ant_count_1 + 1);
            API_DFS_2 = API_DFS_2 / (Ant_count_2 + 1);
            API_DFS_3 = API_DFS_3 / (Ant_count_3 + 1);
            API_DFS_4 = API_DFS_4 / (Ant_count_4 + 1);


            Console.WriteLine(Row_count);
            Ant_1 = 0; Ant_2 = 0; Ant_3 = 0; Ant_4 = 0;
            Ant_count_1 = 0; Ant_count_2 = 0; Ant_count_3 = 0; Ant_count_4 = 0;
            API_DFS_1 = 0; API_DFS_2 = 0; API_DFS_3 = 0; API_DFS_4 = 0;

            if (Row_count > 10000)
            {
                reader.Stop();
                reader.Disconnect();
                Environment.Exit(0);
            }
        }


        // 
        void OnTagsReported(ImpinjReader sender, TagReport report)
        {
            // This event handler is called asynchronously 
            // when tag reports are available.
            // Loop through each tag in the report 
            // Print the data.
            // and write them to the txt

            foreach (Tag tag in report)
            {
                if (!START_TIME_FLAG)
                {
                    startTime = Convert.ToDouble(tag.LastSeenTime.ToString());
                    START_TIME_FLAG = true;
                }
                //tag.Epc.ToString() == "3008 33B2 DDD9 0140 0000 0000"   
                if (tag.Epc.ToString() == "1111 1111 1111 1111 1111 0003" || tag.Epc.ToString() == "1111 1111 1111 1111 1111 0006" /*|| tag.Epc.ToString() == "1111 1111 1111 1111 1111 0009"|| tag.Epc.ToString() == "0908 2014 9630 0000 0000 6667" || tag.Epc.ToString() == "0908 2014 9630 0000 0000 6669" || tag.Epc.ToString() == "0908 2014 9630 0000 0000 6668" || tag.Epc.ToString() == "0908 2014 9630 0000 0000 001E"*/)
                {
                    //Console.WriteLine("lalalallalala: {0}", tag.Ep   c.ToString());
                    if (tag.AntennaPortNumber == 1)
                    {
                        if (Row_count == 1)
                        {
                            phaseAngle_1_Past = tag.PhaseAngleInRadians;
                            timestamp_1_Past = Convert.ToDouble(tag.LastSeenTime.ToString());
                            Ant_1 = 0;
                            Ant_1_previous = 0;
                            tmp_phase = 0;
                            API_DFS_1 = tag.RfDopplerFrequency;
                            channel_1_Past = tag.ChannelInMhz;
                            Row_count++;

                        }
                        else if (tag.ChannelInMhz == channel_1_Past)
                        {

                            delta_Pahse_Angle_1_tmp = (tag.PhaseAngleInRadians - phaseAngle_1_Past);
                            delta_Timestamp_1_tmp = (Convert.ToDouble(tag.LastSeenTime.ToString()) - timestamp_1_Past) / 1000000;
                            tmp_phase = phaseAngle_1_Past;
                            phaseAngle_1_Past = tag.PhaseAngleInRadians;
                            timestamp_1_Past = Convert.ToDouble(tag.LastSeenTime.ToString());
                            Ant_1 = (1 / (4 * Math.PI)) * (delta_Pahse_Angle_1_tmp / delta_Timestamp_1_tmp);
                            API_DFS_1 = tag.RfDopplerFrequency;
                            Ant_count_1++;
                            timeWriteFile = Convert.ToDouble(tag.LastSeenTime.ToString());
                            if (Math.Abs(Ant_1 - 0) < 5 && Math.Abs(Ant_1 - Ant_1_previous) <= 5)
                            {
                                //Write_file(1, Ant_1, timeWriteFile);
                                Ant_1_previous = Ant_1;
                                Row_count++;
                            }
                            else {
                                //reject
                            }
                        }
                        else {
                            //Console.WriteLine("Hopping here");
                            tmp_phase = 0;
                            phaseAngle_1_Past = tag.PhaseAngleInRadians;
                            timestamp_1_Past = Convert.ToDouble(tag.LastSeenTime.ToString());
                            channel_1_Past = tag.ChannelInMhz;

                        }
                    }
                    else if (tag.AntennaPortNumber == 2)
                    {
                        initPastPhase(phaseAngle_2_Past, tag.Epc.ToString());
                        initAntPrevious(Ant_2_previous, tag.Epc.ToString());
                        initChannelPrevious(channel_2_Past, tag.Epc.ToString());

                        if (Row_count == 1)
                        {
                            if (phaseAngle_2_Past.ContainsKey(tag.Epc.ToString()))
                            {

                                phaseAngle_2_Past[tag.Epc.ToString()] = tag.PhaseAngleInRadians;
                            }
                            else {
                                phaseAngle_2_Past.Add(tag.Epc.ToString(), tag.PhaseAngleInRadians);
                            }

                            timestamp_2_Past[tag.Epc.ToString()] = Convert.ToDouble(tag.LastSeenTime.ToString());
                            Ant_2 = 0;
                            Ant_2_previous[tag.Epc.ToString()] = 0;
                            tmp_phase = 0;
                            API_DFS_2 = tag.RfDopplerFrequency;
                            channel_2_Past[tag.Epc.ToString()] = tag.ChannelInMhz;
                            Row_count++;

                        }
                        else if (tag.ChannelInMhz == channel_2_Past[tag.Epc.ToString()])
                        {

                            delta_Pahse_Angle_2_tmp = (tag.PhaseAngleInRadians - phaseAngle_2_Past[tag.Epc.ToString()]) % (2 * Math.PI);

                            //delta_Pahse_Angle_2_tmp = filtingDeltaAngle(tag);

                            delta_Timestamp_2_tmp = (Convert.ToDouble(tag.LastSeenTime.ToString()) - timestamp_2_Past[tag.Epc.ToString()]) / 1000000;
                            tmp_phase = phaseAngle_2_Past[tag.Epc.ToString()];
                            phaseAngle_2_Past[tag.Epc.ToString()] = tag.PhaseAngleInRadians;
                            timestamp_2_Past[tag.Epc.ToString()] = Convert.ToDouble(tag.LastSeenTime.ToString());
                            Ant_2 = (1 / (4 * Math.PI)) * (delta_Pahse_Angle_2_tmp / delta_Timestamp_2_tmp);
                            API_DFS_2 = tag.RfDopplerFrequency;
                            Ant_count_2++;
                            timeWriteFile = Convert.ToDouble(tag.LastSeenTime.ToString());
                            if (Math.Abs(Ant_2 - 0) < 5 && Math.Abs(Ant_2 - Ant_2_previous[tag.Epc.ToString()]) <= 5)
                            {
                                RFID_Beta_5.Velocity v = RFID_Beta_5.Velocity.getVelocity();
                                double velocity = v.v_calculator(tag.ChannelInMhz, Ant_2);
                                double apiVelocity = v.v_calculator(tag.ChannelInMhz, API_DFS_2);
                                //*************************************************************************
                                Write_file(tag.Epc.ToString(), Ant_2, API_DFS_2, velocity, Convert.ToDouble(tag.LastSeenTime.ToString()));
                                //*************************************************************************

                                //RfidVelocityList.Add(tag.Epc.ToString(),)





                                //Console.WriteLine(velocity);
                                RecordTagVelocity(tag.Epc.ToString(), velocity, Convert.ToDouble(tag.LastSeenTime.ToString()), delta_Pahse_Angle_2_tmp, API_DFS_2);

                                //_formatEpc(tag.LastSeenTime.ToString());
                                //if (tag.Epc.ToString() == "0908 2014 9630 0000 0000 6668") {
                                //    m.showGraph(Ant_2);
                                //}

                                Ant_2_previous[tag.Epc.ToString()] = Ant_2;
                                Row_count++;
                            }
                            else {
                                //reject
                            }
                        }
                        else {
                            //Console.WriteLine("Hopping here");
                            tmp_phase = 0;
                            phaseAngle_2_Past[tag.Epc.ToString()] = tag.PhaseAngleInRadians;
                            timestamp_2_Past[tag.Epc.ToString()] = Convert.ToDouble(tag.LastSeenTime.ToString());
                            channel_2_Past[tag.Epc.ToString()] = tag.ChannelInMhz;

                        }
                    }
                    else if (tag.AntennaPortNumber == 3)
                    {
                        if (Row_count == 1)
                        {
                            phaseAngle_3_Past = tag.PhaseAngleInRadians;
                            timestamp_3_Past = Convert.ToDouble(tag.LastSeenTime.ToString());
                            Ant_2 = 0;
                            Ant_2_previous[tag.Epc.ToString()] = 0;
                            tmp_phase = 0;
                            API_DFS_2 = tag.RfDopplerFrequency;
                            channel_2_Past[tag.Epc.ToString()] = tag.ChannelInMhz;
                            Row_count++;

                        }
                        else if (tag.ChannelInMhz == channel_2_Past[tag.Epc.ToString()])
                        {

                            delta_Pahse_Angle_3_tmp = (tag.PhaseAngleInRadians - phaseAngle_3_Past);
                            delta_Timestamp_3_tmp = (Convert.ToDouble(tag.LastSeenTime.ToString()) - timestamp_3_Past) / 1000000;
                            tmp_phase = phaseAngle_3_Past;
                            phaseAngle_3_Past = tag.PhaseAngleInRadians;
                            timestamp_3_Past = Convert.ToDouble(tag.LastSeenTime.ToString());
                            Ant_3 = (1 / (4 * Math.PI)) * (delta_Pahse_Angle_3_tmp / delta_Timestamp_3_tmp);
                            API_DFS_3 = tag.RfDopplerFrequency;
                            Ant_count_3++;
                            timeWriteFile = Convert.ToDouble(tag.LastSeenTime.ToString());
                            if (Math.Abs(Ant_3 - 0) < 5 && Math.Abs(Ant_3 - Ant_3_previous) <= 5)
                            {
                                //Write_file(3, Ant_3, timeWriteFile);
                                Ant_3_previous = Ant_3;
                                Row_count++;
                            }
                            else {
                                //reject
                            }
                        }
                        else {
                            //Console.WriteLine("Hopping here");
                            tmp_phase = 0;
                            phaseAngle_3_Past = tag.PhaseAngleInRadians;
                            timestamp_3_Past = Convert.ToDouble(tag.LastSeenTime.ToString());
                            channel_2_Past[tag.Epc.ToString()] = tag.ChannelInMhz;

                        }
                    }
                    else if (tag.AntennaPortNumber == 4)
                    {
                        if (Row_count == 1)
                        {
                            phaseAngle_4_Past = tag.PhaseAngleInRadians;
                            timestamp_4_Past = Convert.ToDouble(tag.LastSeenTime.ToString());
                            API_DFS_4 += tag.RfDopplerFrequency;
                            channel_4_Past = tag.ChannelInMhz;
                        }
                        else {
                            if (Math.Abs(tag.ChannelInMhz - channel_4_Past) == 0)
                            {
                                double delta_Pahse_Angle = (tag.PhaseAngleInRadians - phaseAngle_4_Past);
                                double delta_Timestamp = (Convert.ToDouble(tag.LastSeenTime.ToString()) - timestamp_4_Past) / 1000000;
                                phaseAngle_4_Past = tag.PhaseAngleInRadians;
                                timestamp_4_Past = Convert.ToDouble(tag.LastSeenTime.ToString());

                                Ant_4 = Ant_4 + (1 / (4 * 3.1416)) * (delta_Pahse_Angle / delta_Timestamp);
                                API_DFS_4 += tag.RfDopplerFrequency;
                                Ant_count_4++;
                            }
                            channel_4_Past = tag.ChannelInMhz;
                        }
                    }
                }

            }
            hasTagReported = true;
        }



        private double filtingDeltaAngle(Tag tag)
        {
            double deltAngle = (tag.PhaseAngleInRadians - phaseAngle_2_Past[tag.Epc.ToString()]) % (2 * Math.PI);
            if (tagAngleBuffer.ContainsKey(tag.Epc.ToString()))
            {
                Queue<double> deltaAngleBuffer = tagAngleBuffer[tag.Epc.ToString()];
                deltaAngleBuffer.Enqueue(deltAngle);
                if (deltaAngleBuffer.Count < 10)
                {

                }
                else {

                    foreach (double angle in deltaAngleBuffer)
                    {
                        deltAngle += angle;
                    }
                    deltAngle = deltAngle / deltaAngleBuffer.Count;
                    deltaAngleBuffer.Dequeue();

                }
            }
            else {
                Queue<double> deltaAngleBuffer = new Queue<double>();
                deltaAngleBuffer.Enqueue(deltAngle);
                tagAngleBuffer.Add(tag.Epc.ToString(), deltaAngleBuffer);
            }
            return deltAngle;
        }

        private void RecordTagVelocity(string tagId, double velocity, double time, double delta_Pahse_Angle_2_tmp, double API_Phase)
        {
            lock (velocityPast)
            {

                var timeReal = GetMbtaDateTime(time);
                //lock (velocityPast)
                //{

                if (!tagLeave.ContainsKey(tagId))
                {
                    tagLeave.Add(tagId, 1);
                }
                if (velocityPast.ContainsKey(tagId)/* && tagLastTime.ContainsKey(tagId)*/)
                {


                    velocityPast[tagId] = velocity;


                    lock (anglesToDraw)
                    {

                        if (anglesToDraw.ContainsKey(tagId))
                        {
                            anglesToDraw[tagId] = velocity;
                        }
                        else {
                            anglesToDraw.Add(tagId, velocity);
                        }

                    }


                }
                else if (!velocityPast.ContainsKey(tagId)/* && !tagLastTime.ContainsKey(tagId)*/)
                {
                    velocityPast.Add(tagId, 0);

                }

                if (apiPhasePast.ContainsKey(tagId))
                {
                    apiPhasePast[tagId] = API_Phase;
                }
                else if (!apiPhasePast.ContainsKey(tagId))
                {
                    apiPhasePast.Add(tagId, API_Phase);
                }
            }
        }


        // Write into a txt file
        public static void Write_file(string tagId, double Ant_2, double API_DFS_2, double velocity, double time)
        {
            FileStream file = new FileStream(fileName, FileMode.Append);
            StreamWriter writer = new StreamWriter(file, Encoding.Default);

            var time2 = GetMbtaDateTime(time);

            writer.WriteLine("{0},{1},{2},{3},{4}",
            tagId, Ant_2, API_DFS_2, velocity, time2.ToString(@"yyyy-MM-dd HH:mm:ss:fff"));

            //Console.WriteLine("{0},{1},{2},{3},{4}",
            //tagId, Ant_2, API_DFS_2, velocity, time2.ToString(@"yyyy-MM-dd HH:mm:ss:fff"));

            writer.Close();
            file.Close();
        }

        public static DateTime FromUnixTime(long unixTime)
        {
            unixTime = unixTime / 1000;
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Local);
            return epoch.AddMilliseconds(unixTime - TIME_DIFFERENCE);
        }

        public static DateTime GetMbtaDateTime(double unixTimestamp)
        {
            unixTimestamp = unixTimestamp / 1000;
            DateTimeZone mbtaTimeZone = DateTimeZoneProviders.Tzdb["America/New_York"];
            DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var mbtaEpochTime = epoch.AddMilliseconds(unixTimestamp);
            var instant = Instant.FromUtc(mbtaEpochTime.Year, mbtaEpochTime.Month,
                mbtaEpochTime.Day, mbtaEpochTime.Hour, mbtaEpochTime.Minute, mbtaEpochTime.Second);
            var nodaTime = instant.InZone(mbtaTimeZone);

            return nodaTime.ToDateTimeUnspecified().AddMilliseconds(mbtaEpochTime.Millisecond);
        }



        public void RFID_stop_recording()
        {
            RFID_Update_Timer.Close();
            reader.Stop();
            reader.Disconnect();
        }

        // modified for multiple reader intances
        public void Stop()
        {

            if (reader.IsConnected)
            {
                try
                {
                    reader.Stop();
                    reader.Disconnect();
                    Console.WriteLine(reader.Name.ToString() + "stoped in RFID");
                    //ErrorLog.LogSystemEvent(reader.Name.ToString() + "stoped in RFID");
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    //ErrorLog.LogSystemError(e.Message);
                }
                finally
                {
                    //if (reader.IsConnected)
                    //{
                    //    reader.Stop();
                    //    Console.WriteLine(reader.Name.ToString() + "stoped in RFID");
                    //    ErrorLog.LogSystemEvent(reader.Name.ToString() + "stoped in RFID");
                    //}
                }

            }
        }
        //ErrorLog.LogSystemEvent("All RFID iS Stopped");
        //ErrorLog.EndLog();

        public void initPastPhase(Dictionary<string, double> phaseAnglePast, string tagId)
        {

            if (phaseAnglePast.ContainsKey(tagId))
            {

                return;
            }
            else {
                phaseAnglePast.Add(tagId, 0);
            }
        }

        private void initAntPrevious(Dictionary<string, double> ant_2_previous, string tagId)
        {
            if (ant_2_previous.ContainsKey(tagId))
            {

                return;
            }
            else {
                ant_2_previous.Add(tagId, 0);
            }
        }

        private void initChannelPrevious(Dictionary<string, double> channel_2_previous, string tagId)
        {
            if (channel_2_previous.ContainsKey(tagId))
            {
                return;
            }
            else {
                channel_2_previous.Add(tagId, 0);
            }
        }

        private void initTimeStampPast(Dictionary<string, double> timestamp_2_previous, string tagId)
        {

            if (timestamp_2_previous.ContainsKey(tagId))
            {
                return;
            }
            else {
                timestamp_2_previous.Add(tagId, 0);
            }

        }

    }
}
