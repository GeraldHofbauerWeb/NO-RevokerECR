using System;
using System.Collections.Generic;
using System.Linq;

namespace NORevokerECR.Core
{
    // The FS-12 carries its flares in one or more ejectors, and the game adds
    // ejectors with the same name together into one counter. This spreads the
    // EA-12's total over them in the proportion of their vanilla counts, so
    // the sum is exact and the left/right balance stays as it was.
    public static class FlareSplit
    {
        public static int[] Split(int total, IReadOnlyList<int> current)
        {
            int n = current.Count;
            var result = new int[n];
            if (n == 0 || total <= 0) return result;

            double[] weights = current.Select(c => (double)Math.Max(0, c)).ToArray();
            double sum = weights.Sum();
            if (sum <= 0) weights = Enumerable.Repeat(1.0, n).ToArray();
            sum = weights.Sum();

            // Largest remainder: floor every share, then hand out what is left
            // to the largest fractions (ties to the lower index).
            var fractions = new double[n];
            int given = 0;
            for (int i = 0; i < n; i++)
            {
                double exact = total * weights[i] / sum;
                result[i] = (int)Math.Floor(exact);
                fractions[i] = exact - result[i];
                given += result[i];
            }
            foreach (int i in Enumerable.Range(0, n).OrderByDescending(i => fractions[i]).ThenBy(i => i).Take(total - given))
                result[i]++;
            return result;
        }
    }
}
