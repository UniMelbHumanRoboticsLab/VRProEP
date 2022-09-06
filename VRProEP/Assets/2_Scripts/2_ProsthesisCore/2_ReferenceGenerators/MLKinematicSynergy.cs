using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRProEP.GameEngineCore;

namespace VRProEP.ProsthesisCore
{
    

    public class MLKinematicSynergy : AdaptiveGenerator
    {
        public const int ZMQ_PULL_PORT = 7000;
        public const float INI_ELBOW = 90.0f;

        private float prevShoulderFE = 0;
        private float prevElbowFE = INI_ELBOW;

        private float theta = 1.0f;
        private float alpha = 0.5f;
        private float beta = 20.0f;
        /// <summary>
        /// Basic synergistic prosthesis reference generator.
        /// Provides position reference for prosthesis joints through a simple linear kinematic synergy.
        /// </summary>
        /// <param name="xBar">The initial references.</param>
        /// <param name="xMin">The lower limit for the references.</param>
        /// <param name="xMax">The upper limit for the references.</param>
        /// <param name="theta">The initial parameters.</param>
        /// <param name="thetaMin">The lower limit for the parameters.</param>
        /// <param name="thetaMax">The upper limit for the parameters.</param>
        public MLKinematicSynergy(float[] xBar, float[] xMin, float[] xMax, float[] theta, float[] thetaMin, float[] thetaMax) 
                                    : base(xBar, xMin, xMax, theta, thetaMin, thetaMax, ReferenceGeneratorType.MLKinematicSynergy)
        {
            //Debug.Log("Machine Learning Reference Generator");
        }


        /// <summary>
        /// Updates the reference for the given channel to be tracked by a controller or device.
        /// Should only be called during Physics updates, Monobehaviour : FixedUpdate.
        /// </summary>
        /// <param name="channel">The channel number.</param>
        /// <param name="input">The input to use to update the reference.</param>
        /// <returns>The updated reference.</returns>
        public override float UpdateReference(int channel, float[] input)
        {
            if(channel == 0) // Only get data at the first update request
                xBar = ZMQSystem.GetLatestPulledData(ZMQ_PULL_PORT);

            if (xBar == null)
                return - Mathf.Deg2Rad * INI_ELBOW;
            else  
            {
                float elbowRef = xBar[channel]; //Elbow ref in rad
                float currentShoulderFE = input[0]; 
                float currentElbowFE = input[1]; //current elbow in rad

                float dElbowFE = elbowRef - currentShoulderFE;
                float dShoulderFE = currentShoulderFE - prevShoulderFE;

                float tempxBar =0;


                tempxBar = theta * currentShoulderFE;

                // Update synergy
                theta = (elbowRef - currentElbowFE) / currentShoulderFE;

                // Move time step further
                prevShoulderFE = currentShoulderFE;
                prevElbowFE = tempxBar;

                Debug.Log("Elbow ref" + Mathf.Rad2Deg * tempxBar);
                // Return the reference value
                return -tempxBar;



                /*
               // Update reference
               float tempDelta = theta * currentShoulderFE;


               if (tempDelta > prevElbowFE)
                   tempDelta = prevElbowFE;

               float tempxBar = (1-alpha) * prevElbowFE + alpha * theta * tempDelta;


               
               */

                //Debug.Log("dSFE" + Mathf.Rad2Deg * dShoulderFE);
                //Debug.Log("dEFE" + Mathf.Rad2Deg * dElbowFE);
                //Debug.Log("Ref" +  elbowRef);
                //Debug.Log("Current EFE" + Mathf.Rad2Deg * currentElbowFE);
                //Debug.Log("Current SFE" + Mathf.Rad2Deg * currentShoulderFE);

            }


        }

        /// <summary>
        /// Updates all the references to be tracked by multiple controllers or devices.
        /// Should only be called within Monobehaviour : FixedUpdate.
        /// </summary>
        /// <param name="input">The input to use to update the references.</param>
        /// <returns>The updated set of references.</returns>
        public override float[] UpdateAllReferences(float[] input)
        {
            //xBar = ZMQSystem.GetLatestPulledData(ZMQ_PULL_PORT);

            //Debug.Log(xBar[0]);
            return xBar;
        }
    }
}

