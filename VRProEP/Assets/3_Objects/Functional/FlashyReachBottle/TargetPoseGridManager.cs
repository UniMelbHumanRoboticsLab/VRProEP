using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRProEP.GameEngineCore;

using System.Linq;
using VRProEP.Utilities;

public class TargetPoseGridManager : MonoBehaviour
{
    public const string SFE_POSE = "ADD_SFE_POSE";
    public const string SAA_POSE = "ADD_SAA_POSE";
    public const string SR_POSE = "ADD_SR_POSE";
    public const string EFE_POSE = "ADD_EFE_POSE";
    public const string WPS_POSE = "ADD_WPS_POSE";
    public const string WFE_POSE = "ADD_WFE_POSE";
    public const string WAA_POSE = "ADD_WAA_POSE";

    private const int SFE_DOF = 0;
    private const int SAA_DOF = 1;
    private const int SR_DOF = 2;
    private const int EFE_DOF = 3;
    private const int WPS_DOF = 4;
    private const int WFE_DOF = 5;
    private const int WAA_DOF = 6;

    private const int MAX_DOF = 7;

    // Config variables
    public enum TargetType { Bottle, Ball }

    [Header("Objects")]
    [SerializeField]
    private GameObject reachBottlePrefab;
    [SerializeField]
    private GameObject reachBallPrefab;
    [SerializeField]
    private GameObject bottleInHand;

    [Header("Use which prefab")]
    [SerializeField]
    private TargetType targetType;

    [Header("Physical Property")]
    [SerializeField]
    private Vector3 shoulderCentreOffset;
    public Vector3 ShoulderCentreOffset { get=> shoulderCentreOffset; set => shoulderCentreOffset = value; }
    [SerializeField]
    private Transform shoulderCentreLoc;
    [SerializeField]
    private float uarmLengthOffset;
    public float UarmLengthOffset { get => uarmLengthOffset; set => uarmLengthOffset = value; }
    [SerializeField]
    private float farmLengthOffset;
    public float FarmLengthOffset { get => farmLengthOffset; set => farmLengthOffset = value; }
    [SerializeField]
    private float handLengthOffset;
    public float HandLengthOffset { get => handLengthOffset; set => handLengthOffset = value; }

    [Header("Limb Models")]
    [SerializeField]
    private GameObject upperArmPrefab;
    [SerializeField]
    private GameObject foreArmPrefab;
    [SerializeField]
    private GameObject wristHandRPrefab;
    [SerializeField]
    private GameObject wrisHandLPrefab;

    [SerializeField]
    private GameObject foreArmLinePrefab;
    [SerializeField]
    private GameObject upperArmLinePrefab;
    [SerializeField]
    private GameObject handLinePrefab;

    [Header("Debug")]
    [SerializeField]
    private bool debug;

    // Dispalying limb
    private LineRenderer uaLine;
    private LineRenderer faLine;
    private LineRenderer handLine;
    private GameObject upperarmGO;
    private GameObject forearmGO;
    private GameObject wristHandGO;

    private static AvatarObjectData activeUpperarmData;
    private static AvatarObjectData activeForearmData;
    private static AvatarObjectData activeHandData;


    // Bottle list
    private List<ReachBottleManager> bottles = new List<ReachBottleManager>();// List of bottles
    private List<TouchyBallManager> balls = new List<TouchyBallManager>(); // List of balls


    // Target pose list
    private List<List<float>> qJoints = new List<List<float>>();
    public List<float[]> QUpperLimb { get; set; } = new List<float[]>();
    private List<float[]> targetPoseList = new List<float[]>();

    // Required dofs
    private List<int> dofList = new List<int>();

    // Postion of rotations of the bottles in the grid
    private List<Vector3> targetPositions = new List<Vector3>();// List of the target postions
    private List<Vector3> elbowPositions = new List<Vector3>();// List of the elbow postions
    private List<Vector3> wristPositions = new List<Vector3>();// List of the wrist postions
    private List<Quaternion> targetRotations = new List<Quaternion>();// List of the target rotations


    [Header("Bottle Offsets")]
    [SerializeField] [Range(-0.1f, 0.1f)] private float xOffestBottle;
    [SerializeField] [Range(-0.1f, 0.1f)] private float yOffestBottle;
    [SerializeField] [Range(-0.1f, 0.1f)] private float zOffestBottle;



    // Signs
    private bool hasSelected = false; // Whether a bottle has been selected
    private int selectedIndex = -1;
    private bool selectedTouched = false;

    // Subject information

    [Header("Height to acromion")]
    [SerializeField]
    private float subjectHeight2SA;

    [Header("Shoulder breadth")]
    [SerializeField]
    private float subjectShoulderBreadth;

    [Header("Upperarm length")]
    [SerializeField]
    private float subjectUALength;

    [Header("Forearm length")]
    [SerializeField]
    private float subjectFALength;

    [Header("Hand length")]
    [SerializeField]
    private float subjectHandLength;

    [Header("Upperarm width")]
    [SerializeField]
    private float subjectUAWidth;

    [Header("Forearm width")]
    [SerializeField]
    private float subjectFAWidth;




    private float subjectHeight;
    private float subjectTrunkLength2SA;
    private bool subjectLefty;
    private float sagittalOffset;

    // Accessor
    public bool SelectedTouched { get => selectedTouched; }
    public int TargetNumber
    {
        get
        {
            if (targetType == TargetType.Ball)
                return balls.Count;
            else
                return bottles.Count;
        }
    }
    public TargetType CurrentTargetType { get => targetType; set => targetType = value; }



    /// <summary>
    /// Config user physiology data
    /// </summary>
    /// <param >
    /// <returns>
    /// 
    private void ConfigUserData()
    {
        //
        // Debug able
        //
        if (debug)
            SaveSystem.LoadUserData("TB1995175");

        //
        // Read the user data
        //
        subjectHeight = SaveSystem.ActiveUser.height;
        subjectFALength = SaveSystem.ActiveUser.forearmLength + farmLengthOffset;
        subjectUALength = SaveSystem.ActiveUser.upperArmLength + uarmLengthOffset;
        subjectFAWidth = SaveSystem.ActiveUser.forearmWidth;
        subjectUAWidth = SaveSystem.ActiveUser.upperArmWidth;
        subjectHandLength = SaveSystem.ActiveUser.handLength + handLengthOffset;
        subjectTrunkLength2SA = SaveSystem.ActiveUser.trunkLength2SA;
        subjectHeight2SA = SaveSystem.ActiveUser.height2SA;
        subjectShoulderBreadth = SaveSystem.ActiveUser.shoulderBreadth;
        subjectLefty = SaveSystem.ActiveUser.lefty;

        //
        shoulderCentreLoc.position = new Vector3(subjectShoulderBreadth / 2.0f, subjectHeight2SA, 0);
        shoulderCentreLoc.position = shoulderCentreLoc.position + shoulderCentreOffset;

        //sagittalOffset = -subjectShoulderBreadth / 4.0f;

        Debug.Log("Gridmanager: load user physical data");
    }

    /// <summary>
    /// Update user physiology data
    /// </summary>
    /// <param >
    /// <returns>
    /// 
    public void UpdateUserData()
    {

        // Shoulder centre
        shoulderCentreLoc.position = new Vector3(subjectShoulderBreadth / 2.0f, subjectHeight2SA, 0);
        shoulderCentreLoc.position = shoulderCentreLoc.position + shoulderCentreOffset;

        // Arm length
        subjectUALength = SaveSystem.ActiveUser.upperArmLength + uarmLengthOffset;
        subjectFALength = SaveSystem.ActiveUser.forearmLength + farmLengthOffset;
        subjectHandLength = SaveSystem.ActiveUser.handLength + handLengthOffset;
        //sagittalOffset = -subjectShoulderBreadth / 4.0f;

        Debug.Log("Gridmanager: update user physical data");
    }

    void Start()
    {

        ConfigUserData();
        // Initialise the joint pose list
        for (int i = 1; i <= MAX_DOF; i++)
            qJoints.Add(new List<float>());

        // Debug
        if (debug)
        {
            /*
            AddJointPose("ADD_SFE_POSE", new float[4]{0,30,60,90});
            AddJointPose("ADD_EFE_POSE", new float[4] { 0, 30, 60, 90 });
            AddJointPose("ADD_WPS_POSE", new float[3] { 0, 70, -70});
            */

            //AddUpperLimbPose(new float[5] { 40, 0, 60, 60, 0 });
            //GenerateTargetLocations();
            //SpawnTargetGrid();
            //InitialiseLimb();
            //SelectTarget(0);
        }

    }

    void Update()
    {
        // Debug: Change selected bottle for debug
        //if (debug)
        //{
        if (Input.GetKeyDown(KeyCode.F1)) // Update the grid parameters
        {
            UpdateUserData();
            GenerateTargetLocations(); // Update the locations if there is any change in the offset
            ShowLimbPose(0);
            CalibrationPose();
        }
        if (Input.GetKeyDown(KeyCode.F2)) //select the next target
        {
            selectedIndex = selectedIndex + 1;
            switch (targetType)
            {
                case TargetType.Ball:
                    if (selectedIndex > balls.Count - 1)
                        selectedIndex = 0;
                    break;
                case TargetType.Bottle:
                    if (selectedIndex > bottles.Count - 1)
                        selectedIndex = 0;
                    break;
            }

            //UpdateUserData();
            //GenerateTargetLocations(); // Update the locations if there is any change in the offset
            SelectTarget(selectedIndex);
            Debug.Log(selectedIndex);

        }

            /*
           if (Input.GetKeyDown(KeyCode.F2)) //Restore the grid parameters 
           {
               ConfigUserData();
               GenerateTargetLocations(); // Update the locations if there is any change in the offset
               ShowLimbPose(selectedIndex);
               CalibrationPose();
           }
           */


        // Check if the selected bottle is reached or not
        CheckReached();
        //Debug.Log(selectedTouched);

    }


    /// <summary>
    /// Check if the selected bottle is reached
    /// </summary>
    /// <param >
    /// <returns bool reached>
    private void CheckReached()
    {

        // Check if the selected ball has been touched

        if (hasSelected)
        {
            switch (targetType)
            {
                case TargetType.Bottle:
                    if (this.bottles[selectedIndex].BottleState == ReachBottleManager.ReachBottleState.Correct)
                        selectedTouched = true;
                    else
                        selectedTouched = false;
                    break;

                case TargetType.Ball:
                    if (this.balls[selectedIndex].BallState == TouchyBallManager.TouchyBallState.Correct)
                        selectedTouched = true;
                    else
                        selectedTouched = false;
                    break;
            }

        }
        //Debug.Log(this.bottles[selectedIndex].BottleState.ToString());
    }


    /// <summary>
    /// Calculate the target locations based on target poses
    /// </summary>
    /// <param >
    /// <returns 
    public void GenerateTargetLocations()
    {
        float sign = 1.0f;
        if (subjectLefty)
        {
            sign = -1.0f;
        }
        this.sagittalOffset = 0.00f * sign;

        ClearTargetLocations();// Clear the target locations

        Vector3 shoulderCentre = shoulderCentreLoc.position;


        foreach (float[] qUA in QUpperLimb)
        {
            float qSfe = qUA[SFE_DOF];
            float qSaa = qUA[SAA_DOF];
            float qEfe = qUA[EFE_DOF];
            float qWps = qUA[WPS_DOF];
            float qWfe = qUA[WFE_DOF];

            Vector3 elbow = CalElbowPosition(qSfe, shoulderCentre);
            AddElbowLocation(elbow);

            Vector3 wrist = CalWristPosition(qSfe, qEfe, shoulderCentre, elbow);
            AddWristLocation(wrist);

            GameObject tempGO = CalTargetPosition(qWps, qWfe, elbow, wrist);
            AddTargetLocation(tempGO.transform.position);
            AddTargetRotation(tempGO.transform.rotation);
            Destroy(tempGO);
        }

    }

    /// <summary>
    /// Calculate wrist position
    /// </summary>
    /// <param >
    /// <returns 
    private Vector3 CalElbowPosition(float qSfe, Vector3 shoulderCentre)
    {
        Vector3 elbow = new Vector3();
        elbow.z = shoulderCentre.z + subjectUALength * Mathf.Sin(Mathf.Deg2Rad * qSfe);
        elbow.y = shoulderCentre.y - subjectUALength * Mathf.Cos(Mathf.Deg2Rad * qSfe);
        elbow.x = shoulderCentre.x;

        return elbow;
    }

    /// <summary>
    /// Calculate elbow position
    /// </summary>
    /// <param >
    /// <returns 
    private Vector3 CalWristPosition(float qSfe,float qEfe, Vector3 shoulderCentre,Vector3 elbow)
    {
        Vector3 wrist = new Vector3();
        wrist.z = elbow.z + subjectFALength * Mathf.Sin(Mathf.Deg2Rad * (qSfe + qEfe));
        wrist.y = elbow.y - subjectFALength * Mathf.Cos(Mathf.Deg2Rad * (qSfe + qEfe));
        wrist.x = shoulderCentre.x;
        return wrist;
    }

   

    /// <summary>
    /// Calculate target position
    /// </summary>
    /// <param >
    /// <returns
    private GameObject CalTargetPosition(float qWps, float qWfe, Vector3 elbow, Vector3 wrist)
    {
        Vector3 forearmVec = wrist - elbow;
        forearmVec.Normalize();

        GameObject tempGO = new GameObject("TargetPoint"); // temp gamobject describes hand orientation
        tempGO.transform.position = wrist;
        tempGO.transform.rotation = Quaternion.LookRotation(forearmVec, Vector3.left);

        tempGO.transform.localRotation *= Quaternion.Euler(0, 0, qWps + 90.0f);
        tempGO.transform.localRotation *= Quaternion.Euler(0, qWfe, 0);
        tempGO.transform.Translate(new Vector3(0, 0, subjectHandLength), Space.Self);

        return tempGO;
    }


    /// <summary>
    /// Clear the list of locations
    /// </summary>
    /// <param >
    /// <returns 
    private void ClearTargetLocations()
    {
        targetPositions.Clear();
        targetRotations.Clear();
        elbowPositions.Clear();
        wristPositions.Clear();
    }

    /// <summary>
    /// Add the locations for the grid
    /// </summary>
    /// <param >
    /// <returns 
    private void AddTargetLocation(Vector3 position)
    {
        targetPositions.Add(position);


    }

    /// <summary>
    /// Add the locations of the elbow
    /// </summary>
    /// <param >
    /// <returns 
    private void AddElbowLocation(Vector3 position)
    {
        elbowPositions.Add(position);


    }

    /// <summary>
    /// Add the locations of the wrist
    /// </summary>
    /// <param >
    /// <returns 
    private void AddWristLocation(Vector3 position)
    {
        wristPositions.Add(position);


    }

    /// <summary>
    /// Add the rotations of the grid
    /// </summary>
    /// <param >
    /// <returns 
    private void AddTargetRotation(Quaternion rotation)
    {
        targetRotations.Add(rotation);


    }


    #region Methods for displaying the limb pose
    /// <summary>
    /// Initialise virtual limb game objects
    /// </summary>
    /// <param >
    /// <returns 
    private void InitialiseLimb()
    {
        //
        // Left sign
        //
        float sign = 1.0f;
        string side = "R";
        if (subjectLefty)
        {
            sign = -1.0f;
            side = "L";
        }

        //
        // The lines that draw the limb
        //
        uaLine = Instantiate(upperArmLinePrefab).GetComponent<LineRenderer>();
        faLine = Instantiate(foreArmLinePrefab).GetComponent<LineRenderer>();
        handLine = Instantiate(handLinePrefab).GetComponent<LineRenderer>();

        //
        // The 3D models
        //

        Material mMaterial = (Material)Resources.Load("Avatars/Limb", typeof(Material));// Load the displaying material different to avatar material
        upperarmGO = Instantiate(upperArmPrefab);
        upperarmGO.GetComponentInChildren<LimbFollower>().enabled = false;
        foreach (MeshRenderer renderer in upperarmGO.GetComponentsInChildren<MeshRenderer>())
            renderer.sharedMaterial = mMaterial;

        forearmGO = Instantiate(foreArmPrefab);
        forearmGO.GetComponent<LimbFollower>().enabled = false;
        forearmGO.GetComponent<CapsuleCollider>().enabled = false;
        forearmGO.GetComponent<Rigidbody>().isKinematic = true;
        foreach (MeshRenderer renderer in forearmGO.GetComponentsInChildren<MeshRenderer>())
            renderer.sharedMaterial = mMaterial;
        //forearmGO.GetComponent<MeshRenderer>().sharedMaterial = mMaterial;

        if (!subjectLefty) //Check to use the left or right hand prefab
            wristHandGO = Instantiate(wristHandRPrefab);

        else
            wristHandGO = Instantiate(wrisHandLPrefab);
        wristHandGO.transform.Find("ACESHand_" + side).gameObject.GetComponent<MeshRenderer>().sharedMaterial = mMaterial;



        //
        // Scale the 3D model
        //
        // Hand scale
        string objectPath = "Avatars/Hands/ACESHand_" + side;
        string objectDataAsJson = Resources.Load<TextAsset>(objectPath).text;
        activeHandData = JsonUtility.FromJson<AvatarObjectData>(objectDataAsJson);
        if (activeHandData == null)
            throw new System.Exception("The requested hand information was not found.");
        // forearm scale
        objectPath = "Avatars/Forearms/ForearmAble";
        objectDataAsJson = Resources.Load<TextAsset>(objectPath).text;
        activeForearmData = JsonUtility.FromJson<AvatarObjectData>(objectDataAsJson);
        if (activeForearmData == null)
            throw new System.Exception("The requested hand information was not found.");
        //residual limb scale
        objectPath = "Avatars/ResidualLimbs/ResidualLimbUpperDefault";
        objectDataAsJson = Resources.Load<TextAsset>(objectPath).text;
        activeUpperarmData = JsonUtility.FromJson<AvatarObjectData>(objectDataAsJson);
        if (activeUpperarmData == null)
            throw new System.Exception("The requested hand information was not found.");
        // Scale the model
        float scaleFactor = subjectUAWidth / activeUpperarmData.dimensions.y;
        upperarmGO.transform.localScale = new Vector3(scaleFactor, scaleFactor, scaleFactor);

        scaleFactor = subjectFAWidth / activeForearmData.dimensions.y;
        forearmGO.transform.localScale = new Vector3(scaleFactor, scaleFactor, scaleFactor);

        scaleFactor = subjectHandLength / activeHandData.dimensions.x;
        wristHandGO.transform.Find("ACESHand_" + side).gameObject.transform.localScale = new Vector3(sign * scaleFactor, sign * scaleFactor, sign * scaleFactor);


        //
        //Initial display
        //
        int index = 0;
        ShowLimbPose(index);
    }

    /// <summary>
    /// Show the limb pose
    /// </summary>
    /// <param >
    /// <returns 
    private void ShowLimbPose(int index)
    {
        // Update the target positions
        if (targetType == TargetType.Ball)
        {
            for (int i = 0; i <= targetPositions.Count - 1; i++)
                balls[i].transform.position = targetPositions[i];
        }
        if (targetType == TargetType.Bottle)
        {
            for (int i = 0; i <= targetPositions.Count - 1; i++)
            {
                bottles[i].transform.position = targetPositions[i];
                bottles[i].transform.Translate(new Vector3(xOffestBottle, yOffestBottle, zOffestBottle), Space.Self);
                bottles[i].transform.rotation = targetRotations[i];
                bottles[i].transform.localRotation *= Quaternion.Euler(0, 0, 180);
                bottles[i].transform.localRotation *= Quaternion.Euler(0, -90, 0);

            }

        }



        //Display the limb as lines
        uaLine.SetPosition(0, shoulderCentreLoc.position);
        uaLine.SetPosition(1, elbowPositions[index]);
        faLine.SetPosition(0, elbowPositions[index]);
        faLine.SetPosition(1, wristPositions[index]);
        handLine.SetPosition(0, wristPositions[index]);
        handLine.SetPosition(1, targetPositions[index]);


        // Display the 3D models   
        upperarmGO.SetActive(false);
        //upperarmGO.transform.position = elbowPositions[index];
        //upperarmGO.transform.rotation = Quaternion.Euler(0, 0, -targetPoseList[index][0]); // rotate to align the pose
        //upperarmGO.transform.Translate(new Vector3(0, 0.1f, 0), Space.Self); // offset the model

        forearmGO.SetActive(false);
        //forearmGO.transform.position = wristPositions[index];
        //forearmGO.transform.rotation = Quaternion.Euler(180, 0, targetPoseList[index][0] + targetPoseList[index][1] +180); // rotate to align the pose
        //forearmGO.transform.Translate(new Vector3(0, 0.07f, 0), Space.Self); // offset the model

        wristHandGO.transform.position = wristPositions[index];
        GameObject tempGO = new GameObject("Wrist Joint");
        tempGO.transform.rotation = targetRotations[index];
        tempGO.transform.localRotation *= Quaternion.Euler(0, -90, 0);
        tempGO.transform.localRotation *= Quaternion.Euler(0, 0, 90);
        wristHandGO.transform.rotation = tempGO.transform.rotation;
        Destroy(tempGO);



    }



    #endregion


    #region public methods
    /// <summary>
    /// Add the joint pose, by dof name and angle value
    /// </summary>
    /// <param >
    /// <returns 
    public void AddJointPose(string type, float[] angle)
    {

        switch (type)
        {
            case SFE_POSE:
                foreach (float value in angle)
                    qJoints[SFE_DOF].Add(value);
                break;
            case SAA_POSE:
                foreach (float value in angle)
                    qJoints[SAA_DOF].Add(value);
                break;
            case SR_POSE:
                foreach (float value in angle)
                    qJoints[SR_DOF].Add(value);
                break;
            case EFE_POSE:
                foreach (float value in angle)
                    qJoints[EFE_DOF].Add(value);
                break;
            case WPS_POSE:
                foreach (float value in angle)
                    qJoints[WPS_DOF].Add(value);
                break;

            case WFE_POSE:
                foreach (float value in angle)
                    qJoints[WFE_DOF].Add(value);
                break;

            case WAA_POSE:
                foreach (float value in angle)
                    qJoints[WAA_DOF].Add(value);
                break;
        }


    }


    /// <summary>
    /// Add the joint pose, by joint angles in serial
    /// </summary>
    /// <param >
    /// <returns 
    public void AddUpperLimbPose(float[] angle)
    {
        QUpperLimb.Add(angle);
    }


    /// <summary>
    /// Generate combinations of the potential joint poses and save in a upper limb pose list.
    /// </summary>
    /// <param >
    /// <returns 
    public void CombJointPose(string[] dof)
    {
        // Active DoF
        List<int> t_dof = new List<int>();
        foreach (string element in dof)
        {
            switch (element)
            {
                case SFE_POSE:
                    t_dof.Add(SFE_DOF);
                    break;
                case SAA_POSE:
                    t_dof.Add(SAA_DOF);
                    break;
                case SR_POSE:
                    t_dof.Add(SR_DOF);
                    break;
                case EFE_POSE:
                    t_dof.Add(EFE_DOF);
                    break;
                case WPS_POSE:
                    t_dof.Add(WPS_DOF);
                    break;
                case WFE_POSE:
                    t_dof.Add(WFE_DOF);
                    break;
                case WAA_POSE:
                    t_dof.Add(WAA_DOF);
                    break;
            }
        }

        dofList.AddRange(t_dof);

        // How many DoFs
        int nDof = t_dof.Count;
        // Track the indices in each DoF's list
        int[] indices = new int[nDof];
        // Initialise with the first element
        for (int i = 0; i < nDof; i++)
            indices[i] = 0;



        while (true)
        {
            // Current combination
            float[] temp = new float[MAX_DOF];
            for (int i = 0; i < nDof; i++)
                temp[t_dof[i]] = qJoints[t_dof[i]][indices[i]];

            AddUpperLimbPose(temp);
            //Debug.Log("Upper limb pose:" + ":" + string.Join(",", temp));


            // Find the rightmost array
            // that has more elements
            // left after the current
            // element in that array
            int next = nDof - 1;
            while (next >= 0 &&
                  (indices[next] + 1 >=
                   qJoints[t_dof[next]].Count))
                next--;

            // No such array is found
            // so no more combinations left
            if (next < 0)
            {
                Debug.Log("Total number of poses: " + QUpperLimb.Count);
                return;

            }


            // If found move to next
            // element in that array
            indices[next]++;

            // For all arrays to the right
            // of this array current index
            // again points to first element
            for (int i = next + 1; i < nDof; i++)
                indices[i] = 0;
        }




    }


    /// <summary>
    /// Generate combinations of the potential joint poses and save in a upper limb pose list.
    /// </summary>
    /// <param >
    /// <returns 
    public List<int> SequentialRandomise(List<int> targetOrder, int batchSize)
    {
        // The order list to return
        List<int> shuffledTargetOrder = new List<int>();



        // temp order list
        List<int> t_targetOrder = new List<int>();
        Debug.Log("Original target order: " + string.Join(",", targetOrder));


        // Totla number
        int N = targetOrder.Count;
        //Initially select a random target
        int idx = Random.Range(0, targetOrder.Count);

        // Generate the sequential randomised target order list
        while (true)
        {
            // Batch size is full?
            if (t_targetOrder.Count >= batchSize)
            {
                shuffledTargetOrder.AddRange(t_targetOrder);
                t_targetOrder.Clear();
                idx = Random.Range(0, targetOrder.Count);
                Debug.Log("Batch size full");
                continue;
            }

            // If reach the maximum, return the order list
            if (shuffledTargetOrder.Count >= N)
            {
                break;
            }


            // Add the target to the temp order list
            Debug.Log("Remain poses: " + string.Join(",", targetOrder.Distinct().ToList()));
            Debug.Log("Add pose: " + targetOrder[idx]);
            t_targetOrder.Add(targetOrder[idx]);
           
            // Remove the previous one from the input list
            targetOrder.RemoveAt(idx);
            

            // Search the next
            // Current target pose
            //Debug.Log(t_targetOrder.Last());

            //List<float> t_pose = qUpperLimb[t_targetOrder.Last()].ToList<float>();
            // Find a different pose
            List<int> range = targetOrder.Distinct().ToList();
            range.Shuffle();
            //Debug.Log(string.Join(",", range));


            int i;
            int j;
            for (i = 0; i <= range.Count - 1; i++)
            {

                float[] t_pose = new float[dofList.Count];
                float[] pose = new float[dofList.Count];
                for (j = 0; j <= dofList.Count - 1; j++)
                {
                    t_pose[j] = QUpperLimb[t_targetOrder.Last()][dofList[j]];
                    pose[j] = QUpperLimb[range[i]][dofList[j]];

                    if (t_pose[j] == pose[j])
                        break;
                }

                if (j == dofList.Count)
                {

                    //Debug.Log("Next Pose: " + string.Join(",", pose));
                    break;
                }


            }


            if (i > range.Count - 1)
                i = range.Count - 1;

            //List<float> pose = qUpperLimb[range[i]].ToList();
            //List<float> duplicates = t_pose.Intersect(pose).ToList();
            //Debug.Log("Duplicates: " + string.Join(",", duplicates));
            //if (duplicates.All(x => x == 0))
            //Debug.Log("Next Pose: " + string.Join(",", pose));
            //break;
            //}




            // Update the idx
            if (range.Count > 0)
            {
                idx = targetOrder.IndexOf(range[i]);
                Debug.Log("Next pose index:" + idx);
            }
            
            //idx = Random.Range(0, targetOrder.Count);

        }


        // Debug
        //shuffledTargetOrder.Sort();
        List<int> temp = shuffledTargetOrder;
        Debug.Log("Shuffled target order: " + string.Join(",", temp));
        return shuffledTargetOrder;


    }

    /// <summary>
    /// Spawn the boottle grid
    /// </summary>
    /// <param >
    private void SortWithIndex(List<float> list, out List<float> sortedList, out List<int> sortedIndex)
    {
        var sorted = list
                        .Select((x, i) => new KeyValuePair<float, int>(x, i))
                        .OrderBy(x => x.Key)
                        .ToList();

        sortedList = sorted.Select(x => x.Key).ToList();
        sortedIndex = sorted.Select(x => x.Value).ToList();
    }


    /// <summary>
    /// Spawn the boottle grid
    /// </summary>
    /// <param >
    /// <returns void>
    public void SpawnTargetGrid()
    {
        UpdateUserData();
        GenerateTargetLocations();
        for (int i = 0; i <= targetPositions.Count - 1; i++)
        {

            if (targetType == TargetType.Ball)
            {
                // Spawn a new bottle with this as parent
                GameObject target = Instantiate(reachBallPrefab, this.transform);
                float scaleFactor = 0.05f;
                //target.transform.localScale = new Vector3(scaleFactor, scaleFactor, scaleFactor);
                // Move the local position of the ball.
                target.transform.localPosition = targetPositions[i];
                // Add bottle to collection
                TouchyBallManager manager = target.GetComponent<TouchyBallManager>();
                balls.Add(manager);

                // Disable the in hand bottle
                bottleInHand.SetActive(false);
            }

            // Only bottle will use rotation requirements
            if (targetType == TargetType.Bottle)
            {
                // Spawn a new bottle with this as parent
                GameObject target = Instantiate(reachBottlePrefab, this.transform);
                // Move the local position of the ball.
                target.transform.position = targetPositions[i];
                target.transform.rotation = targetRotations[i];

                // Add bottle to collection
                ReachBottleManager manager = target.GetComponent<ReachBottleManager>();
                bottles.Add(manager);

                // Eable the in hand bottle
                bottleInHand.SetActive(true);
                manager.SetBottlInHand(this.bottleInHand); // Set in hand bottle gameobject 
            }


        }

        InitialiseLimb();
        ShowLimbPose(0);
        CalibrationPose();

    }

    /// <summary>
    /// Select a bottle by index
    /// </summary>
    /// <param int index>
    /// <returns>
    public void SelectTarget(int index)
    {
        // Reset previous selections
        ResetTargetSelection();

        selectedIndex = index;

        Renderer[] renderer;
        switch (targetType)
        {
            
            case TargetType.Bottle:
                renderer = bottles[index].gameObject.GetComponentsInChildren<Renderer>();
                foreach (Renderer element in renderer)
                    element.enabled = true;
                bottles[index].SetSelected();

                break;
            case TargetType.Ball:
                renderer = balls[index].gameObject.GetComponentsInChildren<Renderer>();
                foreach (Renderer element in renderer)
                    element.enabled = true;
                balls[index].SetSelected();
                break;
        }

        hasSelected = true;
        selectedTouched = false;
        ShowLimbPose(index);
    }

    /// <summary>
    /// Select a bottle by index
    /// </summary>
    /// <param int index>
    /// <returns>
    public void CalibrationPose()
    {

        float qSfe = 0.0f;
        float qEfe = 90.0f;
        float qWps = 0.0f;
        float qWfe = 0.0f;

        Vector3 shoulderCentre = shoulderCentreLoc.position;
        Vector3 elbow = CalElbowPosition(qSfe, shoulderCentre);
        Vector3 wrist = CalWristPosition(qSfe, qEfe, shoulderCentre, elbow);
        GameObject targetGO = CalTargetPosition(qWps, qWfe, elbow, wrist);

        //Display the limb as lines
        uaLine.SetPosition(0, shoulderCentreLoc.position);
        uaLine.SetPosition(1, elbow);
        faLine.SetPosition(0, elbow);
        faLine.SetPosition(1, wrist);
        handLine.SetPosition(0, wrist);
        handLine.SetPosition(1, targetGO.transform.position);



        // Display the 3D models   
        upperarmGO.SetActive(false);

        forearmGO.SetActive(false);

        wristHandGO.transform.position = wrist;
        GameObject tempGO = new GameObject("Wrist Joint");
        tempGO.transform.rotation = targetGO.transform.rotation;
        tempGO.transform.localRotation *= Quaternion.Euler(0, -90, 0);
        tempGO.transform.localRotation *= Quaternion.Euler(0, 0, 90);
        wristHandGO.transform.rotation = tempGO.transform.rotation;
        Destroy(tempGO);
        Destroy(targetGO);
    }

    /// <summary>
    /// Clears the current traget selection
    /// </summary>
    public void ResetTargetSelection()
    {
        Renderer[] renderer;
        switch (targetType)
        {
            case TargetType.Bottle:
                foreach (ReachBottleManager bottle in bottles)
                {
                    bottle.ClearSelection();
                    renderer = bottle.gameObject.GetComponentsInChildren<Renderer>();
                    foreach (Renderer element in renderer)
                        element.enabled = false;
                }
                break;
            case TargetType.Ball:
                foreach (TouchyBallManager ball in balls)
                {
                    ball.ClearSelection();
                    renderer = ball.gameObject.GetComponentsInChildren<Renderer>();
                    foreach (Renderer element in renderer)
                        element.enabled = false;
                }
                break;
        }

        hasSelected = false;
        selectedTouched = false;
    }

    /// <summary>
    /// Clears the current traget selection
    /// </summary>
    public Vector3 GetPosError()
    {
        Vector3 error = new Vector3(0.0f,0.0f,0.0f);
        switch (targetType)
        {
            case TargetType.Bottle:
                error = bottles[selectedIndex].ErrorPos;
                break;
            case TargetType.Ball:
                break;
        }

        return error;
    }

    /// <summary>
    /// Clears the current traget selection
    /// </summary>
    public float GetAngError()
    {
        float error = 0.0f;
        switch (targetType)
        {
            case TargetType.Bottle:
                error = bottles[selectedIndex].ErrorAng;
                break;
            case TargetType.Ball:
                break;
        }
        return error;
    }



    #endregion
}


