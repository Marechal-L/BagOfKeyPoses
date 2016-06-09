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

using System.Diagnostics;
using System;
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

        static int NB_FEATURES = 20;
        static int DIM_FEATURES = 3;                        //Dimension of each feature
        static int MAX_GENERATION_WITHOUT_CHANGE = 100;
        static int MAX_GENERATION = 500;

        private static readonly object lockEqualIndividual = new object();

        static Population.IndividualType SelectedIndividualType = Population.IndividualType.FEATURES;

        //Entry point of the evolutionary algorithm.
        static void Main(string[] args)
        {
            realDataset = DatasetParser.loadDatasetSkeleton(NB_FEATURES, "../../../../BagOfKeyPoses_Library/datasets/MSR/AS1", ' ');
            realDataset.normaliseSkeletons();

            //Parameters of the evolutionary algorithm
            Individual.NB_FEATURES = NB_FEATURES;
            Individual.NB_LABELS = realDataset.Labels.Count();
            Individual.NB_INSTANCES = realDataset.Data.Count();

            int populationSize = 10, offspringSize = 1;
            int generations_without_change = 0;
            Individual individual, equalIndividual;

            //Create initial populations

            Population[] array_populations = new Population[3];

            array_populations[0] = new Population(populationSize, offspringSize);
            array_populations[0].createFirstGeneration(Population.IndividualType.FEATURES);

            array_populations[1] = new Population(populationSize, offspringSize);
            array_populations[1].createFirstGeneration(Population.IndividualType.PARAMETERS);

            array_populations[2] = new Population(populationSize, offspringSize);
            array_populations[2].createFirstGeneration(Population.IndividualType.INSTANCES);

            //Evaluate the fitness of each individual of each population
            foreach(Population pop in array_populations)
            {
                pop.evaluateFitness();
                pop.order(populationSize);
            }


            double prev_best_fitness = -1;
            double fitness_round = -1;

            //Main loop of the algorithm
            int generationNumber = 0;
            do
            {
                //Select the population to evolve
                Population population = array_populations[UsualFunctions.random.Next(array_populations.Length)];
                SelectedIndividualType = population.PopulationType;

                //Create new individual
                UsualFunctions.Recombine(population, ref population.Generation[populationSize + offspringSize - 1]);
                Individual individual1 = population.Generation[populationSize + offspringSize - 1];
                individual1.mutate();

                //Select individuals from other populations
                Population[] other_populations = array_populations.Where(x => x != population).ToArray();
                Individual individual2 = other_populations[0].Generation[0];                                //Parameters or Features
                Individual individual3 = other_populations[1].Generation[0];                                //Instances or Parameters

                //evaluateFitness(Features, Parameters, Instances);
                switch ((int)SelectedIndividualType)
                {
                    case 0: fitness_round = evaluateFitness((IndividualFeatures)individual1, (IndividualParameters)individual2, (IndividualInstances)individual3); break;
                    case 1: fitness_round = evaluateFitness((IndividualFeatures)individual2, (IndividualParameters)individual1, (IndividualInstances)individual3); break;
                    case 2: fitness_round = evaluateFitness((IndividualFeatures)individual2, (IndividualParameters)individual3, (IndividualInstances)individual1); break;
                }
                Console.WriteLine(fitness_round);


                //Ordering the all population according to the fitness
                foreach (Population pop in array_populations)
                {
                    pop.order(populationSize + offspringSize);
                }

                //End loop verifications
                if (prev_best_fitness < fitness_round)
                {
                    prev_best_fitness = fitness_round;
                    generations_without_change = 0;
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
                Console.WriteLine("Generation : " + generationNumber);
                Console.WriteLine();

                generationNumber++;
            } while (generations_without_change < MAX_GENERATION_WITHOUT_CHANGE && generationNumber < MAX_GENERATION);
            
            /*
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
            */
            Console.ReadKey();
        }

        /// <summary>
        /// Evaluate the fitness score of the given individual
        /// </summary>
        /// <returns>Boolean representing if the score is better or not</returns> 
        public static bool evaluateFitness(Individual individual)
        {
            ResultSet result = null;
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

                DataType trainData, testData;
                InitTrainAndTestData(individual_instances, realDataset, 50, out trainData, out testData);

                result = ValidationTest.crossValidationResultSet(learning_params, trainData, testData);
            }

            double old_f = individual.FitnessScore;

            if (individual.GetType() != typeof(IndividualInstances))
                result = ValidationTest.twoFoldActorsTrainingSet(modifiedDataset, learning_params, new string[] { "s01", "s03", "s05", "s07", "s09" }, 1);

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
            ResultSet result = null;
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
            DataType trainData, testData;
            InitTrainAndTestData(individual_instances, realDataset, 50, out trainData, out testData);
            result = ValidationTest.crossValidationResultSet(learning_params, trainData, testData);

            //evaluateFitness();
            switch ((int)SelectedIndividualType)
            {
                case 0:  
                        individual_features.FitnessScore = result.getAverage();
                        if (individual_parameters.FitnessScore < individual_features.FitnessScore)
                            individual_parameters.FitnessScore = individual_features.FitnessScore;
                        if (individual_instances.FitnessScore < individual_features.FitnessScore)
                            individual_instances.FitnessScore = individual_features.FitnessScore;
                        break;
                case 1:  
                        individual_parameters.FitnessScore = result.getAverage();
                        if (individual_features.FitnessScore < individual_parameters.FitnessScore)
                            individual_features.FitnessScore = individual_parameters.FitnessScore;
                        if (individual_instances.FitnessScore < individual_parameters.FitnessScore)
                            individual_instances.FitnessScore = individual_parameters.FitnessScore;
                        break;
                case 2: 
                        individual_instances.FitnessScore = result.getAverage();
                        if (individual_features.FitnessScore < individual_instances.FitnessScore)
                            individual_features.FitnessScore = individual_instances.FitnessScore;
                        if (individual_parameters.FitnessScore < individual_instances.FitnessScore)
                            individual_parameters.FitnessScore = individual_instances.FitnessScore;
                        break;
            }
            return result.getAverage();
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

            //Shuffler.Shuffle(dataset.Data);
            int i;
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
        }

        #endregion
    }
}
