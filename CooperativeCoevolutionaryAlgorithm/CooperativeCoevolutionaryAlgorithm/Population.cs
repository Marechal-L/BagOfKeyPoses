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

#define PARALLEL

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Xml;
using Validator;

namespace CooperativeCoevolutionaryAlgorithm
{
    /// <summary>
    /// Represents a population of individuals
    /// </summary>
    class Population
    {
        public enum FirstGenerationType { RANDOM = 0, ONE_DEFAULT = 1, LOAD = 2 };
        public enum IndividualType { FEATURES = 0, PARAMETERS = 1, INSTANCES = 2 };

        public Individual[] Generation;
        public int PopulationSize, OffspringSize;
        public IndividualType PopulationType;
        public FirstGenerationType FirstGeneration = FirstGenerationType.RANDOM;

        public Population(int populationSize, int offspringSize)
        {
            PopulationSize = populationSize;
            OffspringSize = offspringSize;
        }


        public void createFirstGeneration(IndividualType type)
        {
            PopulationType = type;
            if (FirstGeneration == FirstGenerationType.RANDOM || FirstGeneration == FirstGenerationType.ONE_DEFAULT)
            {
                Generation = new Individual[PopulationSize + OffspringSize];
                for (int i = 0; i < PopulationSize + OffspringSize; i++)
                {
                    switch (type)
                    {
                        case IndividualType.PARAMETERS: Generation[i] = new IndividualParameters(); break;
                        case IndividualType.FEATURES: Generation[i] = new IndividualFeatures(); break;
                        case IndividualType.INSTANCES: Generation[i] = new IndividualInstances(); break;
                    }
                }

                if (FirstGeneration == FirstGenerationType.ONE_DEFAULT)
                {
                    switch (type)
                    {
                        case IndividualType.PARAMETERS: Generation[0] = new IndividualParameters(0); break;
                        case IndividualType.FEATURES: Generation[0] = new IndividualFeatures(0); break;
                        case IndividualType.INSTANCES: Generation[0] = new IndividualInstances(0); break;
                    }
                }
            }
        }

        //firstGeneration == FirstGeneration.LOAD
        public void createFirstGeneration(IndividualType type, string xmlFileName)
        {
            if (FirstGeneration != FirstGenerationType.LOAD)
            {
                createFirstGeneration(type);
                return;
            }

            PopulationType = type;
            Generation = new Individual[PopulationSize + OffspringSize];
            XmlDocument doc = new XmlDocument();
            doc.Load(xmlFileName);
            LoadXML(doc);
        }

        /// <summary>
        /// Returns an individual equal to the given individual if exists.
        /// </summary>
        public Individual equal(Individual individual)
        {
            for (int i = 0; i < PopulationSize; i++)
            {
                if (Generation[i].Equals(individual))
                    return Generation[i];
            }
            return null;
        }

        /// <summary>
        /// Evaluate the fitness of all the population
        /// </summary>
        public void evaluateFitness()
        {
            int progressCount = 0;
#if PARALLEL
            Parallel.For(0, PopulationSize, i =>
            {
                Program.evaluateFitness(Generation[i]);
                Interlocked.Increment(ref progressCount);
                Console.Write("\r" + progressCount + "/" + PopulationSize);
            });
#else
            for (int i = 0; i < PopulationSize; i++)
            {
                Program.evaluateFitness(Generation[i]);
                progressCount++;
                Console.Write("\r" + progressCount + "/" + PopulationSize);
            }
#endif
        }

        /// <summary>
        /// Mutate each individual of the population
        /// </summary>
        public void mutate()
        {
            foreach (Individual individual in Generation)
            {
                individual.mutate();
            }
        }

        /// <summary>
        /// Order the population by fitness
        /// </summary>
        public void order(int subset)
        {
            if (subset != PopulationSize + OffspringSize)
            {
                //Sort By Using Linq
                var tmp_subset = Generation.Take(subset).ToArray();
                tmp_subset = tmp_subset.OrderByDescending(x => x.FitnessScore).ToArray();

                Array.Copy(tmp_subset, 0, Generation, 0, subset);
            }
            else
            {
                Generation = Generation.OrderByDescending(x => x.FitnessScore).ToArray();
            }
        }

        public override string ToString()
        {
            string s = "";

            for (int i = 0; i < PopulationSize; i++)
            {
                if (Generation[i] == null)
                    break;
                s += "" + Generation[i] + "\n";
            }
            return s;
        }

        public XmlDocument ToXML()
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml("<population></population>");

            for (int i = 0; i < PopulationSize; i++)
            {
                Individual indi = Generation[i];
                if (indi == null) continue;
                XmlDocument indi_doc = indi.ToXML();
                doc.DocumentElement.AppendChild(doc.ImportNode(indi_doc.DocumentElement, true));
            }
            return doc;
        }

        public void LoadXML(XmlDocument doc)
        {
            XmlNodeList nodeList = doc.DocumentElement.ChildNodes;

            int i = 0;
            foreach (XmlElement item in nodeList)
            {
                XmlDocument indi_doc = new XmlDocument();
                indi_doc.LoadXml("<individual>" + item.InnerXml + "</individual>");
                switch (item.Name)
                {
                    case "IndividualParameters": Generation[i] = new IndividualParameters(); break;
                    case "IndividualFeatures": Generation[i] = new IndividualFeatures(); break;
                    case "IndividualInstances": Generation[i] = new IndividualInstances(); break;
                }
                Generation[i].LoadXML(indi_doc);
                i++;
            }
        }
    }

    /// <summary>
    /// Individual of the population
    /// </summary>
    abstract class Individual
    {
        public static int NB_FEATURES = 1;
        public static int NB_LABELS = 1;
        public static int NB_INSTANCES = 1;

        public double FitnessScore = -1;
        public ResultSet result;

        /// <summary>
        /// Create an individual as random as possible by applying several mutations.
        /// </summary>
        public Individual()
        {

        }

        public Individual(Individual individual)
        {

        }

        /// <summary>
        /// Mutate several features of the individual.
        /// </summary>
        public abstract void mutate();
        public abstract void Recombine(Individual parent);
        public abstract void Copy(Individual parent);

        public abstract XmlDocument ToXML();
        public abstract void LoadXML(XmlDocument doc);
        public override abstract bool Equals(Object o);

        public static Boolean operator >(Individual o1, Individual o2)
        {
            return (o1.FitnessScore > o2.FitnessScore);
        }

        public static Boolean operator <(Individual o1, Individual o2)
        {
            return !(o1 > o2);
        }

        public override string ToString()
        {
            string s = "" + FitnessScore + " : ";

            return s;
        }

        /// <summary>
        /// Unused : Only defined to remove implementation warnings
        /// </summary>
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }

    class IndividualFeatures : Individual
    {
        public bool[] Features;

        /// <summary>
        /// Create an individual.
        /// </summary>
        public IndividualFeatures()
        {
            Features = new bool[NB_FEATURES];

            //At least 25% of ones
            double PROB_ONES = 0.25 + (double)UsualFunctions.random.NextDouble() * 0.75;

            for (int i = 0; i < Features.Length; i++)
            {
                Features[i] = ((double)UsualFunctions.random.NextDouble() < PROB_ONES);
            }
        }

        public IndividualFeatures(int k)
        {
            Features = new bool[NB_FEATURES];
            for (int i = 0; i < Features.Length; i++)
            {
                Features[i] = true;
            }
        }

        public IndividualFeatures(IndividualFeatures individual)
        {
            this.Features = (bool[])individual.Features.Clone();
        }

        /// <summary>
        /// Returns the number of activated features.
        /// </summary>
        public int getNbOfOnes()
        {
            return Features.Count(x => x);
        }

        /// <summary>
        /// Mutate several features of the individual.
        /// </summary>
        public override void mutate()
        {
            double PROB_MUTATION = (double)UsualFunctions.random.NextDouble() * 3.0 / NB_FEATURES;

            for (int feature = 0; feature < NB_FEATURES; ++feature)
            {
                if (UsualFunctions.random.NextDouble() < PROB_MUTATION)
                {
                    Features[feature] = !Features[feature];
                }
            }
        }

        public override void Recombine(Individual parent)
        {
            IndividualFeatures indi = (IndividualFeatures)parent;
            int crossover_point = UsualFunctions.random.Next(this.Features.Length);

            for (int i = crossover_point; i < this.Features.Length; ++i)
                this.Features[i] = indi.Features[i];
        }

        public override void Copy(Individual parent)
        {
            IndividualFeatures indi = (IndividualFeatures)parent;
            this.Features = (bool[])indi.Features.Clone();
        }

        public override string ToString()
        {
            string s = "" + FitnessScore + " : ";
            foreach (var val in Features)
            {
                s += (val) ? ("1") : ("0");
            }

            return s;
        }

        public override XmlDocument ToXML()
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml("<IndividualFeatures></IndividualFeatures>");

            //<FitnessScore>
            XmlNode element = doc.CreateElement("FitnessScore");
            XmlAttribute attribute = doc.CreateAttribute("value");
            attribute.Value = "" + this.FitnessScore;

            element.Attributes.Append(attribute);
            doc.DocumentElement.AppendChild(doc.ImportNode(element, true));

            //<Features>
            int i = 0;
            string s = "";
            foreach (var val in Features)
            {
                s += (i == 0) ? ("") : (" ");
                s += (val) ? ("1") : ("0");
                i++;
            }

            element = doc.CreateElement("Features");
            attribute = doc.CreateAttribute("value");
            attribute.Value = s;

            element.Attributes.Append(attribute);
            doc.DocumentElement.AppendChild(doc.ImportNode(element, true));

            return doc;
        }

        public override void LoadXML(XmlDocument doc)
        {
            this.FitnessScore = double.Parse(doc.GetElementsByTagName("FitnessScore")[0].Attributes["value"].Value);

            string s_Features = doc.GetElementsByTagName("Features")[0].Attributes["value"].Value;
            s_Features = s_Features.Replace(" ", "");
            Features = s_Features.Select(c => c == '1').ToArray();
        }

        /// <summary>
        /// Individuals are equals if the genes are equals.
        /// </summary>
        public override bool Equals(Object o)
        {
            if (o.GetType() == typeof(IndividualFeatures))
            {
                IndividualFeatures ind = (IndividualFeatures)o;

                if (ind.Features.Length != this.Features.Length)
                    return false;

                for (int i = 0; i < ind.Features.Length; i++)
                {
                    if (ind.Features[i] != this.Features[i])
                    {
                        return false;
                    }
                }
                return true;
            }
            return false;
        }

        /// <summary>
        /// Unused : Only defined to remove implementation warnings
        /// </summary>
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }

    class IndividualParameters : Individual
    {
        public static int MIN_K = 5, MAX_K = 130, DEFAULT_K = 8;

        public int[] K;

        /// <summary>
        /// Create an individual as random as possible by applying several mutations.
        /// </summary>
        public IndividualParameters()
        {
            K = new int[NB_LABELS];

            for (int i = 0; i < NB_LABELS; i++)
            {
                K[i] = UsualFunctions.random.Next(MIN_K, MAX_K);
            }
        }

        public IndividualParameters(int k)
        {
            K = new int[NB_LABELS];
            for (int i = 0; i < K.Length; i++)
            {
                K[i] = DEFAULT_K;
            }
        }

        public IndividualParameters(IndividualParameters individual)
        {
            this.K = (int[])individual.K.Clone();
        }

        /// <summary>
        /// Mutate several features of the individual.
        /// </summary>
        public override void mutate()
        {
            double PROB_MUTATION = (double)UsualFunctions.random.NextDouble() * 3.0 / NB_LABELS;

            if(UsualFunctions.random.NextDouble() < 0.5)
            {
                //Normal random value
                int mean = (MAX_K + MIN_K)/2;
                int gaussianValue = (int)UsualFunctions.nextGaussian(mean, (MAX_K-mean)/3);
            }
            else
            {
                //Random increase or decrease
                for (int i = 0; i < NB_LABELS; i++)
                {
                    if (UsualFunctions.random.NextDouble() < PROB_MUTATION)
                    {
                        double rand = UsualFunctions.random.NextDouble();

                        if (rand < 0.25)
                            K[i] += 1;
                        if (rand > 0.75)
                            K[i] -= 1;

                        if (K[i] <= 0)
                            K[i] = 1;
                    }
                }
            }
        }

        public override void Recombine(Individual parent)
        {
            IndividualParameters indi = (IndividualParameters)parent;

            int crossover_point = UsualFunctions.random.Next(this.K.Length);

            for (int i = crossover_point; i < this.K.Length; ++i)
                this.K[i] = indi.K[i];
        }

        public override void Copy(Individual parent)
        {
            IndividualParameters indi = (IndividualParameters)parent;
            this.K = (int[])indi.K.Clone();
        }

        public override string ToString()
        {
            string s = "" + FitnessScore + " : ";

            foreach (var val in K)
            {
                s += val + " ";
            }

            return s;
        }

        public override XmlDocument ToXML()
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml("<IndividualParameters></IndividualParameters>");

            //<FitnessScore>
            XmlNode element = doc.CreateElement("FitnessScore");
            XmlAttribute attribute = doc.CreateAttribute("value");
            attribute.Value = "" + this.FitnessScore;

            element.Attributes.Append(attribute);
            doc.DocumentElement.AppendChild(doc.ImportNode(element, true));

            //<K>
            int i = 0;
            string s = "";
            foreach (var val in K)
            {
                s += (i == 0) ? ("") : (" ");
                s += "" + val;
                i++;
            }

            element = doc.CreateElement("K");
            attribute = doc.CreateAttribute("value");
            attribute.Value = s;

            element.Attributes.Append(attribute);
            doc.DocumentElement.AppendChild(doc.ImportNode(element, true));

            return doc;
        }

        public override void LoadXML(XmlDocument doc)
        {
            this.FitnessScore = double.Parse(doc.GetElementsByTagName("FitnessScore")[0].Attributes["value"].Value);

            string s_K = doc.GetElementsByTagName("K")[0].Attributes["value"].Value;
            K = Array.ConvertAll(s_K.Split(' '), int.Parse);
        }

        /// <summary>
        /// Individuals are equals if the genes are equals.
        /// </summary>
        public override bool Equals(Object o)
        {
            if (o.GetType() == typeof(IndividualFeatures))
            {
                IndividualParameters ind = (IndividualParameters)o;

                for (int i = 0; i < ind.K.Length; i++)
                {
                    if (ind.K[i] != this.K[i])
                    {
                        return false;
                    }
                }
                return true;
            }
            return false;
        }

        /// <summary>
        /// Unused : Only defined to remove implementation warnings
        /// </summary>
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }

    class IndividualInstances : Individual
    {
        public bool[] Instances;

        /// <summary>
        /// Create an individual as random as possible by applying several mutations.
        /// </summary>
        public IndividualInstances()
        {
            Instances = new bool[NB_INSTANCES];

            //At least 25% of ones
            double PROB_ONES = 0.25 + (double)UsualFunctions.random.NextDouble() * 0.75;

            for (int i = 0; i < Instances.Length; i++)
            {
                Instances[i] = ((double)UsualFunctions.random.NextDouble() < PROB_ONES);
            }
        }

        public IndividualInstances(int k)
        {
            Instances = new bool[NB_INSTANCES];
            for (int i = 0; i < Instances.Length; i++)
            {
                Instances[i] = true;
            }
        }

        public IndividualInstances(IndividualInstances individual)
        {
            this.Instances = (bool[])individual.Instances.Clone();
        }

        /// <summary>
        /// Mutate several features of the individual.
        /// </summary>
        public override void mutate()
        {
            double PROB_MUTATION = (double)UsualFunctions.random.NextDouble() * 3.0 / NB_INSTANCES;

            for (int instance = 0; instance < NB_INSTANCES; ++instance)
            {
                if (UsualFunctions.random.NextDouble() < PROB_MUTATION)
                {
                    Instances[instance] = !Instances[instance];
                }
            }
        }

        public override void Recombine(Individual parent)
        {
            IndividualInstances indi = (IndividualInstances)parent;

            int crossover_point = UsualFunctions.random.Next(this.Instances.Length);

            for (int i = crossover_point; i < this.Instances.Length; ++i)
                this.Instances[i] = indi.Instances[i];
        }

        public override void Copy(Individual parent)
        {
            IndividualInstances indi = (IndividualInstances)parent;
            this.Instances = (bool[])indi.Instances.Clone();
        }

        public override string ToString()
        {
            string s = "" + FitnessScore + " : ";
            foreach (var val in Instances)
            {
                s += (val) ? ("1") : ("0");
            }

            return s;
        }

        public override XmlDocument ToXML()
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml("<IndividualInstances></IndividualInstances>");

            //<FitnessScore>
            XmlNode element = doc.CreateElement("FitnessScore");
            XmlAttribute attribute = doc.CreateAttribute("value");
            attribute.Value = "" + this.FitnessScore;

            element.Attributes.Append(attribute);
            doc.DocumentElement.AppendChild(doc.ImportNode(element, true));

            //<Instances>
            int i = 0;
            string s = "";
            foreach (var val in Instances)
            {
                s += (i == 0) ? ("") : (" ");
                s += (val) ? ("1") : ("0");
                i++;
            }

            element = doc.CreateElement("Instances");
            attribute = doc.CreateAttribute("value");
            attribute.Value = s;

            element.Attributes.Append(attribute);
            doc.DocumentElement.AppendChild(doc.ImportNode(element, true));

            return doc;
        }

        public override void LoadXML(XmlDocument doc)
        {
            this.FitnessScore = double.Parse(doc.GetElementsByTagName("FitnessScore")[0].Attributes["value"].Value);

            string s = doc.GetElementsByTagName("Instances")[0].Attributes["value"].Value;
            s = s.Replace(" ", "");
            Instances = s.Select(c => c == '1').ToArray();
        }

        /// <summary>
        /// Individuals are equals if the genes are equals.
        /// </summary>
        public override bool Equals(Object o)
        {
            if (o.GetType() == typeof(IndividualInstances))
            {
                IndividualInstances ind = (IndividualInstances)o;

                if (ind.Instances.Length != this.Instances.Length)
                    return false;

                for (int i = 0; i < ind.Instances.Length; i++)
                {
                    if (ind.Instances[i] != this.Instances[i])
                    {
                        return false;
                    }
                }
                return true;
            }
            return false;
        }

        /// <summary>
        /// Unused : Only defined to remove implementation warnings
        /// </summary>
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
