using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Util;
using Parser;
using BagOfKeyPoses;
using TrainDataType = Util.AssociativeArray<string, System.Collections.Generic.List<System.Collections.Generic.List<double[]>>>;

namespace Validator
{
    public class ResultSet
    {
        public int[] TestsPerLabels, SuccessesPerLabels;
        public double TotalTests, TotalSuccesses;

        private double[,] confusionMatrix;
        private List<string> labels;
        private Boolean matrixCreated;

        public ResultSet(List<string> label)
        {
            this.labels = label;
            matrixCreated = false;
            TotalTests = 0;
            TotalSuccesses = 0;

            
            int nbOfLabels = labels.Count;
            TestsPerLabels = new int[nbOfLabels];
            SuccessesPerLabels = new int[nbOfLabels];
            confusionMatrix = new double[nbOfLabels, nbOfLabels];
        }

        public double[,] getConfusionMatrix()
        {
            if (!matrixCreated)
            {
                createConfusionMatrix();
            }
            return confusionMatrix;
        }

        public double getAverage()
        {
            return (TotalSuccesses / TotalTests) * 100;
        }

        public void saveTest(string testedLabel, string recognizedLabel)
        {
            TotalTests++;
            int testIndex = labels.IndexOf(testedLabel);
            int resultIndex = labels.IndexOf(recognizedLabel);

            TestsPerLabels[testIndex] += 1;

            if (testedLabel == recognizedLabel)
            {
                SuccessesPerLabels[testIndex] += 1;
                TotalSuccesses++;
            }

            confusionMatrix[testIndex, resultIndex] += 1;
        }

        private void createConfusionMatrix()
        {
            matrixCreated = true;
            
            for (int i = 0; i < confusionMatrix.Length / labels.Count; i++)
            {
                for (int j = 0; j < confusionMatrix.Length / labels.Count; j++)
                {
                    confusionMatrix[i, j] /= TestsPerLabels[i];
                    confusionMatrix[i, j] *= 100;
                }
            }

        }
    }

    public static class ValidationTest
    {
        /// <summary>
        /// Performs a cross validation at random on the given dataset.
        /// </summary>
        public static double atRandom(Dataset dataset, LearningParams learning_params, int nbOfRounds = 10)
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
        public static double leaveOneActorOut(Dataset dataset, LearningParams learning_params, int nbOfRounds = 10)
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
        public static double twoFoldHalfActors(Dataset dataset, LearningParams learning_params, out double[,] confusionMatrix, int nbOfRounds = 10)
        {
            Console.WriteLine("Cross Validation 2-fold half actors");

            confusionMatrix = new double[dataset.Labels.Count, dataset.Labels.Count];

            TrainDataType trainData, testData;

            double average = 0;
            for (int i = 0; i < nbOfRounds; i++)
            {
                Console.WriteLine("Data extraction...");

                dataset.initTrainAndTestData(out trainData, out testData);
                average += crossValidationConfusion(learning_params, trainData, testData, confusionMatrix);
                confusionMatrix = new double[dataset.Labels.Count, dataset.Labels.Count];
                average += crossValidationConfusion(learning_params, testData, trainData, confusionMatrix);
            }

            average /= nbOfRounds * 2;
            return average;
        }

        /// <summary>
        /// Performs a 2-fold cross validation on the given dataset, randomly choose the half of actors for training.
        /// </summary>
        public static ResultSet twoFoldHalfActors(Dataset dataset, LearningParams learning_params, int nbOfRounds = 10)
        {
            Console.WriteLine("Cross Validation 2-fold half actors");

            ResultSet resultSet1 = null, resultSet2 = null;
            ResultSet resultSet = new ResultSet(learning_params.ClassLabels);

            TrainDataType trainData, testData;

            for (int i = 0; i < nbOfRounds; i++)
            {
                Console.WriteLine("Data extraction...");

                dataset.initTrainAndTestData(out trainData, out testData);
                resultSet1 = crossValidationResultSet(learning_params, trainData, testData);
                
                resultSet2 = crossValidationResultSet(learning_params, testData, trainData);
               
            }
            double average = (resultSet1.getAverage()+resultSet2.getAverage()) / 2;
            Console.WriteLine("Result : (" + resultSet1.getAverage() + ") , (" + resultSet2.getAverage() + ") => " + average);

            ConsolePrinter.PrintArray(resultSet2.getConfusionMatrix(), learning_params.ClassLabels, learning_params.ClassLabels);

            return resultSet;
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

        private static ResultSet crossValidationResultSet(LearningParams learning_params, TrainDataType trainData, TrainDataType testData)
        {
            //Cross Validation     
            ResultSet resultSet = new ResultSet(learning_params.ClassLabels);

            Console.WriteLine("Training...");
            BoKP bokp = new BoKP(learning_params);
            bokp.Train(trainData.Dictionary);

            Console.WriteLine("Testing...");
            foreach (string label in learning_params.ClassLabels)
            {
                foreach (var sequence in testData[label])
                {
                    string recognition = bokp.EvaluateSequence(sequence);
                    resultSet.saveTest(label,recognition);
                }
            }

            return resultSet;
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

        private static double crossValidationConfusion(LearningParams learning_params, TrainDataType trainData, TrainDataType testData, double[,] confusionMatrix)
        {
            //Cross Validation
            double nbTests = 0, roundSuccess = 0;
            int[] testsPerLabels = new int[learning_params.ClassLabels.Count];

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

                    //Modify the confusion matrix
                    int testIndex = learning_params.ClassLabels.IndexOf(label);
                    int resultIndex = learning_params.ClassLabels.IndexOf(recognition);

                    testsPerLabels[testIndex] += 1;
                    confusionMatrix[testIndex, resultIndex] += 1;
                }
            }

            //Divide
            for (int i = 0; i < confusionMatrix.Length / learning_params.ClassLabels.Count; i++)
            {
                for (int j = 0; j < confusionMatrix.Length / learning_params.ClassLabels.Count; j++)
                {
                    confusionMatrix[i, j] /= testsPerLabels[i];
                }
            }

            double res = (roundSuccess / nbTests) * 100;
            Console.WriteLine("Success : " + res);
            return res;
        }
    }
}
