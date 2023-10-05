// System
using System.Collections;
using System.Collections.Generic;
using System.IO;

// Unity
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// SteamVR
using Valve.VR;
using Valve.VR.InteractionSystem;

//NetMQ
using NetMQ;

// GameMaster includes
using VRProEP.ExperimentCore;
using VRProEP.GameEngineCore;
using VRProEP.ProsthesisCore;
using VRProEP.Utilities;

public class DataCollection2022GM : GameMaster
{
    // Here you can place all your Unity (GameObjects or similar)
    #region Unity objects
    [Header("Sensor settings")]
    [SerializeField]
    private bool delsysEMGEnable = false;
    [SerializeField]
    private bool foreArmBandFMGEnable = false;
    [SerializeField]
    private bool wristBandFMGEnable = false;
    [SerializeField]
    private bool trackerEnable = false;
    [SerializeField]
    private bool fourTrackerEnable = false;
    [SerializeField]
    private bool xrSkip = false;

    [Header("Data format")]
    [SerializeField]
    private string ablebodiedDataFormat = "pose,t,aDotE,bDotE,gDotE,aE,bE,gE,xE,yE,zE,aDotUA,bDotUA,gDotUA,aUA,bUA,gUA,xUA,yUA,zUA,aDotSH,bDotSH,gDotSH,aSH,bSH,gSH,xSH,ySH,zSH,aDotUB,bDotUB,gDotUB,aUB,bUB,gUB,xUB,yUB,zUB,xHand,yHand,zHand,aHand,bHand,gHand";
    [SerializeField]
    private string positionDataFormat = "pose,t,step,x1,y1,z1,w1,qx1,qy1,qz1, x2,y2,z2,w2,qx2,qy2,qz2, x3,y3,z3,w3,qx3,qy3,qz3, x4,y4,z4,w4,qx4,qy4,qz4";
    [SerializeField]
    private string performanceDataFormat = "i,pose,name,tF";
    [SerializeField]
    private string foreArmBandDataFormat = "t,ch1,ch2,ch3,ch4,ch5,ch6,ch7,ch8,ch9,ch10,ch11,ch12,ch13,ch14,ch15,aEul,bEul,gEul,xGyro,yGyro,zGyro,xAcc,yAcc,zAcc,xMag,yMag,zMag,ch1z,ch2z,ch3z,ch4z,ch5z,ch6z,ch7z,ch8z,ch9z,ch10z,ch11z,ch12z,ch13z,ch14z,ch15z";
    [SerializeField]
    private string wristBandDataFormat = "t,ch1,ch2,ch3,ch4,ch5,ch6,ch7,ch8,ch9,ch10,aEuler,bEuler,gEuler,xGyro,yGyro,zGyro,xAcc,yAcc,zAcc,xMag,yMag,zMag,ch1z,ch2z,ch3z,ch4z,ch5z,ch6z,ch7z,ch8z,ch9z,ch10z";


    [Header("Grid manager")]
    [SerializeField]
    private TextMeshPro taskPoseText;

    [SerializeField]
    private ADLPoseListManager poseListManager;

   
    [Header("Prefab Experiment configuration:")]
    [SerializeField]
    [Tooltip("The subject's shoulder start angle in degrees.")]
    [Range(-180.0f, 180.0f)]
    private float startShoulderAngle = -90.0f;

    [SerializeField]
    [Tooltip("The subject's elbow start angle in degrees.")]
    [Range(-180.0f, 180.0f)]
    private float startElbowAngle = 90.0f;

    [SerializeField]
    [Tooltip("The start angle tolerance in degrees.")]
    [Range(0.0f, 90.0f)]
    private float startTolerance = 15.0f;

    [SerializeField]
    [Tooltip("The number of iterations per target.")]
    [Range(1, 100)]
    private int iterationsPerTarget = 10;

    [SerializeField]
    [Tooltip("Max time for each iteration")]
    [Range(1, 20)]
    private float maxTaskTime = 2.0f;

    [SerializeField]
    [Tooltip("Holding time for each iteration")]
    [Range(0, 20)]
    private float holdingTime = 1.0f;



    [SerializeField]
    private bool checkStartPosition;

    [SerializeField]
    private GameObject startPosPhoto;

    [SerializeField]
    private AudioClip startAudioClip;
    private bool startAudioPlayed = false;

    [SerializeField]
    private AudioClip holdAudioClip;

    [SerializeField]
    private AudioClip returnAudioClip;

    [SerializeField]
    private AudioClip testAudioClip;

    [SerializeField]
    private AudioClip nextAudioClip;
    private bool nextAudioPlayed = false;


    #endregion

    protected SteamVR_Action_Boolean padAction = SteamVR_Input.GetAction<SteamVR_Action_Boolean>("InterfaceEnableButton");

    // Additional data logging
    private DataStreamLogger performanceDataLogger;
    private float doneTime;
    private int subStep;

    // Delsys EMG background data collection
    private DelsysEMG delsysEMG = new DelsysEMG();

    // FMG arm band data collection
    private TCPTactileArmBandManager foreArmBandFMG;
    [SerializeField]
    private int foremArmBandTactileCh;
    [SerializeField]
    private string foremArmBandIPAddress;
    [SerializeField]
    private int foreArmBandPort;


    private TCPTactileArmBandManager wristBandFMG;
    [SerializeField]
    private int wristBandTactileCh;
    [SerializeField]
    private string wristBandIPAddress;
    [SerializeField]
    private int wristBandPort;

    // Target management variables
    private int targetNumber = 9; // The total number of targets
    private List<int> targetOrder = new List<int>(); // A list of target indexes ordered for selection over iterations in a session.

    // Motion tracking for experiment management and adaptation (check for start position)
    private VIVETrackerManager upperArmTracker;
    private VIVETrackerManager lowerArmTracker;
    private VIVETrackerManager shoulderTracker;
    private VIVETrackerManager c7Tracker;
    private VirtualPositionTracker handTracker;

    // Flow control
    private bool hasReached = false;
    private bool taskStarted = false;
    private bool taskComplete = false;
    private bool startRecording = false;
    private bool emgIsRecording = false;
    private bool failIteration = false;

    // Lefty subject sign
    private float leftySign = 1.0f;

    // Timer slide bar
    private Slider timerSliderBar;

    //Audio
    AudioSource audio;

   
    #region Private methods
    private string ConfigEMGFilePath()
    {
        //string emgDataFilename =  Path.Combine(taskDataLogger.ActiveDataPath, "session_" + sessionNumber , "i" + "_" + iterationNumber + "EMG.csv");
        string emgDataFilename = taskDataLogger.ActiveDataPath + "/session_" + sessionNumber + "/i" + "_" + iterationNumber + "EMG.csv";
        return emgDataFilename;
    }

    private string ConfigFMGFilePath()
    {
        //string emgDataFilename =  Path.Combine(taskDataLogger.ActiveDataPath, "session_" + sessionNumber , "i" + "_" + iterationNumber + "EMG.csv");
        string fmgDataFilename = taskDataLogger.ActiveDataPath + "/session_" + sessionNumber + "/i" + "_" + iterationNumber + "FMG.csv";
        return fmgDataFilename;
    }

    private string ConfigFMGFilePath(string suffix)
    {
        //string emgDataFilename =  Path.Combine(taskDataLogger.ActiveDataPath, "session_" + sessionNumber , "i" + "_" + iterationNumber + "EMG.csv");
        string fmgDataFilename = taskDataLogger.ActiveDataPath + "/session_" + sessionNumber + "/i" + "_" + iterationNumber + "FMG_" + suffix +".csv";
        return fmgDataFilename;
    }

    private IEnumerator DisplayTaskText(int index)
    {
        poseListManager.SelectPose(index);
        taskPoseText.text = poseListManager.SelectedPose(index) + " "+ iterationNumber.ToString();
        // Play the audio
        if (!audio.isPlaying && !nextAudioPlayed)
        {
            audio.PlayOneShot(nextAudioClip);
            nextAudioPlayed = true;
        }
            
        Debug.Log("Ite:" + iterationNumber + ". Next pose:" + poseListManager.SelectedPose(index) + ".");
        yield return new WaitForSecondsRealtime(holdingTime);
        //audio.clip = poseListManager.GetPoseAudio(index);
        //audio.Play();
    }


    private bool IsAtRightPosition(float qShoudlerRef, float qElbowRef, float tolerance)
    {
  

        // Check that upper and lower arms are within the tolerated start position.
        float qShoulder = leftySign * Mathf.Rad2Deg * (upperArmTracker.GetProcessedData(5) + Mathf.PI); // Offsetting to horizontal position being 0.
        float qElbow = 0;

        HudManager.colour = HUDManager.HUDColour.Orange;
        qElbow = Mathf.Rad2Deg * (lowerArmTracker.GetProcessedData(5)) - qShoulder; // Offsetting to horizontal position being 0.
                                                                                    // The difference to the start position
        float qSDiff = qShoulder - qShoudlerRef;
        float qEDiff = qElbow - qElbowRef;


        //
        // Update information displayed for debugging purposes
        //


        //InstructionManager.DisplayText(qShoulder.ToString() + "\n" + qElbow.ToString() + "\n");

        if (Mathf.Abs(qSDiff) < tolerance && Mathf.Abs(qEDiff) < tolerance)
        {
            HudManager.colour = HUDManager.HUDColour.Orange;
            return true;
        }
        // Provide instructions when not there yet
        else
        {

            string helpText = "";
            if (qSDiff < 0 && Mathf.Abs(qSDiff) > tolerance)
                helpText += "SH: up (" + qShoulder.ToString("F0") + "/" + qShoudlerRef.ToString("F0") + ").\n";
            else if (qSDiff > 0 && Mathf.Abs(qSDiff) > tolerance)
                helpText += "SH: down (" + qShoulder.ToString("F0") + "/" + qShoudlerRef.ToString("F0") + ").\n";
            else
                helpText += "SH: ok (" + qShoulder.ToString("F0") + "/" + qShoudlerRef.ToString("F0") + ").\n";


            if (qEDiff < 0 && Mathf.Abs(qEDiff) > tolerance)
                helpText += "EB: up (" + qElbow.ToString("F0") + "/" + qElbowRef.ToString("F0") + ").\n";
            else if (qEDiff > 0 && Mathf.Abs(qEDiff) > tolerance)
                helpText += "EB: down (" + qElbow.ToString("F0") + "/" + qElbowRef.ToString("F0") + ").\n"; 
            else
                helpText += "EB: ok (" + qElbow.ToString("F0") + "/" + qElbowRef.ToString("F0") + ").\n"; 
            HudManager.DisplayText(helpText);
            HudManager.colour = HUDManager.HUDColour.Red;


            return false;
        }

        
    }

    #endregion


    #region Dynamic configuration

    //
    // Configuration class:
    // Modify this class to be able to configure your experiment from a configuration file
    //
    private class Configurator
    {
        public int iterationsPerTarget = 10;
        public int totalSessionNumber = 2;
        public int initSessionNumber = 1;
    }
    private Configurator configurator;

    #endregion

    // Here are all the methods you need to write for your experiment.
    #region GameMaster Inherited Methods

    protected override void FixedUpdate()
    {
       
        // Override fixed update to start the emg recording when the start performing the task
        if ( GetCurrentStateName() == State.STATE.PERFORMING_TASK && startRecording)
        {
            if (!taskStarted)
            {
                // Comment out when sEMG is connected
                if (delsysEMGEnable)
                {
                    delsysEMG.StartRecording(ConfigEMGFilePath());
                    //emgIsRecording = true;
                }

                if (foreArmBandFMGEnable)
                {
                    foreArmBandFMG.FileName = ConfigFMGFilePath("FA");
                    foreArmBandFMG.StartRecording();
                }
                if(wristBandFMGEnable)
                { 
                    wristBandFMG.FileName = ConfigFMGFilePath("W");
                    wristBandFMG.StartRecording();
                }

                Debug.Log("Ite:" + iterationNumber + ". Start task");
                //audio.clip = startAudioClip;
                //audio.Play();
                if (!audio.isPlaying && !startAudioPlayed)
                {
                    audio.PlayOneShot(startAudioClip);
                    startAudioPlayed = true;
                }
                
                
                taskStarted = true;
            }
               
        }

        // Slider bar UI
        timerSliderBar.value = this.taskTime;

        base.FixedUpdate();

    }
    // Place debug stuff here, for when you want to test the experiment directly from the world without 
    // having to load it from the menus.
    private void Awake()
    {
        if (debug)
        {
            
            SaveSystem.LoadUserData("TB1995175"); // Load the test/demo user (Mr Demo)
            //
            // Debug using able-bodied configuration
            //
            Debug.Log("Load Avatar to Debug.");
            AvatarSystem.LoadPlayer(SaveSystem.ActiveUser.type, AvatarType.AbleBodied);
            AvatarSystem.LoadAvatar(SaveSystem.ActiveUser, AvatarType.AbleBodied);
        }

    }

    /// <summary>
    /// Gets the progress text to be displayed to the subject.
    /// Displays status, current time, experiment progress (session), and session progress (iteration) by default.
    /// </summary>
    /// <returns>The text to be displayed as a string.</returns>
    public override string GetDisplayInfoText()
    {
        // You can choose to use the base initialisation or get rid of it completely.
        string text = base.GetDisplayInfoText();

        return text;
    }

    /// <summary>
    /// Default implementation of HUD colour behaviour.
    /// Green: When state is "Paused", "Resting" or "End".
    /// Red: Any other state and !setActive.
    /// Blue: Any other state and setActive.
    /// Can be overriden to specify a different colour behaviour.
    /// </summary>
    /// <param name="setActive">Sets the HUD colour as active (Blue).</param>
    public override void HandleHUDColour(bool setActive = false)
    {
            base.HandleHUDColour();
    }

    /// <summary>
    /// Configures the experiment from a text file.
    /// The method needs to be extended to extract data from the configuration file that is automatically loaded.
    /// If no configuration is needed, then leave empty.
    /// </summary>
    public override void ConfigureExperiment()
    {
        // First call the base method to load the file
        base.ConfigureExperiment();

        //configAsset = Resources.Load<TextAsset>("Experiments/" + ExperimentSystem.ActiveExperimentID);

        // Convert configuration file to configuration class.
        configurator = JsonUtility.FromJson<Configurator>(configAsset.text);

        // Load from config file
        iterationsPerTarget = configurator.iterationsPerTarget;
        for (int i = 0; i <= configurator.totalSessionNumber-1; i++)
        {
            iterationsPerSession.Add(0);
        }
        

    }

    /// <summary>
    /// Initializes the ExperimentSystem and its components.
    /// Verifies that all components needed for the experiment are available.
    /// This must be done in Start.
    /// Extend this method by doing your own implementation, with base.InitExperimentSystem() being called at the start.
    /// </summary>
    public override void InitialiseExperimentSystems()
    {

        // Set data format
        if (!xrSkip) // If not using the XR to read sensor data
            taskDataFormat = ablebodiedDataFormat;
        else
            taskDataFormat = positionDataFormat;
        // Lefty sign
        /*
        if (SaveSystem.ActiveUser.lefty)
            leftySign = -1.0f;
            */
        // Audio 
        audio = GetComponent<AudioSource>();
        audio.clip = testAudioClip;

        #region Modify base
        // Then run the base initialisation which is needed, with a small modification
        //
        // Set the experiment name only when debugging. Take  the name from the gameobject + Debug
        //
        if (debug)
            ExperimentSystem.SetActiveExperimentID(this.gameObject.name + "_Debug");

        // Make sure flow control is initialised
        sessionNumber = configurator.initSessionNumber;
        iterationNumber = 1;

        //
        // Create the default data loggers
        //

        string suffix = "";

        if (delsysEMGEnable & (foreArmBandFMGEnable || wristBandFMGEnable))
            suffix = "";
        else if (delsysEMGEnable)
            suffix = "_EMG";
        else if (foreArmBandFMGEnable || wristBandFMGEnable)
            suffix = "_FMG";

        taskDataLogger = new DataStreamLogger("TaskData" + suffix + "/" + AvatarSystem.AvatarType.ToString());
        ExperimentSystem.AddExperimentLogger(taskDataLogger);
        taskDataLogger.AddNewLogFile(sessionNumber, iterationNumber, taskDataFormat); // Add file

        //
        // Create the performance data loggers
        //
        performanceDataLogger = new DataStreamLogger("PerformanceData" + suffix);
        ExperimentSystem.AddExperimentLogger(performanceDataLogger);
        performanceDataLogger.AddNewLogFile(AvatarSystem.AvatarType.ToString(), sessionNumber, performanceDataFormat); // Add file

        // Send the player to the experiment centre position
        TeleportToStartPosition();
        #endregion

        #region Initialize EMG sensors
        //Initialise Delsys EMG sensor
        if (delsysEMGEnable)
        {
            delsysEMG.Init();
            delsysEMG.Connect();
        }



        #endregion

        #region Initialize FMG armband sensors
        if (foreArmBandFMGEnable)
        {
            foreArmBandFMG = new TCPTactileArmBandManager(foremArmBandTactileCh,foremArmBandIPAddress, foreArmBandPort);
            foreArmBandFMG.MinDataNum = (int)(maxTaskTime * 10.0f - 1.0f); // 10 Hz, plus/minus 1 data point tolerance
            foreArmBandFMG.MaxDataNum = (int)(maxTaskTime * 10.0f + 1.0f);
            foreArmBandFMG.FileHeader = foreArmBandDataFormat;
        }
        if (wristBandFMGEnable)
        {
            wristBandFMG = new TCPTactileArmBandManager(wristBandTactileCh,wristBandIPAddress, wristBandPort);
            wristBandFMG.MinDataNum = (int)(maxTaskTime * 10.0f - 1.0f);
            wristBandFMG.MaxDataNum = (int)(maxTaskTime * 10.0f + 1.0f);
            wristBandFMG.FileHeader = wristBandDataFormat;
        }
        
            
        
        #endregion

        #region  Initialize motion sensors
        //
        // Add arm motion trackers for able-bodied case.
        //
        // Lower limb motion tracker
        if (trackerEnable)
        {
            GameObject llMotionTrackerGO = GameObject.FindGameObjectWithTag("ForearmTracker");
            lowerArmTracker = new VIVETrackerManager(llMotionTrackerGO.transform);
            ExperimentSystem.AddSensor(lowerArmTracker);

            // Upper limb motion tracker
            GameObject ulMotionTrackerGO = AvatarSystem.AddMotionTracker();
            ulMotionTrackerGO.tag = "UpperarmTracker";
            upperArmTracker = new VIVETrackerManager(ulMotionTrackerGO.transform);
            ExperimentSystem.AddSensor(upperArmTracker);

            if (fourTrackerEnable)
            {
                // Shoulder acromium head tracker
                GameObject motionTrackerGO1 = AvatarSystem.AddMotionTracker();
                shoulderTracker = new VIVETrackerManager(motionTrackerGO1.transform);
                motionTrackerGO1.tag = "SATracker";
                ExperimentSystem.AddSensor(shoulderTracker);
                // C7 tracker
                GameObject motionTrackerGO2 = AvatarSystem.AddMotionTracker();
                c7Tracker = new VIVETrackerManager(motionTrackerGO2.transform);
                motionTrackerGO2.tag = "C7Tracker";
                ExperimentSystem.AddSensor(c7Tracker);
            }
        }


        

        //
        // Hand tracking sensor
        //
        GameObject handGO = GameObject.FindGameObjectWithTag("Hand");
        handTracker = new VirtualPositionTracker(handGO.transform);
        ExperimentSystem.AddSensor(handTracker);

        #endregion

        //
        // Target ADL poses
        //
        for (int i = 1; i <= poseListManager.PoseNumber; i++)
        {
            string pose = "Gesture" + i.ToString();
            poseListManager.AddPose(pose);
        }
        

        //
        // Timer slider bar
        //
        timerSliderBar = gameObject.GetComponentInChildren<Slider>();
        timerSliderBar.maxValue = maxTaskTime;
        timerSliderBar.value = 0.0f;

        //
        // Start EMG readings
        //
        if (delsysEMGEnable)
            delsysEMG.StartAcquisition();

        //
        // Start FMG readings
        //
        if (foreArmBandFMGEnable)
            foreArmBandFMG.StartAcquisition();
        if (wristBandFMGEnable)
            wristBandFMG.StartAcquisition();
        
         
    }

    /// <summary>
    /// Coroutine for the welcome text.
    /// Implement your welcome loop here.
    /// </summary>
    /// <returns>Yield instruction</returns>
    public override IEnumerator WelcomeLoop()
    {
        // First flag that we are in the welcome routine
        welcomeDone = false;
        inWelcome = true;
        
        welcomeDone = true;

        HudManager.DisplayText("Look to the top right. Instructions will be displayed there.");
        InstructionManager.DisplayText("Hi " + SaveSystem.ActiveUser.name + "! Welcome to the virtual world. \n\n (Press the trigger button to continue...)");
        yield return WaitForSubjectAcknowledgement(); // And wait for the subject to cycle through them.

        InstructionManager.DisplayText("Make sure you are standing on top of the green circle. \n\n (Press the trigger button to continue...)");
        yield return WaitForSubjectAcknowledgement(); // And wait for the subject to cycle through them.

        InstructionManager.DisplayText("Test audio. \n\n (If you can hear the audio, press the trigger button to continue...)");
        audio.loop = true;
        audio.volume = 0.4f;
        audio.Play();
        
        yield return WaitForSubjectAcknowledgement(); // And wait for the subject to cycle through them.
        audio.loop = false;
        audio.Stop();
        audio.volume = 1.0f;
        //
        // Hud intro
        InstructionManager.DisplayText("Alright " + SaveSystem.ActiveUser.name + ", let me introduce you to your assistant, the Heads Up Display (HUD)." + "\n\n (Press the trigger)");
        yield return WaitForSubjectAcknowledgement(); // And wait for the subject to cycle through them.
        InstructionManager.DisplayText("Look at the HUD around your left eye. It's saying hi!");
        HudManager.DisplayText("Hi! I'm HUD!" + "\n (Press trigger)");
        yield return WaitForSubjectAcknowledgement(); // And wait for the subject to cycle through them.
        HudManager.DisplayText("I'm here to help!" + "\n (Press trigger)");
        yield return WaitForSubjectAcknowledgement(); // And wait for the subject to cycle through them.
        HudManager.DisplayText("Look at the screen.", 3);


        //
        // Experiment overall intro
        InstructionManager.DisplayText("Alright " + SaveSystem.ActiveUser.name + ", let me explain what we are doing today." + "\n\n (Press the trigger)");
        yield return WaitForSubjectAcknowledgement(); // And wait for the subject to cycle through them.
        InstructionManager.DisplayText("Today, the experiment will require you to reach to the targets in front of you." + "\n\n (Press the trigger)");
        yield return WaitForSubjectAcknowledgement(); // And wait for the subject to cycle through them.
        InstructionManager.DisplayText("You will do 2 sessions, 1st 90 iterations and 2nd 270 iterations " + "\n\n (Press the trigger)");
        yield return WaitForSubjectAcknowledgement(); // And wait for the subject to cycle through them.
        InstructionManager.DisplayText("A 60 sec rest occurs every 35 iterations" + "\n\n (Press the trigger)");
        yield return WaitForSubjectAcknowledgement(); // And wait for the subject to cycle through them.

        // Now that you are done, set the flag to indicate we are done.
        
        welcomeDone = true;

    }

    /// <summary>
    /// Performs initialisation procedures for the experiment. Sets variables to their zero state.
    /// </summary>
    public override void InitialiseExperiment()
    {
        #region Spawn grid
        // Spawn the grid
        //gridManager.CurrentTargetType = TargetGridManager.TargetType.Ball; // Type is the ball
        poseListManager.ResetPoseSelection();
        Debug.Log("Spawn the list!");
        #endregion

        #region Iteration settings
        // Set iterations variables for flow control.
        targetNumber = poseListManager.TargetNumber;
        iterationsPerSession[sessionNumber-1] = targetNumber * iterationsPerTarget;

        // Create the list of target indexes and shuffle it.
        for (int i = 1; i < targetNumber; i++)
        {
            for (int j = 0; j < iterationsPerTarget; j++)
            {
                targetOrder.Add(i);
                Debug.Log(targetOrder[targetOrder.Count - 1]);
            }
                
        }
        targetOrder.Shuffle();
        targetOrder.Insert(0, 0);

        #endregion
        StartCoroutine(DisplayTaskText(targetOrder[0]));
        
    }

    /// <summary>
    /// Coroutine for the experiment instructions.
    /// Implement your instructions loop here.
    /// </summary>
    /// <returns>Yield instruction</returns>
    public override IEnumerator InstructionsLoop()
    {
        // First flag that we are in the instructions routine
        instructionsDone = false;
        inInstructions = true;

        //Instructions
        if (sessionNumber == 1) // first session
        {
            InstructionManager.DisplayText("Alright, the sphere targets should have spawned for you." + "\n\n (Press the trigger)");
            yield return WaitForSubjectAcknowledgement(); // And wait for the subject to cycle through them.
            InstructionManager.DisplayText("In the 1st session, you will need to reach to the spheres using your index finger." + "\n\n (Press the trigger)");
            yield return WaitForSubjectAcknowledgement(); // And wait for the subject to cycle through them.
            InstructionManager.DisplayText("If you are ready, let's start training!" + "\n\n (Press the trigger)");
            yield return WaitForSubjectAcknowledgement(); // And wait for the subject to cycle through them.

        }
        else if (sessionNumber == 2) // second session
        {
            InstructionManager.DisplayText("You've finished the 1st session, well done. Let's start the second session" + "\n\n (Press the trigger)");
            yield return WaitForSubjectAcknowledgement(); // And wait for the subject to cycle through them.
            InstructionManager.DisplayText("The sphere targets have been replaced by bottle targets and a bottle has been placed in your hand." + "\n\n (Press the trigger)");
            yield return WaitForSubjectAcknowledgement(); // And wait for the subject to cycle through them.
            InstructionManager.DisplayText("In the 2nd session, you will need to match bothe the target locations and orientations ." + "\n\n (Press the trigger)");
            yield return WaitForSubjectAcknowledgement(); // And wait for the subject to cycle through them.
            InstructionManager.DisplayText("If you are ready, let's start training!" + "\n\n (Press the trigger)");
            yield return WaitForSubjectAcknowledgement(); // And wait for the subject to cycle through them.
        }



        // Now that you are done, set the flag to indicate we are done.
        instructionsDone = true;

    }


    /// <summary>
    /// Coroutine for the experiment training.
    /// Implement your training loop here.
    /// </summary>
    /// <returns>Yield instruction</returns>
    public override IEnumerator TrainingLoop()
    {
        // First flag that we are in the training routine
        trainingDone = false;
        inTraining = true;


        if (sessionNumber == 1)
        {

            InstructionManager.DisplayText("Let's start training then!" + "\n\n (Press the trigger)");
            yield return WaitForSubjectAcknowledgement(); // And wait for the subject to cycle through them.


            //
            // HUD Colours
            InstructionManager.DisplayText("First, let's tell you about the colour of the HUD. It will tell you what you need to do." + "\n\n (Press the trigger)");
            yield return WaitForSubjectAcknowledgement(); // And wait for the subject to cycle through them.
            InstructionManager.DisplayText("<Red>: for returning back and adjusting your start position." + "\n\n (Press the trigger)");
            HudManager.DisplayText("I'm red!");
            HudManager.colour = HUDManager.HUDColour.Red;
            yield return WaitForSubjectAcknowledgement(); // And wait for the subject to cycle through them.
            InstructionManager.DisplayText("<Orange>: for waiting for the countdown." + "\n\n (Press the trigger)");
            HudManager.DisplayText("I'm orange!");
            HudManager.colour = HUDManager.HUDColour.Orange;
            yield return WaitForSubjectAcknowledgement(); // And wait for the subject to cycle through them.
            InstructionManager.DisplayText("<Blue>: for reaching for the target.!" + "\n\n (Press the trigger)");
            HudManager.DisplayText("I'm blue!");
            HudManager.colour = HUDManager.HUDColour.Blue;
            yield return WaitForSubjectAcknowledgement(); // And wait for the subject to cycle through them.
            InstructionManager.DisplayText("You need to hold on you reaching position for a while until the HUD says 'Well Done', like" + "\n\n (Press the trigger)");
            HudManager.colour = HUDManager.HUDColour.Red;
            HudManager.DisplayText("Well Done");
            yield return WaitForSubjectAcknowledgement(); // And wait for the subject to cycle through them.

            HudManager.ClearText();
            HudManager.colour = HUDManager.HUDColour.Red;

            //
            // Reaching practice
            InstructionManager.DisplayText("Next, the colour of the targets will tell you the status of the target." + "\n\n (Press the trigger)");
            yield return WaitForSubjectAcknowledgement(); // And wait for the subject to cycle through them.
            InstructionManager.DisplayText("<Blue>: the target is selected as the next target." + "\n\n (Press the trigger)");
            yield return WaitForSubjectAcknowledgement(); // And wait for the subject to cycle through them.
            InstructionManager.DisplayText("<Green>: you have successfully reached the selected one." + "\n\n (Press the trigger)");
            yield return WaitForSubjectAcknowledgement(); // And wait for the subject to cycle through them.


            //
            // Explain routine
            InstructionManager.DisplayText("Alright, let's explain the task routine." + "\n\n (Press the trigger)");
            yield return WaitForSubjectAcknowledgement(); // And wait for the subject to cycle through them.
            InstructionManager.DisplayText("The task routine is: first keep your start position for 3 seconds, and HUD will tell you when to go." + "\n\n (Press the trigger)");
            yield return WaitForSubjectAcknowledgement(); // And wait for the subject to cycle through them.
            InstructionManager.DisplayText("Then reach to the target in <blue> using your index finger." + "\n\n (Press the trigger)");
            yield return WaitForSubjectAcknowledgement(); // And wait for the subject to cycle through them.
            InstructionManager.DisplayText("Finally, when you reach, hold on for a while, until you hear 'Return' and HUD says 'Well done'." + "\n\n (Press the trigger)");
            yield return WaitForSubjectAcknowledgement(); // And wait for the subject to cycle through them.

            //
            // Important messages
            InstructionManager.DisplayText("Important!: Keep your final reaching position when you hear 'Hold', like !!! " + "\n\n (Press the trigger to play)");
            yield return WaitForSubjectAcknowledgement(); // And wait for the subject to cycle through them.
            audio.clip = holdAudioClip;
            audio.Play();
            HudManager.DisplayText("Hold on for a while!!");
            InstructionManager.DisplayText("Important!: Keep your final reaching position when you hear 'Hold', like !!! " + "\n\n (Press the trigger to continue)");
            yield return WaitForSubjectAcknowledgement(); // And wait for the subject to cycle through them.

            InstructionManager.DisplayText("Important!: Do not return until HUD says 'Well Done' and you hear 'Return', like !!! " + "\n\n (Press the trigger to play)");
            yield return WaitForSubjectAcknowledgement(); // And wait for the subject to cycle through them.
            audio.clip = returnAudioClip;
            audio.Play();
            HudManager.DisplayText("Well done!!");
            InstructionManager.DisplayText("Important!: Do not return until HUD says 'Well Done' and you hear 'Return', like !!! " + "\n\n (Press the trigger to continue)");
            yield return WaitForSubjectAcknowledgement(); // And wait for the subject to cycle through them.

            //
            // Have a try
            InstructionManager.DisplayText("Ok, let's have a try of a routine." + "\n\n (Press the trigger)");
            HudManager.ClearText();
            HudManager.colour = HUDManager.HUDColour.Red;
            yield return WaitForSubjectAcknowledgement(); // And wait for the subject to cycle through them.


            //
            // Start position
            InstructionManager.DisplayText("First, let's show you the start position." + "\n\n (Press the trigger)");
            yield return WaitForSubjectAcknowledgement(); // And wait for the subject to cycle through them.

            //
            // Start position
            InstructionManager.DisplayText("Your upper arm and elbow should point downards." + "\n\n (Press the trigger)");
            yield return WaitForSubjectAcknowledgement(); // And wait for the subject to cycle through them.
            InstructionManager.DisplayText("Try it. The HUD displays: (current upper and fore arm angle/the desired one)");
            //startPosPhoto.SetActive(true);
            //startPosPhoto.transform.Rotate(new Vector3(0.0f, 90.0f, 0.0f), Space.World);
            yield return new WaitUntil(() => IsReadyToStart());
            //startPosPhoto.SetActive(false);
            HudManager.ClearText();

            //Start practice
            poseListManager.SelectPose(0);
            InstructionManager.DisplayText("The sphere that you need to reach will turn blue. Don't reach now." + "\n\n (Press the trigger)");
            yield return WaitForSubjectAcknowledgement(); // And wait for the subject to cycle through them.
            InstructionManager.DisplayText("You'll have to wait for a three second countdown. Look at the sphere and get ready!" + "\n\n (Press the trigger)");
            yield return WaitForSubjectAcknowledgement(); // And wait for the subject to cycle through them.
            HudManager.colour = HUDManager.HUDColour.Orange;
            HUDCountDown(1);
            yield return new WaitUntil(() => CountdownDone); // And wait 
            InstructionManager.DisplayText("Reach for it by index finger!!");
            HudManager.DisplayText("Reach for it by index finger!!");
            HudManager.colour = HUDManager.HUDColour.Blue;
            yield return new WaitUntil(() => IsTaskDone());
            // Signal the subject that the task is done
            HudManager.DisplayText("Hold on your current position!");
            yield return new WaitForSecondsRealtime(holdingTime);
            audio.clip = returnAudioClip;
            audio.Play();
            HudManager.colour = HUDManager.HUDColour.Red;
            HudManager.DisplayText("Well done (you can return to start position)!");
            // Reset flags
            hasReached = false;
            taskComplete = false;
            HudManager.DisplayText("You can relax now. Look to the top right.", 3);

            //
            // End
            InstructionManager.DisplayText("Important: Do not return until you hear 'Return' and HUD says 'Well Done' !!! " + "\n\n (Press the trigger)");
            yield return WaitForSubjectAcknowledgement(); // And wait for the subject to cycle through them.
            InstructionManager.DisplayText("Otherwise, you look ready to go! Good luck!" + "\n\n (Press the trigger)");
            yield return WaitForSubjectAcknowledgement(); // And wait for the subject to cycle through them.
        }
        if (sessionNumber == 2)
        {
            InstructionManager.DisplayText("Let's start training then!" + "\n\n (Press the trigger)");
            yield return WaitForSubjectAcknowledgement(); // And wait for the subject to cycle through them.

            //
            // Reaching practice
            InstructionManager.DisplayText("Everthing is the same except you need to match the orientations." + "\n\n (Press the trigger)");
            yield return WaitForSubjectAcknowledgement(); // And wait for the subject to cycle through them.
            InstructionManager.DisplayText("Ok, let's have a try." + "\n\n (Press the trigger)");
            HudManager.ClearText();
            HudManager.colour = HUDManager.HUDColour.Red;
            yield return WaitForSubjectAcknowledgement(); // And wait for the subject to cycle through them.

            //Start practice         
            poseListManager.SelectPose(2);
            InstructionManager.DisplayText("The sphere that you need to reach will turn blue. Do not reach now." + "\n\n (Press the trigger)");
            yield return WaitForSubjectAcknowledgement(); // And wait for the subject to cycle through them.
            InstructionManager.DisplayText("You'll have to wait for a three second countdown. Look at the sphere and get ready!" + "\n\n (Press the trigger)");
            yield return WaitForSubjectAcknowledgement(); // And wait for the subject to cycle through them.
            HudManager.colour = HUDManager.HUDColour.Orange;
            HUDCountDown(3);
            yield return new WaitUntil(() => CountdownDone); // And wait 
            InstructionManager.DisplayText("Reach for it!!");
            HudManager.DisplayText("Reach for it!!");
            HudManager.colour = HUDManager.HUDColour.Blue;
            yield return new WaitUntil(() => IsTaskDone());
            // Signal the subject that the task is done
            HudManager.DisplayText("Hold on!");
            yield return new WaitForSecondsRealtime(holdingTime);
            HudManager.colour = HUDManager.HUDColour.Red;
            HudManager.DisplayText("Well done (you can return to start position)!");
            // Reset flags
            hasReached = false;
            taskComplete = false;
            HudManager.DisplayText("You can relax now. Look to the top right.", 3);

            //
            // End
            InstructionManager.DisplayText("Important: Hold your final reaching position until you hear 'Return' and HUD says 'Well Done' !!! " + "\n\n (Press the trigger)");
            yield return WaitForSubjectAcknowledgement(); // And wait for the subject to cycle through them.
            InstructionManager.DisplayText("Otherwise, you look ready to go! Good luck!" + "\n\n (Press the trigger)");
            yield return WaitForSubjectAcknowledgement(); // And wait for the subject to cycle through them.

        }

        // Now that you are done, set the flag to indicate we are done.
        trainingDone = true; 

    }

    /// <summary>
    /// Checks whether the subject is ready to start performing the task.
    /// </summary>
    /// <returns>True if ready to start.</returns>
    public override bool IsReadyToStart()
    {
        if (checkStartPosition)
        {
            //return Input.GetKey(KeyCode.UpArrow);
            //return (Input.GetKey(KeyCode.UpArrow) || buttonAction.GetState(SteamVR_Input_Sources.Any));
            return true;
        }

        else
            return true;

    
    }

    /// <summary>
    /// Prepares variables and performs any procedures needed when the subject has succeded in the preparation
    /// to start performing the task.
    /// </summary>
    public override void PrepareForStart()
    {
        // Here you can do stuff like preparing objects/assets, like setting a different colour to the object

        // Select target
        
    }

    /// <summary>
    /// Performs a procedure needed when the subject fails to start the task (goes out of the start condition).
    /// This can be: display some information, reset variables, change something in the experiment.
    /// </summary>
    public override void StartFailureReset()
    {
        // If our subject fails, do some resetting. 
        // Clear bottle selection
        poseListManager.ResetPoseSelection();

    }

    /// <summary>
    /// Handles task data logging which runs on FixedUpdate.
    /// Logs data from sensors registered in the AvatarSystem and ExperimentSystem by default.
    /// Can be exteded to add more data by implementing an override method in the derived class which first adds data
    /// to the logData string (e.g. logData +=  myDataString + ","), and then calls base.HandleTaskDataLogging().
    /// </summary>
    public override void HandleTaskDataLogging()
    {
        

        if (!startRecording && (padAction.GetStateDown(SteamVR_Input_Sources.Any) || Input.GetKeyUp(KeyCode.F1)))
        {
            Debug.Log("Data Recording Started.");
            startRecording = true;
        }

        if (!startRecording)
            return;


        //
        // Add your custom data logging here
        //
        logData += targetOrder[iterationNumber - 1] + ",";  // Make sure you always end your custom data with a comma! Using CSV for data logging.

        //
        // Continue with data logging.
        //
        if (!xrSkip)
            base.HandleTaskDataLogging();
        else
        {
            logData += taskTime.ToString();
            logData += "," + subStep.ToString();
            if (trackerEnable)
            {
                GameObject got = GameObject.FindGameObjectWithTag("ForearmTracker");
                logData += "," + got.transform.position.x.ToString() + "," + got.transform.position.y.ToString() + "," + got.transform.position.z.ToString();
                logData += "," + got.transform.rotation.w.ToString() + "," + got.transform.rotation.x.ToString() + "," + got.transform.rotation.y.ToString() + "," + got.transform.rotation.z.ToString();

                got = GameObject.FindGameObjectWithTag("UpperarmTracker");
                logData += "," + got.transform.position.x.ToString() + "," + got.transform.position.y.ToString() + "," + got.transform.position.z.ToString();
                logData += "," + got.transform.rotation.w.ToString() + "," + got.transform.rotation.x.ToString() + "," + got.transform.rotation.y.ToString() + "," + got.transform.rotation.z.ToString();

                if (fourTrackerEnable)
                {
                    got = GameObject.FindGameObjectWithTag("SATracker");
                    logData += "," + got.transform.position.x.ToString() + "," + got.transform.position.y.ToString() + "," + got.transform.position.z.ToString();
                    logData += "," + got.transform.rotation.w.ToString() + "," + got.transform.rotation.x.ToString() + "," + got.transform.rotation.y.ToString() + "," + got.transform.rotation.z.ToString();

                    got = GameObject.FindGameObjectWithTag("C7Tracker");
                    logData += "," + got.transform.position.x.ToString() + "," + got.transform.position.y.ToString() + "," + got.transform.position.z.ToString();
                    logData += "," + got.transform.rotation.w.ToString() + "," + got.transform.rotation.x.ToString() + "," + got.transform.rotation.y.ToString() + "," + got.transform.rotation.z.ToString();
                }
            }
   

            //
            // Log current data and clear before next run.
            //
            taskDataLogger.AppendData(logData);
            logData = "";

            // Update run time
            taskTime += Time.fixedDeltaTime;
        }
            



    }

    /// <summary>
    /// Handles procedures that occurs while the task is being executed (and not related to data logging).
    /// </summary>
    /// <returns></returns>
    public override void HandleInTaskBehaviour()
    {
        HudManager.colour = HUDManager.HUDColour.Blue;
    }

    /// <summary>
    /// Checks whether the task has be successfully completed or not.
    /// </summary>
    /// <returns>True if the task has been successfully completed.</returns>
    public override bool IsTaskDone()
    {
        // You can implement whatever condition you want, maybe touching an object in the virtual world or being in a certain posture.

        //gridManager.SelectedTouched && !hasReached
        if ( (buttonAction.GetStateDown(SteamVR_Input_Sources.Any) || Input.GetKeyDown(KeyCode.F2)) || taskTime >= maxTaskTime  && !hasReached) //Input.GetKey(KeyCode.DownArrow)
        {
            doneTime = taskTime;
            //audio.clip = holdAudioClip;
            //audio.Play();
            Debug.Log("Ite:" + iterationNumber + ". Task done. t=" + doneTime.ToString() + ".");
            StartCoroutine(EndTaskCoroutine());
        }
            
        return taskComplete;
    }


    /// <summary>
    /// Raises the complete task flag after 1 second to allow for additional data to be gathered
    /// after the subject touches the selected sphere.
    /// </summary>
    /// <returns></returns>
    private IEnumerator EndTaskCoroutine()
    {
        hasReached = true;
        // Signal the subject that the task is done
        //HudManager.DisplayText("Hold on for a while!!");
        //HudManager.colour = HUDManager.HUDColour.Green;
        //HudManager.colour = HUDManager.HUDColour.Green;
        yield return new WaitForSecondsRealtime(holdingTime);

        taskComplete = true;
    }

    /// <summary>
    /// Handles procedures that occur as soon as the task is completed.
    /// Extend this method by doing your own implementation, with base.HandleTaskCompletion() being called at the start.
    /// </summary>
    public override void HandleTaskCompletion()
    {
        // Stop data reading and save data
        startRecording = false;

        if (delsysEMGEnable)
            delsysEMG.StopRecording();

        bool successFA = false;
        bool successW = false;
        if(foreArmBandFMGEnable)
            successFA =  foreArmBandFMG.StopRecording();
        if(wristBandFMGEnable)
            successW = wristBandFMG.StopRecording();

        

       
        // Reset flags
        hasReached = false;
        taskStarted = false;
        taskComplete = false;
        startAudioPlayed = false;
        nextAudioPlayed = false;

        base.HandleTaskCompletion();

        if (foreArmBandFMGEnable || foreArmBandFMGEnable)
        {
            // Restart the iteration if the data is not recored properly
            if (!successFA || !successW)
            {
                Debug.LogWarning("Data point not enought, redo the iteration!");
                failIteration = true;
                //HandleFailIteration();
                return;
            }
        }
    
        

        failIteration = false;
        
    }

    /// <summary>
    /// Handles procedures that occur as soon as the task is completed.
    /// </summary>
    private void HandleFailIteration()
    {
        hasReached = false;
        taskComplete = false;
        if (iterationNumber == 1)
        {
            foreArmBandFMG.SetOffset = true;
            wristBandFMG.SetOffset = true;
        }
        logData = "";
        iterationNumber--;
        Debug.Log(iterationNumber);
        failIteration = true;
        HandleIterationInitialisation();
    }

    /// <summary>
    /// Handles the procedures performed when analysing results.
    /// </summary>
    public override void HandleResultAnalysis()
    {
        // Do some analysis

        if (!failIteration)
        {
            string iterationResults = iterationNumber + "," + targetOrder[iterationNumber - 1] + "," + poseListManager.GetPoseName(targetOrder[iterationNumber - 1]) + "," + doneTime.ToString();
            // Log results
            performanceDataLogger.AppendData(iterationResults);
            performanceDataLogger.SaveLog();
        }
        
    }

    /// <summary>
    /// Handles procedures performed when initialising the next iteration.
    /// Updates iteration number, resets the task time, and starts a new data log by default.
    /// Extend this method by doing your own implementation, with base.HandleIterationInitialisation() being called at the start.
    /// </summary>
    public override void HandleIterationInitialisation()
    {

        //
        // Update iteration number and flow control
        //
        if(!failIteration)
            iterationNumber++;

        subStep = 1;
        taskTime = 0.0f;

        // 
        // Update log
        //
        taskDataLogger.AddNewLogFile(sessionNumber, iterationNumber, taskDataFormat);

        StartCoroutine(DisplayTaskText(targetOrder[iterationNumber - 1]));
        

    }

    /// <summary>
    /// Checks if the condition for changing experiment session has been reached.
    /// </summary>
    /// <returns>True if the condition for changing sessions has been reached.</returns>
    public override bool IsEndOfSession()
    {
        // You can do your own implementation of this
        if (!failIteration)
            return iterationNumber >= iterationsPerSession[sessionNumber - 1];
        else
            return false;
       
    }

    /// <summary>
    /// Handles procedures performed when initialising the next iteration.
    /// Updates iteration number, session number, resets the task time, and starts a new data log by default.
    /// Extend this method by doing your own implementation, with base.HandleSessionInitialisation() being called at the start.
    /// </summary>
    public override void HandleSessionInitialisation()
    {


        base.HandleSessionInitialisation();

        #region Iteration settings
        // Set iterations variables for flow control.
        targetNumber = poseListManager.TargetNumber;
        iterationsPerSession[sessionNumber - 1] = targetNumber * iterationsPerTarget;


        // Create the list of target indexes and shuffle it.
        targetOrder.Clear(); //clear the target order
        for (int i = 1; i < targetNumber; i++)
        {
            for (int j = 0; j < iterationsPerTarget; j++)
            {
                targetOrder.Add(i);
                Debug.Log(targetOrder[targetOrder.Count - 1]);
            }


        }


        targetOrder.Shuffle();

        targetOrder.Insert(0, 0);

        if (foreArmBandFMGEnable)
            foreArmBandFMG.SetOffset = true;
        if (wristBandFMGEnable)
            wristBandFMG.SetOffset = true;

        // New file for the performance data logger
        performanceDataLogger.CloseLog();
        performanceDataLogger.AddNewLogFile(AvatarSystem.AvatarType.ToString(), sessionNumber, performanceDataFormat); // Add file

        // Display task text
        StartCoroutine(DisplayTaskText(targetOrder[0]));


        if (debug)
        {
            foreach (int target in targetOrder)
            {
                Debug.Log("Pose:" + target);
            }
        }
        #endregion




    }

    /// <summary>
    /// Checks if the condition for ending the experiment has been reached.
    /// </summary>
    /// <returns>True if the condition for ending the experiment has been reached.</returns>
    public override bool IsEndOfExperiment()
    {
        // You can do your own implementation of this
        return base.IsEndOfExperiment();
    }

    /// <summary>
    /// Performs all the required procedures to end the experiment.
    /// Closes all UPD Sensors and all logs by default.
    /// Extend this method by doing your own implementation, with base.EndExperiment() being called at the start.
    /// </summary>
    public override void EndExperiment()
    {
        base.EndExperiment();
        if (delsysEMGEnable)
        {
            delsysEMG.StopAcquisition();
            delsysEMG.Close();
        }
        if (foreArmBandFMGEnable)
            foreArmBandFMG.StopAcquisition();
        if (wristBandFMGEnable)
            wristBandFMG.StopAcquisition();


        // You can do your own end of experiment stuff here
    }

    /// <summary>
    /// Checks if the condition for the rest period has been reached.
    /// </summary>
    /// <returns>True if the rest condition has been reached.</returns>
    public override bool IsRestTime()
    {
        // For example, give rest time after the fifth iteration.
        return iterationNumber % RestIterations == 0;
    }

    /// <summary>
    /// Avoid unity freeze
    /// </summary>
    /// <returns></returns>
    private void OnApplicationQuit()
    {
        //ZMQSystem.AddPushData(zmq, new float[] { 0.0f });
        //ZMQSystem.CloseZMQSocket(zmqPort, ZMQSystem.SocketType.Pusher);
        if (delsysEMGEnable)
        {
            delsysEMG.StopAcquisition();
            delsysEMG.Close();
        }
        if (foreArmBandFMGEnable)
            foreArmBandFMG.StopAcquisition();
        if(wristBandFMGEnable)
            wristBandFMG.StopAcquisition();
        ExperimentSystem.CloseAllExperimentLoggers();
        //NetMQConfig.Cleanup(false);
    }
}

    #endregion

   
