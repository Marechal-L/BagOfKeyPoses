using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CooperativeCoevolutionaryAlgorithm
{
    class UnitTest
    {
        static void Main(string[] args)
        {
            //populationSelectionAtRandom();
            gaussianTest();

            Console.ReadKey();
        }

        public static void populationSelectionAtRandom()
        {
            Population[] array_populations = new Population[3];
            array_populations[0] = new Population(1, 1);
            array_populations[1] = new Population(1, 1);
            array_populations[2] = new Population(1, 1);

            double res1 = 0 ,prob1 = 3 / 6.0;
            double res2 = 0, prob2 = 2 / 6.0;
            double res3 = 0, prob3 = 1 / 6.0;


            double NbOfTests = 1000;
            for (int i = 0; i < NbOfTests; i++)
            {
                //Select the population to evolve
                Population population = Program.selectPopulationAtRandom(array_populations, new double[] { prob1, prob2, prob3 });
                switch(Array.IndexOf(array_populations,population))
                {
                    case 0: res1 += 1; break;
                    case 1: res2 += 1; break;
                    case 2: res3 += 1; break;
                }
            }

            Console.WriteLine("res1 : " + res1 / NbOfTests + "(" + prob1 + ")");
            Console.WriteLine("res2 : " + res2 / NbOfTests + "(" + prob2 + ")");
            Console.WriteLine("res3 : " + res3 / NbOfTests + "(" + prob3 + ")");
        }

        public static void gaussianTest()
        {
            int MIN_K = 5, MAX_K = 130;

            int[] values = new int[130];

            int mean = (MAX_K + MIN_K) / 2;
            int deriv = (MAX_K - mean) / 4;

            mean = 5;
            deriv = 1;

            Console.WriteLine("Mean : " + mean + "  Deriv : "+deriv);


            double nbTests = 1000;
            for (int i = 0; i < nbTests; i++)
            {
                int val = (int)UsualFunctions.nextGaussian(mean, deriv);

                if(val >= 0)
                    values[val] += 1;
            }
            


            for (int i = 0; i < values.Length; i++)
			{
                if (values[i] != 0)
                    Console.WriteLine(i+" : "+(values[i]/nbTests) * 100);
			}
        }

    }
}
