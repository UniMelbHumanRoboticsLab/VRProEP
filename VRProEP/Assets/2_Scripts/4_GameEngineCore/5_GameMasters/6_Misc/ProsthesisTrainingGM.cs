﻿using System.Collections;
using Valve.VR;
using UnityEngine;

// GameMaster includes
using VRProEP.ExperimentCore;
using VRProEP.GameEngineCore;
using VRProEP.ProsthesisCore;
using VRProEP.Utilities;

public class ProsthesisTrainingGM : GameMaster
{
    public AvatarType avatarType = AvatarType.Transhumeral;
    [Header("Debug EMG configuration:")]
    public bool emgEnable = false;
    public string ip = "192.168.137.2";
    public int port = 2390;
    public int channelSize = 1;


    [Header("Training mode configuration:")]
    public Camera TrainingCamera;
    public Transform fixedProsthesisPosition;
    public HUDManager trainingHudManager;

    [Header("Instructions:")]
    [TextArea]
    public string synergyInstructions;
    [TextArea]
    public string emgInstructions;

    private float taskTime = 0.0f;
    private string instructionsText;
    private GameObject residualLimbGO;
    private LimbFollower limbFollower;
    private AngleFollower angleFollower;
    private ConfigurableElbowManager elbowManager;

    // Debug
    // private GameObject handGO;
    // private GameObject elbowGO;

    // Start is called before the first frame update
    protected override void Start()
    {
        TrainingCamera.enabled = false;
        if (debug)
        {
            // Load player
            SaveSystem.LoadUserData("MD1942");
            AvatarSystem.LoadPlayer(SaveSystem.ActiveUser.type, avatarType);
        }
        else
        {
            InitialiseExperimentSystems();
            InitializeUI();
        }
    }

    // Update is called once per frame
    protected override void Update()
    {
        switch (experimentState)
        {
            /*
             *************************************************
             *  HelloWorld
             *************************************************
             */
            // Welcome subject to the virtual world.
            case ExperimentState.Welcome:
                if (debug)
                {
                    LoadDebugAvatar();
                    InitialiseExperimentSystems();
                    InitializeUI();
                }

                MonitorManager.DisplayText("Instructions:\n" + instructionsText);
                experimentState = ExperimentState.Initialising;
                break;
            /*
             *************************************************
             *  InitializingApplication
             *************************************************
             */
            // Perform initialization functions before starting experiment.
            case ExperimentState.Initialising:
                //
                // Perform experiment initialization procedures
                //

                //
                // Initialize data logs
                //

                //
                // Go to training
                //
                experimentState = ExperimentState.Training;
                break;
            /*
             *************************************************
             *  Practice
             *************************************************
             */
            // Perform initialization functions before starting experiment.
            case ExperimentState.Training:
                //
                // Guide subject through training
                //

                //
                // Go to instructions
                //
                experimentState = ExperimentState.Instructions;
                break;
            /*
             *************************************************
             *  GivingInstructions
             *************************************************
             */
            case ExperimentState.Instructions:
                //
                // Go to waiting for start
                //
                HudManager.DisplayText("Loading...", 2.0f);
                // Turn targets clear
                experimentState = ExperimentState.WaitingForStart;

                break;
            /*
             *************************************************
             *  WaitingForStart
             *************************************************
             */
            case ExperimentState.WaitingForStart:

                // Check if pause requested
                UpdatePause();
                switch (waitState)
                {
                    // Waiting for subject to get to start position.
                    case WaitState.Waiting:
                        SetWaitFlag(3.0f);
                        waitState = WaitState.Countdown;
                        break;
                    case WaitState.Countdown:
                        if (WaitFlag)
                        {
                            // Enable training camera
                            TrainingCamera.enabled = true;
                            experimentState = ExperimentState.PerformingTask;
                        }
                        break;
                    default:
                        break;
                }
                break;
            /*
             *************************************************
             *  PerformingTask
             *************************************************
             */
            case ExperimentState.PerformingTask:
                // Task performance is handled deterministically in FixedUpdate.
                break;
            /*
             *************************************************
             *  AnalizingResults
             *************************************************
             */
            case ExperimentState.AnalizingResults:
                // Allow 3 seconds after task end to do calculations
                SetWaitFlag(3.0f);

                //
                // Data analysis and calculations
                //

                //
                // System update
                //

                // 
                // Data logging
                //

                //
                // Flow managment
                //
                // Rest for some time when required
                if (IsRestTime())
                {
                    SetWaitFlag(RestTime);
                    experimentState = ExperimentState.Resting;
                }
                // Check whether the new session condition is met
                else if (IsEndOfSession())
                {
                    experimentState = ExperimentState.InitializingNext;
                }
                // Check whether the experiment end condition is met
                else if (IsEndOfExperiment())
                {
                    experimentState = ExperimentState.End;
                }
                else
                    experimentState = ExperimentState.UpdatingApplication;
                break;
            /*
             *************************************************
             *  UpdatingApplication
             *************************************************
             */
            case ExperimentState.UpdatingApplication:
                if (WaitFlag)
                {
                    //
                    // Update iterations and flow control
                    //

                    // 
                    // Update log requirements
                    //

                    //
                    //
                    // Go to start of next iteration
                    experimentState = ExperimentState.WaitingForStart;
                }
                break;
            /*
             *************************************************
             *  InitializingNext
             *************************************************
             */
            case ExperimentState.InitializingNext:
                //
                // Perform session closure procedures
                //

                //
                // Initialize new session variables and flow control
                //
                iterationNumber = 1;
                sessionNumber++;
                skipInstructions = true;

                //
                // Initialize data logging
                //
                //ExperimentSystem.GetActiveLogger(1).AddNewLogFile(sessionNumber, iterationNumber, "Data format");

                experimentState = ExperimentState.Initialising; // Initialize next session
                break;
            /*
             *************************************************
             *  Resting
             *************************************************
             */
            case ExperimentState.Resting:
                //
                // Check for session change or end request from experimenter
                //
                if (UpdateNext())
                {
                    ConfigureNextSession();
                    break;
                }
                else if (UpdateEnd())
                {
                    EndExperiment();
                    break;
                }
                //
                // Restart after flag is set by wait coroutine
                //
                if (WaitFlag)
                {
                    HudManager.DisplayText("Get ready to restart!", 3.0f);
                    SetWaitFlag(5.0f);
                    experimentState = ExperimentState.UpdatingApplication;
                    break;
                }
                break;
            /*
             *************************************************
             *  Paused
             *************************************************
             */
            case ExperimentState.Paused:
                //
                // Check for session change or end request from experimenter
                //
                UpdatePause();
                if (UpdateNext())
                {
                    ConfigureNextSession();
                    break;
                }
                else if (UpdateEnd())
                {
                    EndExperiment();
                    break;
                }
                break;
            /*
             *************************************************
             *  End
             *************************************************
             */
            case ExperimentState.End:
                //
                // Update log data and close logs.
                //
                EndExperiment();
                break;

            //
            // Return to main menu
            //
            default:
                break;
        }

        //
        // Update information displayed on monitor
        //

        //
        // Update information displayed for debugging purposes
        //

        //
        // Update HUD state
        //
        if (elbowManager.IsEnabled)
            trainingHudManager.colour = HUDManager.HUDColour.Blue;
        else
            trainingHudManager.colour = HUDManager.HUDColour.Red;
    }

    private void FixedUpdate()
    {
        //
        // Tasks performed determinalistically throughout the experiment
        // E.g. data gathering.
        //
        switch (experimentState)
        {
            case ExperimentState.PerformingTask:
                //
                // Gather data while experiment is in progress
                //
                string displayData = "Training time elapsed: " + taskTime.ToString() + " seconds.";
                // Read from all user sensors
                foreach (ISensor sensor in AvatarSystem.GetActiveSensors())
                {
                    if (sensor.GetSensorType().Equals(SensorType.EMGWiFi))
                    {
                        displayData += "\n" + sensor.GetSensorType().ToString() + " Command:";
                        float[] sensorData = sensor.GetAllProcessedData();
                        foreach (float element in sensorData)
                        displayData += "\n" + element.ToString();
                    }
                    else if (sensor.GetSensorType().Equals(SensorType.VirtualEncoder))
                    {
                        displayData += "\n" + sensor.GetSensorType().ToString() + " Command:";
                        float[] sensorData = sensor.GetAllProcessedData();
                        foreach (float element in sensorData)
                            displayData += "\n" + element.ToString();
                    }
                }
                

                InstructionManager.DisplayText(displayData);
                //hudManager.DisplayText(logData);

                //
                // Append data to lists
                //
                taskTime += Time.fixedDeltaTime;

                //
                // Raycast debug
                //
                // Vector3 shoulderToElbow = elbowGO.transform.position - residualLimbGO.transform.position;
                // Vector3 shoulderToHand = handGO.transform.position - (residualLimbGO.transform.position - 0.25f * shoulderToElbow);
                // Debug.DrawRay(residualLimbGO.transform.position - 0.2f * shoulderToElbow, shoulderToHand, Color.magenta);

                //
                // Save log and reset flags when successfully compeleted task
                //
                if (IsTaskDone())
                {
                    //
                    // Perform data management, such as appending data to lists for analysis
                    //

                    //
                    // Save logger for current experiment and change to data analysis
                    //
                    //ExperimentSystem.GetActiveLogger(1).CloseLog();

                    //
                    // Clear data management buffers
                    //
                    experimentState = ExperimentState.AnalizingResults;
                    break;
                }

                // Check if requested to end training
                UpdateEnd();

                break;
            default:
                break;
        }
    }

    private void OnApplicationQuit()
    {
        //
        // Handle application quit procedures.
        //
        // Check if UDP sensors are available
        foreach (ISensor sensor in AvatarSystem.GetActiveSensors())
        {
            if (sensor.GetSensorType().Equals(SensorType.EMGWiFi))
            {
                UDPSensorManager udpSensor = (UDPSensorManager)sensor;
                udpSensor.StopSensorReading();
            }
        }

        //
        // Save and close all logs
        //
        ExperimentSystem.CloseAllExperimentLoggers();
    }

    private void LoadDebugAvatar()
    {
        // Load avatar
        if (avatarType == AvatarType.Transhumeral)
        {
            AvatarSystem.LoadAvatar(SaveSystem.ActiveUser, AvatarType.Transhumeral);

            // Find the residual limb and change the follower
            GameObject residualLimbGO = GameObject.FindGameObjectWithTag("ResidualLimbAvatar");
            LimbFollower limbFollower = residualLimbGO.GetComponent<LimbFollower>();
            Destroy(limbFollower);
            AngleFollower angleFollower = residualLimbGO.AddComponent<AngleFollower>();
            angleFollower.fixedTransform = fixedProsthesisPosition;


            // Initialize prosthesis
            GameObject prosthesisManagerGO = GameObject.FindGameObjectWithTag("ProsthesisManager");
            ConfigurableElbowManager elbowManager = prosthesisManagerGO.AddComponent<ConfigurableElbowManager>();
            elbowManager.InitializeProsthesis(SaveSystem.ActiveUser.upperArmLength, (SaveSystem.ActiveUser.forearmLength + SaveSystem.ActiveUser.handLength / 2.0f));
            // Set the reference generator to jacobian-based.
            elbowManager.ChangeReferenceGenerator("VAL_REFGEN_JACOBIANSYN");
            instructionsText = synergyInstructions;

            // Enable & configure EMG
            if (emgEnable)
            {
                // Create and add sensor
                //EMGWiFiManager emgSensor = new EMGWiFiManager(ip, port, channelSize);
                ThalmicMyobandManager emgSensor = new ThalmicMyobandManager();
                //emgSensor.ConfigureLimits(0, 1023, 0);
                //emgSensor.ConfigureLimits(1, 1023, 0);
                AvatarSystem.AddActiveSensor(emgSensor);
                elbowManager.AddSensor(emgSensor);
                //emgSensor.StartSensorReading();

                // Set active sensor and reference generator to EMG.
                elbowManager.ChangeSensor("VAL_SENSOR_SEMG");
                elbowManager.ChangeReferenceGenerator("VAL_REFGEN_EMGPROP");
                instructionsText = emgInstructions;
            }
        }
        else
        {
            throw new System.NotImplementedException();
        }
    }


    private void KeepOnLoad()
    {
        // Keep player and avatar objects
        GameObject playerGO = GameObject.FindGameObjectWithTag("Player");
        GameObject avatarGO = GameObject.FindGameObjectWithTag("Avatar");
        if (playerGO == null || avatarGO == null)
        {
            MonitorManager.DisplayText("The user or avatar has not been loaded.");
            throw new System.Exception("The player or avatar has not been loaded.");
        }
        DontDestroyOnLoad(playerGO);
        DontDestroyOnLoad(avatarGO);
    }

    #region Inherited methods overrides

    public override void HandleResultAnalysis()
    {
        throw new System.NotImplementedException();
    }
    public override void HandleInTaskBehaviour()
    {
        throw new System.NotImplementedException();
    }
    public override void HandleTaskCompletion()
    {
        throw new System.NotImplementedException();
    }
    public override void PrepareForStart()
    {
        throw new System.NotImplementedException();
    }
    public override void StartFailureReset()
    {
        throw new System.NotImplementedException();
    }
    public override void InitialiseExperiment()
    {
        throw new System.NotImplementedException();
    }

    /// <summary>
    /// Gets the progress text to be displayed to the subject.
    /// </summary>
    /// <returns>The text to be displayed as a string.</returns>
    public override string GetDisplayInfoText()
    {
        string Text;
        Text = "Status: " + experimentState.ToString() + ".\n";
        Text += "Time: " + System.DateTime.Now.ToString("H:mm tt") + ".\n";
        return Text;
    }

    /// <summary>
    /// Initializes the ExperimentSystem and its components.
    /// Verifies that all components needed for the experiment are available.
    /// </summary>
    public override void InitialiseExperimentSystems()
    {
        //
        // Set the experiment type and ID
        //
        if (AvatarSystem.AvatarType == AvatarType.AbleBodied)
        {
            experimentType = ExperimentType.TypeOne;
            MonitorManager.DisplayText("Wrong avatar used. Please use a Transhumeral avatar.");
            throw new System.Exception("Able-bodied avatar not suitable for prosthesis training.");
        }
        else if (AvatarSystem.AvatarType == AvatarType.Transhumeral)
        {
            // Check if EMG is available
            bool EMGAvailable = false;
            foreach (ISensor sensor in AvatarSystem.GetActiveSensors())
            {
                if (sensor.GetSensorType().Equals(SensorType.EMGWiFi))
                {
                    EMGAvailable = true;
                    UDPSensorManager udpSensor = (UDPSensorManager)sensor;
                    udpSensor.StartSensorReading();
                }
                else if(sensor.GetSensorType().Equals(SensorType.ThalmicMyo))
                    EMGAvailable = true;
            }
            // Set whether emg or synergy based
            if (EMGAvailable)
            {
                experimentType = ExperimentType.TypeTwo;
                instructionsText = emgInstructions;
                // Set EMG sensor and reference generator as active.
                // Get prosthesis
                GameObject prosthesisManagerGO = GameObject.FindGameObjectWithTag("ProsthesisManager");
                elbowManager = prosthesisManagerGO.GetComponent<ConfigurableElbowManager>();
                // Set active sensor and reference generator to EMG.
                //elbowManager.ChangeSensor("VAL_SENSOR_SEMG");
                elbowManager.ChangeSensor("VAL_SENSOR_THALMYO");
                elbowManager.ChangeReferenceGenerator("VAL_REFGEN_EMGPROP");
            }
            else
            {
                experimentType = ExperimentType.TypeThree;
                instructionsText = synergyInstructions;
                // Set VIVE tracker and Jacobian synergy as active.
                // Get prosthesis
                GameObject prosthesisManagerGO = GameObject.FindGameObjectWithTag("ProsthesisManager");
                elbowManager = prosthesisManagerGO.GetComponent<ConfigurableElbowManager>();
                if (elbowManager.GetInterfaceType() == ReferenceGeneratorType.JacobianSynergy)
                {
                    // Set the reference generator to jacobian-based.
                    elbowManager.ChangeSensor("VAL_SENSOR_VIVETRACKER");
                    elbowManager.ChangeReferenceGenerator("VAL_REFGEN_JACOBIANSYN");
                }
                else if (elbowManager.GetInterfaceType() == ReferenceGeneratorType.LinearKinematicSynergy)
                {
                    // Set the reference generator to linear synergy.
                    elbowManager.ChangeSensor("VAL_SENSOR_VIVETRACKER");
                    elbowManager.ChangeReferenceGenerator("VAL_REFGEN_LINKINSYN");
                }
                else
                    throw new System.Exception("The prosthesis interface available is not supported.");
            }
            
            if(!debug)
            {
                // Find the residual limb and change the follower
                residualLimbGO = GameObject.FindGameObjectWithTag("ResidualLimbAvatar");
                limbFollower = residualLimbGO.GetComponent<LimbFollower>();
                limbFollower.enabled = false;
                angleFollower = residualLimbGO.AddComponent<AngleFollower>();
                angleFollower.fixedTransform = fixedProsthesisPosition;
            }
        }
        else
            throw new System.NotImplementedException();

        // Get Raycast Debug info
        // handGO = GameObject.FindGameObjectWithTag("Hand");
        // elbowGO = GameObject.FindGameObjectWithTag("Elbow_Upper");
        // residualLimbGO = GameObject.FindGameObjectWithTag("ResidualLimbAvatar");
    }


    /// <summary>
    /// Checks whether the subject is ready to start performing the task.
    /// </summary>
    /// <returns>True if ready to start.</returns>
    public override bool IsReadyToStart()
    {
        throw new System.NotImplementedException();
    }

    /// <summary>
    /// Checks whether the task has be successfully completed or not.
    /// </summary>
    /// <returns>True if the task has been successfully completed.</returns>
    public override bool IsTaskDone()
    {
        //
        // Perform some condition testing
        //
        return false;
    }

    /// <summary>
    /// Checks if the condition for the rest period has been reached.
    /// </summary>
    /// <returns>True if the rest condition has been reached.</returns>
    public override bool IsRestTime()
    {
        throw new System.NotImplementedException();
    }

    /// <summary>
    /// Checks if the condition for changing experiment session has been reached.
    /// </summary>
    /// <returns>True if the condition for changing sessions has been reached.</returns>
    public override bool IsEndOfSession()
    {
        throw new System.NotImplementedException();
    }

    /// <summary>
    /// Checks if the condition for ending the experiment has been reached.
    /// </summary>
    /// <returns>True if the condition for ending the experiment has been reached.</returns>
    public override bool IsEndOfExperiment()
    {
        throw new System.NotImplementedException();
    }

    /// <summary>
    /// Launches the next session. Performs all the required preparations.
    /// </summary>
    public void ConfigureNextSession()
    {
        throw new System.NotImplementedException();
    }

    /// <summary>
    /// Finishes the experiment. Performs all the required procedures.
    /// </summary>
    public override void EndExperiment()
    {
        if (!debug)
        {
            // Find the residual limb and change the follower
            Destroy(angleFollower);
            limbFollower.enabled = true;

            KeepOnLoad();
            // Load experiment.
            SteamVR_LoadLevel.Begin("JacobianSynergyExperiment");
        }
    }

    public override IEnumerator WelcomeLoop()
    {
        throw new System.NotImplementedException();
    }

    public override IEnumerator InstructionsLoop()
    {
        throw new System.NotImplementedException();
    }

    public override IEnumerator TrainingLoop()
    {
        throw new System.NotImplementedException();
    }

    #endregion
}
