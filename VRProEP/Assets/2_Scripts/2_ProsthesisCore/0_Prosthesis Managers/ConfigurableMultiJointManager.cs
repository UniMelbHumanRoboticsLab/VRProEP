//======= Copyright (c) Melbourne Robotics Lab, All rights reserved. ===============
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRProEP.GameEngineCore;



namespace VRProEP.ProsthesisCore
{
    public class ConfigurableMultiJointManager : MonoBehaviour
    {
        private ConfigurableInputManager inputManager;
        //private ElbowManager elbowManager;

        private IdealJointManager elbowManager;
        private IdealJointManager wristPronManager;
        private IdealJointManager wristFlexManager;


        public float[] ElbowState { get; set; }
        public float[] WristPronState { get; set; }
        public float[] WristFlexState { get; set; }

        public float ElbowRef { get; set; }
        public float WristPronRef { get; set; }
        public float WristFlexRef { get; set; }

        private bool isConfigured = false;
        private bool isEnabled = false;

        public bool IsEnabled { get => isEnabled; }

        private float[] xBar = { Mathf.Deg2Rad * -90.0f, Mathf.Deg2Rad * 0.0f, Mathf.Deg2Rad * 0.0f };
        private float[] xMin = { Mathf.Deg2Rad * -145.0f, Mathf.Deg2Rad * -90.0f, Mathf.Deg2Rad * 0.0f };
        private float[] xMax = { Mathf.Deg2Rad * -0.1f, Mathf.Deg2Rad * 90.0f, Mathf.Deg2Rad * 0.0f };


        public const float MAX_EFE_VEL = 200.0f;
        public const float MAX_WPS_VEL = 200.0f;
        public const float MAX_WFE_VEL = 200.0f;

        /// <summary>
        /// Initializes the multi joint prosthesis with basic functionality.
        /// Must be called only after the avatar is available.
        /// </summary>
        public void InitializeProsthesis(float upperArmLength, float lowerArmLength, float synValue = 1.0f)
        {
            //
            // ConfigurableInputManagar
            //
            // Find ResdiualLimbTracker GameObject and extract its Transform.


            GameObject residualLimbTrackerGO = GameObject.FindGameObjectWithTag("ResidualLimbTracker");

            //residualLimbTrackerGO.transform.localScale = new Vector3(2.0f, 2.0f, 2.0f);
            //Debug.Log("Residual tracker number: " + residualLimbTrackerGO.Length);

            // Create a VIVETracker with the obtained transform
            VIVETrackerManager trackerManager = new VIVETrackerManager(residualLimbTrackerGO.transform);
            //Debug.Log("Residual tracker number: " + trackerManager.TrackerNumber);

            // Create a basic reference generator: Integrator.
            IntegratorReferenceGenerator integratorRG = new IntegratorReferenceGenerator(xBar, xMin, xMax);
            // Create configurable input manager with the created sensor and RG.
            inputManager = new ConfigurableInputManager(trackerManager, integratorRG);


            //
            // ElbowManager
            //
            // Find Elbow_Lower GameObject and extract its HingeJoint and Rigidbody
            GameObject elbowLowerGO = GameObject.FindGameObjectWithTag("Elbow_Lower");
            if (elbowLowerGO == null)
                throw new System.Exception("Could not find and active elbow prosthesis avatar (GameObject).");
            HingeJoint elbowJoint = elbowLowerGO.GetComponent<HingeJoint>();
            Rigidbody elbowRB = elbowLowerGO.GetComponent<Rigidbody>();
            // Create VirtualEncoder and attach to HingeJoint.
            VirtualEncoderManager virtualElbowEncoder = new VirtualEncoderManager(elbowJoint);
            // Ideal tracking version
            // Create ElbowManager with the given elbowJoint.
            //elbowManager = new IdealJointManager(elbowJoint);
            elbowManager = new IdealJointManager(elbowJoint, 3000.0f, 120.0f);
            elbowManager.MaxAngVel = MAX_EFE_VEL;


            //
            // WristPronatorManager
            //
            // Find WristPronator GameObject and extract its HingeJoint and Rigidbody
            GameObject wristPronGO = GameObject.FindGameObjectWithTag("WristPronator");
            if (wristPronGO == null)
                throw new System.Exception("Could not find and active elbow prosthesis avatar (GameObject).");
            HingeJoint wristPronJoint = wristPronGO.GetComponent<HingeJoint>();
            Rigidbody wristPronRB = wristPronGO.GetComponent<Rigidbody>();
            // Create VirtualEncoder and attach to HingeJoint.
            VirtualEncoderManager virtualWristPronEncoder = new VirtualEncoderManager(wristPronJoint);
            // Ideal tracking version
            // Create ElbowManager with the given elbowJoint.
            //wristPronManager = new IdealJointManager(wristPronJoint);
            wristPronManager = new IdealJointManager(wristPronJoint,2000.0f,80.0f);
            wristPronManager.MaxAngVel = MAX_WPS_VEL;

            //
            // WristFlexorManager
            //
            // Find WristFlex GameObject and extract its HingeJoint and Rigidbody
            GameObject wristFlexGO = GameObject.FindGameObjectWithTag("WristFlexer");
            if (wristFlexGO == null)
                throw new System.Exception("Could not find and active elbow prosthesis avatar (GameObject).");
            HingeJoint wristFlexJoint = wristFlexGO.GetComponent<HingeJoint>();
            Rigidbody wristFlexRB = wristFlexGO.GetComponent<Rigidbody>();
            // Create VirtualEncoder and attach to HingeJoint.
            VirtualEncoderManager virtualWristFlexEncoder = new VirtualEncoderManager(wristFlexJoint);
            // Ideal tracking version
            // Create ElbowManager with the given elbowJoint.
            wristFlexManager = new IdealJointManager(wristFlexJoint, 2000.0f, 80.0f);
            wristFlexManager.MaxAngVel = MAX_WFE_VEL;
           
            //
            // Sensors
            //

            // Add VIVE controller as sensor to enable manual inputs.
            VIVEControllerManager controllerManager = new VIVEControllerManager();
            inputManager.Configure("CMD_ADD_SENSOR", controllerManager);

            // Add joint encoder as sensor for jacobian synergy
            inputManager.Configure("CMD_ADD_SENSOR", virtualElbowEncoder);

            // Add the created sensors to the list of available sensors.
            AvatarSystem.AddActiveSensor(trackerManager);
            AvatarSystem.AddActiveSensor(virtualElbowEncoder);
            //AvatarSystem.AddActiveSensor(controllerManager);


            //
            // Reference generators
            //
            // Add a Linear Kinematic Synergy to the prosthesis
            float[] theta = { -synValue, synValue, -synValue };
            float[] thetaMin = { -3.5f, -3.5f, -3.5f };
            float[] thetaMax = { -0.1f, -0.1f, -0.1f };
            LinearKinematicSynergy linSyn = new LinearKinematicSynergy(xBar, xMin, xMax, theta, thetaMin, thetaMax);
            inputManager.Configure("CMD_ADD_REFGEN", linSyn);

            // Add a Machine Learning based Kinematic Synergy to the prosthesis, machine learaning run at other plaform such as Matlab or Python and stream results to Unity through ZMQ
            MLKinematicSynergy mlSyn = new MLKinematicSynergy(xBar, xMin, xMax, theta, thetaMin, thetaMax);
            inputManager.Configure("CMD_ADD_REFGEN", mlSyn);

            /*
            // Add a Jacobian based Kinematic Synergy
            JacobianSynergy jacSyn = new JacobianSynergy(xBar, xMin, xMax, upperArmLength, lowerArmLength);
            inputManager.Configure("CMD_ADD_REFGEN", jacSyn);

            // Add an EMG reference generator
            List<float> emgGains = new List<float>(1);
            // emgGains.Add(1.3f); // single site
            emgGains.Add(0.015f);
            EMGInterfaceReferenceGenerator emgRG = new EMGInterfaceReferenceGenerator(xBar, xMin, xMax, emgGains, EMGInterfaceType.dualSiteProportional);
            inputManager.Configure("CMD_ADD_REFGEN", emgRG);
            */

            // Enable
            isConfigured = true;
        }


        // Update the prosthesis state deterministically
        public void FixedUpdate()
        {
            if (isConfigured)
            {
                //Get the readings
                ElbowState = elbowManager.GetJointStates();
                //Debug.Log("Elbow at: " + ElbowState[0] * Mathf.Rad2Deg);
                WristPronState = wristPronManager.GetJointStates();
                WristFlexState = wristFlexManager.GetJointStates();

                // Generate new reference
                ElbowRef = inputManager.GenerateReference(0);
                WristPronRef = inputManager.GenerateReference(1);
                WristFlexRef = inputManager.GenerateReference(2);

                // Update device state
                elbowManager.UpdateState(0, ElbowRef);
                wristPronManager.UpdateState(0, WristPronRef);
                wristFlexManager.UpdateState(0, WristFlexRef);

                isEnabled = inputManager.IsEnabled();
            }
        }

        /// <summary>
        /// Changes the active sensor for reference generation.
        /// Available sensors:
        /// - "VAL_SENSOR_VIVETRACKER";
        /// - "VAL_SENSOR_VIVECONTROLLER";
        /// - "VAL_SENSOR_VIRTUALENCODER";
        /// </summary>
        /// <param name="sensorName"></param>
        public void ChangeSensor(string sensorName)
        {
            inputManager.Configure("CMD_SET_ACTIVE_SENSOR", sensorName);
        }

        /// <summary>
        /// Changes the active reference generator.
        /// Available reference generatprs:
        /// - Linear kinematic synergy: "VAL_REFGEN_LINKINSYN";
        /// - Jacobian-based synergy: "VAL_REFGEN_JACOBIANSYN";
        /// - Integrator: "VAL_REFGEN_INTEGRATOR";
        /// - Gradient-to-point: "VAL_REFGEN_POINTGRAD";
        /// </summary>
        /// <param name="rgName"></param>
        public void ChangeReferenceGenerator(string rgName)
        {
            inputManager.Configure("CMD_SET_ACTIVE_REFGEN", rgName);
        }

        /// <summary>
        /// Adds the given sensor to the elbow.
        /// </summary>
        /// <param name="sensors">The sensor.</param>
        public void AddSensor(ISensor sensor)
        {
            inputManager.Configure("CMD_ADD_SENSOR", sensor);
        }

        /// <summary>
        /// Adds the given reference generator to the elbow.
        /// </summary>
        /// <param name="refGens">The reference generator.</param>
        public void AddRefGen(IReferenceGenerator refGen)
        {
            inputManager.Configure("CMD_ADD_REFGEN", refGen);
        }

        public ReferenceGeneratorType GetInterfaceType()
        {
            return inputManager.GetActiveReferenceGeneratorType();
        }

        /// <summary>
        /// Sets the synergy value for a synergistic elbow.
        /// </summary>
        /// <param name="theta">The synergy value.</param>
        public void SetSynergy(float theta)
        {
            inputManager.Configure("CMD_SET_SYNERGY", -theta);
        }

        /// <summary>
        /// Returns the current elbow joint angle.
        /// </summary>
        /// <returns></returns>
        public float[] GetElbowAngle()
        {
            return ElbowState;
        }

        /// <summary>
        /// Sets the elbow to a given value.
        /// </summary>
        /// <param name="elbowAngle">The desired elbow angle in radians.</param>
        public void SetElbowAngle(float elbowAngle)
        {
            if (elbowAngle > xMax[0] || elbowAngle < xMin[0])
                throw new System.ArgumentOutOfRangeException("The provided elbow angle is out of the allowed range.");

            inputManager.Configure("CMD_SET_REFERENCE", elbowAngle);
            ElbowState[0] = elbowAngle;
        }
    }
}
