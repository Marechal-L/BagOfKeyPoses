﻿/*
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

namespace EvolutionaryAlgorithm
{
    /// <summary>
    /// Represents a population of individuals
    /// </summary>
    class Population
    {
        public enum FirstGenerationType { RANDOM = 0, ONE_DEFAULT = 1, LOAD = 2 };

        public Individual[] Generation;
        public int PopulationSize, OffspringSize;
        public FirstGenerationType FirstGeneration = FirstGenerationType.RANDOM;

        public Population(int populationSize, int offspringSize, int nbOfFeatures)
        {
            PopulationSize = populationSize;
            OffspringSize = offspringSize;

            Individual.NB_FEATURES = nbOfFeatures;
        }

        public void createFirstGeneration()
        {
            if (FirstGeneration == FirstGenerationType.RANDOM || FirstGeneration == FirstGenerationType.ONE_DEFAULT)
            {
                Generation = new Individual[PopulationSize + OffspringSize];
                for (int i = 0; i < PopulationSize; i++)
                {
                    Generation[i] = new Individual();
                }

                if (FirstGeneration == FirstGenerationType.ONE_DEFAULT)
                    Generation[0] = new Individual(0);
            }
        }


        //firstGeneration == FirstGeneration.LOAD
        public void createFirstGeneration(string xmlFileName)
        {
            if (FirstGeneration != FirstGenerationType.LOAD)
            {
                createFirstGeneration();
                return;
            }

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
                tmp_subset = tmp_subset.OrderByDescending(x => x.FitnessScore).ThenByDescending(x => x.getNbOfOnes()).ToArray();

                Array.Copy(tmp_subset, 0, Generation, 0, subset);
            }
            else
            {
                Generation = Generation.OrderByDescending(x => x.FitnessScore).ThenBy(x => x.getNbOfOnes()).ToArray();
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
            XmlNodeList nodeList = doc.GetElementsByTagName("individual");

            for (int i = 0; i < PopulationSize; i++)
            {
                Individual individual = new Individual();
                XmlDocument indi_doc = new XmlDocument();
                indi_doc.LoadXml("<individual>" + nodeList[i].InnerXml + "</individual>");
                individual.LoadXML(indi_doc);
                Generation[i] = individual;
            }
        }
    }

    /// <summary>
    /// Individual of the population
    /// </summary>
    class Individual
    {
        public static int NB_FEATURES = 1;
        public bool[] Genes;
        public double FitnessScore = -1;
        public ResultSet result;

        /// <summary>
        /// Create an individual.
        /// </summary>
        public Individual()
        {
            Genes = new bool[NB_FEATURES];

            //At least 25% of ones
            double PROB_ONES = 0.25 + (double)UsualFunctions.random.NextDouble() * 0.75;

            for (int i = 0; i < Genes.Length; i++)
            {
                Genes[i] = ((double)UsualFunctions.random.NextDouble() < PROB_ONES);
            }
        }

        public Individual(int k)
        {
            Genes = new bool[NB_FEATURES];
            for (int i = 0; i < Genes.Length; i++)
            {
                Genes[i] = true;
            }
        }

        public Individual(Individual individual)
        {
            this.Genes = (bool[])individual.Genes.Clone();
        }

        /// <summary>
        /// Returns the number of activated features.
        /// </summary>
        public int getNbOfOnes()
        {
            return Genes.Count(x => x);
        }

        /// <summary>
        /// Mutate several features of the individual.
        /// </summary>
        public void mutate()
        {
            double PROB_MUTATION_FEATURES = (double)UsualFunctions.random.NextDouble() * 3.0 / NB_FEATURES;

            for (int feature = 0; feature < NB_FEATURES; ++feature)
            {
                if (UsualFunctions.random.NextDouble() < PROB_MUTATION_FEATURES)
                {
                    Genes[feature] = !Genes[feature];
                }
            }
        }

        public override string ToString()
        {
            string s = "" + FitnessScore + " : ";
            foreach (var val in Genes)
            {
                s += (val) ? ("1") : ("0");
            }

            return s;
        }

        public XmlDocument ToXML()
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml("<individual></individual>");

            //<FitnessScore>
            XmlNode element = doc.CreateElement("FitnessScore");
            XmlAttribute attribute = doc.CreateAttribute("value");
            attribute.Value = "" + this.FitnessScore;

            element.Attributes.Append(attribute);
            doc.DocumentElement.AppendChild(doc.ImportNode(element, true));

            //<Features>
            int i = 0;
            string s_features = "";
            foreach (var val in Genes)
            {
                s_features += (i == 0) ? ("") : (" ");
                s_features += (val) ? ("1") : ("0");
                i++;
            }

            element = doc.CreateElement("Features");
            attribute = doc.CreateAttribute("value");
            attribute.Value = s_features;

            element.Attributes.Append(attribute);
            doc.DocumentElement.AppendChild(doc.ImportNode(element, true));

            return doc;
        }

        public void LoadXML(XmlDocument doc)
        {
            this.FitnessScore = double.Parse(doc.GetElementsByTagName("FitnessScore")[0].Attributes["value"].Value);

            string s_Genes = doc.GetElementsByTagName("Features")[0].Attributes["value"].Value;
            s_Genes = s_Genes.Replace(" ", "");
            Genes = s_Genes.Select(c => c == '1').ToArray();
        }

        /// <summary>
        /// Individuals are equals if the genes are equals.
        /// </summary>
        public override bool Equals(Object o)
        {
            if (o.GetType() == typeof(Individual))
            {
                Individual ind = (Individual)o;

                if (ind.Genes.Length != this.Genes.Length)
                    return false;

                for (int i = 0; i < ind.Genes.Length; i++)
                {
                    if (ind.Genes[i] != this.Genes[i])
                    {
                        return false;
                    }
                }

                return true;
            }
            return false;
        }

        public static Boolean operator >(Individual o1, Individual o2)
        {
            return (o1.FitnessScore > o2.FitnessScore) || (o1.FitnessScore == o2.FitnessScore && o1.getNbOfOnes() < o2.getNbOfOnes());
        }

        public static Boolean operator <(Individual o1, Individual o2)
        {
            return !(o1 > o2);
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
