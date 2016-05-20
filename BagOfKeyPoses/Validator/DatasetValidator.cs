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
    /// <summary>
    /// Store the results of a cross validation (average and confusion matrix).
    /// Same class for a single result or a global result.
    /// </summary>
    public class ResultSet
    {
        public int[] TestsPerLabels, SuccessesPerLabels;
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
            TestsPerLabels = new int[nbOfLabels];
            SuccessesPerLabels = new int[nbOfLabels];
            confusionMatrix = new double[nbOfLabels, nbOfLabels];
        }

        /// <summary>
        /// Returns the confusion matrix. 
        /// Numbers : single result
        /// Percentage : global result
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
                        if (isGlobalResultSet && TestsPerLabels[i] != 0)
                            tmp[i, j] = (confusionMatrix[i, j] / TestsPerLabels[i]) * 100;
                    }
                }
                return tmp;
            }
            else
                return confusionMatrix;
        }

        /// <summary>
        /// Returns the confusion matrix as percentage for a single result.
        /// </summary>
        public double[,] getConfusionMatrixPercent()
        {
            double[,] tmp = new double[labels.Count,labels.Count];

            for (int i = 0; i < tmp.Length / labels.Count; i++)
            {
                for (int j = 0; j < tmp.Length / labels.Count; j++)
                {
                    if (TestsPerLabels[i] != 0)
                        tmp[i, j] = (confusionMatrix[i, j] / TestsPerLabels[i]) * 100;
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
        /// Adds a test to the result. 
        /// </summary>
        public void addTest(string testedLabel, string recognizedLabel)
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

        /// <summary>
        /// Adds a single result to the global result. 
        /// </summary>
        public void addResult(ResultSet r)
        {
            isGlobalResultSet = true;

            TotalSuccesses += r.getAverage();

            for (int i = 0; i < TestsPerLabels.Length; i++)
            {
                if (r.TestsPerLabels[i] != 0)
                    TestsPerLabels[i] += 1;
            }

            for (int i = 0; i < confusionMatrix.Length / labels.Count; i++)
            {
                for (int j = 0; j < confusionMatrix.Length / labels.Count; j++)
                {
                    confusionMatrix[i, j] += r.getConfusionMatrixPercent()[i, j] / 100;
                }
            }

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

            s += "\t TotalTets : " + TotalTests + "\n";

            return s;
        } 
    }

    public static class ValidationTest
    {
        static int FILE_ID = 0;

        /// <summary>
        /// Performs a cross validation at random on the given dataset.
        /// </summary>
        public static ResultSet atRandom(Dataset dataset, LearningParams learning_params, int nbOfRounds = 10)
        {
            Console.WriteLine("Cross Validation at Random");

            ResultSet tmp = null, globalResult = new ResultSet(learning_params.ClassLabels);
            TrainDataType trainData, testData;

            nbOfRounds = (nbOfRounds <= 0) ? 1 : nbOfRounds;
            for (int i = 0; i < nbOfRounds; i++)
            {
                Console.WriteLine("Data extraction...");
                dataset.initTrainAndTestData(50, out trainData, out testData);
                tmp = crossValidationResultSet(learning_params, trainData, testData);
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
                    resultSet.addTest(label, recognition);                 
                }
            }

#if DEBUG
            Console.WriteLine(resultSet);
            //resultSet.fileOutput("results/result_"+FILE_ID+".log");
            //FILE_ID++;
#endif
            return resultSet;
        }
    }
}
