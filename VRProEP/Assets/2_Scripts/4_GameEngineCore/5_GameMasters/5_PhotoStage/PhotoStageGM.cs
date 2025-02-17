﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

// GameMaster includes
using VRProEP.ExperimentCore;
using VRProEP.GameEngineCore;
using VRProEP.ProsthesisCore;
using VRProEP.Utilities;

public class PhotoStageGM : GameMaster
{
    [Header("Configuration")]
    public Transform objectTransform;
    public Transform objectStart;
    public bool isAble = true;
    public bool isEMG = false;
    public List<GameObject> dropOffLocations = new List<GameObject>();
    public float standOffset = 0.1f;
    public Transform subjectStandLocation;

    private ConfigurableElbowManager elbowManager;
    //private SteamVR_Action_Boolean buttonAction = SteamVR_Input.GetAction<SteamVR_Action_Boolean>("ObjectInteractButton");
    private float theta = 0.5f;


    private void Awake()
    {
        if (isAble && !isEMG)
            LoadAbleBodiedAvatar();
        else if (!isAble && !isEMG)
            LoadTHAvatar();
    }

    private void Start()
    {
        float subjectHeight = SaveSystem.ActiveUser.height;
        float subjectArmLength = SaveSystem.ActiveUser.upperArmLength + SaveSystem.ActiveUser.forearmLength + (SaveSystem.ActiveUser.handLength / 2);
        //Debug.Log(subjectArmLength);

        List<float> dropOffHeightMultipliers = new List<float>();
        dropOffHeightMultipliers.Add(0.65f);
        dropOffHeightMultipliers.Add(0.65f);
        dropOffHeightMultipliers.Add(0.65f);
        dropOffHeightMultipliers.Add(0.9f);
        List<float> dropOffReachMultipliers = new List<float>();
        dropOffReachMultipliers.Add(0.75f);
        dropOffReachMultipliers.Add(1.0f);
        dropOffReachMultipliers.Add(0.5f);
        dropOffReachMultipliers.Add(1.0f);
        // Set drop-off locations
        int i = 0;
        foreach (GameObject dropOff in dropOffLocations)
        {
            Transform dropOffTransform = dropOff.transform;
            dropOffTransform.localPosition = new Vector3((subjectStandLocation.localPosition.x - (dropOffReachMultipliers[i] * subjectArmLength)), dropOffHeightMultipliers[i] * subjectHeight, dropOffTransform.localPosition.z);
            i++;
        }

        if (isEMG)
            InitialiseExperimentSystems();

    }

    private void FixedUpdate()
    {
        if (buttonAction.GetStateDown(SteamVR_Input_Sources.Any))
        {
            theta += 0.05f;
            elbowManager.SetSynergy(theta);
        }
    }

    public void LoadAbleBodiedAvatar()
    {
        // Load
        SaveSystem.LoadUserData("MD1942");
        AvatarSystem.LoadPlayer(SaveSystem.ActiveUser.type, AvatarType.AbleBodied);
        AvatarSystem.LoadAvatar(SaveSystem.ActiveUser, AvatarType.AbleBodied, false);
        // Initialize UI.
        //InitializeUI();

        // Change the number for the forearm tracker being used
        GameObject faTrackerGO = GameObject.FindGameObjectWithTag("ForearmTracker");
        SteamVR_TrackedObject steamvrConfig = faTrackerGO.GetComponent<SteamVR_TrackedObject>();
        steamvrConfig.index = SteamVR_TrackedObject.EIndex.Device5;
        // Configure the grasp manager
        GameObject graspManagerGO = GameObject.FindGameObjectWithTag("GraspManager");
        if (graspManagerGO == null)
            throw new System.Exception("Grasp Manager not found.");
        GraspManager graspManager = graspManagerGO.GetComponent<GraspManager>();
        graspManager.managerType = GraspManager.GraspManagerType.Assisted;
        graspManager.managerMode = GraspManager.GraspManagerMode.Restriced;
    }

    public void LoadTHAvatar()
    {
        SaveSystem.LoadUserData("MD1942");
        AvatarSystem.LoadPlayer(UserType.Ablebodied, AvatarType.Transhumeral);
        AvatarSystem.LoadAvatar(SaveSystem.ActiveUser, AvatarType.Transhumeral);

        // Initialize prosthesis
        GameObject prosthesisManagerGO = GameObject.FindGameObjectWithTag("ProsthesisManager");
        elbowManager = prosthesisManagerGO.AddComponent<ConfigurableElbowManager>();
        elbowManager.InitializeProsthesis(SaveSystem.ActiveUser.upperArmLength, (SaveSystem.ActiveUser.forearmLength + SaveSystem.ActiveUser.handLength / 2.0f), 1.5f);
        // Set the reference generator to jacobian-based.
        elbowManager.ChangeReferenceGenerator("VAL_REFGEN_LINKINSYN");
        //elbowManager.ChangeReferenceGenerator("VAL_REFGEN_JACOBIANSYN");

        // Initialize UI.
        //InitializeUI();
        // Configure the grasp manager
        GameObject graspManagerGO = GameObject.FindGameObjectWithTag("GraspManager");
        if (graspManagerGO == null)
            throw new System.Exception("Grasp Manager not found.");
        GraspManager graspManager = graspManagerGO.GetComponent<GraspManager>();
        graspManager.managerType = GraspManager.GraspManagerType.Assisted;
        graspManager.managerMode = GraspManager.GraspManagerMode.Restriced;

        // set syn
        elbowManager.SetSynergy(theta);
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
        experimentType = ExperimentType.TypeOne;
        ExperimentSystem.SetActiveExperimentID("PhotoStage");

        //
        // Create data loggers
        //
        // Restart EMG readings
        foreach (ISensor sensor in AvatarSystem.GetActiveSensors())
        {
            if (sensor.GetSensorType().Equals(SensorType.EMGWiFi))
            {
                UDPSensorManager udpSensor = (UDPSensorManager)sensor;
                //Debug.Log(wifiSensor.RunThread);
                udpSensor.StartSensorReading();
                //Debug.Log(wifiSensor.RunThread);
            }
        }
        // Set EMG sensor and reference generator as active.
        // Get prosthesis
        GameObject prosthesisManagerGO = GameObject.FindGameObjectWithTag("ProsthesisManager");
        ConfigurableElbowManager elbowManager = prosthesisManagerGO.GetComponent<ConfigurableElbowManager>();
        // Set active sensor and reference generator to EMG.
        elbowManager.ChangeSensor("VAL_SENSOR_SEMG");
        elbowManager.ChangeReferenceGenerator("VAL_REFGEN_EMGPROP");
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
        if (true)
        {
            return true;
        }
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
        throw new System.NotImplementedException();
    }

    #endregion
}
