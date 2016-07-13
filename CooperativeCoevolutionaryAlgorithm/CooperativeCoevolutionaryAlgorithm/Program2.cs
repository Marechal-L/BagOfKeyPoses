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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Parser;
using Validator;
using BagOfKeyPoses;
using DataType = Util.AssociativeArray<string, System.Collections.Generic.List<System.Collections.Generic.List<double[]>>>;

namespace CooperativeCoevolutionaryAlgorithm
{
    /// <summary>
    /// A simple program to run a validation with specific individuals
    /// </summary>
    class Program2
    {

        public static int NB_VALIDATION_TESTS = 5;                  //Define the number of rounds per each validation test.
        public static int NB_FEATURES = 20;
        public static int DIM_FEATURES = 3;
        public static string FeaturesIndividualFile = "Individuals/BestIndividualFeatures.xml";
        public static string ParametersIndividualFile = "Individuals/BestIndividualParameters.xml";
        public static string InstancesIndividualFile = "Individuals/BestIndividualInstances.xml";

        static void Main(string[] args)
        {
            ValidationTest.TRAINING = ValidationTest.TRAINING_MODES.CLASSIC;
            FeaturesIndividualFile = "Individuals/3Joints.xml";

            Program.realDataset = DatasetParser.loadDatasetSkeleton(NB_FEATURES, "../../../../BagOfKeyPoses_Library/datasets/MSR/AS1", ' ');
            Program.realDataset.normaliseSkeletons();
            XmlDocument doc = new XmlDocument();

            IndividualFeatures individual_features = new IndividualFeatures();
            doc.Load(FeaturesIndividualFile);
            individual_features.LoadXML(doc);

            IndividualParameters individual_parameters = new IndividualParameters();
            doc.Load(ParametersIndividualFile);
            individual_parameters.LoadXML(doc);

            IndividualInstances individual_instances = new IndividualInstances();
            doc.Load(InstancesIndividualFile);
            individual_instances.LoadXML(doc);

            Program.evaluateFitness(individual_features, individual_parameters, individual_instances, true);
            Program.evaluateFitness(individual_features);

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
            learning_params.ClassLabels = Program.realDataset.Labels;
            learning_params.Clustering = LearningParams.ClusteringType.Kmeans;
            learning_params.InitialK = 8;

            if (individual.GetType() == typeof(IndividualFeatures))
            {
                IndividualFeatures individual_features = (IndividualFeatures)individual;

                learning_params.FeatureSize = individual_features.getNbOfOnes() * DIM_FEATURES;
                Program.modifyDataset(ref modifiedDataset, individual_features);
            }
            else if (individual.GetType() == typeof(IndividualParameters))
            {
                IndividualParameters individual_parameters = (IndividualParameters)individual;

                learning_params.SetK(individual_parameters.K);
                modifiedDataset = Program.realDataset;
            }
            else if (individual.GetType() == typeof(IndividualInstances))
            {
                IndividualInstances individual_instances = (IndividualInstances)individual;

                result = ValidationFunctions.TwoFoldTrainingSet(new string[] { "s01", "s03", "s05", "s07", "s09" }, modifiedDataset, learning_params, individual_instances, NB_VALIDATION_TESTS);
            }

            if (individual.GetType() != typeof(IndividualInstances))
                result = ValidationTest.twoFoldActorsTrainingSet(modifiedDataset, learning_params, new string[] { "s01", "s03", "s05", "s07", "s09" }, 2);

            Console.WriteLine(result);

            return false;
        }

        /// <summary>
        /// Evaluate the fitness score of the coevolutionary algorithm by merging all individuals
        /// </summary>
        /// <returns>Boolean representing if the score is better or not</returns> 
        public static double evaluateFitness(IndividualFeatures individual_features, IndividualParameters individual_parameters, IndividualInstances individual_instances)
        {
            ResultSet result = new ResultSet(Program.realDataset.Labels);
            Dataset modifiedDataset = null;
            LearningParams learning_params = null;

            learning_params = new LearningParams();
            learning_params.ClassLabels = Program.realDataset.Labels;
            learning_params.Clustering = LearningParams.ClusteringType.Kmeans;
            learning_params.InitialK = 8;

            //individual_features
            learning_params.FeatureSize = individual_features.getNbOfOnes() * DIM_FEATURES;
            Program.modifyDataset(ref modifiedDataset, individual_features);

            //individual_parameters
            learning_params.SetK(individual_parameters.K);

            //individual_instances
            if (individual_instances != null)
            {
                result = ValidationFunctions.TwoFoldTrainingSet(new string[] { "s01", "s03", "s05", "s07", "s09" }, modifiedDataset, learning_params, individual_instances, NB_VALIDATION_TESTS);
            }
            else
            {
                result = ValidationTest.twoFoldActorsTrainingSet(modifiedDataset, learning_params, new string[] { "s01", "s03", "s05", "s07", "s09" }, NB_VALIDATION_TESTS);
            }

            Console.WriteLine(result);
            return result.getAverage();
        }
    }
}
