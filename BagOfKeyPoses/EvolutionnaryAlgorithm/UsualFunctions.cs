using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EvolutionnaryAlgorithm
{
    class UsualFunctions
    {
        public static Random random = new Random();

        //Selection functions

        public static Individual RankSelection(Population population)
        {
            //Selection by ranking
            int n = population.PopulationSize;
            int sum = (n * (n + 1)) / 2;
            int randomValue = random.Next(sum);
            //Console.Write("Random value= " + randomValue + " / " + sum);

            int accum = 0;

            for (int individual = 0; individual < n; ++individual)
            {
                accum += (n - individual);
                if (randomValue < accum)
                {
                    return population.Generation[individual];
                }
            }
            return population.Generation[n - 1];
        }

        public static Individual RouletteSelection(Population population)
        {
            int n = population.PopulationSize;

            double totalFitness = 0.0;
            for (int individual = 0; individual < n; ++individual)
                totalFitness += population.Generation[individual].FitnessScore;

            double randomValue = random.NextDouble() * totalFitness;

            //Console.Write("Random value= " + randomValue);

            double accum = 0;

            for (int individual = 0; individual < n; ++individual)
            {
                accum += population.Generation[individual].FitnessScore;

                if (randomValue < accum)
                {
                    return population.Generation[individual];
                }
            }
            return population.Generation[n - 1];
        }

        public static void TournamentSelection()
        {

        }
        

        //Recombination functions
        public static void Recombine(Population population, ref Individual individual)
        {
            Individual father, mother;
            int crossover_point;
 
            //Console.WriteLine("Selecting parents");
 
            father = RankSelection(population);
            do //Sex is better with others
            {
                mother = RankSelection(population);
            } while (father == mother);
 
            /*for (int feature = 0; feature < Individual.NB_FEATURES; ++feature)
                population[individual].feature_vector[feature] = population[father].feature_vector[feature];
            */
            individual = new Individual(father);

            if (random.NextDouble() < 0.75)
            {
#if RECOMBINE_JOINTS
                    RecombineJoints(individual, mother, NUM_FEATURES);
#else
                    crossover_point = random.Next(Individual.NB_FEATURES);

                    for (int feature = crossover_point; feature < Individual.NB_FEATURES; ++feature)
                        individual.Genes[feature] = mother.Genes[feature];
#endif
            }
        }

        public static void RecombineJoints()
        {
            //TODO  - Tree improvement 
        }
    }
}
