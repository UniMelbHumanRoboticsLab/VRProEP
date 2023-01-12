// System
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;

// Unity
using UnityEngine;
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

public class AugmentedFeedback2022 : GameMaster
{
    // Here you can place all your Unity (GameObjects or similar)
    #region Unity objects
    [SerializeField]
    //private string ablebodiedDataFormat = "loc,t,Tfe,Tabd,Tr,Scde,Scpr,Sfe,Sabd,Sr,aDotE,bDotE,gDotE,aE,bE,gE,xE,yE,zE,aDotUA,bDotUA,gDotUA,aUA,bUA,gUA,xUA,yUA,zUA,aDotSH,bDotSH,gDotSH,aSH,bSH,gSH,xSH,ySH,zSH,aDotUB,bDotUB,gDotUB,aUB,bUB,gUB,xUB,yUB,zUB,xHand,yHand,zHand,aHand,bHand,gHand";
    private string ablebodiedDataFormat = "loc,t,Tfe,Tabd,Tr,Scde,Scpr,Sfe,Sabd,Sr,Efe,Wps,aDotE,bDotE,gDotE,aE,bE,gE,xE,yE,zE,aDotUA,bDotUA,gDotUA,aUA,bUA,gUA,xUA,yUA,zUA,aDotSH,bDotSH,gDotSH,aSH,bSH,gSH,xSH,ySH,zSH,aDotUB,bDotUB,gDotUB,aUB,bUB,gUB,xUB,yUB,zUB,errorX,errorY,errorZ,errorAng";
    [SerializeField]
    private string transhumeralDataFormat = "loc,t,Tfe,Tabd,Tr,Scde,Scpr,Sfe,Sabd,Sr,Efe,Wps,aDotE,bDotE,gDotE,aE,bE,gE,xE,yE,zE,aDotUA,bDotUA,gDotUA,aUA,bUA,gUA,xUA,yUA,zUA,aDotSH,bDotSH,gDotSH,aSH,bSH,gSH,xSH,ySH,zSH,aDotUB,bDotUB,gDotUB,aUB,bUB,gUB,xUB,yUB,zUB,pxHand,pyHand,pzHand,paHand,pbHand,pgHand,pEfe,pDotEfe,pWps,pDotWps,errorX,errorY,errorZ,errorAng";
    [SerializeField]
    private string performanceDataFormat = "i,loc,t_f,qt_sfe,qt_saa,qt_sr,qt_efe,qt_wps,qt_wfe,qt_waa";

    

    [Header("Experiment configuration: Start position")]
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

   
    [Header("Tasks")]
    [SerializeField]
    private TargetPoseGridManager gridManager;
    [SerializeField]
    private int[] iterationsPerTarget;
    [SerializeField]
    private int iterationBatchSize;
    [SerializeField]
    // Time to hold the final pose
    private float holdingTime;
    [SerializeField]
    // Max time to reach
    private float maxTaskTime;

    [Header("Media")]
    [SerializeField]
    private GameObject startPosPhoto;
    [SerializeField]
    private AudioClip holdAudioClip;
    [SerializeField]
    private AudioClip returnAudioClip;
    [SerializeField]
    private AudioClip nextAudioClip;
    [SerializeField]
    private AudioClip testAudioClip;

    [Header("Avatar option")]
    [SerializeField]
    private bool amputeeAvatar;
    [SerializeField]
    private bool controlllerHand;
    [SerializeField]
    private bool avatarCalibration;

    [Header("Flags")]
    // If allow using controller to complete tasks
    [SerializeField]
    private bool dummyControlEnable;

    [SerializeField]
    private bool fullTrackerEnable;
   
    // Delsys EMG background data collection
    [SerializeField]
    private bool delsysEnable;
    private DelsysEMG delsysEMG = new DelsysEMG();

    // Push the motion tracker data to other platform through ZMQ
    [SerializeField]
    private bool zmqPushEnable;
    private readonly int zmqPushPort = ZMQ_PUSH_PORT;

    // Pull the joint reference from e.g. Matlab
    [SerializeField]
    private bool zmqPullEnable;
    private readonly int zmqPullPort = MLKinematicSynergy.ZMQ_PULL_PORT;

    // Request matlab interface output through ZMQ:
    [SerializeField]
    private bool zmqReqEnable;
    private readonly int zmqReqPort = ZMQ_REQ_PORT;


    [SerializeField]
    private bool skipXR;
    [SerializeField]
    private bool checkStartPosition;

    #endregion

    // Additional data logging
    private DataStreamLogger performanceDataLogger;
    private float iterationDoneTime;
    private float feedbackScore;



    // Prosthesis handling objects
    private GameObject prosthesisManagerGO;
    private ConfigurableMultiJointManager multiJointManager;

    // Target management variables
    private float[] sfePose;
    private float[] efePose;
    private float[] wpsPose;
    private int targetNumber; // The total number of targets
    private List<int> targetOrder = new List<int>(); // A list of target indexes ordered for selection over iterations in a session.

    // Motion tracking for experiment management and adaptation (check for start position)
    private VIVETrackerManager upperArmTracker;
    private VIVETrackerManager lowerArmTracker;
    private VIVETrackerManager shoulderTracker;
    private VIVETrackerManager c7Tracker;
    private VirtualPositionTracker handTracker;

    private Quaternion c7InitOrient;
    private Vector3 c7InitPos;
    private Quaternion shInitOrient;
    private Vector3 shInitPos;
    //private List<Quaternion> initialOrientation = new List<Quaternion>();
    //private List<Vector3> initialPosition = new List<Vector3>();



    // Flow control
    private bool loopTraining = true;
    private bool inPractice = false;
    private bool hasReached = false;
    private bool taskComplete = false;
    private bool emgIsRecording = false;

    // Lefty subject sign
    private float leftySign = 1.0f;

    // Audio
    AudioSource audio;

    // Communication constants
    public const float PUSH_ENABLE = -1.0f;
    public const float ITE_RESET = -2.0f;

    protected SteamVR_Action_Boolean padAction = SteamVR_Input.GetAction<SteamVR_Action_Boolean>("InterfaceEnableButton");

    #region Private methods
    private string ConfigEMGFilePath()
    {
        //string emgDataFilename =  Path.Combine(taskDataLogger.ActiveDataPath, "session_" + sessionNumber , "i" + "_" + iterationNumber + "EMG.csv");
        string emgDataFilename = taskDataLogger.ActiveDataPath + "/session_" + sessionNumber + "/i" + "_" + iterationNumber + "EMG.csv";
        return emgDataFilename;
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

    private int selectIdx = 0;


    #region Dynamic configuration

    //
    // Configuration class:
    // Modify this class to be able to configure your experiment from a configuration file
    //
    private class AugmentedFeedbackConfigurator
    {
        public int[] iterationsPerTarget = {2};
        public int iterationBatchSize = 3;
        public int initIteration = 1;
        public float holdingTime = 0.5f;
        public float maxTaskTime = 100.0f;
        public float[] sfePose;
        public float[] efePose;
        public float[] wpsPose;
    }
    private AugmentedFeedbackConfigurator configurator;

    //
    // Configuration class:
    // Modify this class to be able to output the customised settings to a configuration file
    //
    private class CustomisedConfigurator
    {
        public List<int> targetOrder;
        public Vector3 shoulderCentreOffset;
        public Vector3 residualFollowerPosOffset;
        public Vector3 residualFollowerAngOffset;
        public float uarmLengthOffset;
        public float farmLengthOffset;
        public float handLengthOffset;
    }
    private CustomisedConfigurator customConfigurator;

    #endregion

    // Here are all the methods you need to write for your experiment.
    #region GameMaster Inherited Methods

    protected override void FixedUpdate()
    {

        // Choose if to try the training again
        if (GetCurrentStateName() == State.STATE.TRAINING && inPractice)
        {
            if (buttonAction.GetStateDown(SteamVR_Input_Sources.Any))
                loopTraining = true;
            else if (padAction.GetStateDown(SteamVR_Input_Sources.Any))
                loopTraining = false;
        }

        // Override fixed update to start the emg recording when the start performing the task
        if ( GetCurrentStateName() == State.STATE.PERFORMING_TASK  && delsysEnable && !emgIsRecording )
        {
            
            delsysEMG.StartRecording(ConfigEMGFilePath());
            emgIsRecording = true;
        }

        if (Input.GetKeyDown(KeyCode.UpArrow) && fullTrackerEnable) //Input.GetKeyDown(KeyCode.UpArrow) padAction.GetStateDown(SteamVR_Input_Sources.Any)
        {
            // Record initial frames
            c7InitOrient = c7Tracker.GetTrackerTransform().rotation;
            c7InitPos = c7Tracker.GetTrackerTransform().position;

            shInitOrient = shoulderTracker.GetTrackerTransform().rotation;
            shInitPos = shoulderTracker.GetTrackerTransform().position;

            Debug.Log("Recalibrated the poses!");
        }

        base.FixedUpdate();

    }
    // Place debug stuff here, for when you want to test the experiment directly from the world without 
    // having to load it from the menus.
    private void Awake()
    {
        if (debug)
        {
            //
            // Debug using the test bot
            //

            SaveSystem.LoadUserData("TB1995175"); // Load the test/demo user (Mr Demo)
            //SaveSystem.LoadUserData("HL1996178");
            //SaveSystem.LoadUserData("RW1995169");
            //SaveSystem.LoadUserData("XG1995174");
            ///SaveSystem.LoadUserData("XY1993188");
            //SaveSystem.LoadUserData("ML1996175");
            //SaveSystem.LoadUserData("JL1993177");


            Debug.Log("Load Avatar to Debug.");

            if (amputeeAvatar)
            {
                AvatarSystem.LoadPlayer(UserType.Ablebodied, AvatarType.Transhumeral);
                AvatarSystem.LoadAvatar(SaveSystem.ActiveUser, AvatarType.Transhumeral);
               
                // Prosthesis manager
                prosthesisManagerGO = GameObject.FindGameObjectWithTag("ProsthesisManager");
                multiJointManager = prosthesisManagerGO.AddComponent<ConfigurableMultiJointManager>();

                // Initialise the forearm tracker first
                if (SaveSystem.ActiveUser.type == UserType.Ablebodied)
                {
                    GameObject llMotionTrackerGO = GameObject.FindGameObjectWithTag("ForearmTracker");
                    lowerArmTracker = new VIVETrackerManager(llMotionTrackerGO.transform);
                }
                
                // Initialise prosthesis
                multiJointManager.InitializeProsthesis(SaveSystem.ActiveUser.upperArmLength, (SaveSystem.ActiveUser.forearmLength + SaveSystem.ActiveUser.handLength / 2.0f));


            }
            else
            {
                AvatarSystem.LoadPlayer(SaveSystem.ActiveUser.type, AvatarType.AbleBodied);
                AvatarSystem.LoadAvatar(SaveSystem.ActiveUser, AvatarType.AbleBodied,true,controlllerHand);
            }
            

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
        string expCode = "";
        if (AvatarSystem.AvatarType == AvatarType.AbleBodied)
            expCode = "_A";
        else if (AvatarSystem.AvatarType == AvatarType.Transhumeral)
            expCode = "_P";

        if (debug)
            configAsset = Resources.Load<TextAsset>("Experiments/" + this.gameObject.name + expCode);
        else
            configAsset = Resources.Load<TextAsset>("Experiments/" + ExperimentSystem.ActiveExperimentID + expCode);


        // Convert configuration file to configuration class.
        configurator = JsonUtility.FromJson<AugmentedFeedbackConfigurator>(configAsset.text);

        // Load from config file
        holdingTime = configurator.holdingTime;
        maxTaskTime = configurator.maxTaskTime;
        iterationsPerTarget = configurator.iterationsPerTarget;
        iterationBatchSize = configurator.iterationBatchSize;
        sfePose = configurator.sfePose;
        efePose = configurator.efePose;
        wpsPose = configurator.wpsPose;
        // Load from config file
        for (int i = 0; i <= iterationsPerTarget.Length - 1; i++)
        {
            iterationsPerSession.Add(0);
        }
        Debug.Log("Size of iterationperTarget: " + iterationsPerTarget.Length);

        
        

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
        if (AvatarSystem.AvatarType == AvatarType.AbleBodied)
            taskDataFormat = ablebodiedDataFormat;
        else if (AvatarSystem.AvatarType == AvatarType.Transhumeral)
            taskDataFormat = transhumeralDataFormat;

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
        sessionNumber = 1;
        
        if(configurator.initIteration % configurator.iterationBatchSize == 0)
            iterationNumber = configurator.initIteration - configurator.iterationBatchSize - 1;
        else
            iterationNumber = configurator.initIteration - (configurator.initIteration % configurator.iterationBatchSize - 1);
        

        //
        // Create the default data loggers
        //
        taskDataLogger = new DataStreamLogger("TaskData/" + AvatarSystem.AvatarType.ToString());
        ExperimentSystem.AddExperimentLogger(taskDataLogger);
        taskDataLogger.AddNewLogFile(sessionNumber, iterationNumber, taskDataFormat); // Add file

        //
        // Create the performance data loggers
        //
        performanceDataLogger = new DataStreamLogger("PerformanceData");
        ExperimentSystem.AddExperimentLogger(performanceDataLogger);
        performanceDataLogger.AddNewLogFile(AvatarSystem.AvatarType.ToString(), sessionNumber, performanceDataFormat); // Add file

        // Send the player to the experiment centre position
        TeleportToStartPosition();
        #endregion



        #region Initialize world positioning

        // Set the subject physiological data for grid 
        //gridManager.ConfigUserData();


        #endregion

        #region  Initialize motion sensors
        //
        // Add arm motion trackers for able-bodied case.
        //
        // Lower limb motion tracker
        if (AvatarSystem.AvatarType != AvatarType.Transhumeral)
        {
            // Lower limb motion tracker
            GameObject llMotionTrackerGO = GameObject.FindGameObjectWithTag("ForearmTracker");
            lowerArmTracker = new VIVETrackerManager(llMotionTrackerGO.transform);
            ExperimentSystem.AddSensor(lowerArmTracker);

            // Upper limb motion tracker
            GameObject ulMotionTrackerGO = AvatarSystem.AddMotionTracker();
            upperArmTracker = new VIVETrackerManager(ulMotionTrackerGO.transform);
            ExperimentSystem.AddSensor(upperArmTracker);

            if (fullTrackerEnable)
            {
                // Shoulder acromium head tracker
                GameObject shMotionTrackerGO = AvatarSystem.AddMotionTracker();
                shoulderTracker = new VIVETrackerManager(shMotionTrackerGO.transform);
                ExperimentSystem.AddSensor(shoulderTracker);
                Debug.Log("Shoulder acromion tracker registerd number:" + shoulderTracker.TrackerNumber + " Total number: " + lowerArmTracker.TotalTrackerNumber);
                // C7 tracker
                GameObject c7MotionTrackerGO = AvatarSystem.AddMotionTracker();
                c7Tracker = new VIVETrackerManager(c7MotionTrackerGO.transform);
                ExperimentSystem.AddSensor(c7Tracker);
                Debug.Log("Trunk tracker registerd number:" + c7Tracker.TrackerNumber + " Total number: " + lowerArmTracker.TotalTrackerNumber);
            }

        }
        else
        {
            // Forearm tracker
            ExperimentSystem.AddSensor(lowerArmTracker);
            Debug.Log("Forearm tracker registerd number:" + lowerArmTracker.TrackerNumber + " Total number: " + lowerArmTracker.TotalTrackerNumber);

            // Get active sensors from avatar system and get the vive tracker being used for the UA
            foreach (ISensor sensor in AvatarSystem.GetActiveSensors())
            {
                if (sensor is VIVETrackerManager)
                    upperArmTracker = (VIVETrackerManager)sensor;
            }
            if (upperArmTracker == null)
                throw new System.NullReferenceException("The residual limb tracker was not found.");
            ExperimentSystem.AddSensor(upperArmTracker);
            Debug.Log("Upperarm tracker registerd number:" + upperArmTracker.TrackerNumber + " Total number: " + upperArmTracker.TotalTrackerNumber);

            // Set VIVE tracker and Linear synergy as active.
            // Get prosthesis
            if (zmqPullEnable)
                // Set the reference generator to linear synergy.
                multiJointManager.ChangeReferenceGenerator("VAL_REFGEN_MLKINSYN");
            else
            {
                multiJointManager.AddSensor(upperArmTracker);
                multiJointManager.ChangeReferenceGenerator("VAL_REFGEN_LINKINSYN");
                multiJointManager.SetSynergy(100.0f/60.0f);
            }

            if (fullTrackerEnable)
            {
                // Shoulder acromium head tracker
                GameObject shMotionTrackerGO = AvatarSystem.AddMotionTracker();
                shoulderTracker = new VIVETrackerManager(shMotionTrackerGO.transform);
                ExperimentSystem.AddSensor(shoulderTracker);
                Debug.Log("Shoulder acromion tracker registerd number:" + shoulderTracker.TrackerNumber + " Total number: " + lowerArmTracker.TotalTrackerNumber);
                // C7 tracker
                GameObject c7MotionTrackerGO = AvatarSystem.AddMotionTracker();
                c7Tracker = new VIVETrackerManager(c7MotionTrackerGO.transform);
                ExperimentSystem.AddSensor(c7Tracker);
                Debug.Log("Trunk tracker registerd number:" + c7Tracker.TrackerNumber + " Total number: " + lowerArmTracker.TotalTrackerNumber);
            }

            // Hand tracking sensor
            GameObject handGO = GameObject.FindGameObjectWithTag("Bottle");
            handTracker = new VirtualPositionTracker(handGO.transform);
            ExperimentSystem.AddSensor(handTracker);
        }
       


        #endregion

        #region  Initialize zmq communication

        //ZMQ
        if (zmqPushEnable)
            ZMQSystem.AddZMQSocket(zmqPushPort, ZMQSystem.SocketType.Pusher);

        if (zmqPullEnable)
        {
            ZMQSystem.AddZMQSocket(zmqPullPort, ZMQSystem.SocketType.Puller);
            ZMQSystem.AddPushData(zmqPushPort, new float[] { PUSH_ENABLE }); // Tell the pusher the puller is ready
        }
            

        if (zmqReqEnable)
            ZMQSystem.AddZMQSocket(zmqReqPort, ZMQSystem.SocketType.Requester);



        #endregion

        #region Initialize EMG sensors
        //Initialse Delsys EMG sensor
        if (delsysEnable)
        {
            delsysEMG.Init();
            delsysEMG.Connect();
            delsysEMG.StartAcquisition();
        }


        #endregion



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

        if (!avatarCalibration)
        {
            HudManager.DisplayText("Look to the top left. Instructions will be displayed there.");
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
            InstructionManager.DisplayText("Today, the experiment will require you to match the position and orientation of the bottles in front of you" + "\n\n (Press the trigger)");
            yield return WaitForSubjectAcknowledgement(); // And wait for the subject to cycle through them.
            InstructionManager.DisplayText("You will do 2 sessions which woudl take about an hour " + "\n\n (Press the trigger)");
            yield return WaitForSubjectAcknowledgement(); // And wait for the subject to cycle through them.
            InstructionManager.DisplayText("A" + this.RestTime + "sec rest occurs every" + this.RestIterations + "iterations" + "\n\n (Press the trigger)");
            yield return WaitForSubjectAcknowledgement(); // And wait for the subject to cycle through them.
        }
       
        //
        // Calibration
        InstructionManager.DisplayText("Before start, we need to do some clibration" + "\n\n (Press the trigger)");
        yield return WaitForSubjectAcknowledgement(); // And wait for the subject to cycle through them.
        InstructionManager.DisplayText("Please stand upright and relax your upper limb.");
        Debug.Log("Press Up key to record calibration pose.");
        yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.UpArrow));
        //yield return new WaitUntil(() => padAction.GetStateDown(SteamVR_Input_Sources.Any));

        if (fullTrackerEnable)
        {
            // Record initial frames
            c7InitOrient = c7Tracker.GetTrackerTransform().rotation;
            c7InitPos = c7Tracker.GetTrackerTransform().position;

            shInitOrient = shoulderTracker.GetTrackerTransform().rotation;
            shInitPos = shoulderTracker.GetTrackerTransform().position;
        }


        // Start
        Debug.Log("Press Down key to start.");
        yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.DownArrow));
        //yield return new WaitUntil(() => buttonAction.GetStateDown(SteamVR_Input_Sources.Any));
        if (delsysEnable & zmqPushEnable)
            delsysEMG.SetZMQPusher(true);

       

        welcomeDone = true;

        #region Debug the zmq communications
        /*
        Debug.Log("Press Up key.");
        yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.UpArrow));
        delsysEMG.SetZMQPusher(false);

        Debug.Log("Press Down key.");
        yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.DownArrow));
        delsysEMG.SetZMQPusher(true);

        Debug.Log("Press Up key.");
        yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.UpArrow));
        zmqRequester.newData(new float[] { 1.0f });
        yield return new WaitUntil(() => zmqRequester.ReceivedResponseFlag);
        double[] response = zmqRequester.GetReceiveData();
        Debug.Log("Feedback Score: " + response[0]);

        Debug.Log("Press Down key.");
        yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.DownArrow));
        zmqRequester.newData(new float[] { 1.0f });
        yield return new WaitUntil(() => zmqRequester.ReceivedResponseFlag);
        response = zmqRequester.GetReceiveData();
        Debug.Log("Feedback Score: " + response[0]);
        */
        #endregion


        #region For simple reaching video recording
        /*
         for (int i = 1; i < 10; i++)
         {
             gridManager.SelectTarget(7);
             HudManager.DisplayText("Well done (you can return to start position)!");
             yield return WaitForSubjectAcknowledgement(); // And wait for the subject to cycle through them.
             hasReached = false;
             // For video
             HudManager.colour = HUDManager.HUDColour.Orange;
             HUDCountDown(1);
             yield return new WaitUntil(() => CountdownDone); // And wait 
             InstructionManager.DisplayText("Reach it!");
             HudManager.DisplayText("Reach it!");
             HudManager.colour = HUDManager.HUDColour.Blue;
             yield return new WaitUntil(() => IsTaskDone());
             // Signal the subject that the task is done

             //HudManager.DisplayText("Hold on your current position!");
             //yield return new WaitForSecondsRealtime(1.0f);
             audio.clip = returnAudioClip;
             audio.Play();
             HudManager.colour = HUDManager.HUDColour.Red;
             HudManager.DisplayText("Well done (you can return to start position)!");

             hasReached = false;
             taskComplete = false;
             gridManager.ResetTargetSelection();

         }
         */
        #endregion



        // Now that you are done, set the flag to indicate we are done.

    }



    /// <summary>
    /// Performs initialisation procedures for the experiment. Sets variables to their zero state.
    /// </summary>
    public override void InitialiseExperiment()
    {
        //Spwan grid
        #region Spawn grid
        bool hasConfigured = false;
        // Convert customised setting file to configuration class. 
        string fileName = SaveSystem.ActiveSaveFolder + "/" + ExperimentSystem.ActiveExperimentID + "/customised_settings.json";
        if (File.Exists(fileName))
        {
            hasConfigured = true;
            string json = File.ReadAllText(fileName);
            customConfigurator = JsonUtility.FromJson<CustomisedConfigurator>(json);
           
            gridManager.ShoulderCentreOffset = customConfigurator.shoulderCentreOffset;
            gridManager.FarmLengthOffset = customConfigurator.farmLengthOffset;
            gridManager.UarmLengthOffset = customConfigurator.uarmLengthOffset;
            gridManager.HandLengthOffset = customConfigurator.handLengthOffset;

            // If prosthesis avatar, we also need to set the prosthesis offset
            if (AvatarSystem.AvatarType == AvatarType.Transhumeral)
            {
                GameObject tempResidualGO = GameObject.FindGameObjectWithTag("ResidualLimbAvatar");
                LimbFollower follower = tempResidualGO.GetComponent<LimbFollower>();
                follower.offset = customConfigurator.residualFollowerPosOffset;
                follower.angularOffset = customConfigurator.residualFollowerAngOffset;
            }
            Debug.Log("Load customised experiment settings");
        }
            
        gridManager.AddJointPose(TargetPoseGridManager.SFE_POSE, sfePose);
        gridManager.AddJointPose(TargetPoseGridManager.EFE_POSE, efePose);
        gridManager.AddJointPose(TargetPoseGridManager.WPS_POSE, wpsPose);
        
        gridManager.CombJointPose(new string[] { TargetPoseGridManager.SFE_POSE, TargetPoseGridManager.EFE_POSE, TargetPoseGridManager.WPS_POSE });

        gridManager.SpawnTargetGrid();
        Debug.Log("Spawn the grid!");
        #endregion



        #region Iteration settings
        // Set iterations variables for flow control.
        targetNumber = gridManager.TargetNumber;
        Debug.Log("Total target number: " + targetNumber);
        iterationsPerSession[sessionNumber-1] = targetNumber * iterationsPerTarget[sessionNumber-1];
        if (!hasConfigured || customConfigurator.targetOrder.Count != targetNumber * iterationsPerTarget[sessionNumber - 1])
        {
            // Create the list of target indexes and shuffle it.
            List<int> tempOrder = new List<int>();
            for (int i = 0; i < targetNumber; i++)
            {
                for (int j = 0; j < iterationsPerTarget[sessionNumber - 1]; j++)
                {
                    tempOrder.Add(i);

                }
            }

            targetOrder = gridManager.SequentialRandomise(tempOrder, iterationBatchSize);
        }
        else
        {
            targetOrder = new List<int>(customConfigurator.targetOrder);
        }
        

        Debug.Log("Total trials:" + targetOrder.Count);
        Debug.Log("Current target pose number: " + targetOrder[iterationNumber - 1]);
        Debug.Log("Target order:" + string.Join(",", targetOrder));
        #endregion
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

        gridManager.CalibrationPose();
        Debug.Log("Calibrate the avatar. Press up arrow to continue.");
        InstructionManager.DisplayText("We also neeed to calibrate your avatar. Relax your shoulder and bend your elbow like the white and blue lines show." );
        yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.UpArrow));
        //yield return WaitForSubjectAcknowledgement(); // And wait for the subject to cycle through them.
        Debug.Log("Calibration done.");

        // Save the customised settings
        customConfigurator = new CustomisedConfigurator();
        customConfigurator.targetOrder = new List<int>(targetOrder);
        customConfigurator.shoulderCentreOffset = gridManager.ShoulderCentreOffset;
        customConfigurator.farmLengthOffset = gridManager.FarmLengthOffset;
        customConfigurator.uarmLengthOffset = gridManager.UarmLengthOffset;
        customConfigurator.handLengthOffset = gridManager.HandLengthOffset;
        if (AvatarSystem.AvatarType == AvatarType.Transhumeral)
        {
            GameObject tempResidualGO = GameObject.FindGameObjectWithTag("ResidualLimbAvatar");
            LimbFollower follower = tempResidualGO.GetComponent<LimbFollower>();
            customConfigurator.residualFollowerPosOffset = follower.offset;
            customConfigurator.residualFollowerAngOffset = follower.angularOffset;
        }
        

        // Covert the setting class to json
        string json = JsonUtility.ToJson(customConfigurator);
        string fileName = SaveSystem.ActiveSaveFolder + "/" + ExperimentSystem.ActiveExperimentID + "/customised_settings.json";
        File.WriteAllText(fileName, json);

        // Done calibration
        InstructionManager.DisplayText("All done! Thanks! Press trigger to continue" + "\n\n (Press the trigger)");
        yield return WaitForSubjectAcknowledgement(); // And wait for the subject to cycle through them.

        if (!avatarCalibration)
        {
            //Instructions
            if (AvatarSystem.AvatarType == AvatarType.AbleBodied) // Able-bodied session
            {
                InstructionManager.DisplayText("Alright, the bottle targets should have spawned for you." + "\n\n (Press the trigger)");
                yield return WaitForSubjectAcknowledgement(); // And wait for the subject to cycle through them.
                InstructionManager.DisplayText("In the 1st session, you will need to match the position and oriention of the bottles in front of you with the bottle in your hand." + "\n\n (Press the trigger)");
                yield return WaitForSubjectAcknowledgement(); // And wait for the subject to cycle through them.
                InstructionManager.DisplayText("If you are ready, let's start training!" + "\n\n (Press the trigger)");
                yield return WaitForSubjectAcknowledgement(); // And wait for the subject to cycle through them.

            }
            else if (AvatarSystem.AvatarType == AvatarType.Transhumeral) // second session
            {
                InstructionManager.DisplayText("You've finished the 1st session, well done. Let's start the 2nd session" + "\n\n (Press the trigger)");
                yield return WaitForSubjectAcknowledgement(); // And wait for the subject to cycle through them.
                InstructionManager.DisplayText("Now, you need to complete the same task, but the virtual forearm will not follow yours but controlled by some algorithm ." + "\n\n (Press the trigger)");
                yield return WaitForSubjectAcknowledgement(); // And wait for the subject to cycle through them.
                InstructionManager.DisplayText("If you are ready, let's start training!" + "\n\n (Press the trigger)");
                yield return WaitForSubjectAcknowledgement(); // And wait for the subject to cycle through them.
            }
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

        Debug.Log("Training Start.");

        InstructionManager.DisplayText("Press trigger to continue" + "\n\n (Press the trigger)");
        yield return WaitForSubjectAcknowledgement(); // And wait for the subject to cycle through them.

        //trainingDone = true;

        if (AvatarSystem.AvatarType == AvatarType.AbleBodied)
        {

            InstructionManager.DisplayText("Let's start training then!" + "\n\n (Press the trigger)");
            yield return WaitForSubjectAcknowledgement(); // And wait for the subject to cycle through them.


            //
            // HUD Colours
            InstructionManager.DisplayText("First, let's tell you about the colour of the HUD. It will tell you what you need to do." + "\n\n (Press the trigger)");
            yield return WaitForSubjectAcknowledgement(); // And wait for the subject to cycle through them.
            InstructionManager.DisplayText("<Red>: return relax." + "\n\n (Press the trigger)");
            HudManager.DisplayText("I'm red!");
            HudManager.colour = HUDManager.HUDColour.Red;
            yield return WaitForSubjectAcknowledgement(); // And wait for the subject to cycle through them.
            InstructionManager.DisplayText("<Orange>: wait for countdown." + "\n\n (Press the trigger)");
            HudManager.DisplayText("I'm orange!");
            HudManager.colour = HUDManager.HUDColour.Orange;
            yield return WaitForSubjectAcknowledgement(); // And wait for the subject to cycle through them.
            InstructionManager.DisplayText("<Blue>: reach!" + "\n\n (Press the trigger)");
            HudManager.DisplayText("I'm blue!");
            HudManager.colour = HUDManager.HUDColour.Blue;
            yield return WaitForSubjectAcknowledgement(); // And wait for the subject to cycle through them.
            InstructionManager.DisplayText("You need to hold on you final position for a while until you hear return or next and HUD says 'Well Done'." + "\n\n (Press the trigger)");
            HudManager.colour = HUDManager.HUDColour.Red;
            HudManager.DisplayText("Well Done");
            yield return WaitForSubjectAcknowledgement(); // And wait for the subject to cycle through them.

            HudManager.ClearText();
            HudManager.colour = HUDManager.HUDColour.Red;

            //
            // Reaching practice
            InstructionManager.DisplayText("The colour of the targets will tell you the status of the target." + "\n\n (Press the trigger)");
            yield return WaitForSubjectAcknowledgement(); // And wait for the subject to cycle through them.
            InstructionManager.DisplayText("<Blue>: the target is selected as the next target." + "\n\n (Press the trigger)");
            yield return WaitForSubjectAcknowledgement(); // And wait for the subject to cycle through them.
            InstructionManager.DisplayText("<Green>: you have successfully reached the selected one." + "\n\n (Press the trigger)");
            yield return WaitForSubjectAcknowledgement(); // And wait for the subject to cycle through them.


            //
            // Explain routine
            InstructionManager.DisplayText("Thanks for the attention, let's explain the task routine." + "\n\n (Press the trigger)");
            yield return WaitForSubjectAcknowledgement(); // And wait for the subject to cycle through them.
            InstructionManager.DisplayText("You will be asked to sequentially match the position and orientation of multiple bottle targets before returning to relax." + "\n\n (Press the trigger)");
            yield return WaitForSubjectAcknowledgement(); // And wait for the subject to cycle through them.
            InstructionManager.DisplayText("The group size in this experiment is: " + iterationBatchSize + "\n\n (Press the trigger)");
            yield return WaitForSubjectAcknowledgement(); // And wait for the subject to cycle through them.
            InstructionManager.DisplayText("Finally, when the target bottle turns green, hold on for a while, until you hear 'Return' or 'Next' and HUD says 'Well Done'." + "\n\n (Press the trigger)");
            yield return WaitForSubjectAcknowledgement(); // And wait for the subject to cycle through them.

            //
            // Important messages
            InstructionManager.DisplayText("Important: Keep your final reaching position when you hear 'Hold', like !!! " + "\n\n (Press the trigger to play)");
            yield return WaitForSubjectAcknowledgement(); // And wait for the subject to cycle through them.
            audio.clip = holdAudioClip;
            audio.Play(0);
            HudManager.DisplayText("Hold on for a while!");
            InstructionManager.DisplayText("Important: Keep your final reaching position when you hear 'Hold', like !!! " + "\n\n (Press the trigger to continue)");
            yield return WaitForSubjectAcknowledgement(); // And wait for the subject to cycle through them.

            InstructionManager.DisplayText("Important: Please not return until HUD says 'Well Done' and you hear 'Next', like !!! " + "\n\n (Press the trigger to play)");
            yield return WaitForSubjectAcknowledgement(); // And wait for the subject to cycle through them.
            audio.clip = nextAudioClip;
            audio.Play(0);
            InstructionManager.DisplayText("Important: Please not return until HUD says 'Well Done' and you hear 'Next', like !!! " + "\n\n (Press the trigger to continue)");
            yield return WaitForSubjectAcknowledgement(); // And wait for the subject to cycle through them.


            InstructionManager.DisplayText("Important: Please not return until HUD says 'Well Done' and you hear 'Return', like !!! " + "\n\n (Press the trigger to play)");
            yield return WaitForSubjectAcknowledgement(); // And wait for the subject to cycle through them.
            audio.clip = returnAudioClip;
            audio.Play(0);
            HudManager.DisplayText("Well done!");
            InstructionManager.DisplayText("Important: Please not return until HUD says 'Well Done' and you hear 'Return', like !!! " + "\n\n (Press the trigger to continue)");
            yield return WaitForSubjectAcknowledgement(); // And wait for the subject to cycle through them.


            //
            // Have a try
            InstructionManager.DisplayText("Thansk for the attention, let's try a task routine." + "\n\n (Press the trigger)");
            HudManager.ClearText();
            HudManager.colour = HUDManager.HUDColour.Red;
            yield return WaitForSubjectAcknowledgement(); // And wait for the subject to cycle through them.

            //
            // Start position
            InstructionManager.DisplayText("Before starting, your upper arm and elbow should point downards and relax." + "\n\n (Press the trigger)");
            yield return WaitForSubjectAcknowledgement(); // And wait for the subject to cycle through them.


            //Start practice
            InstructionManager.DisplayText("The bottle that you need to reach will turn blue. Don't reach now." + "\n\n (Press the trigger)");
            yield return WaitForSubjectAcknowledgement(); // And wait for the subject to cycle through them.
            InstructionManager.DisplayText("You'll have to wait for a countdown. Look at the sphere and get ready!" + "\n\n (Press the trigger)");
            yield return WaitForSubjectAcknowledgement(); // And wait for the subject to cycle through them.

            while (loopTraining)
            {
                inPractice = true;
                PrepareForStart();
                HudManager.colour = HUDManager.HUDColour.Orange;
                HUDCountDown(1);
                yield return new WaitUntil(() => CountdownDone); // And wait 
                InstructionManager.DisplayText("Reach!");
                HudManager.DisplayText("Reach!");
                HudManager.colour = HUDManager.HUDColour.Blue;
                yield return new WaitUntil(() => IsTaskDone());
                // Signal the subject that the task is done
                audio.clip = returnAudioClip;
                audio.Play();
                HudManager.colour = HUDManager.HUDColour.Red;
                HudManager.DisplayText("Well done (you can return to start position)!");


                // Reset flags
                hasReached = false;
                taskComplete = false;
                iterationNumber = 0;
                HandleIterationInitialisation();

                // Do you want to retry?
                InstructionManager.DisplayText("To retry (Press the trigger)" + "\n\n" + "To continue (Press the touchpad)");
                yield return WaitForTrainingInstruction();
            }
            inPractice = false;

            //
            // End
            InstructionManager.DisplayText("Important: Hold your position until you hear the sound !!! " + "\n\n (Press the trigger)");
            yield return WaitForSubjectAcknowledgement(); // And wait for the subject to cycle through them.
            InstructionManager.DisplayText("Otherwise, you look ready to go! Good luck!" + "\n\n (Press the trigger)");
            yield return WaitForSubjectAcknowledgement(); // And wait for the subject to cycle through them.
        }

        // Amputee case
        if (AvatarSystem.AvatarType == AvatarType.Transhumeral)
        {
            InstructionManager.DisplayText("Let's start training then!" + "\n\n (Press the trigger)");
            yield return WaitForSubjectAcknowledgement(); // And wait for the subject to cycle through them.

            //
            // Reaching practice
            InstructionManager.DisplayText("This time the forearm will not track yours but controlled by signals from your upperarm" + "\n\n (Press the trigger)");
            yield return WaitForSubjectAcknowledgement(); // And wait for the subject to cycle through them.
            InstructionManager.DisplayText("Ok, let's have a try." + "\n\n (Press the trigger)");
            HudManager.ClearText();
            HudManager.colour = HUDManager.HUDColour.Red;
            yield return WaitForSubjectAcknowledgement(); // And wait for the subject to cycle through them.

            //Start practice         
            InstructionManager.DisplayText("The virtual elbow and wrist are controlled by your upperarm. You can have a try." + "\n\n (Press the trigger)");
            yield return WaitForSubjectAcknowledgement(); // And wait for the subject to cycle through them.
            while (loopTraining)
            {
                inPractice = true;
                PrepareForStart();
                HudManager.colour = HUDManager.HUDColour.Orange;
                HUDCountDown(1);
                yield return new WaitUntil(() => CountdownDone); // And wait 
                InstructionManager.DisplayText("Reach!");
                HudManager.DisplayText("Reach!");
                HudManager.colour = HUDManager.HUDColour.Blue;
                yield return new WaitUntil(() => IsTaskDone());
                // Signal the subject that the task is done
                audio.clip = returnAudioClip;
                audio.Play();
                HudManager.colour = HUDManager.HUDColour.Red;
                HudManager.DisplayText("Well done (you can return to start position)!");


                // Reset flags
                hasReached = false;
                taskComplete = false;
                iterationNumber = 0;
                HandleIterationInitialisation();

                // Do you want to retry?
                InstructionManager.DisplayText("To retry (Press the trigger)" + "\n\n" + "To continue (Press the touchpad)");
                yield return WaitForTrainingInstruction();
            }
            inPractice = false;


            //
            // End
            InstructionManager.DisplayText("You look ready to go! Good luck!" + "\n\n (Press the trigger to start experiment)");
            yield return WaitForSubjectAcknowledgement(); // And wait for the subject to cycle through them.

        }

        Debug.Log("Training complete.");
        // Now that you are done, set the flag to indicate we are done.
        trainingDone = true; 

    }
    /// <summary>
    /// Coroutine check if continue the training
    /// </summary>
    /// <returns></returns>
    private IEnumerator WaitForTrainingInstruction()
    {
        yield return new WaitUntil(() => buttonAction.GetStateDown(SteamVR_Input_Sources.Any) || padAction.GetStateDown(SteamVR_Input_Sources.Any));
        yield return new WaitForSeconds(0.5f);
    }

    /// <summary>
    /// Checks whether the subject is ready to start performing the task.
    /// </summary>
    /// <returns>True if ready to start.</returns>
    public override bool IsReadyToStart()
    {

        if (checkStartPosition)
        {
            return IsAtRightPosition(startShoulderAngle, startElbowAngle, startTolerance);

        }

        if (dummyControlEnable)
        {
            return (Input.GetKey(KeyCode.UpArrow) || buttonAction.GetState(SteamVR_Input_Sources.Any));
        }

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
        gridManager.ResetTargetSelection();
        gridManager.SelectTarget(targetOrder[iterationNumber - 1]);

    }

    /// <summary>
    /// Performs a procedure needed when the subject fails to start the task (goes out of the start condition).
    /// This can be: display some information, reset variables, change something in the experiment.
    /// </summary>
    public override void StartFailureReset()
    {
        // If our subject fails, do some resetting. 
        // Clear bottle selection
        gridManager.ResetTargetSelection();

    }

    /// <summary>
    /// Handles task data logging which runs on FixedUpdate.
    /// Logs data from sensors registered in the AvatarSystem and ExperimentSystem by default.
    /// Can be exteded to add more data by implementing an override method in the derived class which first adds data
    /// to the logData string (e.g. logData +=  myDataString + ","), and then calls base.HandleTaskDataLogging().
    /// </summary>
    public override void HandleTaskDataLogging()
    {
        // Add custom data logging here
        logData += targetOrder[iterationNumber - 1] + ",";  // Make sure you always end your custom data with a comma! Using CSV for data logging.

        #region Rewrite the base method
        //
        // Gather data while experiment is in progress
        //
        logData += taskTime.ToString();


        // Get kinematic postural features and push through zmq
        if (fullTrackerEnable)
        {
            float[] zmqData = new float[] { 1, taskTime };

            float[] pose = PosturalFeatureExtractor.ExtractTrunkPose(c7InitOrient, c7Tracker.GetTrackerTransform().rotation);
            var temp = zmqData.Concat(pose).ToArray(); zmqData = new float[temp.Length]; temp.CopyTo(zmqData, 0);

            pose = PosturalFeatureExtractor.ExtractScapularPose(c7InitOrient, shInitOrient, c7Tracker.GetTrackerTransform().rotation, shoulderTracker.GetTrackerTransform().rotation, SaveSystem.ActiveUser.shoulderBreadth);
            //pose = PosturalFeatureExtractor.extractScapularPose(initialPosition[trackNum - offset], initialPosition[trackNum - offset - 1], 
                                                                //c7Tracker.GetTrackerTransform().position, shoulderTracker.GetTrackerTransform().position, c7Tracker.GetTrackerTransform().rotation);
            temp = zmqData.Concat(pose).ToArray(); zmqData = new float[temp.Length]; temp.CopyTo(zmqData, 0);



            pose = PosturalFeatureExtractor.ExtractShoulderPose(c7Tracker.GetTrackerTransform().rotation, upperArmTracker.GetTrackerTransform().rotation);
            temp = zmqData.Concat(pose).ToArray(); zmqData = new float[temp.Length]; temp.CopyTo(zmqData, 0);


            pose = PosturalFeatureExtractor.ExtractElbowPose(upperArmTracker.GetTrackerTransform().rotation, lowerArmTracker.GetTrackerTransform().rotation);
            temp = zmqData.Concat(pose).ToArray(); zmqData = new float[temp.Length]; temp.CopyTo(zmqData, 0);

            pose = PosturalFeatureExtractor.ExtractWristPose(upperArmTracker.GetTrackerTransform().rotation, lowerArmTracker.GetTrackerTransform().rotation);
            temp = zmqData.Concat(pose).ToArray(); zmqData = new float[temp.Length]; temp.CopyTo(zmqData, 0);

            // Log the postural features
            foreach (float element in zmqData.Skip(2))
            {
                logData += "," + element.ToString();
            }


            // Push data to other platform through ZMQ
            if (zmqPushEnable)
            {
                ZMQSystem.AddPushData(zmqPushPort, zmqData);
                //Debug.Log("ZMQ pushed");
            }
        }
       

        /*
        // Read from all user sensors
        foreach (ISensor sensor in AvatarSystem.GetActiveSensors())
        {
            float[] sensorData = sensor.GetAllProcessedData();
            foreach (float element in sensorData)
                logData += "," + element.ToString();
        }
        */
        // Read from all experiment sensors
        foreach (ISensor sensor in ExperimentSystem.GetActiveSensors())
        {
            float[] sensorData = sensor.GetAllProcessedData();
            // Append data to local data logger
            foreach (float element in sensorData)
            {
                logData += "," + element.ToString();
            }
   
        }


        // If in transhumeral prosthesis mode add prosthetic elbow and wrist states
        if (AvatarSystem.AvatarType == AvatarType.Transhumeral)
        {
            logData += "," + -multiJointManager.ElbowState[0] + "," + - multiJointManager.ElbowState[1];
            logData += "," + -multiJointManager.WristPronState[0] + "," + - multiJointManager.WristPronState[1];
        }

        // Log task error
        Vector3 errorTemp = gridManager.GetPosError();
        logData += "," + errorTemp.x + "," + errorTemp.y + "," + errorTemp.z + ","  + gridManager.GetAngError();

       //
       // Log current data and clear before next run.
       //
        taskDataLogger.AppendData(logData);
        logData = "";

        // Update run time
        taskTime += Time.fixedDeltaTime;

        #endregion

        #region Archive 
        //Debug the zmq data
        //string debugString="";
        //foreach (float element in zmqData)
        //debugString += "," + element.ToString();
        //Debug.Log(debugString);
        // Continue with data logging.
        //base.HandleTaskDataLogging();
        //HudManager.DisplayText(GameObject.Find("Bottle").transform.localEulerAngles.ToString());
        //PosturalFeatureExtractor.extractScapularPose(initialPosition[3], initialPosition[2], c7Tracker.GetTrackerTransform().position, 
        //shoulderTracker.GetTrackerTransform().position, c7Tracker.GetTrackerTransform().rotation);
        #endregion

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
        bool tempCompleteFlag = false;

        if (dummyControlEnable)
            tempCompleteFlag = buttonAction.GetStateDown(SteamVR_Input_Sources.Any) || Input.GetKeyDown(KeyCode.UpArrow); //Input.GetKey(KeyCode.DownArrow)
        else
            tempCompleteFlag = gridManager.SelectedTouched || taskTime > maxTaskTime;

        if (tempCompleteFlag && !hasReached)
        {
            iterationDoneTime = taskTime;
            Debug.Log("Ite:" + iterationNumber + ". Task done. t=" + iterationDoneTime.ToString() + ".");

            bool skipStateMachine = iterationBatchSize > 1 && (iterationNumber % iterationBatchSize) != 0;
            if (skipStateMachine)
            {
                StartCoroutine(EndTaskCoroutine(skipStateMachine));
            }
            else
            {
                StartCoroutine(EndTaskCoroutine(skipStateMachine));
            }
            
        }
        return taskComplete;
    }


    /// <summary>
    /// Raises the complete task flag after 1 second to allow for additional data to be gathered
    /// after the subject touches the selected sphere.
    /// </summary>
    /// <returns></returns>
    private IEnumerator EndTaskCoroutine(bool skipStateMachine)
    {
        hasReached = true;

        audio.clip = holdAudioClip;
        audio.Play();
        // Signal the subject that the task is done
        HudManager.DisplayText("Hold on for a while!!");
        HudManager.colour = HUDManager.HUDColour.Green;
        
       
        yield return new WaitForSecondsRealtime(holdingTime);



        if (skipStateMachine)
        {
            // Because this iteration is still in the batch
            // Skip the gamemaster flow control, but initialise next iteration
            AltHandleTaskCompletion();
            AltHandleResultAnalysis();
            HandleIterationInitialisation();
            PrepareForStart();
            HudManager.DisplayText("Next Pose!");
            HudManager.colour = HUDManager.HUDColour.Blue;
            //
            yield return new WaitForSecondsRealtime(1.0f);
            
            taskComplete = false;
        }
        else
            taskComplete = true;
    }

    /// <summary>
    /// Handles procedures that occur as soon as the task is completed.
    /// Extend this method by doing your own implementation, with base.HandleTaskCompletion() being called at the start.
    /// </summary>
    public override void HandleTaskCompletion()
    {
        // Stop EMG reading and save data
        if (delsysEnable)
        {
            delsysEMG.StopRecording();
            emgIsRecording = false;
        }
      
        // Save log data
        base.HandleTaskCompletion();
        
        // Reset flags
        hasReached = false;
        taskComplete = false;

        // Play return audio
        audio.clip = returnAudioClip;
        audio.Play();

        // Update the pusher the next iteration happen;
        if(zmqPushEnable)
            ZMQSystem.AddPushData(zmqPushPort, new float[] { ITE_RESET });

    }

    /// <summary>
    /// Handles procedures that occur as soon as the task is completed.
    /// This method is not in the GameMaster state machine flow control
    /// </summary>
    private void AltHandleTaskCompletion()
    {
        // Stop EMG reading and save data, skip the training state
        if (delsysEnable && GetCurrentStateName() != State.STATE.TRAINING)
        {
            delsysEMG.StopRecording();
            emgIsRecording = false;
        }

        // Save log data
        taskDataLogger.SaveLog();

        // Reset flags
        hasReached = false;
        taskComplete = false;

        // Play return audio
        audio.clip = nextAudioClip;
        audio.Play();

        // Update the pusher the next iteration happen;
        if (zmqPushEnable)
            ZMQSystem.AddPushData(zmqPushPort, new float[] { ITE_RESET });
    }


    /// <summary>
    /// Handles the procedures performed when analysing results.
    /// </summary>
    public override void HandleResultAnalysis()
    {
        analysisDone = false;
        // Do some analysis
        //StartCoroutine(HandleResultAnalysisCoroutine());
        //Debug.Log("Try feedback");

        string iterationResults = iterationNumber + "," + targetOrder[iterationNumber - 1] + "," + iterationDoneTime.ToString();

        foreach (float element in gridManager.QUpperLimb[targetOrder[iterationNumber-1]])
        {
            iterationResults += "," + element;
        }

        // Log results
        performanceDataLogger.AppendData(iterationResults);
        performanceDataLogger.SaveLog();

        analysisDone = true;
    }

    /// <summary>
    /// Handles the procedures performed when analysing results.
    /// </summary>
    private void AltHandleResultAnalysis()
    {
        string iterationResults = iterationNumber + "," + targetOrder[iterationNumber - 1] + "," + iterationDoneTime.ToString(); //+ "," + feedbackScore.ToString();

        foreach (float element in gridManager.QUpperLimb[targetOrder[iterationNumber - 1]])
        {
            iterationResults += "," + element;
        }
        // Log results
        performanceDataLogger.AppendData(iterationResults);
        performanceDataLogger.SaveLog();
    }

    /// <summary>
    /// Handles the coroutine need to be done after task completion
    ///
    /// </summary>
    private IEnumerator HandleResultAnalysisCoroutine()
    {
        //Get feedback score
        if (zmqReqEnable)
        {
            //zmqRequester.newData(new float[] { (float)sessionNumber, (float)iterationNumber, iterationDoneTime });
            // yield return new WaitUntil(() => zmqRequester.ReceivedResponseFlag);
            //double[] response = zmqRequester.GetReceiveData();
            double[] response = ZMQSystem.AddRequest(zmqReqPort, new float[] { (float)sessionNumber, (float)iterationNumber, iterationDoneTime });
            feedbackScore = (float)response[0];
            HudManager.DisplayText("Score: " + feedbackScore);
            Debug.Log("Feedback Score: " + feedbackScore);

            HudManager.DisplayText("Score: " + feedbackScore + ". Press to continue.");
        }
        
        yield return WaitForSubjectAcknowledgement();
    }


    /// <summary>
    /// Handles procedures performed when initialising the next iteration.
    /// Updates iteration number, resets the task time, and starts a new data log by default.
    /// Extend this method by doing your own implementation, with base.HandleIterationInitialisation() being called at the start.
    /// </summary>
    public override void HandleIterationInitialisation()
    {
        //StartCoroutine(HandleIterationInitialisationCoroutine());
        
        base.HandleIterationInitialisation();
        Debug.Log("Current target pose number: " + targetOrder[iterationNumber - 1]);

    }


    /*
    /// <summary>
    /// Coroutine when initialising the next iteration
    /// </summary>
    private IEnumerator HandleIterationInitialisationCoroutine()
    {
        yield return;
    }
    */

    /// <summary>
    /// Checks if the condition for changing experiment session has been reached.
    /// </summary>
    /// <returns>True if the condition for changing sessions has been reached.</returns>
    public override bool IsEndOfSession()
    {
        // You can do your own implementation of this
        return base.IsEndOfSession();
       
    }

    /// <summary>
    /// Handles procedures performed when initialising the next iteration.
    /// Updates iteration number, session number, resets the task time, and starts a new data log by default.
    /// Extend this method by doing your own implementation, with base.HandleSessionInitialisation() being called at the start.
    /// </summary>
    public override void HandleSessionInitialisation()
    {

        //HudManager.DisplayText("New session");
        /*
        if (gridManager.CurrentTargetType == TargetPoseGridManager.TargetType.Ball)
        {
            GameObject[] targets = GameObject.FindGameObjectsWithTag("TouchyBall");
            foreach (GameObject target in targets)
            {
                Destroy(target);
            }
            targetOrder.Clear();

            // Generate new grid
            gridManager.CurrentTargetType = TargetPoseGridManager.TargetType.Bottle;
            gridManager.SpawnTargetGrid();
            gridManager.ResetTargetSelection();
            Debug.Log("Spawn the grid!");
        }
        */

        base.HandleSessionInitialisation();

        #region Iteration settings
        // Set iterations variables for flow control.
        targetNumber = gridManager.TargetNumber;
        iterationsPerSession[sessionNumber-1] = targetNumber * iterationsPerTarget[sessionNumber - 1];


        // Create the list of target indexes and shuffle it.
        for (int i = 0; i < targetNumber; i++)
        {
            for (int j = 0; j < iterationsPerTarget[sessionNumber - 1]; j++)
            {
                targetOrder.Add(i);
                Debug.Log(targetOrder[targetOrder.Count-1]);
            }
                

        }

        targetOrder.Shuffle();
        
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

        if (zmqPullEnable)
            ZMQSystem.CloseZMQSocket(zmqPullPort, ZMQSystem.SocketType.Puller);

        if (zmqPushEnable || zmqReqEnable)
            ZMQSystem.CloseZMQSocket(zmqPushPort,ZMQSystem.SocketType.Pusher);


        if (zmqReqEnable)
            ZMQSystem.CloseZMQSocket(zmqReqPort, ZMQSystem.SocketType.Requester);

        // You can do your own end of experiment stuff here
        if (delsysEnable)
        {
            delsysEMG.StopAcquisition();
            delsysEMG.Close();
        }
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

    #endregion

    /// <summary>
    /// Avoid unity freeze
    /// </summary>
    /// <returns></returns>
    private void OnApplicationQuit()
    {
        ExperimentSystem.CloseAllExperimentLoggers();

        if (zmqPushEnable)
        {
            ZMQSystem.AddPushData(zmqPushPort, new float[] { 0.0f });
            ZMQSystem.CloseZMQSocket(zmqPushPort,ZMQSystem.SocketType.Pusher);
        }

        if (zmqPullEnable)
            ZMQSystem.CloseZMQSocket(zmqPullPort, ZMQSystem.SocketType.Puller);


        if (zmqReqEnable)
            ZMQSystem.CloseZMQSocket(zmqReqPort,ZMQSystem.SocketType.Requester);

        if (delsysEnable)
        {
            delsysEMG.StopAcquisition();
            delsysEMG.Close();
        }

        NetMQConfig.Cleanup(false);
    }
}
