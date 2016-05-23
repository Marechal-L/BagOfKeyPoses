using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using Util;

namespace Parser
{
    public static class SkeletonNormalisation
    {
        private static int nbJointRightShoulder = 1, nbJointLeftShoulder = 2, nbJointNeck = 3, nbJointTorso = 4, nbJointRightHip = 5, nbJointLeftHip = 6;

        /// <summary>
        /// Entry point for the validation, normalisation and rotation of a sequence.
        /// </summary>
        public static List<double[]> normaliseSequenceSkeleton(List<double[]> sequence)
        {
           List<double[]> modifiedSequence = validateSequence(sequence);
           modifiedSequence = normaliseSequence(modifiedSequence);
           modifiedSequence = rotateSequence(modifiedSequence);

           return modifiedSequence;
        }

        /// <summary>
        /// Normalise the skeletons by translating and dividing by the normalising length.
        /// </summary>
        private static List<double[]> normaliseSequence(List<double[]> sequence)
        {
            List<double[]> normalisedSequence = new List<double[]>();

            double[] origin = new double[] { 0,0,0 };

            //Foreach feature = skeleton (20 joints)
            for (int i = 0; i < sequence.Count; i++)
            {
                double[] skeleton = sequence[i];
               
                //Initialisation of the center of mass
                int cont_joints = 0;
                origin[0] = 0;
                origin[1] = 0;
                origin[2] = 0;

                //Calculation of the center of mass
                for (int j = 0; j < skeleton.Length; j+=3)
                {
                    origin[0] += skeleton[j];
                    origin[1] += skeleton[j+1];
                    origin[2] += skeleton[j+2];

                    cont_joints++;
                }

                origin[0] /= cont_joints;
                origin[1] /= cont_joints;
                origin[2] /= cont_joints;


                // Calculate the average distance from each joint to the center of mass
                double normDistance = 0.0;
                for (int j = 0; j < skeleton.Length; j += 3)
                {
                    double[] pjoint = new double[] { skeleton[j], skeleton[j+1], skeleton[j+2] };                  
                    normDistance += Functions.EuclideanDistance(origin, pjoint);
                }

                normDistance /= cont_joints;

               /*  
                *  Translate according to the new reference centre
                *  Normalise the coordinates according to the normalising length
                */
                for (int j = 0; j < skeleton.Length; j += 3)
                {
                    skeleton[j] = (float)((skeleton[j] - origin[0]) / normDistance);
                    skeleton[j + 1] = (float)((skeleton[j + 1] - origin[1]) / normDistance);
                    skeleton[j + 2] = (float)((skeleton[j + 2] - origin[2]) / normDistance);  
                }
                normalisedSequence.Add(skeleton);
            }
            return normalisedSequence;
        }

        /// <summary>
        /// Rotation of the skeleton with respect to the Kinect.
        /// According to the Fig.6 in the paper "Evolutionary joint selection to improve human action recognition with RGB-D devices" (http://www.sciencedirect.com/science/article/pii/S0957417413006210)
        /// </summary>
        private static List<double[]> rotateSequence(List<double[]> sequence)
        {
            List<double[]> rotatedSequence = new List<double[]>();

            double angle = Double.MaxValue;

            double[] jointNeck = new double[] { 0, 0, 0 };
            double[] jointLeftShoulder, jointRightShoulder, jointLeftHip, jointRightHip;

            //Foreach feature skeleton (20 joints)
            for (int i = 0; i < sequence.Count; i++)
            {
                double[] skeleton = sequence[i];

                // Calculate the angle for the first skeleton in the sequence. The following skeletons will be rotated according to that angle   
                if (angle == Double.MaxValue)
                {
                    //skeleton[0] skeleton[1] skeleton[2]
                    jointNeck = new double[] { skeleton[(nbJointNeck-1)*3], skeleton[(nbJointNeck - 1) * 3 + 1], skeleton[(nbJointNeck - 1) * 3 + 2] };                                                  
                    jointLeftShoulder = new double[] { skeleton[(nbJointLeftShoulder - 1) * 3], skeleton[(nbJointLeftShoulder - 1) * 3 + 1], skeleton[(nbJointLeftShoulder - 1) * 3 + 2] };                                           
                    jointRightShoulder = new double[] { skeleton[(nbJointRightShoulder - 1) * 3], skeleton[(nbJointRightShoulder - 1) * 3 + 1], skeleton[(nbJointRightShoulder - 1) * 3 + 2] };                                         
                    jointLeftHip = new double[] { skeleton[(nbJointLeftHip - 1) * 3], skeleton[(nbJointLeftHip - 1) * 3 + 1], skeleton[(nbJointLeftHip - 1) * 3 + 2] };                                             
                    jointRightHip = new double[] { skeleton[(nbJointRightHip - 1) * 3], skeleton[(nbJointRightHip - 1) * 3 + 1], skeleton[(nbJointRightHip - 1) * 3 + 2] };                                            
                    
                    // The angle is calculated as the average coordinate between the left shoulder and hip, and the average coordinate of the right shoulder and hip
                    angle = Math.Atan(((jointLeftShoulder[2] + jointLeftHip[2]) / 2 - (jointRightShoulder[2] + jointRightHip[2]) / 2) / ((jointLeftShoulder[0] + jointLeftHip[0]) / 2 - (jointRightShoulder[0] + jointRightHip[0]) / 2));

                    Debug.WriteLine("Angle= " + angle * 180 / Math.PI);
                }

                // Rotation of the whole sequence according to the angle of the first skeleton.
                for (int j = 0; j < skeleton.Length; j += 3)
                {
                    double[] pjoint = new double[] { skeleton[j], skeleton[j + 1], skeleton[j + 2] };

                    double a = pjoint[0] - jointNeck[0];
                    double b = pjoint[2] - jointNeck[2];

                    pjoint[0] = a * Math.Cos(angle) + b * Math.Sin(angle);
                    pjoint[2] = -a * Math.Sin(angle) + b * Math.Cos(angle);

                    skeleton[j] = pjoint[0];
                    skeleton[j + 1] = pjoint[1];
                    skeleton[j + 2] = pjoint[2];
                }
                rotatedSequence.Add(skeleton);
            }
            return rotatedSequence;
        }


        /// <summary>
        /// Remove all invalids skeletons according to the distance between the neck and the torso.
        /// </summary>
        private static List<double[]> validateSequence(List<double[]> sequence)
        {
            int origLength = sequence.Count;

            List<double[]> validatedSequence = new List<double[]>();

            int invalidSkeletons = 0;
            for (int i = 0; i < sequence.Count; i++)
            {
                double[] skeleton = sequence[i];

                double[] jointNeck = new double[] { skeleton[(nbJointNeck-1)*3], skeleton[(nbJointNeck-1) * 3 + 1], skeleton[(nbJointNeck-1) * 3 + 2] };
                double[] jointTorso = new double[] { skeleton[(nbJointTorso - 1) * 3], skeleton[(nbJointTorso - 1) * 3], skeleton[(nbJointTorso - 1) * 3] };

                double distance = Functions.EuclideanDistance(jointNeck, jointTorso);

                if (distance != 0)
                    validatedSequence.Add(skeleton);
                else invalidSkeletons++;
            }

            if (invalidSkeletons > 0)
                Debug.WriteLine("Invalid skeletons: " + invalidSkeletons);

            return validatedSequence;
        }

    }
}
