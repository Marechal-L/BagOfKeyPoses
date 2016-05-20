using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Util;
using Point = System.Collections.Generic.List<double>;


namespace Parser
{
    public static class ContourSelection
    {
        static int PIECE_SIZE = 30;                         // Number of points per piece of pie (RadialAdjustment)
        public static int NUM_PIECES = 14;                         // Number of pie pieces (RadialAdjustment)


        /// <summary>
        /// Takes the contour as a pie of L pieces and returns a summary for each piece.
        /// Takes into account which were selected and which not. (config.ContourSelection)
        /// </summary>
        /// <param name="contour"></param>
        /// <param name="parameters"></param>
        /// <returns>RadialSummary starting at 6h counter-clockwise</returns>
        public static double[] processRadialSummary(List<Point> contour)
        {
            // Compute feature of SelectedL of L summaries
            double[] vec = new double[NUM_PIECES];

            if (contour.Count <= 1) // If only one point is available, distances will be 0
                return vec;

            // Compute the centroid
            Point centroid = getCentroid(contour);

            // Reorder the contour (unnecessary)
            // contour = reorder(contour, upperMost(contour, centroid));

            // Track all the sector points (can't follow a clockwise order because the contour could visit a sector multiple times)
            AssociativeArray<int, List<double>> sectors = new AssociativeArray<int, List<double>>();
            double size = 360.0 / (double)NUM_PIECES; // Size of a sector

            for (int i = 0; i < contour.Count; ++i)
            {
                double dist;
                int idx = radialSector(contour[i], centroid, size, out dist);

                //if (!parameters.UseGenetic || parameters.FeatureSelection[cam][idx] == 1) // But not all are selected
                sectors[idx].Add(dist);
            }

            int sector = 0;
            double sumDists = 0;
            List<int> keys = sectors.Keys;
            keys.Sort();
            foreach (int id in keys) // Compute the summary of each sector (0...L-1, with possible skipping)
            {
                vec[sector] = computeSummary(sectors[id]);
                sumDists += vec[sector];
                sector++;
            }

            // Unit-sum vector normalisation.
            vec = Functions.Normalize(vec, sumDists);

            return vec;
        }

        /// <summary>
        /// Given the current point and the centroid this function returns the distance in between and the current radial sector [0, L-1]
        /// </summary>
        /// <param name="current"></param>
        /// <param name="centroid"></param>
        /// <param name="step"></param>
        /// <param name="distance"></param>
        /// <returns>Sector from 6h counter-clockwise (12h clockwise if its normal coordinates instead of images)</returns>
        private static int radialSector(Point current, Point centroid, double step, out double distance)
        {
            // Use the centroid as the origin
            Point p = new Point{current[0] - centroid[0], current[1] - centroid[1]};
            distance = Math.Sqrt(Math.Pow(p[0], 2.0) + Math.Pow(p[1], 2.0));

            // Compute the inner angle wrt the upper most point and the centroid
            double alpha;
            if (distance == 0)
                alpha = 0;
            else
                alpha = Math.Asin(p[1] / distance) * 180.0 / Math.PI;

            // Take the quadrant into account
            if (alpha > 0)
            {
                if (p[0] >= 0) alpha = 90 - alpha; // First quadrant
                else alpha = 270 + alpha; // Second quadrant
            }
            else
            {
                if (p[0] > 0) alpha = 90 - alpha; // Fourth quadrant
                else alpha = 270 + alpha; // Third quadrant
            }

            return (int)(alpha / step);
        }

        /// <summary>
        /// Computes the centroid of a contour
        /// </summary>
        /// <param name="contour"></param>
        /// <returns></returns>
        private static Point getCentroid(List<Point> contour)
        {
            Point centroid;

            double cx = 0, cy = 0;
            foreach (Point p in contour)
            {
                cx += p[0];
                cy += p[1];
            }

            cx /= (double)contour.Count;
            cy /= (double)contour.Count;

            centroid = new Point{(int)Math.Round(cx, 0), (int)Math.Round(cy, 0)};

            return centroid;
        }

        /// <summary>
        /// Returns a summary for the given distances (highest - lowest value)
        /// </summary>
        /// <param name="distances"></param>
        /// <returns></returns>
        private static double computeSummary(List<double> distances)
        {
            // MAX DEVIATION
            double maxD;
            double min = double.MaxValue, max = double.MinValue;

            foreach (double d in distances)
            {
                if (d < min) min = d;
                if (d > max) max = d;
            }

            maxD = max - min;

            return maxD;
        }
    }
}
