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
*       - MSR (MSR Action3D Dataset - http://research.microsoft.com/en-us/um/people/zliu/actionrecorsrc/ - Files : Skeleton Data in real world coordinates)
*           Folders : AS1, AS2, AS3 and AS
*       - WEIZMANN (Actions as Space-Time Shapes - http://www.wisdom.weizmann.ac.il/~vision/SpaceTimeActions.html - File matlab : http://www.wisdom.weizmann.ac.il/~vision/VideoAnalysis/Demos/SpaceTimeActions/DB/classification_masks.mat)
*           Folders : Weizmann_contours and Weizmann_contours - without skip
 */
#define MSR

/*
*   Leave One Out definition : https://en.wikipedia.org/wiki/Cross-validation_(statistics)#Leave-one-out_cross-validation
*   2-Fold definition : https://en.wikipedia.org/wiki/Cross-validation_(statistics)#2-fold_cross-validation   
*   
*   Define values for the Validation methods
*       - LOSO : leaveOneSequenceOut                - Exhaustive          
*       - LOSOR : leaveOneSequenceOutRandom         - Non Exhaustive (number of rounds)
*       - LOAO : leaveOneActorOut                   - Exhaustive
*       - LOAOR : leaveOneActorOutRandom            - Non Exhaustive (number of rounds)
*       - TWOFOLDSQ : twoFoldOnSequences            - Exhaustive
*       - TWOFOLD : twoFoldHalfActors               - Exhaustive
*       - TWOFOLDS : twoFoldActorsTrainingSet       - Exhaustive
*/
#define LOSO


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Util;
using BagOfKeyPoses;
using Parser;
using Validator;
using Sequence = System.Collections.Generic.List<double[]>;
using TrainDataType = Util.AssociativeArray<string, System.Collections.Generic.List<System.Collections.Generic.List<double[]>>>;

namespace SampleUsage
{
    class Program
    {
        static void Main(string[] args)
        {
            // Run usage sample.
            validateDataset();

            Console.ReadKey();
        }

        /// <summary>
        /// This sample shows how to load the dataset from a directory and how to perform a cross validation.
        /// </summary>
        private static void validateDataset()
        {
            LearningParams learning_params;

            //Load the dataset
            Dataset dataset = loadDataset(out learning_params);

            ResultSet result = null;
            
            //You can change here the save path for the result.
            string filename = "logs/result_";


            //Here are all tests already implemented, you can change which test is executed by changing the #define at the beginning of the file.

#if LOSO
            result = ValidationTest.leaveOneSequenceOut(dataset, learning_params);
            Console.Write("leaveOneSequenceOut : ");
            filename += "LOSO";
#endif
#if LOSOR
            result = ValidationTest.leaveOneSequenceOutRandom(dataset, learning_params,20);
            Console.Write("leaveOneSequenceOutRandom : ");
            filename += "LOSOR";
#endif
#if LOAO
            result = ValidationTest.leaveOneActorOut(dataset, learning_params);
            Console.Write("leaveOneActorOut : ");
            filename += "LOAO";
#endif
#if LOAOR
            result = ValidationTest.leaveOneActorOutRandom(dataset, learning_params);
            Console.Write("leaveOneActorOutRandom : ");
            filename += "LOAOR";
#endif
#if TWOFOLDSQ
            result = ValidationTest.twoFoldOnSequences(dataset, learning_params, 50);
            Console.Write("twoFoldOnSequences : ");
            filename += "TWOFOLDSQ";
#endif
#if TWOFOLD
            result = ValidationTest.twoFoldHalfActors(dataset, learning_params);
            Console.WriteLine("twoFoldHalfActors : ");
            filename += "TWOFOLD";
#endif
#if TWOFOLDSET
            result = ValidationTest.twoFoldActorsTrainingSet(dataset, learning_params, new string[] { "s01", "s03", "s05", "s07", "s09" });
            Console.Write("twoFoldActorsTrainingSet : ");
            filename += "TWOFOLDSET";
#endif

            //Display the result on the console
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
            Dataset dataset = DatasetParser.loadDatasetSkeleton(nbOfJoints, "../../../datasets/MSR/AS", ' ');

            //Normalisation
            dataset.normaliseSkeletons();

            //Init learning_params
            learning_params = new LearningParams();
            learning_params.ClassLabels = dataset.Labels;
            learning_params.Clustering = LearningParams.ClusteringType.Kmeans;
            learning_params.InitialK = 8;
            learning_params.FeatureSize = nbOfJoints * 3;                               //nbOfJoints * dim(xyz)
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
