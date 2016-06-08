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

//Define or comment it if you want to use the joints recombination or not.
#define RECOMBINE_JOINTS

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace EvolutionaryAlgorithm
{
    class UsualFunctions
    {
        public static Random random = new Random();
        public static TreeNode tree = initMSRTree();

        /// <summary>
        /// Create the tree used for the joints recombination process.
        /// </summary>
        public static TreeNode initMSRTree()
        {
            TreeNode rightArm = new TreeNode(0, new TreeNode(7, new TreeNode(9, new TreeNode(11))));
            TreeNode leftArm = new TreeNode(1, new TreeNode(8, new TreeNode(10, new TreeNode(12))));
            TreeNode rightLeg = new TreeNode(4, new TreeNode(13, new TreeNode(15, new TreeNode(17))));
            TreeNode leftLeg = new TreeNode(5, new TreeNode(14, new TreeNode(16, new TreeNode(18))));
            TreeNode torso = new TreeNode(3, new TreeNode(6, new TreeNode[] { rightLeg, leftLeg }));
            TreeNode neck = new TreeNode(2, new TreeNode[] {rightArm, torso, leftArm });
            TreeNode tree = new TreeNode(19, neck);

            return tree;
        }

        //Selection functions
        public static Individual RankSelection(Population population)
        {
            //Selection by ranking
            int n = population.PopulationSize;
            int sum = (n * (n + 1)) / 2;
            int randomValue = random.Next(sum);

            int accum = 0;

            for (int individual = 0; individual < n; ++individual)
            {
                accum += (n - individual);
                if (randomValue < accum)
                {
                    return population.Generation[individual];
                }
            }
            return population.Generation[n - 1];
        }

        public static Individual RouletteSelection(Population population)
        {
            int n = population.PopulationSize;

            double totalFitness = 0.0;
            for (int individual = 0; individual < n; ++individual)
                totalFitness += population.Generation[individual].FitnessScore;

            double randomValue = random.NextDouble() * totalFitness;

            //Console.Write("Random value= " + randomValue);

            double accum = 0;

            for (int individual = 0; individual < n; ++individual)
            {
                accum += population.Generation[individual].FitnessScore;

                if (randomValue < accum)
                {
                    return population.Generation[individual];
                }
            }
            return population.Generation[n - 1];
        }

        public static Individual TournamentSelection(Population population, int k)
        {
            Individual tmp, best = null;
            for (int i = 0; i < k; i++)
			{
			    tmp = population.Generation[random.Next(1,population.PopulationSize)]; 
                if (best == null || tmp > best)
                    best = tmp;
			}
            return best;
        }
        
        //Recombination functions
        public static void Recombine(Population population, ref Individual individual)
        {
            Individual father, mother;

            //Console.WriteLine("Selecting parents");
#if TOURNAMENT_SELECTION
            int k = 2;
            father = TournamentSelection(population,k);
            do
            {
                mother = TournamentSelection(population,k);
            } while (father == mother);
            
#elif ROULETTE_SELECTION
            father = RouletteSelection(population);
            do
            {
                mother = RouletteSelection(population);
            } while (father == mother);
#else
            father = RankSelection(population);
            do
            {
                mother = RankSelection(population);
            } while (father == mother);
#endif

            individual = new Individual(father);

            if (random.NextDouble() < 0.75)
            {
#if RECOMBINE_JOINTS
                    RecombineJoints(individual, mother);
#else
                    int crossover_point = random.Next(Individual.NB_FEATURES);

                    for (int feature = crossover_point; feature < Individual.NB_FEATURES; ++feature)
                        individual.Genes[feature] = mother.Genes[feature];
#endif
            }
        }

        public static void RecombineJoints(Individual child, Individual mother)
        {
            int crossover_point = random.Next(Individual.NB_FEATURES);
            try { 
                TreeNode node = tree.findValue(crossover_point);
                node.recombineJoints(ref child.Genes, mother.Genes);
            }
            catch (Exception e)
            {
                throw new Exception("(UsualFunctions::RecombineJoints) Error occurred ( "+crossover_point+" is not a valid joint number ) : " + e.Message + " [" + e.InnerException + "]");
            }
        }

        public static void RecombineJoints(Individual child, Individual mother, int crossover_point)
        {
            try
            {
                TreeNode node = tree.findValue(crossover_point);
                node.recombineJoints(ref child.Genes, mother.Genes);
            }
            catch (Exception e)
            {
                throw new Exception("(UsualFunctions::RecombineJoints) Error occurred ( " + crossover_point + " is not a valid joint number ) : " + e.Message + " [" + e.InnerException + "]");
            }
        }
    }

    /// <summary>
    ///  Represents a tree node used for the joints recombination process.
    /// </summary>
    class TreeNode
    {
        public List<TreeNode> Children;
        public int Value;

        /// <summary>
        /// Initialise a simple node by giving a value
        /// </summary>
        public TreeNode(int val)
        {
            Value = val;
            Children = new List<TreeNode>();
        }

        /// <summary>
        /// Initialise a node by giving value and a children array.
        /// </summary>
        public TreeNode(int val, TreeNode[] nodes)
        {
            Value = val;
            Children = nodes.ToList<TreeNode>();
        }

        /// <summary>
        /// Initialise a node by giving value and a single child.
        /// </summary>
        public TreeNode(int val, TreeNode node)
        {
            Value = val;
            Children = new List<TreeNode>();
            Children.Add(node);
        }

        public void addChild(TreeNode node)
        {
            Children.Add(node);
        }

        /// <summary>
        /// Find a node in the tree by its value.
        /// </summary>
        public TreeNode findValue(int value)
        {
            TreeNode tmp = null;

            if(Value == value)
                return this;

            if(Children.Count == 0)
                return null;

            foreach (var child in Children)
            {
                tmp = child.findValue(value);
                if (tmp != null)
                    return tmp;
            }

            return tmp;
        }

        /// <summary>
        /// 
        /// </summary>
        public void recombineJoints(ref Boolean[] child, Boolean[] adult)
        {
            child[Value] = adult[Value];
            foreach (var node in Children)
            {
                node.recombineJoints(ref child, adult);
            }
        }
    }
}
