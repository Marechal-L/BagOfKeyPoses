/*
   Copyright (C) 2014 Alexandros Andre Chaaraoui

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
using Util;
using BagOfKeyPoses;
using Parser; 
using Sequence = System.Collections.Generic.List<double[]>;
using TrainDataType = Util.AssociativeArray<string, System.Collections.Generic.List<System.Collections.Generic.List<double[]>>>;


namespace SampleUsage
{
    class Program
    {
        static void Main(string[] args)
        {
            // Run usage samples.

            /*
            sequenceBasedSimpleSample();
            sequenceBasedAdvancedSample();
            continuousRecognitionSample();
            
            */

            datasetLoadingAndValidationSample();

            Console.ReadKey();
        }
        
        /// <summary>
        /// This sample shows how to load skeletons from a dataset and performs a cross validation on the dataset. 
        /// </summary>
        private static void datasetLoadingAndValidationSample()
        {
            int nbOfJoints = 20;

            Console.WriteLine("Dataset loading...");
            //Load Skeleton Dataset
            Dataset dataset = DatasetParser.loadDatasetSkeleton(nbOfJoints, "../../../AS1", ' ');

            //Normalisation
            for (int i = dataset.Datas.Count-1; i > 0; i--)
            {
                DatasetEntry entry = dataset.Datas[i];
                entry.Sequence = SkeletonNormalisation.normaliseSequenceSkeleton(entry.Sequence);
                if (entry.Sequence.Count == 0)
                    dataset.Datas.Remove(entry);
            }

            //Init learning_params
            LearningParams learning_params = new LearningParams();
            learning_params.ClassLabels = dataset.Labels;
            learning_params.Clustering = LearningParams.ClusteringType.Kmeans;
            learning_params.InitialK = 8;
            learning_params.FeatureSize = nbOfJoints * 3;

            double average = 0;
            /*average = atRandom(dataset, learning_params);
            Console.WriteLine("AtRandom : " + average);
            average = leaveOneActorOut(dataset, learning_params);
            Console.WriteLine("leaveOneActorOut : " + average);
            average = leaveOneSequenceOut(dataset, learning_params);
            Console.WriteLine("leaveOneSequenceOut : " + average);*/
            average = twoFoldHalfActors(dataset, learning_params);
            Console.WriteLine("twoFoldHalfActors : " + average);
            //average = twoFoldActorsTrainingSet(dataset, learning_params, new string[]{ "s01", "s03", "s05", "s07", "s09" });
            //Console.WriteLine("twoFoldActorsTrainingSet : " + average);


        }

        /// <summary>
        /// Performs a cross validation at random on the given dataset.
        /// </summary>
        private static double atRandom(Dataset dataset, LearningParams learning_params, int nbOfRounds = 10)
        {
            Console.WriteLine("Cross Validation at Random");

            TrainDataType trainData, testData;

            double average = 0;
            for (int i = 0; i < nbOfRounds; i++)
            {
                Console.WriteLine("Data extraction...");
                dataset.initTrainAndTestData(50, out trainData, out testData);
                average += crossValidation(learning_params, trainData, testData);
            }

            
            average /= nbOfRounds;
            return average;
        }

        /// <summary>
        /// Performs a leave-One-Actor-Out (LOAO) cross validation on the given dataset.
        /// </summary>
        private static double leaveOneActorOut(Dataset dataset, LearningParams learning_params, int nbOfRounds = 10)
        {
            Console.WriteLine("Cross Validation LOAO");

            TrainDataType trainData, testData;

            double average = 0;
            for (int i = 0; i < nbOfRounds; i++)
            {
                Console.WriteLine("Data extraction...");
                string subject = dataset.getRandomSubject();
                dataset.initTrainAndTestData(subject, out trainData, out testData);
                average += crossValidation(learning_params, trainData, testData);
            }

           
            average /= nbOfRounds;
            return average;
        }

        /// <summary>
        /// Performs a 2-fold cross validation on the given dataset, randomly choose the half of actors for training.
        /// </summary>
        private static double twoFoldHalfActors(Dataset dataset, LearningParams learning_params, int nbOfRounds = 10)
        {
            Console.WriteLine("Cross Validation 2-fold half actors");
            
            TrainDataType trainData, testData;

            double average = 0;
            for (int i = 0; i < nbOfRounds; i++)
            {
                Console.WriteLine("Data extraction...");

                dataset.initTrainAndTestData(out trainData, out testData);
                average += crossValidation(learning_params, trainData, testData);
                average += crossValidation(learning_params, testData, trainData);
            }
            
            average /= nbOfRounds * 2;
            return average;
        }

        /// <summary>
        /// Performs a 2-fold cross validation on the given dataset, take the actors set given for training and the others for testing.
        /// </summary>
        private static double twoFoldActorsTrainingSet(Dataset dataset, LearningParams learning_params, string[] actorsTrainingSet, int nbOfRounds = 10)
        {
            Console.WriteLine("Cross Validation 2-fold training actors set");

            TrainDataType trainData, testData;

            double average = 0;
            for (int i = 0; i < nbOfRounds; i++)
            {
                Console.WriteLine("Data extraction...");

                dataset.initTrainAndTestData(actorsTrainingSet, out trainData, out testData);
                average += crossValidation(learning_params, trainData, testData);
                average += crossValidation(learning_params, testData, trainData);
            }

            average /= nbOfRounds * 2;
            return average;
        }

        /// <summary>
        /// Performs a leave-One-Sequence-Out (LOSO) cross validation on the given dataset.
        /// </summary>
        private static double leaveOneSequenceOut(Dataset dataset, LearningParams learning_params, int nbOfRounds = 10)
        {
            Console.WriteLine("Cross Validation LOSO");

            TrainDataType trainData, testData;

            double average = 0;
            for (int i = 0; i < nbOfRounds; i++)
            {
                Console.WriteLine("Data extraction...");
                Sequence sequence = dataset.getRandomSequence();
                dataset.initTrainAndTestData(sequence, out trainData, out testData);
                average += crossValidation(learning_params, trainData, testData);
            }
            average /= nbOfRounds;
            return average;
        }

        /// <summary>
        ///  The core of the cross validation. Train the model and test it with the given parameters.
        /// </summary>
        private static double crossValidation(LearningParams learning_params, TrainDataType trainData, TrainDataType testData)
        {
            //Cross Validation
            double nbTests = 0, roundSuccess = 0;

            Console.WriteLine("Training...");
            BoKP bokp = new BoKP(learning_params);
            bokp.Train(trainData.Dictionary);

            Console.WriteLine("Testing...");
            foreach (string label in learning_params.ClassLabels)
            {
                foreach (var sequence in testData[label])
                {
                    nbTests++;
                    string recognition = bokp.EvaluateSequence(sequence);
                    if (label == recognition)
                    {
                        roundSuccess++;
                    }
                }
            }

            double res = (roundSuccess / nbTests) * 100;
            Console.WriteLine("Success : " + res);
            return res;
        }
    }
}
