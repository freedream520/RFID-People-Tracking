using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PeopleTrackingGui
{
    public class Correlation
    {
        public double[] sequence1, sequence2;
        public int length;

        //Correlation(double[] sequence1,double[] sequence2) {
            
        //    if (sequence1.Length > sequence2.Length)
        //    {
        //        length = sequence2.Length;
        //    }
        //    else {
        //        length = sequence1.Length;
        //    }

        //}

        //public double getCost() {
        //    double cost = 0;
        //    for (int i = 0; i < length; i++) {
        //        cost+=sequence1[i] - sequence2[i];
        //    }
        //    return cost;
        //}

        public double ComputeCoeff(double[] values1, double[] values2)
        {
            double[] val1, val2;
            if (values1.Length > values2.Length)
            {
                val1 = new double[values2.Length];
                Array.ConstrainedCopy(values1, (values1.Length - values2.Length), val1, 0, values2.Length);
                val2 = values2;
            }
            else {
                val2 = new double[values1.Length];
                Array.ConstrainedCopy(values2, (values2.Length - values1.Length), val2, 0, values1.Length);
                val1 = values1;
            }

            var avg1 = val1.Average();
            var avg2 = val2.Average();

            var sum1 = val1.Zip(val2, (x1, y1) => (x1 - avg1) * (y1 - avg2)).Sum();

            var sumSqr1 = val1.Sum(x => Math.Pow((x - avg1), 2.0));
            var sumSqr2 = val2.Sum(y => Math.Pow((y - avg2), 2.0));

            var result = sum1 / Math.Sqrt(sumSqr1 * sumSqr2);

            return result;
        }

        public static double deviation(double[] sequence) {

            double avg=sequence.Average();

            double deviation = 0;

            for (int i = 0; i < sequence.Length; i++) {
                deviation += (sequence[i] - avg) * (sequence[i] - avg);
            }
            return Math.Sqrt(deviation);
        }

        public static double meanDeviation(double[] values1, double[] values2)
        {
            double[] val1, val2;
            if (values1.Length > values2.Length)
            {
                val1 = new double[values2.Length];
                Array.ConstrainedCopy(values1, (values1.Length - values2.Length), val1, 0, values2.Length);
                val2 = values2;
            }
            else {
                val2 = new double[values1.Length];
                Array.ConstrainedCopy(values2, (values2.Length - values1.Length), val2, 0, values1.Length);
                val1 = values1;
            }

            var sum = val1.Zip(val2, (x1, y1) => Math.Abs((x1 - y1))).Sum();
            return sum;
        }
    }
}
