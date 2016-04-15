using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RFID_Beta_5
{
    class Velocity 
    {
        public double v = 0;
        public double Ft = 0;
        public double Fd = 0;
        private static Velocity velocity = null; 
        private Velocity() {
            //Ft = F_tag;
        }

        public static Velocity getVelocity() {
            if (velocity == null) {
                velocity = new Velocity();
            }

            return velocity;
        }

        //public Velocity(double v)
        //{
        //    this.v = v;
        //}
        public double v_calculator(double Ft,double Fd) {
            v = (Fd * 3e8) / (1 * Ft*1e6);

            return v;
        }


        public void  v_calculator(double Ft)
        {
            //Ft = 1;
            string[] lines = System.IO.File.ReadAllLines(@"C:\People_Tracking_RFID\Data\Doppler.txt");

            foreach (string line in lines) {

                try
                {
                    string subLine=line.Substring(line.IndexOf(","));
                    string[] num_time=subLine.Split(',');
                    Fd = Convert.ToDouble(num_time[1]);
                    v = (Fd * 3e8) / (2 * Ft);
                    using (StreamWriter writer = new StreamWriter(@"C:\People_Tracking_RFID\Data\result.txt", true))
                    {
                        writer.WriteLine("{0},{1}", v, num_time[2]);
                    }

                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
                
            }
            
            
        }

    }
}
