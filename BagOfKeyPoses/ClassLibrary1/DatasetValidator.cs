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
        private Boolean matrixComputed, averageComputed;
        private double average;
        

        public ResultSet(List<string> label)
        {
            this.labels = label;
            matrixComputed = averageComputed = false;
            TotalTests = 0;
            TotalSuccesses = 0;
            average = 0;

            
            int nbOfLabels = labels.Count;
            TestsPerLabels = new int[nbOfLabels];
            SuccessesPerLabels = new int[nbOfLabels];
            confusionMatrix = new double[nbOfLabels, nbOfLabels];
        }

        public double[,] getConfusionMatrix()
        {
            if (!matrixComputed)
            {
                computeConfusionMatrix();
            }
            return confusionMatrix;
        }

        public double getAverage()
        {
            if (!averageComputed)
            {
                computeAverage();
            }
            return average * 100;
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
                average++;
            }

            confusionMatrix[testIndex, resultIndex] += 1;
        }

        private void computeConfusionMatrix()
        {
            for (int i = 0; i < confusionMatrix.Length / labels.Count; i++)
            {
                for (int j = 0; j < confusionMatrix.Length / labels.Count; j++)
                {
                    confusionMatrix[i, j] /= TestsPerLabels[i];
                }
            }

            matrixComputed = true;
        }

        public void computeAverage()
        {
            average /= TotalTests;

            for (int i = 0; i < confusionMatrix.Length / labels.Count; i++)
            {
                for (int j = 0; j < confusionMatrix.Length / labels.Count; j++)
                {
                    confusionMatrix[i, j] /= TotalTests;
                }
            }

            averageComputed = true;
            matrixComputed = true;
        }

        public void add(ResultSet r)
        {
            average += r.average;

            for (int i = 0; i < confusionMatrix.Length / labels.Count; i++)
            {
                for (int j = 0; j < confusionMatrix.Length / labels.Count; j++)
                {
                    confusionMatrix[i, j] += r.confusionMatrix[i, j];
                }
            }

            TotalTests++;
        }

        public override string ToString()
        {
            string s = "\n";

            s += "\t Average : "+getAverage()+"%\n";

            s += "\t Confusion Matrix (%) : \n";
            s += ConsolePrinter.getArrayString(getConfusionMatrix(), labels, labels);

            return s;
        }
    }

    public static class ValidationTest
    {
        /// <summary>
        /// Performs a cross validation at random on the given dataset.
        /// </summary>
        public static ResultSet atRandom(Dataset dataset, LearningParams learning_params, int nbOfRounds = 10)
        {
            Console.WriteLine("Cross Validation at Random");

            ResultSet resultSet = null;
            TrainDataType trainData, testData;

            nbOfRounds = (nbOfRounds <= 0) ? 1 : nbOfRounds;
            for (int i = 0; i < nbOfRounds; i++)
            {
                Console.WriteLine("Data extraction...");
                dataset.initTrainAndTestData(50, out trainData, out testData);
                resultSet = crossValidationResultSet(learning_params, trainData, testData);
            }

            return resultSet;
        }

        /// <summary>
        /// Performs a leave-One-Actor-Out (LOAO) cross validation on the given dataset.
        /// </summary>
        public static ResultSet leaveOneActorOut(Dataset dataset, LearningParams learning_params, int nbOfRounds = 10)
        {
            Console.WriteLine("Cross Validation LOAO");

            ResultSet resultSet = null;
            TrainDataType trainData, testData;

            nbOfRounds = (nbOfRounds <= 0) ? 1 : nbOfRounds;
            for (int i = 0; i < nbOfRounds; i++)
            {
                Console.WriteLine("Data extraction...");
                string subject = dataset.getRandomSubject();
                dataset.initTrainAndTestData(subject, out trainData, out testData);
                resultSet = crossValidationResultSet(learning_params, trainData, testData);
            }

            return resultSet;
        }

        /// <summary>
        /// Performs a 2-fold cross validation on the given dataset, randomly choose the half of actors for training.
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
                globalResult.add(tmp);

                tmp = crossValidationResultSet(learning_params, testData, trainData);
                Console.WriteLine("Average : " + tmp.getAverage());
                globalResult.add(tmp);
            }

            return globalResult;
        }

        /// <summary>
        /// Performs a 2-fold cross validation on the given dataset, take the actors set given for training and the others for testing.
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
                globalResult.add(tmp);

                tmp = crossValidationResultSet(learning_params, testData, trainData);
                Console.WriteLine("Average : " + tmp.getAverage());
                globalResult.add(tmp);
            }

            globalResult.computeAverage();

            return globalResult;
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
    }
}
