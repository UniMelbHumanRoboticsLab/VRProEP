﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

// GameMaster includes
using VRProEP.ExperimentCore;
using VRProEP.GameEngineCore;
using VRProEP.ProsthesisCore;

public class EMGDataGM : GameMaster
{
    [Header("Experiment configuration:")]
    public int iterationsPerAngle = 20;
    public List<int> startAngleList = new List<int>();
    public List<int> endAngleList = new List<int>();
    public List<float> movementTimeList = new List<float>();
    public ArmGuideManager guideManager;

    private int angleNumber = 1;
    private int timeNumber = 1;
    private int timeIterations = 1;
    private int totalIterations = 1;
    private int timeIterationLimit;
    private int totalIterationLimit;

    // Data logging:
    private DataStreamLogger motionLogger;
    private const string motionDataFormat = "ref,t,emg1,emg2,emg1raw,emg2raw,aDotS,bDotS,gDotS,aS,bS,gS,aDotSH,bDotSH,gDotSH,aSH,bSH,gSH";
    private float taskTime = 0.0f;
    
    // Start is called before the first frame update
    void Start()
    {
        if (debug)
        {
            SaveSystem.LoadUserData("RG1988");
            AvatarSystem.LoadPlayer(UserType.AbleBodied, AvatarType.AbleBodied);
            AvatarSystem.LoadAvatar(SaveSystem.ActiveUser, AvatarType.AbleBodied);
        }
        // Initialize ExperimentSystem
        InitExperimentSystem();
        
        // Initialize UI.
        InitializeUI();

        // Initialize iteration management.
        timeIterationLimit = iterationsPerAngle * startAngleList.Count;
        totalIterationLimit = iterationsPerAngle * startAngleList.Count * movementTimeList.Count;

        //
        SetWaitFlag(5.0f);
    }

    // Update is called once per frame
    void Update()
    {
        switch (experimentState)
        {
            /*
             *************************************************
             *  HelloWorld
             *************************************************
             */
            // Welcome subject to the virtual world.
            case ExperimentState.HelloWorld:
                if (WaitFlag)
                {
                    hudManager.ClearText();
                    experimentState = ExperimentState.InitializingApplication;
                }
                else
                {
                    hudManager.DisplayText("Welcome!");
                }
                break;
            /*
             *************************************************
             *  InitializingApplication
             *************************************************
             */
            // Perform initialization functions before starting experiment.
            case ExperimentState.InitializingApplication:
                //
                // Perform experiment initialization procedures
                //
                // Enable colliders
                AvatarSystem.EnableAvatarColliders();
                // Initialize arm guide
                guideManager.Initialize(startAngleList[angleNumber - 1], endAngleList[angleNumber - 1], movementTimeList[timeNumber - 1]);
                guideManager.GoToStart();

                //
                // Initialize data logs
                //
                motionLogger.AddNewLogFile(startAngleList[angleNumber - 1] + "_" + endAngleList[angleNumber - 1] + "_" + movementTimeList[timeNumber - 1], iterationNumber, motionDataFormat);

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
                experimentState = ExperimentState.GivingInstructions;
                break;
            /*
             *************************************************
             *  GivingInstructions
             *************************************************
             */
            case ExperimentState.GivingInstructions:
                hudManager.DisplayText(movementTimeList[timeNumber - 1] + " sec. movement.", 2.0f);
                // Skip instructions when repeating sessions
                if (skipInstructions)
                {
                    //hudManager.DisplayText("Move to guide", 2.0f);
                    experimentState = ExperimentState.WaitingForStart;
                    break;
                }

                //
                // Give instructions
                //

                //
                // Go to waiting for start
                //
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
                    // Waiting for subject to grab the object.
                    case WaitState.Waiting:
                        if (guideManager.StartGuiding())
                        {
                            //StopAllCoroutines();
                            hudManager.ClearText();
                            taskTime = 0.0f;
                            HUDCountDown(3);
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
                hudManager.DisplayText("Rest your arm.", 5.0f);
                // Allow 3 seconds after task end to do calculations
                SetWaitFlag(5.0f);

                //
                // Data analysis and calculations
                //

                //
                // Adaptive system update (when available)
                //

                // 
                // Data logging and log management
                //
                motionLogger.CloseLog();

                //
                // Flow managment
                //

                // Rest for some time when required
                if (CheckRestCondition())
                {
                    hudManager.ClearText();
                    hudManager.DisplayText("Rest your arm.", 2.0f);
                    SetWaitFlag(restTime);
                    experimentState = ExperimentState.Resting;
                    break;
                }
                else if (CheckEndCondition())
                {
                    experimentState = ExperimentState.End;
                    break;
                }
                // Check whether the new session condition is met
                else if (CheckNextSessionCondition())
                {
                    //
                    // Update iterations and flow control
                    //
                    iterationNumber++;
                    timeIterations++;
                    totalIterations++;
                    // Go to next
                    experimentState = ExperimentState.InitializingNextSession;
                    break;
                }
                else
                {
                    experimentState = ExperimentState.UpdatingApplication;
                }
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
                    iterationNumber++;
                    timeIterations++;
                    totalIterations++;

                    //
                    // Update experiment object
                    //
                    guideManager.GoToStart();

                    // 
                    // Update log requirements
                    //
                    motionLogger.AddNewLogFile(startAngleList[angleNumber - 1] + "_" + endAngleList[angleNumber - 1] + "_" + movementTimeList[timeNumber - 1], iterationNumber, motionDataFormat);

                    //
                    // Go to start of next iteration
                    //
                    hudManager.DisplayText("Move to guide", 2.0f);
                    experimentState = ExperimentState.WaitingForStart;
                }
                break;
            /*
             *************************************************
             *  InitializingNext
             *************************************************
             */
            case ExperimentState.InitializingNextSession:
                if (WaitFlag)
                {
                    //
                    // Perform session closure procedures
                    //

                    //
                    // Initialize new session variables and flow control
                    //
                    iterationNumber = 1;
                    sessionNumber++;
                    // Still doing the angle repetitions for the same time
                    if (timeIterations < timeIterationLimit)
                    {
                        angleNumber++;
                    }
                    // Done all the angle repetitions for the given time, reset and go to next time
                    else
                    {
                        angleNumber = 1;
                        timeIterations = 1;
                        timeNumber++;
                    }

                    //
                    // Update experiment object
                    //
                    guideManager.GoToStart();
                    //
                    // Initialize data logging
                    //

                    experimentState = ExperimentState.InitializingApplication; // Initialize next session
                }
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
                    LaunchNextSession();
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
                    if (CheckEndCondition())
                    {
                        experimentState = ExperimentState.End;
                        break;
                    }
                    // Check whether the new session condition is met
                    else if (CheckNextSessionCondition())
                    {
                        //
                        // Update iterations and flow control
                        //
                        iterationNumber++;
                        timeIterations++;
                        totalIterations++;
                        // Go to next
                        experimentState = ExperimentState.InitializingNextSession;
                        break;
                    }
                    else
                    {
                        hudManager.DisplayText("Get ready!", 3.0f);
                        SetWaitFlag(3.0f);
                        experimentState = ExperimentState.UpdatingApplication;
                    }
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
                    LaunchNextSession();
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

                hudManager.DisplayText("Experiment end, thanks!", 5.0f);
                //
                // Return to main menu
                //
                break;
            default:
                break;
        }

        //
        // Update information displayed on monitor
        //
        string experimentInfoText = "Experiment info: \n";
        experimentInfoText += "Iteration: " + iterationNumber + "/" + iterationsPerAngle + ".\n";
        experimentInfoText += "Session: " + sessionNumber + "/" + startAngleList.Count * movementTimeList.Count + ".\n";
        int j = 0;
        instructionManager.DisplayText(experimentInfoText);

        //
        // Update information displayed for debugging purposes
        //
        if (debug)
        {
            string debugText = "Debug info: \n";
            debugText += experimentState.ToString() + ".\n";
            if (experimentState == ExperimentState.WaitingForStart)
                debugText += waitState.ToString() + ".\n";
            debugText += "Angle number:" + angleNumber + ".\n";
            debugText += "Time iterations:" + timeIterations + ".\n";
            instructionManager.DisplayText(debugText + "\n" + experimentInfoText);
        }
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
                string logData = guideManager.CurrentAngle.ToString();
                logData += "," + taskTime.ToString();
                // Read from all user sensors
                foreach (ISensor sensor in AvatarSystem.GetActiveSensors())
                {
                    float[] sensorData = sensor.GetAllRawData();
                    foreach (float element in sensorData)
                        logData += "," + element.ToString();
                }
                // Read from all experiment sensors
                foreach (ISensor sensor in ExperimentSystem.GetActiveSensors())
                {
                    float[] sensorData = sensor.GetAllProcessedData();
                    foreach (float element in sensorData)
                        logData += "," + element.ToString();
                }

                //
                // Update data and append
                //
                taskTime += Time.fixedDeltaTime;


                //
                // Log current data
                //
                motionLogger.AppendData(logData);

                //
                // Save log and reset flags when successfully compeleted task
                //
                if (CheckTaskCompletion())
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

                break;
            default:
                break;
        }
    }

    private void OnApplicationQuit()
    {
        // Check if WiFi sensors are available
        foreach (ISensor sensor in AvatarSystem.GetActiveSensors())
        {
            if (sensor.GetSensorType().Equals(SensorType.EMGWiFi))
            {
                WiFiSensorManager wifiSensor = (WiFiSensorManager)sensor;
                wifiSensor.StopSensorReading();
            }
        }
        //
        // Save and close all logs
        //
        ExperimentSystem.CloseAllExperimentLoggers();
    }

    #region Inherited methods overrides

    /// <summary>
    /// Initializes the ExperimentSystem and its components.
    /// Verifies that all components needed for the experiment are available.
    /// </summary>
    protected override void InitExperimentSystem()
    {
        // Check the experiment parameters
        if (startAngleList.Count <= 0 || endAngleList.Count <= 0 || movementTimeList.Count <= 0)
            throw new System.Exception("An experiment configuration list is empty.");

        if (startAngleList.Count != endAngleList.Count)
            throw new System.Exception("The angle lists do not match in size.");

        //
        // Set the experiment type and ID
        //
        if (AvatarSystem.AvatarType == AvatarType.AbleBodied)
        {
            // Check if EMG is available
            bool EMGAvailable = false;
            foreach (ISensor sensor in AvatarSystem.GetActiveSensors())
            {
                if (sensor.GetSensorType().Equals(SensorType.EMGWiFi))
                {
                    EMGAvailable = true;
                    
                    WiFiSensorManager wifiSensor = (WiFiSensorManager)sensor;
                    //Debug.Log(wifiSensor.RunThread);
                    wifiSensor.StartSensorReading();
                    //Debug.Log(wifiSensor.RunThread);
                }
            }
            // Set whether emg or synergy based
            if (EMGAvailable)
            {
                experimentType = ExperimentType.TypeTwo;
                ExperimentSystem.SetActiveExperimentID("EMG_Data");
            }
            else
            {
                if (debug)
                {
                    // DEBUG ONLY
                    experimentType = ExperimentType.TypeTwo;
                    ExperimentSystem.SetActiveExperimentID("EMG_Data");
                    // DEBUG ONLY
                }
                else
                    throw new System.Exception("An EMG measurement device is required.");
            }
        }
        else
            throw new System.NotImplementedException();

        //
        // Create data loggers
        //
        motionLogger = new DataStreamLogger("Motion");
        ExperimentSystem.AddExperimentLogger(motionLogger);

        //
        // Check and add experiment sensors
        //
        //
        // Add VIVE Trackers.
        //
        if (!debug)
        {
            GameObject motionTrackerGO = AvatarSystem.AddMotionTracker();
            VIVETrackerManager upperArmTracker = new VIVETrackerManager(motionTrackerGO.transform);
            ExperimentSystem.AddSensor(upperArmTracker);
            // Shoulder acromium head tracker
            GameObject motionTrackerGO1 = AvatarSystem.AddMotionTracker();
            VIVETrackerManager shoulderTracker = new VIVETrackerManager(motionTrackerGO1.transform);
            ExperimentSystem.AddSensor(shoulderTracker);

            // Set arm guide position tracking
            guideManager.shoulderLocationTransform = motionTrackerGO1.transform;
        }
    }

    /// <summary>
    /// Checks whether the task has be successfully completed or not.
    /// </summary>
    /// <returns>True if the task has been successfully completed.</returns>
    public override bool CheckTaskCompletion()
    {
        //
        // Perform some condition testing
        //
        if (guideManager.Success)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    /// <summary>
    /// Checks if the condition for the rest period has been reached.
    /// </summary>
    /// <returns>True if the rest condition has been reached.</returns>
    public override bool CheckRestCondition()
    {
        return false;
    }

    /// <summary>
    /// Checks if the condition for changing experiment session has been reached.
    /// </summary>
    /// <returns>True if the condition for changing sessions has been reached.</returns>
    public override bool CheckNextSessionCondition()
    {
        if (iterationNumber >= iterationsPerAngle)
            return true;
        else
            return false;
    }

    /// <summary>
    /// Checks if the condition for ending the experiment has been reached.
    /// </summary>
    /// <returns>True if the condition for ending the experiment has been reached.</returns>
    public override bool CheckEndCondition()
    {
        if (totalIterations >= totalIterationLimit)
            return true;
        else
            return false;
    }

    /// <summary>
    /// Launches the next session. Performs all the required preparations.
    /// </summary>
    public override void LaunchNextSession()
    {
        throw new System.NotImplementedException();
    }

    /// <summary>
    /// Finishes the experiment. Performs all the required procedures.
    /// </summary>
    public override void EndExperiment()
    {
        throw new System.NotImplementedException();
    }


    #endregion
}