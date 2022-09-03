using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRProEP.GameEngineCore;

namespace VRProEP.ProsthesisCore
{
    

    public class MLKinematicSynergy : AdaptiveGenerator
    {
        public const int ZMQ_PULL_PORT = 7000;
        

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
            Debug.Log("Machine Learning Reference Generator");
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
            
            xBar = ZMQSystem.GetLatestPulledData(ZMQ_PULL_PORT);
            if (xBar == null)
                return 0;
            else
            {
                Debug.Log("ML Ref: " + xBar[0]);
                return xBar[0];
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
            xBar = ZMQSystem.GetLatestPulledData(ZMQ_PULL_PORT);
            Debug.Log(xBar[0]);
            return xBar;
        }
    }
}

