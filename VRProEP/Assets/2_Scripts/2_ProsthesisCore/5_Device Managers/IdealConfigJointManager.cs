//======= Copyright (c) Melbourne Robotics Lab, All rights reserved. ===============
using UnityEngine;

namespace VRProEP.ProsthesisCore
{
    public class IdealConfigJointManager : BasicDeviceManager
    {
        public enum Type{ Position, Velocity}
        public Type type;
        // sensor and controllers unnecessary for this elbow device.
        private ConfigurableJoint configJoint;


        private float errorP = 0.0f;
        private float errorPP = 0.0f;
        public float KP  { get; set; }
        public float KI { get; set; }
        public float KD { get; set; }


        public float MaxAngVel { get; set; } // in deg/sec
        /// <summary>
        /// Manager for a virtual elbow prosthetic device with ideal tracking.
        /// </summary>
        /// <param name="sensor">The Unity HingeJoint for the elbow.</param>
        public IdealConfigJointManager(ConfigurableJoint joint,Type type)
        {
            if (joint == null)
                throw new System.ArgumentNullException("The provided Configurable joint object is empty.");
            this.configJoint = joint;

        }

        /// <summary>
        /// Returns all pre-processed joint states in an array.
        /// 0: Angular displacement given in radians.
        /// 1: Angular velocity given in radians per second.
        /// </summary>
        /// <returns>The array with pre-processed angular position and velocity data.</returns>
        public float[] GetJointStates()
        {
            float[] x = new float[2];
            //x[0] = configJoint.angle * Mathf.Deg2Rad;
            //x[1] = configJoint.velocity * Mathf.Deg2Rad;
            //return (Quaternion.FromToRotation(joint.axis, joint.connectedBody.transform.rotation.eulerAngles));
            return x;
        }

        /// <summary>
        /// Returns all pre-processed joint states in an array.
        /// 0: Angular displacement given in radians.
        /// 1: Angular velocity given in radians per second.
        /// </summary>
        /// <returns>The array with pre-processed angular position and velocity data.</returns>
        public float GetJointAngle()
        {
            return 0.0f;
        }

        /// <summary>
        /// Returns all pre-processed joint states in an array.
        /// 0: Angular displacement given in radians.
        /// 1: Angular velocity given in radians per second.
        /// </summary>
        /// <returns>The array with pre-processed angular position and velocity data.</returns>
        public float GetJointAngVel()
        {
            return 0.0f;
        }

        /// <summary>
        /// Updates the state of the device for the given channel.
        /// Since it's 1DOF, only one channel available.
        /// Reference determines the desired joint displacement.
        /// Should only be called during Physics updates, Monobehaviour : FixedUpdate.
        /// </summary>
        /// <param name="channel">The channel number.</param>
        /// <param name="reference">The reference for the device to track.</param>
        public override void UpdateState(int channel, float reference)
        {
            if (channel != 0)
                throw new System.ArgumentException("Only channel 0 available since 1DOF.");


        }

        /// <summary>
        /// Updates all the states of the device. Since it's 1DOF, only one channel available.
        /// Reference determines the desired joint displacement, only length 1 allowed.
        /// Should only be called during Physics updates, Monobehaviour : FixedUpdate.
        /// </summary>
        /// <param name="references">The set of references for the device to track.</param>
        public override void UpdateAllStates(float[] references)
        {
            if (references.Length != 1)
                throw new System.ArgumentException("Only 2 references (position and velocity) required since 1DOF.");

            UpdateState(1, references[0]);
        }


        /// <summary>
        /// PID controller
        /// </summary>
        /// <param></param>
        private float IncPID(float KP, float KI, float KD, float error, float errorP, float errprPP )
        {
            float output = 0.0f;

            output = KP * (error - errorP) + KI * error + KD * (error - 2 * errorP + errorPP);

            return output;
        }


        /// <summary>
        /// Customised sgn function, return -1 for negative, return 1 for positive, return 0 for zero
        /// </summary>
        /// <param></param>
        private float Sign(float number)
        {
            return number < 0 ? -1 : (number > 0 ? 1 : 0);
        }
    }
}