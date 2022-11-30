//======= Copyright (c) Melbourne Robotics Lab, All rights reserved. ===============
using UnityEngine;

namespace VRProEP.ProsthesisCore
{
    public class IdealJointManager : BasicDeviceManager
    {
        // sensor and controllers unnecessary for this elbow device.
        private HingeJoint joint;
        private JointSpring jointSpring;
        private JointMotor jointMotor;

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
        public IdealJointManager(HingeJoint elbowJoint)
        {
            if (elbowJoint == null)
                throw new System.ArgumentNullException("The provided HingeJoint object is empty.");

            this.joint = elbowJoint;

            // Configure spring
            /*
            jointSpring = this.joint.spring;
            jointSpring.spring = 2000.0f;
            jointSpring.damper = 60.0f;
            this.joint.useSpring = true;
            this.joint.spring = jointSpring;
            */


            
            //Configure motor
            jointMotor = this.joint.motor;
            jointMotor.freeSpin = false;
            jointMotor.force = 1000;
            jointMotor.targetVelocity = MaxAngVel;
            this.joint.useMotor = true;
            this.joint.motor = jointMotor;

            this.KP = 100.0f;
            this.KI = 60.0f;
            this.KD = 0.0f;
            
        }

        /// <summary>
        /// Manager for a virtual elbow prosthetic device with ideal tracking
        /// Spring and damper customisabel
        /// </summary>
        /// <param name="sensor">The Unity HingeJoint for the elbow.</param>
        public IdealJointManager(HingeJoint elbowJoint, float spring, float damper)
        {
            if (elbowJoint == null)
                throw new System.ArgumentNullException("The provided HingeJoint object is empty.");

            this.joint = elbowJoint;

            // Configure spring
            jointSpring = this.joint.spring;
            jointSpring.spring = spring;
            jointSpring.damper = damper;
            this.joint.useSpring = true;
            this.joint.spring = jointSpring;
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
            x[0] = joint.angle * Mathf.Deg2Rad;
            x[1] = joint.velocity * Mathf.Deg2Rad;
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
            return joint.angle* Mathf.Deg2Rad;
        }

        /// <summary>
        /// Returns all pre-processed joint states in an array.
        /// 0: Angular displacement given in radians.
        /// 1: Angular velocity given in radians per second.
        /// </summary>
        /// <returns>The array with pre-processed angular position and velocity data.</returns>
        public float GetJointAngVel()
        {
            return joint.velocity * Mathf.Deg2Rad;
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


            /*
            #region Simple stop and go
            float error = reference - this.GetJointAngle();

            if ((this.Sign(error) * this.Sign(errorPrev)) < 0)
            {
                jointMotor.targetVelocity = 0.0f;
            }
            else
            {
                jointMotor.targetVelocity = MaxAngVel * this.Sign(error);
            }
            joint.motor = jointMotor;

            //Debug.Log("Reference: " + reference + " Current State: " + this.GetJointAngle() + " " + MaxAngVel * this.Sign(error));

            errorPrev = error;
            #endregion
            */



            #region PID velocity control
            float error = Mathf.Rad2Deg * (reference - this.GetJointAngle());
            jointMotor.targetVelocity = jointMotor.targetVelocity + IncPID(KP,KI,KD,error,errorP,errorPP);


            if (jointMotor.targetVelocity > MaxAngVel)
            {
                jointMotor.targetVelocity = MaxAngVel;
            }
            else if (jointMotor.targetVelocity < -MaxAngVel)
            {
                jointMotor.targetVelocity = -MaxAngVel;
            }
               

            joint.motor = jointMotor;
            Debug.Log("Reference: " + Mathf.Rad2Deg * reference + " Current State: " + Mathf.Rad2Deg * this.GetJointAngle() + "Controller Output: " + jointMotor.targetVelocity + "Saturation Speed: " + MaxAngVel);

            errorPP = errorP;
            errorP = error;

            #endregion



            /*
            float currentPos = this.GetJointAngle();
            float currentVel = this.GetJointAngVel();
            float relativeVel = Mathf.Abs(reference - currentPos) * Mathf.Rad2Deg / Time.fixedDeltaTime;

            Debug.Log("Reference: " + reference + " Current State: " + currentPos + ", required speed: " + relativeVel);
            // Constrain the maximum velocity
            if (Mathf.Abs(currentVel) * Mathf.Rad2Deg > this.MaxAngVel)
            {
                float validRef = currentPos + Mathf.Sign(reference - currentPos) * MaxAngVel * Time.fixedDeltaTime * Mathf.Deg2Rad;
                //Debug.Log("Adjusted reference: " + validRef *Mathf.Rad2Deg + " dt: " + Time.fixedDeltaTime);
                jointSpring.targetPosition = (float)System.Math.Round(Mathf.Rad2Deg * validRef, 1);
            }
            else
            {
                jointSpring.targetPosition = (float)System.Math.Round(Mathf.Rad2Deg * reference, 1);
                //Debug.Log("Original reference: " + reference * Mathf.Rad2Deg + " dt: " + Time.fixedDeltaTime);
            }
            joint.spring = jointSpring;
            */

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