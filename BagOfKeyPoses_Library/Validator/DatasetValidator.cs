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


//Look at crossValidationResultSet() function for the purpose of replaying the validation.
//Define value to set if the train model will be saved, loaded or executed normally.
/*
 * SAVE
 * LOAD
 */
#define LOAD

using System;
using System.Xml;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using Util;
using Parser;
using BagOfKeyPoses;
using TrainDataType = Util.AssociativeArray<string, System.Collections.Generic.List<System.Collections.Generic.List<double[]>>>;

namespace Validator
{
    /// <summary>
    /// Store and compute the results of a validation (average and confusion matrix).
    /// Same class for a single result or a global result.
    /// </summary>
    public class ResultSet
    {
        public int[] TestsPerLabel, SuccessesPerLabel;
        public double TotalTests, TotalSuccesses;

        private double[,] confusionMatrix;
        private List<string> labels;
        private bool isGlobalResultSet;

        public ResultSet()
        {
            TotalTests = 0;
            TotalSuccesses = 0;
            isGlobalResultSet = false;
        }

        public ResultSet(List<string> label)
            :this()
        {
            this.labels = label;
          
            int nbOfLabels = labels.Count;
            TestsPerLabel = new int[nbOfLabels];
            SuccessesPerLabel = new int[nbOfLabels];
            confusionMatrix = new double[nbOfLabels, nbOfLabels];
        }

        /// <summary>
        /// Computes and returns the confusion matrix. 
        /// single result : Numbers format
        /// global result : Percentage format 
        /// </summary>
        public double[,] getConfusionMatrix()
        {
            if (isGlobalResultSet)
            {
                double[,] tmp = new double[labels.Count, labels.Count];

                for (int i = 0; i < tmp.Length / labels.Count; i++)
                {
                    for (int j = 0; j < tmp.Length / labels.Count; j++)
                    {
                        if (isGlobalResultSet && TestsPerLabel[i] != 0)
                            tmp[i, j] = (confusionMatrix[i, j] / TestsPerLabel[i]) * 100;
                    }
                }
                return tmp;
            }
            else
                return confusionMatrix;
        }

        /// <summary>
        /// Computes and returns the confusion matrix as percentage for a single result.
        /// </summary>
        public double[,] getConfusionMatrixPercent()
        {
            double[,] tmp = new double[labels.Count,labels.Count];

            for (int i = 0; i < tmp.Length / labels.Count; i++)
            {
                for (int j = 0; j < tmp.Length / labels.Count; j++)
                {
                    if (TestsPerLabel[i] != 0)
                        tmp[i, j] = (confusionMatrix[i, j] / TestsPerLabel[i]) * 100;
                }
            }

            return tmp;
        }

        /// <summary>
        /// Returns the average of successes.
        /// </summary>
        public double getAverage()
        {
            return TotalSuccesses / TotalTests;
        }

        /// <summary>
        /// Adds a test to a single result. 
        /// </summary>
        /// <param name="testedLabel">The real label under test</param>
        /// <param name="recognizedLabel">The answer of the recognition algorithm</param>
        public void addTest(string testedLabel, string recognizedLabel)
        {
            TotalTests++;
         
            int testIndex = labels.IndexOf(testedLabel);
            int resultIndex = labels.IndexOf(recognizedLabel);

            TestsPerLabel[testIndex] += 1;

            if (testedLabel == recognizedLabel)
            {
                SuccessesPerLabel[testIndex] += 1;
                TotalSuccesses++;
            }

            confusionMatrix[testIndex, resultIndex] += 1;
        }

        /// <summary>
        /// Adds a single result to a global result. 
        /// </summary>
        public void addResult(ResultSet r)
        {
            //By adding a single result, this result become a global result.
            isGlobalResultSet = true;

            //Then, the total Successes become the sum of the averages.
            TotalSuccesses += r.getAverage();

            /*
             * We increment the tests counter of a label only if there is at least one test
             * to avoid considering an untested label. 
             */
            for (int i = 0; i < TestsPerLabel.Length; i++)
            {
                if (r.TestsPerLabel[i] != 0)
                    TestsPerLabel[i] += 1;
            }

            //The confusion matrix is the sum of each confusion matrices.
            for (int i = 0; i < confusionMatrix.Length / labels.Count; i++)
            {
                for (int j = 0; j < confusionMatrix.Length / labels.Count; j++)
                {
                    confusionMatrix[i, j] += r.getConfusionMatrixPercent()[i, j] / 100;
                }
            }

            //Number of single results added to the global result.
            TotalTests++;
        }

        /// <summary>
        /// Writes the result into a file. 
        /// </summary>
        public void fileOutput(string filename)
        {
            System.IO.File.Create(filename).Close();

            System.IO.StreamWriter writer = new System.IO.StreamWriter(filename);
            writer.Write(this);
            writer.Close();
        }

        public override string ToString()
        {
            string s = "\n";

            s += "\t Average : "+getAverage()*100+"%\n";

            if (isGlobalResultSet)
            { 
                s += "\t Confusion Matrix (%) : \n";
                s += ConsolePrinter.getArrayString(getConfusionMatrix(), labels, labels);
            }
            else
            {
                s += "\t Confusion Matrix (numbers) : \n";
                s += ConsolePrinter.getArrayString(getConfusionMatrix(), labels, labels);
                s += "\t Confusion Matrix (%) : \n";
                s += ConsolePrinter.getArrayString(getConfusionMatrixPercent(), labels, labels);
            }

            s += "\t TotalTests : " + TotalTests + "\n";

            return s;
        } 
    }

    public static class ValidationTest
    {
        public enum TRAINING_MODES{CLASSIC = 0, SAVE = 1, LOAD = 2};

        private static int FILE_ID = 0;
        public static TRAINING_MODES TRAINING = TRAINING_MODES.CLASSIC;

        /// <summary>
        /// Performs a 2-fold validation on the given dataset, chooses randomly a percentage for the train data.
        /// </summary>
        public static ResultSet twoFoldOnSequences(Dataset dataset, LearningParams learning_params,int percentageOfTrainData, int nbOfRounds = 10)
        {
            Console.WriteLine("Cross Validation twoFoldOnSequences");

            ResultSet tmp = null, globalResult = new ResultSet(learning_params.ClassLabels);
            TrainDataType trainData, testData;

            nbOfRounds = (nbOfRounds <= 0) ? 1 : nbOfRounds;
            for (int i = 0; i < nbOfRounds; i++)
            {
                Console.WriteLine("Data extraction...");
                dataset.initTrainAndTestData(percentageOfTrainData, out trainData, out testData);

                tmp = crossValidationResultSet(learning_params, trainData, testData);
                Console.WriteLine("Average : " + tmp.getAverage());
                globalResult.addResult(tmp);

                tmp = crossValidationResultSet(learning_params, testData, trainData);
                Console.WriteLine("Average : " + tmp.getAverage());
                globalResult.addResult(tmp);
            }

            return globalResult;
        }

        /// <summary>
        /// Performs a leave-One-Actor-Out Random (LOAOR) cross validation on the given dataset.
        /// Selects one actor at random at each round.
        /// </summary>
        public static ResultSet leaveOneActorOutRandom(Dataset dataset, LearningParams learning_params, int nbOfRounds = 10)
        {
            Console.WriteLine("Cross Validation LOAO");

            ResultSet tmp = null, globalResult = new ResultSet(learning_params.ClassLabels);
            TrainDataType trainData, testData;

            nbOfRounds = (nbOfRounds <= 0) ? 1 : nbOfRounds;
            for (int i = 0; i < nbOfRounds; i++)
            {
                Console.WriteLine("Data extraction...");
                string subject = dataset.getRandomSubject();
                dataset.initTrainAndTestData(subject, out trainData, out testData);

                tmp = crossValidationResultSet(learning_params, trainData, testData);
                Console.WriteLine("Average : " + tmp.getAverage());
                globalResult.addResult(tmp);
            }

            return globalResult;
        }

        /// <summary>
        /// Performs a leave-One-Actor-Out (LOAO) cross validation on the given dataset.
        /// Tests all actors one by one.
        /// </summary>
        public static ResultSet leaveOneActorOut(Dataset dataset, LearningParams learning_params)
        {
            Console.WriteLine("Cross Validation LOAO");

            ResultSet tmp = null, globalResult = new ResultSet(learning_params.ClassLabels);
            TrainDataType trainData, testData;

            List<string> subjects = dataset.Subjects;
            Shuffler.Shuffle(subjects);

            
            for (int i = 0; i < subjects.Count; i++)
            {
                Console.WriteLine("Data extraction...");
                dataset.initTrainAndTestData(subjects[i], out trainData, out testData);

                tmp = crossValidationResultSet(learning_params, trainData, testData);
                Console.WriteLine("Average : " + tmp.getAverage());
                globalResult.addResult(tmp);
            }

            return globalResult;
        }

        /// <summary>
        /// Performs a leave-One-Sequence-Out Random (LOSOR) cross validation on the given dataset.
        /// Selects one sequence at random at each round.
        /// </summary>
        public static ResultSet leaveOneSequenceOutRandom(Dataset dataset, LearningParams learning_params, int nbOfRounds = 10)
        {
            Console.WriteLine("Cross Validation LOSO");

            ResultSet tmp = null, globalResult = new ResultSet(learning_params.ClassLabels);
            TrainDataType trainData, testData;

            nbOfRounds = (nbOfRounds <= 0) ? 1 : nbOfRounds;
            for (int i = 0; i < nbOfRounds; i++)
            {
                Console.WriteLine("Data extraction...");
                List<double[]> sequence = dataset.getRandomSequence();
                dataset.initTrainAndTestData(sequence, out trainData, out testData);

                tmp = crossValidationResultSet(learning_params, trainData, testData);
                Console.WriteLine("Average : " + tmp.getAverage());
                globalResult.addResult(tmp);
            }

            return globalResult;
        }

        /// <summary>
        /// Performs a leave-One-Sequence-Out (LOSO) cross validation on the given dataset.
        /// Tests all sequences one by one.
        /// </summary>
        public static ResultSet leaveOneSequenceOut(Dataset dataset, LearningParams learning_params)
        {
            Console.WriteLine("Cross Validation LOSO");

            ResultSet tmp = null, globalResult = new ResultSet(learning_params.ClassLabels);
            TrainDataType trainData, testData;

            List<DatasetEntry> entries = dataset.Data;
            Shuffler.Shuffle(entries);

            for (int i = 0; i < entries.Count; i++)
            {
                Console.WriteLine("Data extraction...");
                
                dataset.initTrainAndTestData(entries[i].Sequence, out trainData, out testData);

                tmp = crossValidationResultSet(learning_params, trainData, testData);
                Console.WriteLine("Average : " + tmp.getAverage());
                globalResult.addResult(tmp);
            }

            return globalResult;
        }

        /// <summary>
        /// Performs a 2-fold cross validation on the given dataset, chooses randomly the half of actors for training.
        /// </summary>
        public static ResultSet twoFoldHalfActors(Dataset dataset, LearningParams learning_params, int nbOfRounds = 10)
        {
            Console.WriteLine("Cross Validation 2-fold half actors");

            ResultSet tmp = null, globalResult = new ResultSet(learning_params.ClassLabels);       
            TrainDataType trainData, testData;

            nbOfRounds = (nbOfRounds <= 0) ? 1 : nbOfRounds;
            for (int i = 0; i < nbOfRounds; i++)
            {
                Console.WriteLine("Data extraction...");

                dataset.initTrainAndTestData(out trainData, out testData);

                tmp = crossValidationResultSet(learning_params, trainData, testData);
                Console.WriteLine("Average : " + tmp.getAverage());
                globalResult.addResult(tmp);

                tmp = crossValidationResultSet(learning_params, testData, trainData);
                Console.WriteLine("Average : " + tmp.getAverage());
                globalResult.addResult(tmp);
            }

            return globalResult;
        }

        /// <summary>
        /// Performs a 2-fold cross validation on the given dataset, takes the actor set given for training and the others for testing.
        /// </summary>
        public static ResultSet twoFoldActorsTrainingSet(Dataset dataset, LearningParams learning_params, string[] actorsTrainingSet, int nbOfRounds = 10)
        {
            Console.WriteLine("Cross Validation 2-fold training actors set");

            ResultSet tmp = null, globalResult = new ResultSet(learning_params.ClassLabels);
            TrainDataType trainData, testData;

            nbOfRounds = (nbOfRounds <= 0) ? 1 : nbOfRounds;
            for (int i = 0; i < nbOfRounds; i++)
            {
                Console.WriteLine("Data extraction...");

                dataset.initTrainAndTestData(actorsTrainingSet, out trainData, out testData);

                tmp = crossValidationResultSet(learning_params, trainData, testData);
                Console.WriteLine("Average : " + tmp.getAverage());
                globalResult.addResult(tmp);

                tmp = crossValidationResultSet(learning_params, testData, trainData);
                Console.WriteLine("Average : " + tmp.getAverage());
                globalResult.addResult(tmp);
            }
 
            return globalResult;
        }

        /// <summary>
        /// Core function of the cross validation.
        /// Trains the model with the given train data and tests it with the test data.
        /// </summary>
        public static ResultSet crossValidationResultSet(LearningParams learning_params, TrainDataType trainData, TrainDataType testData)
        {
            //Cross Validation     

            //Create a new resultSet
            ResultSet resultSet = new ResultSet(learning_params.ClassLabels);

            //Create and train a new model
            
            BoKP bokp = new BoKP(learning_params);

            string folderName = "Config";
            System.IO.Directory.CreateDirectory(folderName);

            //Train the model and save the config
            if(TRAINING == TRAINING_MODES.SAVE)
            {
                Console.WriteLine("Training...");
                bokp.Train(trainData.Dictionary);
            
                //Save the train config into a file.
                folderName = "Config";
                System.IO.Directory.CreateDirectory(folderName);
                bokp.Config.ToXML().Save(folderName + "/TrainConfig_"+ FILE_ID +".xml");
            }
            //Train the model by loading a config file
            else if(TRAINING == TRAINING_MODES.LOAD)
            {
                Console.WriteLine("Reading training file ...");
                XmlDocument doc = new XmlDocument();
                doc.Load(folderName + "/TrainConfig_" + FILE_ID + ".xml");
                bokp.Config.LoadXML(doc);
            }
            //Train the model only
            else
            {
                Console.WriteLine("Training...");
                bokp.Train(trainData.Dictionary);
            }

            //Evaluate each sequence
            Console.WriteLine("Testing ...");
            foreach (string label in learning_params.ClassLabels)
            {
                foreach (var sequence in testData[label])
                {
                    string recognition = bokp.EvaluateSequence(sequence);

                    //Add the test to the resultSet
                    resultSet.addTest(label, recognition);               
                }
            }
<<<<<<< HEAD
=======

>>>>>>> cooperative
#if DEBUG
            Console.WriteLine(resultSet);

            //Here you can print each results into a file
            //System.IO.Directory.CreateDirectory("results");
            //resultSet.fileOutput("results/result_"+FILE_ID+".log");
#endif
            FILE_ID++;
            return resultSet;
        }
    }
}
