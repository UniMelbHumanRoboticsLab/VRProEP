using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRProEP.GameEngineCore;

// SteamVR
using Valve.VR;
using Valve.VR.InteractionSystem;

public class ClothespinTaskManager : MonoBehaviour
{
    public enum TaskType { AblePoseRecord, AbleDataCollect, ProstEvaluat}
    private TaskType currentTaskType;
    public TaskType CurrentTaskType { get => CurrentTaskType; set { currentTaskType = value; } }

    [SerializeField]
    private float height;
    public float Height { get => height; set { height = value; } }

    [SerializeField]
    private float distance;
    public float Distance { get => distance; set { distance = value; } }


    [SerializeField]
    private GameObject clothespinPrefab;

    [SerializeField]
    private List<ClothespinManager> clothespinList;

    private readonly string[] horizontalAttachPoint = { "AttachPoint_H2_1", "AttachPoint_H2_2" };
    private readonly string[] verticalAttachPoint = { "AttachPoint_V1_1", "AttachPoint_V1_2" };

    [SerializeField]
    private List<GameObject> attachGOList;

    [SerializeField]
    private List<GameObject> poseGOList;
    private GameObject poseBufferGO;


    [SerializeField]
    private int currentTrial = 0;

    [SerializeField]
    private int currentPinIndex;

    [SerializeField]
    private int currentSegment;

    // Relocation task path, {from, to, pinIndex}. Target numbering: [horizontal, vertical]
    public readonly int[,] TASK_PATH = new int[,] { {0, 2, 0}, {1, 3, 1}, {3, 1, 1},{2, 0, 0} };
    private const int GET_INIT_INDEX = 0;
    private const int GET_FINAL_INDEX = 1;
    private const int GET_PIN_INDEX = 2;
    public int PathNumber { get => TASK_PATH.GetLength(0); }

    // Task segmentation 
    private const int REACH_INIT = 1;
    private const int PINCH = 2;
    private const int REACH_FINAL = 3;
    private const int OPEN_HAND = 4;
    private const int SEG_NUM = OPEN_HAND;

    private int totalPathSegment = 1;
    public int TotalPathSegment { get => totalPathSegment; }

    // Flow control
    private bool trialComplete = false;
    public bool TrialComplete { get => trialComplete; set { trialComplete = value; } }

    // Buttons
    protected SteamVR_Action_Boolean padAction = SteamVR_Input.GetAction<SteamVR_Action_Boolean>("InterfaceEnableButton");
    protected SteamVR_Action_Boolean buttonAction = SteamVR_Input.GetAction<SteamVR_Action_Boolean>("ObjectInteractButton");

    // Hand Models
    [SerializeField]
    private GameObject handRPrefab;
    [SerializeField]
    private GameObject handLPrefab;
    [SerializeField]
    private Material mMaterial;

    private GameObject handGO;

    // Flags
    private bool rackInit = false;
    private bool reachHandInit = false;

    [SerializeField]
    private bool debug;

    private void Awake()
    {
        // Temporary debug
        if (debug)
        {
            for (int i = 1; i <= 4; i++)
            {
                GameObject temp = GameObject.Find("HandPose_" + i);
                poseGOList.Add(temp);
            }
        }
    }
    // Start is called before the first frame update
    void Start()
    {
        poseBufferGO = new GameObject("PoseBuffer");
        poseBufferGO.SetActive(false);

    }

    // Update is called once per frame
    void Update()
    {
        // The task has been initliased
        if (currentTrial != 0)
        {
            // Handle extra updates for different task types
            switch (currentTaskType)
            {
                case TaskType.AblePoseRecord:
                    // Record the current avatar transform when pressed trigger to grasp
                    GameObject tempHandGO = GameObject.FindGameObjectWithTag("Hand");
                    if (buttonAction.GetStateDown(SteamVR_Input_Sources.Any))
                    {
                        poseBufferGO.SetActive(true);
                        poseBufferGO.transform.position = tempHandGO.transform.position;
                        poseBufferGO.transform.rotation = tempHandGO.transform.rotation;
                        poseBufferGO.transform.localScale = tempHandGO.transform.localScale;

                    }
                    // If the clothespin is confirmed to reach final target and in idle, record the previously recorded avatar hand pose.
                    if (poseBufferGO.activeSelf && clothespinList[currentPinIndex].PinState == ClothespinManager.ClothespinState.Idle)
                    {
                        GameObject temp = new GameObject("HandPose");
                        temp.transform.position = poseBufferGO.transform.position;
                        temp.transform.rotation = poseBufferGO.transform.rotation;
                        poseBufferGO.SetActive(false);
                        poseGOList.Add(temp);
                        Debug.Log("Hand pose recorded!");
                    }
                    break;
                case TaskType.AbleDataCollect:
                    // Do nothing
                    break;
                case TaskType.ProstEvaluat:
                    // Do nothing
                    break;

            }

            // Check if the selected pin reached
            if (CheckReached(currentPinIndex))
            {
                // Reset flag
                clothespinList[currentPinIndex].ReachedFinal = false;
                trialComplete = true;
            }



        }
    }


    #region public methods


    //
    // Initialise the task
    //
    public void Initialise()
    {
        // For first session - never initialised
        if (!rackInit)
        {
            SetupRackPosition();
            GetAllTargetTransform();
            InitClothespin();
            rackInit = true;
        }
        

        // Init some constants
        totalPathSegment = 1;
        currentTrial = 1;
        currentPinIndex = 0;
        currentSegment = 1;
        SetupTargetPin(currentTrial);

        // For able-bodied data collection we need to have the hand displayed as target
        if (currentTaskType == TaskType.AbleDataCollect)
        {
            totalPathSegment = SEG_NUM;
            currentSegment = 1;
            if (!reachHandInit)
            {
                InitialiseHand();
                reachHandInit = true;
            }
            
            // Select the first
            SetupHandPose(currentTrial,currentSegment);
        }
           
    }

    //
    // Setup the next trial
    //
    public void NextTrial()
    {
        // Reset flag
        trialComplete = false;

        // Update variables
        if (currentTaskType == TaskType.AbleDataCollect)
        {
            currentSegment++;
            if (currentSegment == REACH_FINAL)
                SetupHandPose(currentTrial, currentSegment);
            else if (currentSegment > totalPathSegment) // If finish all the segments of the path 
            {
                currentSegment = 1;
                currentTrial++;
                SetupHandPose(currentTrial, currentSegment);
                currentPinIndex = SetupTargetPin(currentTrial);
            }

        }
        else
        {
            currentTrial++;
            currentPinIndex = SetupTargetPin(currentTrial);
        }
       
        
    }


    //
    // Set experiment type
    //
    public void SetTaskType(TaskType type)
    {
        this.currentTaskType = type;
    }

    #endregion


    #region private methods
    //
    // Initilise hand model
    //
    private void InitialiseHand()
    {
        //
        // Left sign
        //
        float sign = 1.0f;
        string side = "R";
        if (SaveSystem.ActiveUser.lefty)
        {
            sign = -1.0f;
            side = "L";
        }

        if (!SaveSystem.ActiveUser.lefty) //Check to use the left or right hand prefab
            handGO = Instantiate(handRPrefab); 

        else
            handGO = Instantiate(handLPrefab);

        
        // Load hand scale
        string objectPath = "Avatars/Hands/ACESHand_" + side;
        string objectDataAsJson = Resources.Load<TextAsset>(objectPath).text;
        AvatarObjectData activeHandData = JsonUtility.FromJson<AvatarObjectData>(objectDataAsJson);
        if (activeHandData == null)
            throw new System.Exception("The requested hand information was not found.");


        float scaleFactor = SaveSystem.ActiveUser.handLength / activeHandData.dimensions.x;
        handGO.transform.localScale = new Vector3(sign * scaleFactor, sign * scaleFactor, sign * scaleFactor);

    }

    //
    // 
    //
    private void SetupHandPose(int trial, int segment)
    {
        int index = 0;

        int trialInCycle = (trial - 1) % TASK_PATH.GetLength(0);
        // Setup the new target
        if(segment == REACH_INIT)
            index = TASK_PATH[trialInCycle, GET_INIT_INDEX];
        else if (segment == REACH_FINAL)
            index = TASK_PATH[trialInCycle, GET_FINAL_INDEX];


        handGO.transform.position = poseGOList[index].transform.position;
        handGO.transform.rotation = poseGOList[index].transform.rotation;

        handGO.GetComponent<ReachHandManager>().SetSelected();
    }

    //
    // Set up rack position
    //
    private void SetupRackPosition()
    {
        Vector3 temp = new Vector3(-distance, height, 0);
        this.transform.position = temp;
    }


    //
    // Set up target
    //
    private int SetupTargetPin(int trial)
    {
        int trialInCycle = (trial - 1) % TASK_PATH.GetLength(0);
        //if (trialInCycle == TASK_PATH.GetLength(0) - 1)
            //this.currentCycle++;
        int pinIndex = TASK_PATH[trialInCycle, GET_PIN_INDEX];

        // Setup the new target
        int fromIndex = TASK_PATH[trialInCycle, GET_INIT_INDEX];
        int toIndex = TASK_PATH[trialInCycle, GET_FINAL_INDEX];
        SelectClothespin(pinIndex);

        if(trial ==1 || trial == 2)
            SetClothespinInitTransform(pinIndex, attachGOList[fromIndex].transform.position, attachGOList[fromIndex].transform.rotation);

        SetClothespinFinalTransform(pinIndex, attachGOList[toIndex].transform.position, attachGOList[toIndex].transform.rotation);
        DisplayAttachPoint(attachGOList[toIndex]);

        return pinIndex;
    }

    //
    // Check if clothespin has reach the target 
    //
    private bool CheckReached(int pinIndex)
    {
        bool reached = false;

        if (currentTaskType == TaskType.AblePoseRecord || currentTaskType == TaskType.ProstEvaluat)
            reached = clothespinList[pinIndex].ReachedFinal;
        else if (currentTaskType == TaskType.AbleDataCollect) // For able-bodied data collection reach the pose instead.
        {
            switch (currentSegment)
            {
                case REACH_INIT:
                    reached = handGO.GetComponent<ReachHandManager>().ReachedFinal;
                    break;
                case PINCH:
                    reached = buttonAction.GetStateDown(SteamVR_Input_Sources.Any);
                    break;
                case REACH_FINAL:
                    reached = handGO.GetComponent<ReachHandManager>().ReachedFinal;
                    break;
                case OPEN_HAND:
                    reached = padAction.GetStateDown(SteamVR_Input_Sources.Any);
                    break;
                default:
                    break;
            }
        }

        return reached; 

    }

 

    //
    // Get target locations
    //
    private void GetAllTargetTransform()
    {
        // First horizontal ones and then vertical ones
        foreach (string name in horizontalAttachPoint)
        {
            GameObject attachPoint = GameObject.Find(name);
            attachGOList.Add(attachPoint);
        }

        foreach (string name in verticalAttachPoint)
        {
            GameObject attachPoint = GameObject.Find(name);
            attachGOList.Add(attachPoint);
        }

    }

    //
    // Set closthespin initial and target transform
    //
    private void SetClothespinInitTransform(int index, Vector3 initPosition, Quaternion initRotation)
    {
        if (index > clothespinList.Count - 1)
            throw new System.ArgumentOutOfRangeException("The requested pin index is invalid.");

        clothespinList[index].SetInitTransform(initPosition, initRotation);
            
    }


    private void SetClothespinFinalTransform(int index, Vector3 finalPosition, Quaternion finalRotation)
    {
        if (index > clothespinList.Count - 1)
            throw new System.ArgumentOutOfRangeException("The requested pin index is invalid.");

        clothespinList[index].SetFinalTransform(finalPosition, finalRotation);
        
    }

    private void DisplayAttachPoint(GameObject attachPoint)
    {
        foreach (GameObject target in attachGOList)
            target.GetComponent<MeshRenderer>().enabled = false;
        attachPoint.GetComponent<MeshRenderer>().enabled = true;
    }

    //
    // Initialise pin locations
    //
    private void InitClothespin()
    {
        for (int i = 0; i < horizontalAttachPoint.Length; i++)
        {
            GameObject target = attachGOList[i];
            GameObject pinGO = Instantiate(clothespinPrefab,
                    target.transform.position,
                    target.transform.rotation);
            clothespinList.Add(pinGO.GetComponentInChildren<ClothespinManager>());
        }
    }

    // Select clothespin as target
    private void SelectClothespin(int index)
    {
        clothespinList[index].SetSelect();
    }

    #endregion


}
