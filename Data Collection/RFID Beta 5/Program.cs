using System;
using System.Data;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using Accord.MachineLearning.DecisionTrees;
using Accord.MachineLearning.DecisionTrees.Learning;
using Accord.Math;
using Accord.Statistics.Analysis;
using Accord.IO;
using AForge;
using System.IO;


namespace RFID_Beta_5
{
    class Program
    {
        static private Thread rThread = null;

        static void Main(string[] args)
        {

            // Generate Trainning Data
            RFID Impinj_RFID = new RFID();

            // If trainning phase is not performed yet, run the following command
            rThread = new Thread(new ThreadStart(Impinj_RFID.run));
            rThread.Start();

        }
    }
}
