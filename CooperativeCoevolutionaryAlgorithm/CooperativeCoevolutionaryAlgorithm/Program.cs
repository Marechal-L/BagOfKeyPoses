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

//There is the same parameter in Population.cs
#define PARALLEL

#define LOAO

using System.Diagnostics;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Threading.Tasks;
using BagOfKeyPoses;
using Parser;
using Validator;
using Util;
using Sequence = System.Collections.Generic.List<double[]>;
using DataType = Util.AssociativeArray<string, System.Collections.Generic.List<System.Collections.Generic.List<double[]>>>;

namespace CooperativeCoevolutionaryAlgorithm
{
    class Program
    {
        public static Dataset realDataset;                 //Dataset generated from txt files.

        public static int NB_VALIDATION_TESTS = 1;                  //Define the number of rounds per each validation test.
        public static int NB_FEATURES = 20;                         //Number of features
        public static int DIM_FEATURES = 3;                         //Dimension of each feature
        public static int MAX_GENERATION_WITHOUT_CHANGE = 250;
        public static int MAX_GENERATION = 800;
        public static string LogFilename = "logFile.log";          //You can change the output log file here.

        public static Stopwatch timer = new Stopwatch();
        private static readonly object lockEqualIndividual = new object();
        static Population.IndividualType SelectedIndividualType = Population.IndividualType.FEATURES;

        //Entry point of the evolutionary algorithm.
        static void Main(string[] args)
        {
            LogFilename = "Logs/" + LogFilename;
            System.IO.Directory.CreateDirectory("Individuals");
            System.IO.Directory.CreateDirectory("Logs");
            System.IO.Directory.CreateDirectory("Config");
            File.Create(LogFilename).Close();
            timer.Start();

            //Choose the dataset to load.
            //You can implement your own parser. If you already did it,
            //make sure to compile the BagOfKeYPoses_Library solution in release mode first to generate the new .dll file.
            Console.WriteLine("Parsing dataset ...");
            realDataset = DatasetParser.loadDatasetSkeleton(NB_FEATURES, "../../../../BagOfKeyPoses_Library/datasets/MSR/AS1", ' ');
            realDataset.normaliseSkeletons();

            //Parameters of the evolutionary algorithm
            Individual.NB_FEATURES = NB_FEATURES;
            Individual.NB_LABELS = realDataset.Labels.Count();
            Individual.NB_INSTANCES = realDataset.Data.Count();
            IndividualParameters.DEFAULT_K = 8;

            int populationSize = 3; 
            int offspringSize = 1;

    #region Initialisation
            Individual best_features = null, best_parameters = null, best_instances = null;

            //Create initial populations
            Population[] array_populations = new Population[3];

            array_populations[0] = new Population(populationSize, offspringSize);
            array_populations[0].FirstGeneration = Population.FirstGenerationType.ONE_DEFAULT;
            array_populations[0].createFirstGeneration(Population.IndividualType.FEATURES, "Individuals/population_FEATURES.xml");

            array_populations[1] = new Population(populationSize, offspringSize);
            array_populations[1].FirstGeneration = Population.FirstGenerationType.ONE_DEFAULT;
            array_populations[1].createFirstGeneration(Population.IndividualType.PARAMETERS, "Individuals/population_PARAMETERS.xml");

            array_populations[2] = new Population(populationSize, offspringSize);
            array_populations[2].FirstGeneration = Population.FirstGenerationType.ONE_DEFAULT;
            array_populations[2].createFirstGeneration(Population.IndividualType.INSTANCES, "Individuals/population_INSTANCES.xml");
    #endregion

    #region First_Evaluation
            //Evaluate the fitness of each individual of each population
            foreach (Population pop in array_populations)
            {
                if(pop.FirstGeneration != Population.FirstGenerationType.LOAD)
                { 
                    Console.WriteLine("Evaluating the first generation");
                    pop.evaluateFitness();
                    Console.WriteLine();
                    pop.order(populationSize);
                }
            }
    #endregion

    #region Evolutionary_Algorithm_Loop
            //Main loop of the algorithm
            int generationNumber = 0;
            double prev_best_fitness = -1;
            double round_fitness = -1;
            int generations_without_change = 0;
            do
            {
                Console.WriteLine("------------------------------- \r\nRound : " + generationNumber);
                Console.WriteLine(DateTime.Now + " - Time elapsed : " + timer.Elapsed + "\n");

                //Select the population to evolve
                Population population = selectPopulationAtRandom(array_populations,new double[]{2/6.0,2/6.0,2/6.0});
                SelectedIndividualType = population.PopulationType;

                Console.WriteLine("Selected Population : "+SelectedIndividualType);

                //Create new individual
                UsualFunctions.Recombine(population, ref population.Generation[populationSize + offspringSize - 1]);
                Individual individual1 = population.Generation[populationSize + offspringSize - 1];
                individual1.mutate();

                //Select individuals from other populations
                Population[] other_populations = array_populations.Where(x => x != population).ToArray();
                Individual individual2 = UsualFunctions.RankSelection(other_populations[0]);                  //Parameters or Features
                Individual individual3 = UsualFunctions.RankSelection(other_populations[1]);                  //Instances or Parameters
                                                 
                //evaluateFitness(Features, Parameters, Instances);
                switch ((int)SelectedIndividualType)
                {
                    case 0: round_fitness = evaluateFitness((IndividualFeatures)individual1, (IndividualParameters)individual2, (IndividualInstances)individual3); break;
                    case 1: round_fitness = evaluateFitness((IndividualFeatures)individual2, (IndividualParameters)individual1, (IndividualInstances)individual3); break;
                    case 2: round_fitness = evaluateFitness((IndividualFeatures)individual2, (IndividualParameters)individual3, (IndividualInstances)individual1); break;
                }

                //Ordering the all population according to the fitness
                foreach (Population pop in array_populations)
                {
                    pop.order(populationSize + offspringSize);
                }

                //End loop verifications
                if (prev_best_fitness < round_fitness)
                {
                    Console.WriteLine("******* NEW BEST : " + round_fitness + " *******");
                    
                    switch ((int)SelectedIndividualType)
                    {
                        case 0: best_features = (IndividualFeatures)individual1; best_parameters = (IndividualParameters)individual2; best_instances = (IndividualInstances)individual3; break;
                        case 1: best_features = (IndividualFeatures)individual2; best_parameters = (IndividualParameters)individual1; best_instances = (IndividualInstances)individual3; break;
                        case 2: best_features = (IndividualFeatures)individual2; best_parameters = (IndividualParameters)individual3; best_instances = (IndividualInstances)individual1; break;
                    }
                    prev_best_fitness = round_fitness;
                    generations_without_change = 0;

                    Console.WriteLine(best_features.result);
                    addRoundToLog(generationNumber, (IndividualFeatures)best_features, (IndividualParameters)best_parameters, (IndividualInstances)best_instances);
                    foreach (Population pop in array_populations)
                    {
                        pop.ToXML().Save("Individuals/population_" + pop.PopulationType.ToString() + ".xml");
                    }
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
                Console.WriteLine("generations_without_change : " + generations_without_change);
                Console.WriteLine();

                

                generationNumber++;
            } while (generations_without_change < MAX_GENERATION_WITHOUT_CHANGE && generationNumber < MAX_GENERATION);


    #endregion

    #region Writing_Results

            Console.WriteLine("END");
            timer.Stop();

            Console.WriteLine("Time elapsed : " + timer.Elapsed);
            Console.WriteLine("Best round : " + prev_best_fitness);

            //Writing of the results on the console and into a file
            string s = "Best Individuals : \r\n" + best_features + "\r\n" + best_parameters + "\r\n" + best_instances + "\r\n" + best_features.result;

            Console.WriteLine(s);
            string filename = "GeneticResult.log";
            System.IO.File.Create(filename).Close();
            System.IO.StreamWriter writer = new System.IO.StreamWriter(filename);
            writer.Write(s);
            writer.Close();

            System.IO.Directory.CreateDirectory("Individuals");

            array_populations[0].Generation[0].ToXML().Save("Individuals/BestIndividualFeatures.xml");
            array_populations[1].Generation[0].ToXML().Save("Individuals/BestIndividualParameters.xml");
            array_populations[2].Generation[0].ToXML().Save("Individuals/BestIndividualInstances.xml");

    #endregion

            Console.ReadKey();
        }

    #region Fitness_Functions
        /// <summary>
        /// Evaluate the fitness score of the given individual
        /// </summary>
        /// <returns>Boolean representing if the score is better or not</returns> 
        public static bool evaluateFitness(Individual individual)
        {
            ResultSet result = new ResultSet(realDataset.Labels);
            Dataset modifiedDataset = null;
            LearningParams learning_params = null;
            learning_params = new LearningParams();
            learning_params.ClassLabels = realDataset.Labels;
            learning_params.Clustering = LearningParams.ClusteringType.Kmeans;
            learning_params.InitialK = 8;

            if (individual.GetType() == typeof(IndividualFeatures))
            {
                IndividualFeatures individual_features = (IndividualFeatures)individual;

                learning_params.FeatureSize = individual_features.getNbOfOnes() * DIM_FEATURES;
                modifyDataset(ref modifiedDataset, individual_features);
            }
            else if (individual.GetType() == typeof(IndividualParameters))
            {
                IndividualParameters individual_parameters = (IndividualParameters)individual;

                learning_params.SetK(individual_parameters.K);
                modifiedDataset = realDataset;
            }
            else if (individual.GetType() == typeof(IndividualInstances))
            {
                IndividualInstances individual_instances = (IndividualInstances)individual;

                result = ValidationFunctions.TwoFoldTrainingSet(new string[] { "s01", "s03", "s05", "s07", "s09" }, realDataset, learning_params, individual_instances, NB_VALIDATION_TESTS);
                //result = ValidationFunctions.CrossValidation(50, realDataset, learning_params, individual_instances, NB_VALIDATION_TESTS);
                //result = ValidationFunctions.LeaveOneActorOut(realDataset, learning_params, individual_instances);
            }

            double old_f = individual.FitnessScore;

            if (individual.GetType() != typeof(IndividualInstances))
                result = ValidationTest.twoFoldActorsTrainingSet(modifiedDataset, learning_params, new string[] { "s01", "s03", "s05", "s07", "s09" }, NB_VALIDATION_TESTS, false);

            double new_f = result.getAverage();

            if (new_f > old_f)
            {
                individual.FitnessScore = new_f;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Evaluate the fitness score of the coevolutionary algorithm by merging all individuals
        /// </summary>
        /// <returns>Boolean representing if the score is better or not</returns> 
        public static double evaluateFitness(IndividualFeatures individual_features, IndividualParameters individual_parameters, IndividualInstances individual_instances)
        {
            ResultSet result = new ResultSet(realDataset.Labels);
            Dataset modifiedDataset = null;
            LearningParams learning_params = null;

            learning_params = new LearningParams();
            learning_params.ClassLabels = realDataset.Labels;
            learning_params.Clustering = LearningParams.ClusteringType.Kmeans;
            learning_params.InitialK = 8;

            //individual_features
            learning_params.FeatureSize = individual_features.getNbOfOnes() * DIM_FEATURES;
            modifyDataset(ref modifiedDataset, individual_features);

            //individual_parameters
            learning_params.SetK(individual_parameters.K);

            //individual_instances
            if(individual_instances != null)
            {
                result = ValidationFunctions.TwoFoldTrainingSet(new string[] { "s01", "s03", "s05", "s07", "s09" }, realDataset, learning_params, individual_instances, NB_VALIDATION_TESTS);
                //result = ValidationFunctions.CrossValidation(50, realDataset, learning_params, individual_instances, NB_VALIDATION_TESTS);
                //result = ValidationFunctions.LeaveOneActorOut(realDataset, learning_params, individual_instances);
            }
            else 
            {
                result = ValidationTest.twoFoldActorsTrainingSet(modifiedDataset, learning_params, new string[] { "s01", "s03", "s05", "s07", "s09" }, NB_VALIDATION_TESTS, false);
            }

            //Save the result into the individuals.
            switch ((int)SelectedIndividualType)
            {
                case 0:  
                        individual_features.FitnessScore = result.getAverage(); 
                        individual_features.result = result;
                        if (individual_parameters.FitnessScore <= individual_features.FitnessScore)
                        { 
                            individual_parameters.FitnessScore = individual_features.FitnessScore;
                            individual_parameters.result = result;
                        }
                        if (individual_instances.FitnessScore <= individual_features.FitnessScore)
                        { 
                            individual_instances.FitnessScore = individual_features.FitnessScore;
                            individual_instances.result = result;
                        }
                        break;
                case 1:  
                        individual_parameters.FitnessScore = result.getAverage();
                        individual_parameters.result = result;
                        if (individual_features.FitnessScore <= individual_parameters.FitnessScore)
                        {
                            individual_features.FitnessScore = individual_parameters.FitnessScore;
                            individual_features.result = result;
                        }
                        if (individual_instances.FitnessScore <= individual_parameters.FitnessScore)
                        { 
                            individual_instances.FitnessScore = individual_parameters.FitnessScore;
                            individual_instances.result = result;
                        }
                        break;
                case 2: 
                        individual_instances.FitnessScore = result.getAverage();
                        individual_instances.result = result;
                        if (individual_features.FitnessScore <= individual_instances.FitnessScore)
                        { 
                            individual_features.FitnessScore = individual_instances.FitnessScore;
                            individual_features.result = result;
                        }
                        if (individual_parameters.FitnessScore <= individual_instances.FitnessScore)
                        {
                            individual_parameters.FitnessScore = individual_instances.FitnessScore;
                            individual_parameters.result = result;
                        }
                        break;
            }
            return result.getAverage();
        }
    #endregion

        public static void addRoundToLog(int numRound, IndividualFeatures best_features, IndividualParameters best_parameters, IndividualInstances best_instances)
        {
            ResultSet result = (best_features.result != null)?(best_features.result):((best_parameters.result != null)?(best_parameters.result):((best_instances.result != null)?(best_instances.result):(null)));

            StreamWriter logFile = File.AppendText(LogFilename);

            logFile.WriteLine(DateTime.Now + " - Time elapsed : " + timer.Elapsed);
            logFile.WriteLine("Round : " + numRound);
            logFile.WriteLine(best_features);
            logFile.WriteLine(best_parameters);
            logFile.WriteLine(best_instances);
            logFile.WriteLine(result);
            logFile.WriteLine("-------------------------------");

            logFile.Close();
        }

        public static Population selectPopulationAtRandom(Population[] array, double[] probabilities)
        {
            if(array.Length != probabilities.Length)
            {
                Console.WriteLine("Population and probabilities arrays should have the same size");
                return null;
            }

            double diceRoll = UsualFunctions.random.NextDouble();
            double cumulative = 0.0;
            for (int i = 0; i < probabilities.Length; i++)
            {
                cumulative += probabilities[i];
                if (diceRoll < cumulative)
                {
                    return array[i];
                }
            }

            //This should never be reached
            return null;
        }

    #region DATASET_MODIFICATIONS

        /// <summary>
        /// Modifiy the dataset by removing disabled features according to the given individual.
        /// </summary>
        public static void modifyDataset(ref Dataset modifiedDataset, IndividualFeatures individual)
        {
            modifiedDataset = new Dataset(realDataset);

            foreach (DatasetEntry entry in realDataset.Data)
            {
                DatasetEntry copy = new DatasetEntry(entry);

                copy.Sequence = removeDisabledFeatures(individual, entry.Sequence);

                modifiedDataset.Data.Add(copy);
            }
        }

        /// <summary>
        /// Removes disabled features from a sequence of frames
        /// </summary>
        public static Sequence removeDisabledFeatures(IndividualFeatures individual, Sequence sequence)
        {
            int nbOfActivated = individual.getNbOfOnes();

            Sequence seq = new Sequence();

            foreach (Double[] frame in sequence)
            {
                Double[] n_frame = new Double[nbOfActivated * DIM_FEATURES];
                int n_count = 0;

                for (int i = 0; i < frame.Length; i += DIM_FEATURES)
                {
                    if (i % DIM_FEATURES == 0 && individual.Features[i / DIM_FEATURES])
                    {
                        Array.Copy(frame, i, n_frame, n_count, DIM_FEATURES);
                        n_count += DIM_FEATURES;
                    }
                }

                seq.Add(n_frame);
            }

            return seq;
        }

        public static void InitTrainAndTestData(IndividualInstances individual, Dataset dataset, int percentageOfTrainData, out DataType trainData, out DataType testData)
        {
            trainData = new DataType();
            testData = new DataType();

            int i;
            /*

            for (i = 0; i < dataset.Data.Count * (percentageOfTrainData / 100.0); i++)
            {
                if(individual.Instances[i])
                    trainData[dataset.Data[i].Label].Add(dataset.Data[i].Sequence);
                else
                    testData[dataset.Data[i].Label].Add(dataset.Data[i].Sequence);
            }

            for (int j = i; j < dataset.Data.Count; j++)
            {
                if (individual.Instances[j])
                    testData[dataset.Data[j].Label].Add(dataset.Data[j].Sequence);
                else
                    trainData[dataset.Data[j].Label].Add(dataset.Data[j].Sequence);
            }
            */

           
#if LI
            //Li - Training Set
            string[] trainingSet = new string[] { "s01", "s03", "s05", "s07", "s09"};
            string[] testingSet = new string[] { "s02", "s04", "s06", "s08", "s10" };

            for (i = 0; i < dataset.Data.Count; i++)
            {
                if(trainingSet.Contains(dataset.Data[i].Subject))
                { 
                    if(individual.Instances[i])
                    {
                        trainData[dataset.Data[i].Label].Add(dataset.Data[i].Sequence);
                    }
                }
                else
                {
                    testData[dataset.Data[i].Label].Add(dataset.Data[i].Sequence);
                }
            }
#elif CROSS
            //CrossValidation
            Shuffler.Shuffle(dataset.Data);

            for (i = 0; i < dataset.Data.Count * (percentageOfTrainData / 100.0); i++)
            {
                if (individual.Instances[i])
                    trainData[dataset.Data[i].Label].Add(dataset.Data[i].Sequence);
                else
                    testData[dataset.Data[i].Label].Add(dataset.Data[i].Sequence);
            }

            for (int j = i; j < dataset.Data.Count; j++)
            {
                if (individual.Instances[j])
                    testData[dataset.Data[j].Label].Add(dataset.Data[j].Sequence);
                else
                    trainData[dataset.Data[j].Label].Add(dataset.Data[j].Sequence);
            }
#elif LOAO
            //LOAO
            string subject = dataset.getRandomSubject();

            for (i = 0; i < dataset.Data.Count; i++)
            {
                if (subject == dataset.Data[i].Subject)
                {
                    if (individual.Instances[i])
                    {
                        trainData[dataset.Data[i].Label].Add(dataset.Data[i].Sequence);
                    }
                }
                else
                {
                    testData[dataset.Data[i].Label].Add(dataset.Data[i].Sequence);
                }
            }
#endif

        }

        #endregion
    }
}
