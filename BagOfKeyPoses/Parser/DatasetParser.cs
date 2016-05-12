using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Util;
using Sequence = System.Collections.Generic.List<double[]>;
using TrainDataType = Util.AssociativeArray<string, System.Collections.Generic.List<System.Collections.Generic.List<double[]>>>;


//TODO : Rename all initTrainAndTestData methods

namespace Parser
{
    /// <summary>
    /// Represents the dataset and contains several functions to manipulate the datas.
    /// </summary>
    public class Dataset
    {
        public System.Collections.Generic.List<Parser.DatasetEntry> Datas;
        public Random rand = new Random();
        public List<string> Labels, Subjects;

        public Dataset() : this(new List<DatasetEntry>(), new List<string>(), new List<string>()) { }
        public Dataset(System.Collections.Generic.List<Parser.DatasetEntry> datas, List<string> labels, List<string> subjects)
        {
            this.Datas = datas;
            this.Labels = labels;
            this.Subjects = subjects;
        }

        public void normaliseSkeletons()
        {
            for (int i = Datas.Count - 1; i > 0; i--)
            {
                DatasetEntry entry = Datas[i];
                entry.Sequence = SkeletonNormalisation.normaliseSequenceSkeleton(entry.Sequence);
                if (entry.Sequence.Count == 0)
                    Datas.Remove(entry);
            }
        }

        public List<Sequence> getByLabel(string label)
        {
            List<Sequence> data = new List<Sequence>();

            foreach(DatasetEntry entry in Datas)
            {
                if (entry.Label == label)
                {
                    data.Add(entry.Sequence);
                }
            }

            return data;
        }

        public string getRandomSubject()
        {
            return Datas[rand.Next(0, Datas.Count - 1)].Subject;
        }

        public Sequence getRandomSequence()
        {
            return Datas[rand.Next(0, Datas.Count - 1)].Sequence;
        }

        public string getRandomLabel()
        {
            return Labels[rand.Next(0, Labels.Count - 1)];
        }

        public TrainDataType getPercentTrainData(double percent)
        {
            TrainDataType data = new TrainDataType();

            foreach(string label in Labels)
            {
                List<Sequence> label_data = getByLabel(label);
                Shuffler.Shuffle<Sequence>(label_data);
                for (int i = 0; i < label_data.Count * (percent / 100.0); i++)
                {
                    data[label].Add(label_data[i]);
                }
            }

            return data;
        }

        
        public void initTrainAndTestData(out TrainDataType trainData, out TrainDataType testData)
        {
            trainData = new TrainDataType();
            testData = new TrainDataType();

            List<String> subjects = new List<string>();
            string subject;

            for (int i = 0; i < Subjects.Count/2; i++)
            {
                do
                {
                    subject = getRandomSubject();
                } while (subjects.Contains(subject));
                subjects.Add(subject);  
            }

            for (int i = 0; i < Datas.Count; i++)
            {
                DatasetEntry entry = Datas[i];
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

        public void initTrainAndTestData(double percentOfTrainData, out TrainDataType trainData, out TrainDataType testData)
        {
            trainData = new TrainDataType();
            testData = new TrainDataType();

            trainData = getPercentTrainData(percentOfTrainData);

            for (int i = 0; i < Datas.Count; i++)
            {
                DatasetEntry entry = Datas[i];
                if(!trainData[entry.Label].Contains(entry.Sequence))
                    testData[entry.Label].Add(entry.Sequence);
            }
        }

        public void initTrainAndTestData(string testedSubject, out TrainDataType trainData, out TrainDataType testData)
        {
            trainData = new TrainDataType();
            testData = new TrainDataType();

            trainData = getAllExcludingOneSubject(testedSubject);

            for (int i = 0; i < Datas.Count; i++)
            {
                DatasetEntry entry = Datas[i];
                if (!trainData[entry.Label].Contains(entry.Sequence))
                {
                    testData[entry.Label].Add(entry.Sequence);
                }
            }
        }

        public void initTrainAndTestData(Sequence sequence, out TrainDataType trainData, out TrainDataType testData)
        {
            trainData = new TrainDataType();
            testData = new TrainDataType();

            trainData = getAllExcludingOneSequence(sequence);

            for (int i = 0; i < Datas.Count; i++)
            {
                DatasetEntry entry = Datas[i];
                if (!trainData[entry.Label].Contains(entry.Sequence))
                {
                    testData[entry.Label].Add(entry.Sequence);
                }
            }
        }

        public void initTrainAndTestData(string[] trainingSet, out TrainDataType trainData, out TrainDataType testData)
        {
            trainData = new TrainDataType();
            testData = new TrainDataType();

            for (int i = 0; i < Datas.Count; i++)
            {
                DatasetEntry entry = Datas[i];
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

        private TrainDataType getAllExcludingOneSubject(string subject)
        {
            TrainDataType data = new TrainDataType();

            foreach (DatasetEntry entry in Datas)
            {
                if (subject != entry.Subject)
                {
                    data[entry.Label].Add(entry.Sequence);
                }
            }

            return data;
        }

        private TrainDataType getAllExcludingOneSequence(Sequence sequence)
        {
            TrainDataType data = new TrainDataType();

            foreach (DatasetEntry entry in Datas)
            {
                if (sequence != entry.Sequence)
                {
                    data[entry.Label].Add(entry.Sequence);
                }
            }

            return data;
        }

        public DatasetEntry getRandomEntry()
        {
            return Datas[rand.Next(0, Datas.Count-1)];
        }
    }

    /// <summary>
    /// Represents an element of the dataset.
    /// </summary>
    public class DatasetEntry
    {
        public string Label;
        public string Subject;
        public int Episode;
        public Sequence Sequence;

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
        /// Read a sequence by storing the joints of each skeleton
        /// </summary>
        /// <param name="nbOfJoints">The number of joints of a skeleton</param>
        /// <param name="separator">Separator between each coordinate of the skeleton's joints in the files</param>
        private static Sequence readSequenceSkeleton(int nbOfJoints, string filename, char separator)
        {
            int coordsDim = 3;

            Sequence sequence = new Sequence();

            string[] lines = System.IO.File.ReadAllLines(filename);

            int i = 0;
            double[] feature = new double[nbOfJoints * coordsDim];                      //All joints of the skeleton
            foreach (string line in lines)
            {
                string[] numbers = line.Split(separator);
             
                for (int j=0;j<coordsDim; j++)
                {
                    feature[(i*3)+j] = double.Parse(numbers[j], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.GetCultureInfo("en-US"));          
                }

                if (i == nbOfJoints - 1)
                {
                    sequence.Add(feature);
                    feature = new double[nbOfJoints * coordsDim];
                    i = -1;
                }
                i++;
            }
            return sequence;
        }

        /// <summary>
        /// Load the MSR dataset from the given folder.
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

        private static Sequence readSequenceSilhouette(string filename, char separator)
        {
            Sequence sequence = new Sequence();

            
            return sequence;
        }

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
                int episode = int.Parse(fields[2].Substring(1));

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