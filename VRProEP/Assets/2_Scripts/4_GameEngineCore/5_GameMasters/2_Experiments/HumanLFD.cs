// System
using System.Collections;
using System.Collections.Generic;
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

public class HumanLFD : GameMaster
{
    // Here you can place all your Unity (GameObjects or similar)
    #region Unity objects
    [Header("Sensor settings")]
    [SerializeField]
    private bool xrSkip = false;

    [Header("Data format")]
    [SerializeField]
    private string ablebodiedDataFormat = "pose,t,aDotE,bDotE,gDotE,aE,bE,gE,xE,yE,zE,aDotUA,bDotUA,gDotUA,aUA,bUA,gUA,xUA,yUA,zUA,aDotSH,bDotSH,gDotSH,aSH,bSH,gSH,xSH,ySH,zSH,aDotUB,bDotUB,gDotUB,aUB,bUB,gUB,xUB,yUB,zUB,xHand,yHand,zHand,aHand,bHand,gHand";
    [SerializeField]
    private string positionDataFormat = "iteration,t,step,x1,y1,z1,w1,qx1,qy1,qz1,x2,y2,z2,w2,qx2,qy2,qz2,x3,y3,z3,w3,qx3,qy3,qz3,x4,y4,z4,w4,qx4,qy4,qz4,x5,y5,z5,w5,qx5,qy5,qz5";
    [SerializeField]
    private string performanceDataFormat = "i,pose,name,t_f";

   
    [Header("Experiment configuration: Start position")]
    [Header("Experiment configuration: Reps and Sets")]
    [SerializeField]
    [Tooltip("The number of iterations per target.")]
    [Range(1, 100)]
    private int iterationsTotal = 10;


    [SerializeField]
    private bool checkStartPosition;

    #endregion

    protected SteamVR_Action_Boolean padAction = SteamVR_Input.GetAction<SteamVR_Action_Boolean>("InterfaceEnableButton");

    // Additional data logging
    private DataStreamLogger performanceDataLogger;
    private float doneTime;
    private int subStep;


    // Target management variables
    private int targetNumber = 9; // The total number of targets
    private List<int> targetOrder = new List<int>(); // A list of target indexes ordered for selection over iterations in a session.

    // Motion tracking for experiment management and adaptation (check for start position)
    private VIVETrackerManager uaTracker;
    private VIVETrackerManager wristTracker;
    private VIVETrackerManager shoulderTracker;
    private VIVETrackerManager tp1Tracker;
    private VIVETrackerManager tp2Tracker;
    private VirtualPositionTracker handTracker;

    // Flow control
    private bool hasReached = false;
    private bool taskComplete = false;

    // Lefty subject sign
    private float leftySign = 1.0f;

    private class VariationTestConfigurator
    {
        public int iterationsTotal = 10;
        public int sessionsTotal = 2;
    }
    private VariationTestConfigurator configurator;

   
    #region Private methods
    private string ConfigEMGFilePath()
    {
        //string emgDataFilename =  Path.Combine(taskDataLogger.ActiveDataPath, "session_" + sessionNumber , "i" + "_" + iterationNumber + "EMG.csv");
        string emgDataFilename = taskDataLogger.ActiveDataPath + "/session_" + sessionNumber + "/i" + "_" + iterationNumber + "EMG.csv";
        return emgDataFilename;
    }

    private IEnumerator DisplayTaskText(int index)
    {
        yield return 0;//new WaitForSecondsRealtime(0.0f);
        Debug.Log("Ite:" + iterationNumber + ". Next pose:" + index + ".");
    }

    #endregion


    #region Dynamic configuration

    //
    // Configuration class:
    // Modify this class to be able to configure your experiment from a configuration file
    //
    private class Configurator
    {
        
    }
    private Configurator config;

    #endregion

    // Here are all the methods you need to write for your experiment.
    #region GameMaster Inherited Methods
    protected override void Update()
    {

        // Override update for stable substep increment
        //if (padAction.GetStateDown(SteamVR_Input_Sources.Any) || Input.GetKeyDown(KeyCode.UpArrow))
        //{
        //    StartCoroutine(subStep_count());
        //}
        if (GetCurrentStateName() == State.STATE.PERFORMING_TASK && Input.GetKeyDown(KeyCode.UpArrow))
        {
            subStep += 1;
            Debug.Log("Session Number: " + sessionNumber + ", Iteration Number: " + iterationNumber + ", Substeps: " + subStep);
            InstructionManager.DisplayText(GetDisplayInfoText());
        }


        base.Update();

    }
    protected override void FixedUpdate()
    {
       
        // Override fixed update to start the emg recording when the start performing the task
        if ( GetCurrentStateName() == State.STATE.PERFORMING_TASK)
        {

            Debug.Log("Session Number: " + sessionNumber + ", Iteration Number: " + iterationNumber + ", Status: PERFORMING_TASK");

        }

        base.FixedUpdate();

    }
    // Place debug stuff here, for when you want to test the experiment directly from the world without 
    // having to load it from the menus.
    private void Awake()
    {
        if (debug)
        {
            Debug.Log("1. System Wake");
            SaveSystem.LoadUserData("TB1995175"); // Load the test/demo user (Mr Demo) into the SaveSystem.ActiveUser

            // Create Player
            AvatarSystem.LoadPlayer(SaveSystem.ActiveUser.type, AvatarType.AbleBodied);
            // Create Avatar
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
        text += "Substep: " + subStep + ".\n";
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
        Debug.Log("2. ConfigureExperiment");
        // First call the base method to load the file
        base.ConfigureExperiment();

        //configAsset = Resources.Load<TextAsset>("Experiments/" + ExperimentSystem.ActiveExperimentID);

        // Convert configuration file to configuration class.
        configurator = JsonUtility.FromJson<VariationTestConfigurator>(configAsset.text);

        // Initialize number of iterations per session
        iterationsTotal = configurator.iterationsTotal;
        Debug.Log("Total Number of Sessions:" + configurator.sessionsTotal);
        Debug.Log("Total Iterations per Session:" + iterationsTotal);
        for (int i = 0; i < configurator.sessionsTotal; i++)
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
        Debug.Log("3: InitialiseExperimentSystems");

        taskDataFormat = positionDataFormat;
        // Lefty sign
        if (SaveSystem.ActiveUser.lefty)
            leftySign = -1.0f;

        #region Modify base
        // Then run the base initialisation which is needed, with a small modification
        //
        // Set the experiment name only when debugging. Take  the name from the gameobject + Debug
        //
        if (debug)
            ExperimentSystem.SetActiveExperimentID(this.gameObject.name + "_Debug");

        // Make sure flow control is initialised
        sessionNumber = 1;
        iterationNumber = 1;

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
       


        #region  Initialize motion sensors
        //
        // Add arm motion trackers for able-bodied case.
        //

        // Lower limb motion tracker
        //GameObject wristGO = GameObject.FindGameObjectWithTag("wristTracker");
        //wristTracker = new VIVETrackerManager(wristGO.transform);
        //ExperimentSystem.AddSensor(wristTracker);

        // wrist motion tracker
        GameObject wristGO = AvatarSystem.AddMotionTracker();
        wristGO.tag = "wristTracker";
        wristTracker = new VIVETrackerManager(wristGO.transform);
        ExperimentSystem.AddSensor(wristTracker);

        // Upper arm motion tracker
        GameObject uaGO = AvatarSystem.AddMotionTracker();
        uaGO.tag = "uaTracker";
        uaTracker = new VIVETrackerManager(uaGO.transform);
        ExperimentSystem.AddSensor(uaTracker);

        // Shoulder tracker
        GameObject shoulderGO = AvatarSystem.AddMotionTracker();
        shoulderGO.tag = "shoulderTracker";
        shoulderTracker = new VIVETrackerManager(shoulderGO.transform);
        ExperimentSystem.AddSensor(shoulderTracker);

        // Task Points tracker
        GameObject tp1GO = AvatarSystem.AddMotionTracker();
        tp1Tracker = new VIVETrackerManager(tp1GO.transform);
        tp1GO.tag = "tp1Tracker";
        ExperimentSystem.AddSensor(tp1Tracker);

        GameObject tp2GO = AvatarSystem.AddMotionTracker();
        tp2Tracker = new VIVETrackerManager(tp2GO.transform);
        tp2GO.tag = "tp2Tracker";
        ExperimentSystem.AddSensor(tp2Tracker);

        //
        // Hand tracking sensor
        //
        GameObject handGO = GameObject.FindGameObjectWithTag("Hand");
        handTracker = new VirtualPositionTracker(handGO.transform);
        ExperimentSystem.AddSensor(handTracker);

        #endregion

    }

    /// <summary>
    /// Coroutine for the welcome text.
    /// Implement your welcome loop here.
    /// </summary>
    /// <returns>Yield instruction</returns>
    public override IEnumerator WelcomeLoop()
    {
        Debug.Log("4. WelcomeLoop");
        // First flag that we are in the welcome routine
        welcomeDone = false;
        inWelcome = true;
        
        //welcomeDone = true;

        //HudManager.DisplayText("Look In Front. Instructions will be displayed there.");
        //InstructionManager.DisplayText("Hi " + SaveSystem.ActiveUser.name + "! Welcome to the virtual world. \n\n (Press space to continue...)");
        //yield return WaitKeyBoardAck(); // And wait for the subject to cycle through them.

        InstructionManager.DisplayText("Today we will collect your motion data for different tasks \n\n (Press the space button to continue...)");
        yield return WaitKeyBoardAck(); // And wait for the subject to cycle through them.

        //// Now that you are done, set the flag to indicate we are done.

        welcomeDone = true;
    }

    /// <summary>
    /// Performs initialisation procedures for the experiment. Sets variables to their zero state.
    /// </summary>
    public override void InitialiseExperiment()
    {
        Debug.Log("5. InitialiseExperiment");

        #region Iteration settings
        // Set iterations variables for flow control.
        iterationsPerSession[sessionNumber - 1] = iterationsTotal;
        #endregion        
    }

    /// <summary>
    /// Coroutine for the experiment instructions.
    /// Implement your instructions loop here.
    /// </summary>
    /// <returns>Yield instruction</returns>
    /// 
    private string changeInstruction(string status,int substep,bool end)
    {
        string text;
        text = "Status: " + status + ".\n";
        text += "Time: " + System.DateTime.Now.ToString("H:mm tt") + ".\n";
        text += "Experiment Progress: " + sessionNumber + "/" + iterationsPerSession.Count + ".\n";
        text += "Session Progress: " + iterationNumber + "/" + iterationsPerSession[sessionNumber - 1] + ".\n";
        text += "Substep: " + substep + ".\n";

        if (end)
        {
            text += "When you returned to Home and is ready to stop, say |STOP|";
        }
        return text;
    }
    public override IEnumerator InstructionsLoop()
    {
        Debug.Log("InstructionsLoop");
        // First flag that we are in the instructions routine
        instructionsDone = false;
        inInstructions = true;
        
        InstructionManager.DisplayText("You will perform 4 different tasks \n\nEach task has 5 differernt task configs \n\nEach task configs needs 3 demos\n");
        yield return WaitKeyBoardAck();

        while (!instructionsDone)
        {
            InstructionManager.DisplayText("You are required to move from Home => Point 1 => Point 2 => Home\n");
            yield return WaitKeyBoardAck();
            InstructionManager.DisplayText("When  Status changes from |Waiting_For_Task| to |Performing_Task| \n\nSay |READY| when you are ready to move.\n\nLet's Try it out");
            yield return WaitKeyBoardAck(); // And wait for the subject to cycle through them.

            subStep = 0;

            InstructionManager.DisplayText(changeInstruction("Waiting_For_Task", subStep, false));
            yield return WaitKeyBoardAck();

            InstructionManager.DisplayText(changeInstruction("Performing_Task", subStep, false));

            yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.UpArrow));
            subStep += 1;
            InstructionManager.DisplayText(changeInstruction("Performing_Task", subStep, false));
            yield return new WaitForSeconds(0.5f);


            yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.UpArrow));
            subStep += 1;
            InstructionManager.DisplayText(changeInstruction("Performing_Task", subStep, false));
            yield return new WaitForSeconds(0.5f);

            yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.UpArrow));
            subStep += 1;
            InstructionManager.DisplayText(changeInstruction("Performing_Task", subStep, true));
            yield return new WaitForSeconds(0.5f);

            yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.UpArrow));
            subStep += 1;
            InstructionManager.DisplayText(changeInstruction("Performing_Task", subStep, true));
            yield return new WaitForSeconds(0.5f);

            yield return WaitKeyBoardAck();
            InstructionManager.DisplayText(changeInstruction("Initializing Next Iteration", subStep, false));

            yield return WaitKeyBoardAck();
            InstructionManager.DisplayText("Good Job. Are you ready to Start?");
            yield return new WaitUntil(() => Input.anyKeyDown);


            if (Input.GetKeyDown(KeyCode.Y))
            {
                instructionsDone = true;
            }

            if (Input.GetKeyDown(KeyCode.N))
            {
                instructionsDone = false;
            }
        }


        // Now that you are done, set the flag to indicate we are done.
        subStep = 0;
        // prevent instructions from coming up twice
        skipInstructions = true;
        instructionsDone = true;

    }


    /// <summary>
    /// Coroutine for the experiment training.
    /// Implement your training loop here.
    /// </summary>
    /// <returns>Yield instruction</returns>
    public override IEnumerator TrainingLoop()
    {
        Debug.Log("TrainingLoop");
        // First flag that we are in the training routine
        trainingDone = false;
        inTraining = true;

        InstructionManager.DisplayText("Training Loop, Press space to continue");
        yield return WaitKeyBoardAck(); // And wait for the subject to cycle through them.

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
            Debug.Log("Session Number: " + sessionNumber + ", Iteration Number: " + iterationNumber + ", Status: checkStartPosition");
            return (Input.GetKey(KeyCode.Space) || buttonAction.GetState(SteamVR_Input_Sources.Any));
        }
        else
            Debug.Log("!!");
            return true;
    }

    /// <summary>
    /// Prepares variables and performs any procedures needed when the subject has succeded in the preparation
    /// to start performing the task.
    /// </summary>
    public override void PrepareForStart()
    {
        // Here you can do stuff like preparing objects/assets, like setting a different colour to the object
        Debug.Log("Session Number: " + sessionNumber + ", Iteration Number: " + iterationNumber + ", Status: PrepareForStart");
    }

    /// <summary>
    /// Performs a procedure needed when the subject fails to start the task (goes out of the start condition).
    /// This can be: display some information, reset variables, change something in the experiment.
    /// </summary>
    public override void StartFailureReset()
    {
        // If our subject fails, do some resetting. 
        // Clear bottle selection
        Debug.Log("Session Number: " + sessionNumber + ", Iteration Number: " + iterationNumber + ", Status: StartFailureReset");
        subStep = 0;
    }

    /// <summary>
    /// Handles task data logging which runs on FixedUpdate.
    /// Logs data from sensors registered in the AvatarSystem and ExperimentSystem by default.
    /// Can be exteded to add more data by implementing an override method in the derived class which first adds data
    /// to the logData string (e.g. logData +=  myDataString + ","), and then calls base.HandleTaskDataLogging().
    /// </summary>
    public override void HandleTaskDataLogging()
    {



        //
        // Add your custom data logging here
        //
        logData += iterationNumber-1 + ",";  // Make sure you always end your custom data with a comma! Using CSV for data logging.

        //
        // Continue with data logging.
        //
        if (!xrSkip)
            base.HandleTaskDataLogging();
        else
        {
            logData += taskTime.ToString();
            logData += "," + subStep.ToString();
            GameObject got = GameObject.FindGameObjectWithTag("wristTracker");
            logData += "," + got.transform.position.x.ToString() + "," + got.transform.position.y.ToString() + "," + got.transform.position.z.ToString();
            logData += "," + got.transform.rotation.w.ToString() + "," + got.transform.rotation.x.ToString() + "," + got.transform.rotation.y.ToString() + "," + got.transform.rotation.z.ToString();

            got = GameObject.FindGameObjectWithTag("uaTracker");
            logData += "," + got.transform.position.x.ToString() + "," + got.transform.position.y.ToString() + "," + got.transform.position.z.ToString();
            logData += "," + got.transform.rotation.w.ToString() + "," + got.transform.rotation.x.ToString() + "," + got.transform.rotation.y.ToString() + "," + got.transform.rotation.z.ToString();

            got = GameObject.FindGameObjectWithTag("shoulderTracker");
            logData += "," + got.transform.position.x.ToString() + "," + got.transform.position.y.ToString() + "," + got.transform.position.z.ToString();
            logData += "," + got.transform.rotation.w.ToString() + "," + got.transform.rotation.x.ToString() + "," + got.transform.rotation.y.ToString() + "," + got.transform.rotation.z.ToString();

            got = GameObject.FindGameObjectWithTag("tp1Tracker");
            logData += "," + got.transform.position.x.ToString() + "," + got.transform.position.y.ToString() + "," + got.transform.position.z.ToString();
            logData += "," + got.transform.rotation.w.ToString() + "," + got.transform.rotation.x.ToString() + "," + got.transform.rotation.y.ToString() + "," + got.transform.rotation.z.ToString();

            got = GameObject.FindGameObjectWithTag("tp2Tracker");
            logData += "," + got.transform.position.x.ToString() + "," + got.transform.position.y.ToString() + "," + got.transform.position.z.ToString();
            logData += "," + got.transform.rotation.w.ToString() + "," + got.transform.rotation.x.ToString() + "," + got.transform.rotation.y.ToString() + "," + got.transform.rotation.z.ToString();
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

        if ( (buttonAction.GetStateDown(SteamVR_Input_Sources.Any) || Input.GetKeyDown(KeyCode.Space)) ) //Input.GetKey(KeyCode.DownArrow)
        {
            doneTime = taskTime;
            StartCoroutine(EndTaskCoroutine());
            Debug.Log("Ite:" + iterationNumber + ". Task done. t=" + doneTime.ToString() + ".");
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
        Debug.Log("Session Number: " + sessionNumber + ", Iteration Number: " + iterationNumber + ", Status: EndTaskCoroutine");
        hasReached = true;
        // Signal the subject that the task is done
        //HudManager.DisplayText("Hold on for a while!!");
        //HudManager.colour = HUDManager.HUDColour.Green;
        //HudManager.colour = HUDManager.HUDColour.Green;
       
        yield return new WaitForSecondsRealtime(1.0f);
        taskComplete = true;
    }

    /// <summary>
    /// Handles procedures that occur as soon as the task is completed.
    /// Extend this method by doing your own implementation, with base.HandleTaskCompletion() being called at the start.
    /// </summary>
    public override void HandleTaskCompletion()
    {
        base.HandleTaskCompletion();

        // Reset flags
        hasReached = false;
        taskComplete = false;
    }

    /// <summary>
    /// Handles the procedures performed when analysing results.
    /// </summary>
    public override void HandleResultAnalysis()
    {
    }

    /// <summary>
    /// Handles procedures performed when initialising the next iteration.
    /// Updates iteration number, resets the task time, and starts a new data log by default.
    /// Extend this method by doing your own implementation, with base.HandleIterationInitialisation() being called at the start.
    /// </summary>
    public override void HandleIterationInitialisation()
    {
        subStep = 0;
        base.HandleIterationInitialisation();
    }

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
        base.HandleSessionInitialisation();

        #region Iteration settings
        // Set iterations variables for flow control.
        iterationsPerSession[sessionNumber - 1] = iterationsTotal;
        subStep = 0;

        // New file for the performance data logger
        performanceDataLogger.CloseLog();
        performanceDataLogger.AddNewLogFile(AvatarSystem.AvatarType.ToString(), sessionNumber, performanceDataFormat); // Add file

        if (debug)
        {

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
        
        // You can do your own end of experiment stuff here
    }

    /// <summary>
    /// Checks if the condition for the rest period has been reached.
    /// </summary>
    /// <returns>True if the rest condition has been reached.</returns>
    public override bool IsRestTime()
    {
        // For example, give rest time after the fifth iteration.
        return iterationNumber % iterationsTotal == 0;
    }

    /// <summary>
    /// Avoid unity freeze
    /// </summary>
    /// <returns></returns>
    private void OnApplicationQuit()
    {
        //ZMQSystem.AddPushData(zmq, new float[] { 0.0f });
        //ZMQSystem.CloseZMQSocket(zmqPort, ZMQSystem.SocketType.Pusher);
        NetMQConfig.Cleanup(false);
    }
}

    #endregion

   
