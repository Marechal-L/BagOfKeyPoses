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
        public static LearningParams learning_params;


        public static Dataset realDataset;                 //Dataset generated from txt files.
        public static Dataset modifiedDataset;             //Dataset without specified features.

        static int NB_FEATURES = 20;
        static int DIM_FEATURES = 3;                        //Dimension of each feature
        static int MAX_GENERATION_WITHOUT_CHANGE = 50;
        static int MAX_GENERATION = 500;


        //Entry point of the evolutionnary algorithm.
        static void Main(string[] args)
        {
            realDataset = DatasetParser.loadDatasetSkeleton(NB_FEATURES, "../../../datasets/MSR/AS1", ' ');
            
            learning_params = new LearningParams();
            learning_params.ClassLabels = realDataset.Labels;
            learning_params.Clustering = LearningParams.ClusteringType.Kmeans;
            learning_params.InitialK = 8;

            realDataset.normaliseSkeletons();

            //Parameters of the evolutionnary algorithm
            Individual.NB_FEATURES = NB_FEATURES;
            int populationSize = 10, offspringSize = 20;
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

            //Main loop of the algorithm
            int generationNumber=0;
            do{
                for (int crossover = 0; crossover < offspringSize; ++crossover)
                {
                    //Recombination
                    UsualFunctions.Recombine(population, ref population.Generation[populationSize + crossover]);
                    
                    individual = population.Generation[populationSize + crossover];

                    //Mutation of the new individual
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

                //Ordering the all population according the fitness
                population.order(populationSize + offspringSize);

                //End loop verifications
                if (prev_best_fitness != population.Generation[0].FitnessScore)
                {
                    prev_best_fitness = population.Generation[0].FitnessScore;
                    generations_without_change = 0;
                }
                else
                {
                    generations_without_change++;
                }
                
                


                //Displaying informations
                Console.WriteLine();
                Console.WriteLine("Worst Individual : " + population.Generation[populationSize - 1]);
                Console.WriteLine("Best individual : " + population.Generation[0]);

                Console.WriteLine();
                Console.WriteLine("generations_without_change : "+generations_without_change);
                Console.WriteLine("Generation : " + generationNumber);
                Console.WriteLine();

                generationNumber++;
            } while (generations_without_change < 10 && generationNumber < 500);



            //Writing of the results on the console and into a file
            string s = "Best Individual (gen. " + i + " ) : " + population.Generation[0] + "\n";
            s += "\nAll population : \n" + population;
            Console.WriteLine(s);

            string filename = "GeneticResult.log";
            System.IO.File.Create(filename).Close();

            System.IO.StreamWriter writer = new System.IO.StreamWriter(filename);
            writer.Write(s);
            writer.Close();

            Console.ReadKey();
        }

        /// <summary>
        /// Evaluate the fitness score of the given individual
        /// </summary>
        /// <returns>Boolean representing if the score is better or not</returns> 
        public static bool evaluateFitness(Individual individual)
        {
            learning_params.FeatureSize = individual.getNbOfOnes() * DIM_FEATURES;
            modifyDataset(individual);

            double old_f = individual.FitnessScore;

            ResultSet result = ValidationTest.twoFoldActorsTrainingSet(modifiedDataset, learning_params, new string[] { "s01", "s03", "s05", "s07", "s09" },2);

            double new_f = result.getAverage();

            if(new_f > old_f)
            {
                individual.FitnessScore = new_f;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Modifiy the dataset by removing disabled features according to the given individual.
        /// </summary>
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

        /// <summary>
        /// Removes disabled features from a sequence of frames
        /// </summary>
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

    /// <summary>
    /// Represents a population of individuals
    /// </summary>
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
            for (int i = 0; i < PopulationSize; i++)
            {
                Program.evaluateFitness(Generation[i]);
            }
        }

        /// <summary>
        /// Mutate each individual of the population
        /// </summary>
        public void mutate()
        {
            foreach(Individual individual in Generation)
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
    }

    /// <summary>
    /// Individual of the population
    /// </summary>
    class Individual
    {
        public static int NB_FEATURES = 1;
        public bool[] Genes;
        public double FitnessScore = -1;

        /// <summary>
        /// Create an individual as random as possible by applying several mutations.
        /// </summary>
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
                s += (val)?("1"):("0");
            }
            
            return s;
        }

        /// <summary>
        /// Individuals are equals if the genes are equals.
        /// </summary>
        public override bool Equals(Object o)
        {
            if (ReferenceEquals(this, o))
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
            return  (o1.FitnessScore > o2.FitnessScore) || (o1.FitnessScore == o2.FitnessScore && o1.getNbOfOnes() < o2.getNbOfOnes());
        }

        public static Boolean operator <(Individual o1, Individual o2)
        {
            return !(o1 > o2);
        }
    }
}
