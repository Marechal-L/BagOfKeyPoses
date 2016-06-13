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

/*
    You can change the Dataset and the Validation method below.
 */

/*
*   Define values for the Dataset 
*       - MSR
*       - WEIZMANN
*       
*      Take a look at the README for more informations. 
*/
#define MSR

/*
*   Define values for the Validation methods
*       - LOSO : leaveOneSequenceOut                - Exhaustive          
*       - LOSOR : leaveOneSequenceOutRandom         - Non Exhaustive (number of rounds)
*       - LOAO : leaveOneActorOut                   - Exhaustive
*       - LOAOR : leaveOneActorOutRandom            - Non Exhaustive (number of rounds)
*       - TWOFOLDSQ : twoFoldOnSequences            - Exhaustive
*       - TWOFOLD : twoFoldHalfActors               - Exhaustive
*       - TWOFOLDSET : twoFoldActorsTrainingSet     - Exhaustive
*       
*      Take a look at the README for more informations. 
*/
#define LOAOR


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BagOfKeyPoses;
using Util;
using Parser;
using Validator;
using Sequence = System.Collections.Generic.List<double[]>;
using TrainDataType = Util.AssociativeArray<string, System.Collections.Generic.List<System.Collections.Generic.List<double[]>>>;

namespace SampleUsage
{
    class SampleUsage_1
    {
        //public static int RandomSeed = 564;                               //Define a specific seed
        public static int RandomSeed = new Random().Next();                 //Let the random be random

        static void Main(string[] args)
        {
            Dataset.RandomSeed = RandomSeed;
            Shuffler.RandomSeed = RandomSeed;
            Functions.RandomSeed = RandomSeed;

            // Run usage sample.
            datasetValidationSample();

            Console.ReadKey();
        }

        /// <summary>
        /// This sample shows how to load the dataset from a directory and how to perform a cross validation.
        /// </summary>
        private static void datasetValidationSample()
        {
            LearningParams learning_params;

            //Load the dataset
            Dataset dataset = loadDataset(out learning_params);

            ResultSet result = null;

            //You can change here the save path for the result.
            System.IO.Directory.CreateDirectory("logs");
            string filename = "logs/result_";


			//Uncomment if you want to save the training
            //ValidationTest.TRAINING = ValidationTest.TRAINING_MODES.SAVE;

            //Here are all tests already implemented, you can change which test is executed by changing the #define at the beginning of the file.

#if LOSO
            result = ValidationTest.leaveOneSequenceOut(dataset, learning_params);
            Console.Write("leaveOneSequenceOut : ");
            filename += "LOSO";
#endif
#if LOSOR
            result = ValidationTest.leaveOneSequenceOutRandom(dataset, learning_params, 20);
            Console.Write("leaveOneSequenceOutRandom : ");
            filename += "LOSOR";
#endif
#if LOAO
            result = ValidationTest.leaveOneActorOut(dataset, learning_params);
            Console.Write("leaveOneActorOut : ");
            filename += "LOAO";
#endif
#if LOAOR
            result = ValidationTest.leaveOneActorOutRandom(dataset, learning_params,4);
            Console.Write("leaveOneActorOutRandom : ");
            filename += "LOAOR";
#endif
#if TWOFOLDSQ
            result = ValidationTest.twoFoldOnSequences(dataset, learning_params, 50, 2);
            Console.Write("twoFoldOnSequences : ");
            filename += "TWOFOLDSQ";
#endif
#if TWOFOLD
            result = ValidationTest.twoFoldHalfActors(dataset, learning_params);
            Console.WriteLine("twoFoldHalfActors : ");
            filename += "TWOFOLD";
#endif
#if TWOFOLDSET
            result = ValidationTest.twoFoldActorsTrainingSet(dataset, learning_params, new string[] { "s01", "s03", "s05", "s07", "s09" },2);
            Console.Write("twoFoldActorsTrainingSet : ");
            filename += "TWOFOLDSET";
#endif

            //Display the final result on the console.
            Console.WriteLine(result);

            //Print the result into a file
            //result.fileOutput(filename+".log");
        }

        /// <summary>
        /// Load a dataset and initializes the learning params 
        /// </summary>
        private static Dataset loadDataset(out LearningParams learning_params)
        {
#if MSR
            //Number of joints of a skeleton
            int nbOfJoints = 20;

            Console.WriteLine("Dataset loading...");

            //Load Dataset of Skeletons
            Dataset dataset = DatasetParser.loadDatasetSkeleton(nbOfJoints, "../../../datasets/MSR/AS1", ' ');

            //Normalisation
            dataset.normaliseSkeletons();

            //Init learning_params
            learning_params = new LearningParams();
            learning_params.ClassLabels = dataset.Labels;
            learning_params.Clustering = LearningParams.ClusteringType.Kmeans;
            learning_params.InitialK = 8;
            learning_params.FeatureSize = nbOfJoints * 3;                                //nbOfJoints * dim(xyz)
#endif
#if WEIZMANN

            // Number of pie pieces (RadialAdjustment)
            Parser.ContourSelection.NUM_PIECES = 14;

            Console.WriteLine("Dataset loading...");
            //Dataset dataset = DatasetParser.loadDatasetSilhouette("../../../datasets/Weizmann/Weizmann_contours", ' ');
            Dataset dataset = DatasetParser.loadDatasetSilhouette("../../../datasets/Weizmann/Weizmann_contours - without skip", ' ');
            
            //Init learning_params
            learning_params = new LearningParams();
            learning_params.ClassLabels = dataset.Labels;
            learning_params.Clustering = LearningParams.ClusteringType.Kmeans;
            learning_params.InitialK = 8;
            learning_params.FeatureSize = Parser.ContourSelection.NUM_PIECES;
#endif

            return dataset;
        }
    }
}
