/*
   Copyright (C) 2016 Ludovic Marechal and Francisco Flórez-Revuelta

   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.
*/

#define PARALLEL

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.IO;
using System.Threading.Tasks;
using BagOfKeyPoses;
using Parser;
using Validator;
using Sequence = System.Collections.Generic.List<double[]>;

namespace EvolutionaryAlgorithm
{
    class Program
    {
        public static Dataset realDataset;                 //Dataset generated from txt files.

        static int NB_FEATURES = 20;
        static int DIM_FEATURES = 3;                        //Dimension of each feature
        static int MAX_GENERATION_WITHOUT_CHANGE = 20;
        static int MAX_GENERATION = 200;

        //You can change the output log file here.
        static string LogFilename = "logFile.log";

        private static readonly object lock_equalIndividual = new object();

        //Entry point of the evolutionary algorithm.
        static void Main(string[] args)
        {
            File.Create(LogFilename).Close();

            //Choose the dataset to load.
            //You can implement your own parser. If you already did it, make sure to compile the BagOfKeYPoses_Library solution in release mode to generate the new .dll file.
            realDataset = DatasetParser.loadDatasetSkeleton(NB_FEATURES, "../../../../BagOfKeyPoses_Library/datasets/MSR/AS1", ' ');
            realDataset.normaliseSkeletons();

            //Parameters of the evolutionary algorithm
            Individual.NB_FEATURES = NB_FEATURES;
            int populationSize = 10, offspringSize = 20;
            int generations_without_change = 0;
            Individual individual, equalIndividual;

    #region Evolutionary_Algorithm
            //Create initial population
            Population population = new Population(populationSize, offspringSize, NB_FEATURES);
            
            //Evalutate Fitness
            population.evaluateFitness();

            //Order
            population.order(populationSize);
            Console.WriteLine(population);

            double prev_best_fitness = population.Generation[0].FitnessScore;

            //Main loop of the algorithm
            int generationNumber=0;
            do{

#if PARALLEL
                Parallel.For(0, offspringSize, crossover =>
                {
                    //Recombination
                    UsualFunctions.Recombine(population, ref population.Generation[populationSize + crossover]);
                    individual = population.Generation[populationSize + crossover];

                    //Mutation of the new individual
                    individual.mutate();

                    equalIndividual = population.equal(individual);
                    if (equalIndividual == null)
                    {
                        evaluateFitness(individual);
                    }
                    else
                    {
                        Individual tmp = equalIndividual;
                        lock (lock_equalIndividual)
                        {
                            if (evaluateFitness(tmp))
                            {
                                Console.WriteLine("*** Individual " + tmp + "***");
                            }
                        }
                    }
                }); // Parallel.For
#else
                //Sequential For
                for (int crossover = 0; crossover < offspringSize; ++crossover)
                {
                    //Recombination
                    UsualFunctions.Recombine(population, ref population.Generation[populationSize + crossover]);
                    individual = population.Generation[populationSize + crossover];

                    //Mutation of the new individual
                    individual.mutate();

                    equalIndividual = population.equal(individual);
                    if (equalIndividual == null)
                    {
                        evaluateFitness(individual);
                    }
                    else
                    {
                        if (evaluateFitness(equalIndividual))
                        {
                            Console.WriteLine("************************* Individual " + equalIndividual + "************");
                        }
                    }
                }
#endif

                //Ordering the all population according to the fitness
                population.order(populationSize + offspringSize);

                //End loop verifications
                if (prev_best_fitness < population.Generation[0].FitnessScore)
                {
                   
                    prev_best_fitness = population.Generation[0].FitnessScore;
                    generations_without_change = 0;
                    Console.WriteLine("******* NEW BEST : " + prev_best_fitness + "*******");
                    addRoundToLog(generationNumber, population.Generation[0]);
                }
                else
                {
                    generations_without_change++;
                }

                //Displaying informations
                Console.WriteLine();
                Console.WriteLine("Worst Individual : " + population.Generation[populationSize - 1]);
                Console.WriteLine("Best individual : " + population.Generation[0]);

                Console.WriteLine();
                Console.WriteLine("generations_without_change : "+generations_without_change);
                Console.WriteLine("Generation : " + generationNumber);
                Console.WriteLine();

                generationNumber++;
            } while (generations_without_change < MAX_GENERATION_WITHOUT_CHANGE && generationNumber < MAX_GENERATION);

    #endregion 

    #region Writing_Results
            //Writing of the results on the console and into a file
            string s = "Best Individual (gen. " + generationNumber + " ) : " + population.Generation[0] + "\n";
            s += "\nAll population : \n" + population;
            Console.WriteLine(s);

            string filename = "GeneticResult.log";
            System.IO.File.Create(filename).Close();

            System.IO.StreamWriter writer = new System.IO.StreamWriter(filename);
            writer.Write(s);
            writer.Close();

            System.IO.Directory.CreateDirectory("Individuals");
            population.Generation[0].ToXML().Save("Individuals/BestIndividual.xml");
    #endregion

            Console.ReadKey();
        }

        /// <summary>
        /// Evaluate the fitness score of the given individual
        /// </summary>
        /// <returns>Boolean representing if the score is better or not</returns> 
        public static bool evaluateFitness(Individual individual)
        {
	#region Learning_Params
            Dataset modifiedDataset = null;
            LearningParams learning_params = new LearningParams();
            learning_params.ClassLabels = realDataset.Labels;
            learning_params.Clustering = LearningParams.ClusteringType.Kmeans;
            learning_params.InitialK = 8;
            learning_params.FeatureSize = individual.getNbOfOnes() * DIM_FEATURES;

            modifyDataset(ref modifiedDataset, individual);
    #endregion

            double old_f = individual.FitnessScore;

            //You can change the validation method here
            ResultSet result = ValidationTest.twoFoldActorsTrainingSet(modifiedDataset, learning_params, new string[] { "s01", "s03", "s05", "s07", "s09" },1);

            double new_f = result.getAverage();

            if(new_f > old_f)
            {
                individual.FitnessScore = new_f;
                individual.result = result;
                return true;
            }
            return false;
        }

        public static void addRoundToLog(int numRound, Individual bestIndividual)
        {
            StreamWriter logFile = File.AppendText(LogFilename);

            logFile.WriteLine("Round : " + numRound);
            logFile.WriteLine(bestIndividual);
            logFile.WriteLine();
            logFile.WriteLine(bestIndividual.result);
            logFile.WriteLine("-------------------------------");

            logFile.Close();
        }

    #region DATASET_MODIFICATIONS

        /// <summary>
        /// Modifiy the dataset by removing disabled features according to the given individual.
        /// </summary>
        public static void modifyDataset(ref Dataset modifiedDataset, Individual individual)
        {
			modifiedDataset = new Dataset(realDataset);
			
			foreach(DatasetEntry entry in realDataset.Data)
			{
				DatasetEntry copy = new DatasetEntry(entry);

                copy.Sequence = removeDisabledFeatures(individual, entry.Sequence);

                modifiedDataset.Data.Add(copy);
			}
        }

        /// <summary>
        /// Removes disabled features from a sequence of frames
        /// </summary>
        public static Sequence removeDisabledFeatures(Individual individual, Sequence sequence) 
        {
            int nbOfActivated = individual.getNbOfOnes();

            Sequence seq = new Sequence();

            foreach (Double[] frame in sequence)
            {
                Double[] n_frame = new Double[nbOfActivated*DIM_FEATURES];
                int n_count = 0;

                for (int i = 0; i < frame.Length; i+=DIM_FEATURES)
                {
                    if(i%DIM_FEATURES == 0 && individual.Genes[i/DIM_FEATURES])
                    {
                        Array.Copy(frame, i, n_frame, n_count, DIM_FEATURES);
                        n_count += DIM_FEATURES;
                    }
                }

                seq.Add(n_frame);
            }

            return seq;
        }

    #endregion
    }
}
