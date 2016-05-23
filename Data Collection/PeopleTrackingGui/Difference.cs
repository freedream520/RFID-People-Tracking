using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PeopleTrackingGui
{
    class Difference
    {

        public static double getDifference(double[] seq1,double[] seq2) {

            double difference = 0;

            int length = seq1.Length > seq2.Length ? seq2.Length : seq1.Length;

            for (int i = 0; i < length; i++) {
                if (avoidStuck(seq1, i) || avoidStuck(seq2, i))
                {
                    continue;
                }
                difference += Math.Abs(seq1[i] - seq2[i]);
            }

            return difference/length;
        }
        private static bool avoidStuck(double[] seq, int index) {

            int thredHold = 100;
            if (index > 0 && index < seq.Length-1) {
                if (Math.Abs(seq[index] - seq[index - 1]) > thredHold || Math.Abs(seq[index] - seq[index + 1]) > thredHold) {
                    return true;
                }
            }
            return false;
        }
    }
}
