using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Util;
using Sequence = System.Collections.Generic.List<double[]>;
using TrainDataType = Util.AssociativeArray<string, System.Collections.Generic.List<System.Collections.Generic.List<double[]>>>;

using Point = System.Collections.Generic.List<double>;
using Contour = System.Collections.Generic.List<System.Collections.Generic.List<double>>;

namespace Parser
{
    /// <summary>
    /// Represents the dataset and contains several functions to manipulate the datas.
    /// </summary>
    public class Dataset
    {
        public System.Collections.Generic.List<Parser.DatasetEntry> Data;
        public Random rand = new Random();
        public List<string> Labels, Subjects;

        public Dataset() : this(new List<DatasetEntry>(), new List<string>(), new List<string>()) { }
        public Dataset(System.Collections.Generic.List<Parser.DatasetEntry> datas, List<string> labels, List<string> subjects)
        {
            this.Data = datas;
            this.Labels = labels;
            this.Subjects = subjects;
        }

        /// <summary>
        /// Nomalise each skeleton of each sequence of the dataset.
        /// <see cref="SkeletonNormalisation.normaliseSequenceSkeleton"/>
        /// </summary>
        public void normaliseSkeletons()
        {
            for (int i = Data.Count - 1; i > 0; i--)
            {
                DatasetEntry entry = Data[i];
                entry.Sequence = SkeletonNormalisation.normaliseSequenceSkeleton(entry.Sequence);
                if (entry.Sequence.Count == 0)
                {
                    Console.WriteLine(entry.Label+"_"+entry.Subject+"_e"+entry.Episode+" removed because all skeletons are wrong.");
                    Data.Remove(entry);
                }
            }
        }

        /// <summary>
        /// Returns all sequences with the given label (or action)
        /// </summary>
        public List<Sequence> getSequencesByLabel(string label)
        {
            List<Sequence> data = new List<Sequence>();

            foreach (DatasetEntry entry in Data)
            {
                if (entry.Label == label)
                {
                    data.Add(entry.Sequence);
                }
            }

            return data;
        }

        /// <summary>
        /// Returns a random subject (or actor) from the dataset
        /// </summary>
        public string getRandomSubject()
        {
            return Data[rand.Next(0, Data.Count - 1)].Subject;
        }

        /// <summary>
        /// Returns a random sequence from the dataset
        /// </summary>
        public Sequence getRandomSequence()
        {
            return Data[rand.Next(0, Data.Count - 1)].Sequence;
        }

        /// <summary>
        /// Returns a random label (or action) from the dataset
        /// </summary>
        public string getRandomLabel()
        {
            return Labels[rand.Next(0, Labels.Count - 1)];
        }

        /// <summary>
        /// Returns a random DatasetEntry from the dataset
        /// <see cref="DatasetEntry">
        /// </summary>
        public DatasetEntry getRandomEntry()
        {
            return Data[rand.Next(0, Data.Count - 1)];
        }

        /// <summary>
        /// Returns a percentage of the dataset for the train data.
        /// </summary>
        public TrainDataType getPercentageOfTrainData(double percentage)
        {
            TrainDataType data = new TrainDataType();

            foreach(string label in Labels)
            {
                List<Sequence> label_data = getSequencesByLabel(label);
                Shuffler.Shuffle<Sequence>(label_data);
                for (int i = 0; i < label_data.Count * (percentage / 100.0); i++)
                {
                    data[label].Add(label_data[i]);
                }
            }   
            return data;
        }

        /// <summary>
        /// Returns train data composed of all subjects except the given one.
        /// </summary>
        private TrainDataType getAllExcludingOneSubject(string subject)
        {
            TrainDataType data = new TrainDataType();

            foreach (DatasetEntry entry in Data)
            {
                if (subject != entry.Subject)
                {
                    data[entry.Label].Add(entry.Sequence);
                }
            }

            return data;
        }

        /// <summary>
        /// Returns train data composed of all sequences except the given one.
        /// </summary>
        private TrainDataType getAllExcludingOneSequence(Sequence sequence)
        {
            TrainDataType data = new TrainDataType();

            foreach (DatasetEntry entry in Data)
            {
                if (sequence != entry.Sequence)
                {
                    data[entry.Label].Add(entry.Sequence);
                }
            }
            return data;
        }

        /// <summary>
        /// Initialise the training data and the testing data.
        /// The training data is composed of the half of the subjects.
        /// </summary>
        public void initTrainAndTestData(out TrainDataType trainData, out TrainDataType testData)
        {
            trainData = new TrainDataType();
            testData = new TrainDataType();

            List<String> subjects = new List<string>();
            string subject;

            //Create a list that contains the half of the subjects selected at random.
            for (int i = 0; i < Subjects.Count/2; i++)
            {
                do
                {
                    subject = getRandomSubject();
                } while (subjects.Contains(subject));
                subjects.Add(subject);  
            }

           /* 
            * Add all the sequences of the subjects in the list to the trainData
            * and the others to the testData.
            */
            for (int i = 0; i < Data.Count; i++)
            {
                DatasetEntry entry = Data[i];
                if (subjects.Contains(entry.Subject))
                { 
                    trainData[entry.Label].Add(entry.Sequence);
                }
                else
                {
                    testData[entry.Label].Add(entry.Sequence);
                }
            }
        }

        /// <summary>
        /// Initialise the training data and the testing data.
        /// The training data is composed of the given percentage of the dataset.
        /// </summary>
        public void initTrainAndTestData(double percentageOfTrainData, out TrainDataType trainData, out TrainDataType testData)
        {
            trainData = new TrainDataType();
            testData = new TrainDataType();

            trainData = getPercentageOfTrainData(percentageOfTrainData);

            for (int i = 0; i < Data.Count; i++)
            {
                DatasetEntry entry = Data[i];
                if(!trainData[entry.Label].Contains(entry.Sequence))
                {
                    testData[entry.Label].Add(entry.Sequence);
                }
            }
        }

        /// <summary>
        /// Initialise the training data and the testing data.
        /// The training data is composed of the half of the subjects
        /// </summary>
        public void initTrainAndTestData(string testedSubject, out TrainDataType trainData, out TrainDataType testData)
        {
            trainData = new TrainDataType();
            testData = new TrainDataType();

            trainData = getAllExcludingOneSubject(testedSubject);

            for (int i = 0; i < Data.Count; i++)
            {
                DatasetEntry entry = Data[i];
                if (!trainData[entry.Label].Contains(entry.Sequence))
                {
                    testData[entry.Label].Add(entry.Sequence);
                }
            }
        }

        /// <summary>
        /// Initialise the training data and the testing data.
        /// The training data is composed of all sequences except the given one.
        /// </summary>
        public void initTrainAndTestData(Sequence sequence, out TrainDataType trainData, out TrainDataType testData)
        {
            trainData = new TrainDataType();
            testData = new TrainDataType();

            trainData = getAllExcludingOneSequence(sequence);

            for (int i = 0; i < Data.Count; i++)
            {
                DatasetEntry entry = Data[i];
                if (!trainData[entry.Label].Contains(entry.Sequence))
                {
                    testData[entry.Label].Add(entry.Sequence);
                }
            }
        }

        /// <summary>
        /// Initialise the training data and the testing data.
        /// The training data is composed of all subjects of the given training set.
        /// </summary>
        public void initTrainAndTestData(string[] trainingSet, out TrainDataType trainData, out TrainDataType testData)
        {
            trainData = new TrainDataType();
            testData = new TrainDataType();

            for (int i = 0; i < Data.Count; i++)
            {
                DatasetEntry entry = Data[i];
                if (trainingSet.Contains(entry.Subject))
                {
                    trainData[entry.Label].Add(entry.Sequence);
                }
                else
                {
                    testData[entry.Label].Add(entry.Sequence);
                }
            }
        } 
    }

    /// <summary>
    /// Represents an element of the dataset.
    /// </summary>
    public class DatasetEntry
    {
        public string Label;                            //Action name
        public string Subject;                          //Actor name
        public int Episode;                             //Number of the try or attempt
        public Sequence Sequence;                       //Sequence of frames

        public DatasetEntry() : this("0", "0", 0, new Sequence())
        {
            
        }

        public DatasetEntry(string label, string subject, int episode, Sequence sequence)
        {
            this.Label = label;
            this.Subject = subject;
            this.Episode = episode;
            this.Sequence = sequence;
        }
    }

    public static class DatasetParser
    {
        /// <summary>
        /// Read a sequence by storing all joints of each skeleton
        /// </summary>
        /// <param name="nbOfJoints">The number of joints of a skeleton</param>
        /// <param name="separator">Separator between each coordinate of the skeleton's joints in the files</param>
        private static Sequence readSequenceSkeleton(int nbOfJoints, string filename, char separator)
        {
            int coordsDim = 3;

            Sequence sequence = new Sequence();

            string[] lines = System.IO.File.ReadAllLines(filename);

            int i = 0;
            double[] frame = new double[nbOfJoints * coordsDim];                      //All joints of the skeleton
            foreach (string line in lines)
            {
                string[] numbers = line.Split(separator);
             
                for (int j=0;j<coordsDim; j++)
                {
                    frame[(i * 3) + j] = double.Parse(numbers[j], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.GetCultureInfo("en-US"));          
                }

                if (i == nbOfJoints - 1)
                {
                    sequence.Add(frame);
                    frame = new double[nbOfJoints * coordsDim];
                    i = -1;
                }
                i++;
            }
            return sequence;
        }

        /// <summary>
        /// Load the MSR dataset from the given folder.
        /// Filename example : a01_s01_e01_skeleton3D (Label_Subject_eTry_...)
        /// </summary>
        /// <param name="nbOfJoints">The number of joints of a skeleton</param>
        /// <param name="separator">Separator between each coordinate of the skeleton's joints in the files</param>
        public static Dataset loadDatasetSkeleton(int nbOfJoints, string foldername, char separator)
        {
            List<DatasetEntry> datas = new List<DatasetEntry>();
            List<string> labels = new List<string>();
            List<string> subjects = new List<string>();

            string[] filePaths = System.IO.Directory.GetFiles(foldername);
            
            foreach (string file in filePaths)
            {
                //System.IO.Path.GetFileName(file);
                string filename = file.Split('\\').Last();
                string[] fields = filename.Split('_');

                string label = fields[0];
                string subject = fields[1];
                int episode = int.Parse(fields[2].Substring(1));

                if (!labels.Contains(label))
                    labels.Add(label);

                if (!subjects.Contains(subject))
                    subjects.Add(subject);

                Sequence sequence = readSequenceSkeleton(nbOfJoints, file, separator);

                datas.Add(new DatasetEntry(label, subject, episode, sequence));
            }
                   
            return new Dataset(datas,labels,subjects);
        }

        /// <summary>
        /// Read a sequence by storing all points of the silhouette and processing a radial summary.
        /// <see cref="ContourSelection.processRadialSummary">
        /// </summary>
        /// <param name="separator">Separator between each coordinate of the skeleton's joints in the files</param>
        private static Sequence readSequenceSilhouette(string filename, char separator)
        {
            Sequence sequence = new Sequence();
            int coordsDim = 2;

            string[] lines = System.IO.File.ReadAllLines(filename);

            int nbOfFrames = int.Parse(lines[0]);

            int frameSize = 0;

            int i = 1;
            for (int n = 0; n < nbOfFrames; n++)
            {
                frameSize = int.Parse(lines[i]);
                Contour contour = new Contour();
                
                i++;
                for (int j = 0; j < frameSize; j++, i++)
                {
                    string[] numbers = lines[i].Split(separator);
                    Point p = new Point();

                    for (int k = 0; k < coordsDim; k++)
                    {
                        double value = double.Parse(numbers[k], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.GetCultureInfo("en-US"));

                        p.Add(value);
                    }
                    contour.Add(p);
                }

                double[] radialSummary = ContourSelection.processRadialSummary(contour);

                sequence.Add(radialSummary);
            }

            return sequence;
        }

        /// <summary>
        /// Load the Weizmann dataset from the given folder.
        /// Filename example : bend_daria_e01 (Label_Subject_eTry)
        /// </summary>
        /// <param name="separator">Separator between each coordinate of the skeleton's joints in the files</param>
        public static Dataset loadDatasetSilhouette(string foldername, char separator)
        {
            List<DatasetEntry> datas = new List<DatasetEntry>();
            List<string> labels = new List<string>();
            List<string> subjects = new List<string>();

            string[] filePaths = System.IO.Directory.GetFiles(foldername);
            foreach (string file in filePaths)
            {
                string filename = file.Split('\\').Last();
                string[] fields = filename.Split('_');

                string label = fields[0];
                string subject = fields[1];
                int episode = int.Parse(fields[2].Substring(1,2));

                if (!labels.Contains(label))
                    labels.Add(label);

                if (!subjects.Contains(subject))
                    subjects.Add(subject);

                Sequence sequence = readSequenceSilhouette(file, separator);

                datas.Add(new DatasetEntry(label, subject, episode, sequence));
            }

            return new Dataset(datas, labels, subjects);
        }
    }
}