using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRProEP.GameEngineCore;

// SteamVR
using Valve.VR;
using Valve.VR.InteractionSystem;

public class ClothespinTaskManager : MonoBehaviour
{
    public enum TaskType { AblePoseRecord, AbleDataCollect, ProstEvaluation}
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

    [SerializeField]
    private int currentTrial = 0;

    [SerializeField]
    private int currentPinIndex;

    // Relocation task path, {from, to, pinIndex}. Target numbering: [horizontal, vertical]
    public readonly int[,] TASK_PATH = new int[,] { {0, 2, 0}, {1, 3, 1}, {3, 1, 1},{2, 0, 0} };
    private readonly int GET_INIT_INDEX = 0;
    private readonly int GET_FINAL_INDEX = 1;
    private readonly int GET_PIN_INDEX = 2;

    public int PathNumber { get => TASK_PATH.GetLength(0); }

    // Flow control
    private bool trialComplete = false;
    public bool TrialComplete { get => trialComplete; set { trialComplete = value; } }

    // Buttons
    protected SteamVR_Action_Boolean padAction = SteamVR_Input.GetAction<SteamVR_Action_Boolean>("InterfaceEnableButton");
    protected SteamVR_Action_Boolean buttonAction = SteamVR_Input.GetAction<SteamVR_Action_Boolean>("ObjectInteractButton");

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        // The task has been initliased
        if (currentTrial != 0)
        {
            // If the trigger is pulled, record the current hand transform
            if (currentTaskType == TaskType.AblePoseRecord)
            {
                if (buttonAction.GetStateDown(SteamVR_Input_Sources.Any) || padAction.GetStateDown(SteamVR_Input_Sources.Any))
                {
                    GameObject tempHandGO = GameObject.FindGameObjectWithTag("Hand");
                    GameObject tempGO = new GameObject();
                    tempGO.transform.position = tempHandGO.transform.position;
                    tempGO.transform.rotation = tempHandGO.transform.rotation;
                    tempGO.transform.localScale = tempHandGO.transform.localScale;
                    poseGOList.Add(tempGO);
                    Debug.Log("Hand pose recorded!");
                }
            }
            

            // If the selected pin reached
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
        SetupPosition();
        GetAllTargetTransform();
        InitClothespin();
        currentTrial = 1;
        currentPinIndex = 0;

        // First trial;
        SetupTarget(currentTrial);
    }

    //
    // Setup the next trial
    //
    public void NextTrial()
    {
        // Check reach based on different task types
        switch (currentTaskType)
        {
            case TaskType.AblePoseRecord:
                break;
            case TaskType.AbleDataCollect:
                break;
            case TaskType.ProstEvaluation:

                break;

        }
        // Update target
        trialComplete = false;
        currentTrial++;
        currentPinIndex = SetupTarget(currentTrial);
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
    // Set up rack position
    //
    private void SetupPosition()
    {
        Vector3 temp = new Vector3(-distance, height, 0);
        this.transform.position += temp;
    }


    //
    // Set up target
    //
    private int SetupTarget(int trial)
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
    private bool CheckReached(int index)
    {
        

        return clothespinList[index].ReachedFinal; ;

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
