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
        private Boolean matrixComputed;

        public ResultSet(List<string> label)
        {
            this.labels = label;
            matrixComputed = false;
            TotalTests = 0;
            TotalSuccesses = 0;

            
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

        private void computeConfusionMatrix()
        {
            for (int i = 0; i < confusionMatrix.Length / labels.Count; i++)
            {
                for (int j = 0; j < confusionMatrix.Length / labels.Count; j++)
                {
                    confusionMatrix[i, j] /= TestsPerLabels[i];
                    confusionMatrix[i, j] *= 100;
                }
            }

            matrixComputed = true;
        }

        public static ResultSet operator +(ResultSet r1, ResultSet r2)
        {
            ResultSet res = new ResultSet(r1.labels);

            res.TotalTests = r1.TotalTests + r2.TotalTests;
            res.TotalSuccesses = r1.TotalSuccesses + r2.TotalSuccesses;

            for (int i = 0; i < res.labels.Count; i++)
            {
                res.TestsPerLabels[i] = r1.TestsPerLabels[i] + r2.TestsPerLabels[i];
                res.SuccessesPerLabels[i] = r1.SuccessesPerLabels[i] + r2.SuccessesPerLabels[i];
            }

            r1.computeConfusionMatrix();
            r2.computeConfusionMatrix();

            for (int i = 0; i < res.confusionMatrix.Length / res.labels.Count; i++)
			{
			    for (int j = 0; j < res.confusionMatrix.Length / res.labels.Count; j++)
			    {
                    res.confusionMatrix[i, j] = (r1.confusionMatrix[i, j] + r2.confusionMatrix[i, j]) / 2; 
			    } 
			}

            res.matrixComputed = true;

            return res;
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

            ResultSet resultSet1 = null, resultSet2 = null;          
            TrainDataType trainData, testData;

            nbOfRounds = (nbOfRounds <= 0) ? 1 : nbOfRounds;
            for (int i = 0; i < nbOfRounds; i++)
            {
                Console.WriteLine("Data extraction...");

                dataset.initTrainAndTestData(out trainData, out testData);
                resultSet1 = crossValidationResultSet(learning_params, trainData, testData);
                resultSet2 = crossValidationResultSet(learning_params, testData, trainData);
            }

            ResultSet resultSet = resultSet1 + resultSet2;
            return resultSet;
        }

        /// <summary>
        /// Performs a 2-fold cross validation on the given dataset, take the actors set given for training and the others for testing.
        /// </summary>
        public static ResultSet twoFoldActorsTrainingSet(Dataset dataset, LearningParams learning_params, string[] actorsTrainingSet, int nbOfRounds = 10)
        {
            Console.WriteLine("Cross Validation 2-fold training actors set");

            ResultSet resultSet1 = null, resultSet2 = null, tmp = null;
            TrainDataType trainData, testData;

            nbOfRounds = (nbOfRounds <= 0) ? 1 : nbOfRounds;
            for (int i = 0; i < nbOfRounds; i++)
            {
                Console.WriteLine("Data extraction...");

                dataset.initTrainAndTestData(actorsTrainingSet, out trainData, out testData);

                tmp = crossValidationResultSet(learning_params, trainData, testData);
                resultSet1 = (resultSet1 == null) ? tmp : resultSet1 += tmp;

                Console.WriteLine("Average : " + resultSet1.getAverage());

                tmp = crossValidationResultSet(learning_params, trainData, testData);
                resultSet2 = (resultSet2 == null) ? tmp : resultSet2 += tmp;

                Console.WriteLine("Average : " + resultSet2.getAverage());
            }

            ResultSet resultSet = resultSet1 + resultSet2;
            return resultSet;
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
