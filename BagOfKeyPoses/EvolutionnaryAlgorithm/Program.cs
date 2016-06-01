using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BagOfKeyPoses;
using Parser;
using Sequence = System.Collections.Generic.List<double[]>;
using Validator;

namespace EvolutionnaryAlgorithm
{
    class Program
    {
        private static LearningParams learning_params;
        private static Dataset realDataset, modifiedDataset;

        static int NB_FEATURES = 20;
        static int DIM_FEATURES = 3;


        static void Main(string[] args)
        {

            realDataset = DatasetParser.loadDatasetSkeleton(NB_FEATURES, "../../../datasets/MSR/AS1", ' ');
            

            learning_params = new LearningParams();
            learning_params.ClassLabels = realDataset.Labels;
            learning_params.Clustering = LearningParams.ClusteringType.Kmeans;
            learning_params.InitialK = 8;

            realDataset.normaliseSkeletons();


            Individual.NB_FEATURES = NB_FEATURES;

            int populationSize = 10, offspringSize = 20, crossover;
            int generations_without_change = 0;
            Individual individual, equalIndividual;

            //Create initial population
            Population population = new Population(populationSize, offspringSize, NB_FEATURES);
            
            //Evalutate Fitness
            population.evaluateFitness();

            //Order
            population.order(populationSize);
            Console.WriteLine(population);

            double prev_best_fitness = population.Generation[0].FitnessScore;

            int i=0;
            do{
               
                for (crossover = 0; crossover < offspringSize; ++crossover)
                {
                    //population[popSize + crossover].generation = generation;
                    UsualFunctions.Recombine(population, ref population.Generation[populationSize + crossover]);
                    individual = population.Generation[populationSize + crossover];
                    individual.mutate();

                    equalIndividual = population.equal(individual);
                    if (equalIndividual == null)
                    {
                        evaluateFitness(individual);
                    }
                    else
                    {
                        if (evaluateFitness(equalIndividual))
                        {
                            Console.WriteLine("************************* Individual " + equalIndividual + "************");
                        }
                    }
                }

                population.order(populationSize + offspringSize);
                Console.WriteLine();
                Console.WriteLine(population.Generation[populationSize-1]);
                Console.WriteLine(population.Generation[0]);

                if (prev_best_fitness != population.Generation[0].FitnessScore)
                {
                    prev_best_fitness = population.Generation[0].FitnessScore;
                    generations_without_change = 0;
                }
                else
                {
                    generations_without_change++;
                }
                
                i++;

                Console.WriteLine();
                Console.WriteLine("generations_without_change : "+generations_without_change);
                Console.WriteLine("Generation : "+i);
                Console.WriteLine();

            }while(generations_without_change < 10 && i < 500);



            //Writing of the results on the console and into a file
            string s = "Best Individual (gen. " + i + " ) : " + population.Generation[0] + "\n";

            foreach (var item in population.Generation[0].Genes)
            {
                s += item + " ";
            }

            Console.WriteLine(s);

            string filename = "GeneticResult.log";
            System.IO.File.Create(filename).Close();

            System.IO.StreamWriter writer = new System.IO.StreamWriter(filename);
            writer.Write(s);
            writer.Close();

            Console.ReadKey();
        }

        public static bool evaluateFitness(Individual individual)
        {
            learning_params.FeatureSize = individual.getNbOfOnes() * DIM_FEATURES;
            modifyDataset(individual);

            double old_f = individual.FitnessScore;

            //ResultSet result = ValidationTest.twoFoldActorsTrainingSet(modifiedDataset, learning_params, new string[] { "s01", "s03", "s05", "s07", "s09" },2);

            double new_f = individual.getNbOfOnes();//result.getAverage();

            if(new_f > old_f)
            {
                individual.FitnessScore = new_f;
                return true;
            }
            return false;
        }

        public static void modifyDataset(Individual individual)
        {
			modifiedDataset = new Dataset(realDataset);
			
			foreach(DatasetEntry entry in realDataset.Data)
			{
				DatasetEntry copy = new DatasetEntry(entry);

                copy.Sequence = removeDisabledFeatures(individual, entry.Sequence);

                modifiedDataset.Data.Add(copy);
			}
        }

        public static Sequence removeDisabledFeatures(Individual individual, Sequence sequence) 
        {
            int nbOfActivated = individual.getNbOfOnes();

            Sequence seq = new Sequence();

            foreach (Double[] frame in sequence)
            {
                Double[] n_frame = new Double[nbOfActivated*DIM_FEATURES];
                int n_count = 0;

                for (int i = 0; i < frame.Length; i+=DIM_FEATURES)
                {
                    if(i%DIM_FEATURES == 0 && individual.Genes[i/DIM_FEATURES])
                    {
                        Array.Copy(frame, i, n_frame, n_count, DIM_FEATURES);
                        n_count += DIM_FEATURES;
                    }
                }

                seq.Add(n_frame);
            }

            return seq;
        }
    }

    class Population
    {
        public Individual[] Generation;
        public int PopulationSize, OffspringSize;

        public Population(int populationSize, int offspringSize ,int nbOfFeatures)
        {
            PopulationSize = populationSize;
            OffspringSize = offspringSize;

            Individual.NB_FEATURES = nbOfFeatures;
            createFirstGeneration();
        }

        public void createFirstGeneration()
        {
            Generation = new Individual[PopulationSize + OffspringSize];
            for (int i = 0; i < PopulationSize; i++)
            {
                Generation[i] = new Individual();
            }
        }

        public Individual equal(Individual individual)
        {
            for (int i = 0; i < PopulationSize; i++)
            {
                if (Generation[i].Equals(individual))
                    return Generation[i];
            }
            return null;
        }

        public void evaluateFitness()
        {
            for (int i = 0; i < PopulationSize; i++)
            {
                Program.evaluateFitness(Generation[i]);
            }
        }

        public void mutate()
        {
            foreach(Individual individual in Generation)
            {
                individual.mutate();
            }
        }

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

            foreach (Individual individual in Generation)
            {
                s += ""+individual+"\n";
            }

            return s;
        }
    }

    class Individual
    {
        public static int NB_FEATURES = 1;
        public bool[] Genes;
        public double FitnessScore = -1;

        public Individual()
        {
            Genes = new bool[NB_FEATURES];
            int nbOfMutations = UsualFunctions.random.Next(NB_FEATURES);
            for (int i = 0; i < nbOfMutations; i++)
            {
                mutate();
            }
        }

        public Individual(Individual individual)
        {
            this.Genes = (bool[]) individual.Genes.Clone();
        }

        public int getNbOfOnes()
        {
            return Genes.Count(x => x);
        }

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
            return ""+FitnessScore;
        }

        public override bool Equals(Object o)
        {
            return (ReferenceEquals(this, o)) && (this.Genes.Equals(((Individual)o).Genes));
        }

        public static Boolean operator >(Individual o1, Individual o2)
        {
            return  (o1.FitnessScore > o2.FitnessScore) || (o1.FitnessScore == o2.FitnessScore && o1.getNbOfOnes() < o2.getNbOfOnes());
        }

        public static Boolean operator <(Individual o1, Individual o2)
        {
            return !(o1 > o2);
        }
    }
}
