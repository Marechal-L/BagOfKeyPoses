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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Parser;
using BagOfKeyPoses;

namespace EvolutionnaryAlgorithm
{
    class UnitTest
    {
        static void Main(string[] args)
        {
            Console.Write("JointsRecombinationTest() : "+JointsRecombinationTest());
            Console.Write("NoFeaturesTest() : " + NoFeaturesTest());

            /*Random rand = new Random(23654);

            for (int i = 0; i < 3; i++)
            {
                Console.WriteLine(rand.Next());
            }*/
          
            Console.ReadKey();
        }

        //According to fig. 4 - http://www.sciencedirect.com/science/article/pii/S0957417413006210 
        static Boolean JointsRecombinationTest()
        {
            Individual.NB_FEATURES = 20;

            Individual father = new Individual();
            father.Genes = new Boolean[] { false, true, false, false, false, false, true, true, false, true, false, true, true, false, true, true, false, false, false, true };

            Individual mother = new Individual();
            mother.Genes = new Boolean[] { true, false, false, true, false, true, true, false, true, true, false, false, true, false, true, true, true, true, false, false };

            Individual child = new Individual(father);

            UsualFunctions.RecombineJoints(child, mother,0);

            Boolean[] expected = new Boolean[] { true, true, false, false, false, false, true, false, false, true, false, false, true, false, true, true, false, false, false, true };

            if (expected.Length != child.Genes.Length)
                return false;

            Boolean result = true;
            for (int i = 0; i < expected.Length; i++)
			{
                if (expected[i] != child.Genes[i])
                {
                    result = false;
                }
            }

            return result;
        }

        static Boolean NoFeaturesTest()
        {
            Individual.NB_FEATURES = 20;
            Individual ind = new Individual();
            ind.Genes = new Boolean[Individual.NB_FEATURES];

            Program.realDataset = DatasetParser.loadDatasetSkeleton(Individual.NB_FEATURES, "../../../../BagOfKeyPoses_Library/datasets/MSR/AS1", ' ');

            Program.learning_params = new LearningParams();
            Program.learning_params.ClassLabels = Program.realDataset.Labels;
            Program.learning_params.Clustering = LearningParams.ClusteringType.Kmeans;
            Program.learning_params.InitialK = 8;

            Program.evaluateFitness(ind);

            return true;
        }
    }
}