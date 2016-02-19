using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace peopleMatch
{
    class Program
    {
        static void Main(string[] args)
        {
            string[] lines = System.IO.File.ReadAllLines(@"C:\PeopleTracking\Record\Position\2-5-2016_21-51-36.txt");

            string firstLine = lines[0];

            string [] first=firstLine.Split(',');
            //Console.WriteLine(first[1].Trim());

            System.Collections.Generic.Dictionary<string, DateTime> personTime;

            Dictionary<string, int> personSecond;

            DateTime firstTime;


            DateTimeFormatInfo dtFormat = new DateTimeFormatInfo();

            dtFormat.ShortDatePattern = "yyyy-MM-dd HH:mm:ss.ddd";

            firstTime = Convert.ToDateTime(replaceExpress(first[1]),dtFormat);
            personTime = new Dictionary<string, DateTime>();
            personSecond = new Dictionary<string, int>();


            foreach (string line in lines) {

                DateTime dt;

                string[] positiondata = line.Split(',');

                string processedTime = replaceExpress(positiondata[1]);

                dt = Convert.ToDateTime(processedTime, dtFormat);

                string skeletonId = positiondata[0].Trim();

                if (personTime.ContainsKey(skeletonId))
                {
                    TimeSpan ts1 = dt.Subtract(personTime[skeletonId]);
                    double secInterval1 = ts1.TotalSeconds;
                    if (secInterval1 > 1) {
                        
                    }
                    personTime[skeletonId] = dt;
                }
                else {
                    personTime[ skeletonId]= dt;
                }


            }
        }

        private static string replaceExpress(string source)
        {

            int lastIndex = source.LastIndexOf(":");

            string str1 = source.Substring(0, lastIndex);

            string str2 = source.Substring(lastIndex + 1);

            return str1 + "." + str2;
        }
    }
}
