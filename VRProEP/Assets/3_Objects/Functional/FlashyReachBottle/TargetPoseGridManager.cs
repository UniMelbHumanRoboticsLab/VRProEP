using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRProEP.GameEngineCore;


public class TargetPoseGridManager : MonoBehaviour
{
    public const string ADD_SFE_POSE = "ADD_SFE_POSE";
    public const string ADD_EFE_POSE = "ADD_EFE_POSE";
    public const string ADD_WPS_POSE = "ADD_WPS_POSE";

    // Config variables
    public enum TargetType { Bottle, Ball}

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
    [SerializeField]
    private Transform shoulderCentreLoc;


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
    
    private List<float> qShoulderFlexionExtension = new List<float>();
    private List<float> qElbowFlexionExtension = new List<float>();
    private List<float> qWristPronationSupination = new List<float>();
    

    private List<float[]> qUpperLimb = new List<float[]>();

    // Postion of rotations of the bottles in the grid
    private List<Vector3> targetPositions = new List<Vector3>();// List of the target postions
    private List<Vector3> elbowPositions = new List<Vector3>();// List of the elbow postions
    private List<Vector3> wristPositions = new List<Vector3>();// List of the wrist postions
    private List<Vector3> targetRotations = new List<Vector3>();// List of the target rotations
    private List<float[]> targetPoseList = new List<float[]>();// List of the target poses

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
    public int TargetNumber {
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
        subjectFALength = SaveSystem.ActiveUser.forearmLength;
        subjectUALength = SaveSystem.ActiveUser.upperArmLength;
        subjectFAWidth = SaveSystem.ActiveUser.forearmWidth;
        subjectUAWidth = SaveSystem.ActiveUser.upperArmWidth;
        subjectHandLength = SaveSystem.ActiveUser.handLength;
        subjectTrunkLength2SA = SaveSystem.ActiveUser.trunkLength2SA;
        subjectHeight2SA = SaveSystem.ActiveUser.height2SA;
        subjectShoulderBreadth = SaveSystem.ActiveUser.shoulderBreadth;
        subjectLefty = SaveSystem.ActiveUser.lefty;

        //
        shoulderCentreLoc.position = new Vector3(0, subjectHeight2SA, -subjectShoulderBreadth/2.0f);
        shoulderCentreLoc.position = shoulderCentreLoc.position - shoulderCentreOffset;

        //sagittalOffset = -subjectShoulderBreadth / 4.0f;

        Debug.Log("Gridmanager: load user data");
    }

    /// <summary>
    /// Update user physiology data
    /// </summary>
    /// <param >
    /// <returns>
    /// 
    public void UpdateUserData()
    {

        //
        shoulderCentreLoc.position = new Vector3(0, subjectHeight2SA, -subjectShoulderBreadth / 2.0f);
        shoulderCentreLoc.position = shoulderCentreLoc.position - shoulderCentreOffset;

        //sagittalOffset = -subjectShoulderBreadth / 4.0f;

        Debug.Log("Gridmanager: update user data");
    }

    void Start()
    {

        ConfigUserData();
        // Debug
        if (debug)
        {
            /*
            AddJointPose("ADD_SFE_POSE", new float[4]{0,30,60,90});
            AddJointPose("ADD_EFE_POSE", new float[4] { 0, 30, 60, 90 });
            AddJointPose("ADD_WPS_POSE", new float[3] { 0, 70, -70});
            */
            //AddJointPose(new float[4] {30, 0, 45, -70});
            //AddJointPose(new float[4] { 80, 0, 130, 0 });

            //AddJointPose(new float[4] { 10, 0, 70, 0 });
            //AddJointPose(new float[4] { 80, 0, 10, -70 });
            AddJointPose(new float[4] { 40, 0, 30, 0 }); AddJointPose(new float[4] { 60, 0, 30, 0 }); AddJointPose(new float[4] { 80, 0, 30, 0 });
            AddJointPose(new float[4] { 40, 0, 55, 0 }); AddJointPose(new float[4] { 60, 0, 55, 0 }); AddJointPose(new float[4] { 80, 0, 55, 0 });
            AddJointPose(new float[4] { 40, 0, 80, 0 }); AddJointPose(new float[4] { 60, 0, 80, 0 }); AddJointPose(new float[4] { 80, 0, 80, 0 });
            GenerateTargetLocations();
            SpawnTargetGrid();
            InitialiseLimb();

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
            ShowLimbPose(selectedIndex);
        }
        if (Input.GetKeyDown(KeyCode.F2)) //Update the grid parameters and select the next target
        {
            selectedIndex = selectedIndex + 1;
            if (selectedIndex > balls.Count - 1)
                selectedIndex = 0;
            UpdateUserData();
            GenerateTargetLocations(); // Update the locations if there is any change in the offset
            SelectTarget(selectedIndex);
                
        }
        if (Input.GetKeyDown(KeyCode.F3)) //Restore the grid parameters 
        {
            ConfigUserData();
            GenerateTargetLocations(); // Update the locations if there is any change in the offset
            ShowLimbPose(selectedIndex);

        }
        //}


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
        /*
        foreach (float qSfe in qShoulderFlexionExtension)
        {
            foreach (float qEfe in qElbowFlexionExtension)
            {
                foreach (float qWps in qWristPronationSupination)
                { 
                    Vector3 target = new Vector3();
                    Vector3 elbow = new Vector3();
                    Vector3 wrist = new Vector3();

                    elbow.x = shoulderCentre.x + subjectUALength * Mathf.Sin(Mathf.Deg2Rad * qSfe);
                    elbow.y = shoulderCentre.y - subjectUALength * Mathf.Cos(Mathf.Deg2Rad * qSfe);
                    elbow.z = shoulderCentre.z;
                    AddElbowLocation(elbow);

                    wrist.x = elbow.x + subjectFALength * Mathf.Sin(Mathf.Deg2Rad * (qSfe + qEfe));
                    wrist.y = elbow.y - subjectFALength * Mathf.Cos(Mathf.Deg2Rad * (qSfe + qEfe));
                    wrist.z = shoulderCentre.z;
                    AddWristLocation(wrist);

                    target.x = elbow.x + (subjectFALength + subjectHandLength) * Mathf.Sin(Mathf.Deg2Rad * (qSfe + qEfe));
                    target.y = elbow.y - (subjectFALength + subjectHandLength) * Mathf.Cos(Mathf.Deg2Rad * (qSfe + qEfe));
                    target.z = shoulderCentre.z + this.sagittalOffset;
                    AddTargetLocation(target);

                    targetPoseList.Add(new float[]{qSfe,qEfe,qWps});
                }
            }

        }
        */

        foreach (float[] qUA in qUpperLimb)
        {
            float qSfe = qUA[0];
            float qSaa = qUA[1];
            float qEfe = qUA[2];
            float qWps = qUA[3];

            Vector3 target = new Vector3();
            Vector3 elbow = new Vector3();
            Vector3 wrist = new Vector3();

            elbow.x = shoulderCentre.x + subjectUALength * Mathf.Sin(Mathf.Deg2Rad * qSfe);
            elbow.y = shoulderCentre.y - subjectUALength * Mathf.Cos(Mathf.Deg2Rad * qSfe);
            elbow.z = shoulderCentre.z;
            AddElbowLocation(elbow);

            wrist.x = elbow.x + subjectFALength * Mathf.Sin(Mathf.Deg2Rad * (qSfe + qEfe));
            wrist.y = elbow.y - subjectFALength * Mathf.Cos(Mathf.Deg2Rad * (qSfe + qEfe));
            wrist.z = shoulderCentre.z;
            AddWristLocation(wrist);

            target.x = elbow.x + (subjectFALength + subjectHandLength) * Mathf.Sin(Mathf.Deg2Rad * (qSfe + qEfe));
            target.y = elbow.y - (subjectFALength + subjectHandLength) * Mathf.Cos(Mathf.Deg2Rad * (qSfe + qEfe));
            target.z = shoulderCentre.z + this.sagittalOffset;
            AddTargetLocation(target);

            targetPoseList.Add(new float[] { qSfe, qEfe, qWps });
        }

    }

    /// <summary>
    /// Clear the list of locations
    /// </summary>
    /// <param >
    /// <returns 
    private void ClearTargetLocations()
    {
        targetPoseList.Clear();
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
    private void AddTargetRotation(Vector3 rotation)
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
        wristHandGO.transform.Find("ACESHand_"+side).gameObject.transform.localScale = new Vector3(sign * scaleFactor, sign * scaleFactor, sign * scaleFactor);
        

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
        for (int i = 0; i <= targetPositions.Count - 1; i++)
            balls[i].transform.position = targetPositions[i];
        

        //Display the limb as lines
        uaLine.SetPosition(0, shoulderCentreLoc.position);
        uaLine.SetPosition(1, elbowPositions[index]);
        faLine.SetPosition(0, elbowPositions[index]);
        faLine.SetPosition(1, wristPositions[index]);
        handLine.SetPosition(0, wristPositions[index]);
        handLine.SetPosition(1, targetPositions[index]);


        // Display the 3D models    
        upperarmGO.transform.position = elbowPositions[index];
        upperarmGO.transform.rotation = Quaternion.Euler(0, 0, targetPoseList[index][0]); // rotate to align the pose
        upperarmGO.transform.Translate(new Vector3(0, 0.1f, 0), Space.Self); // offset the model

        forearmGO.transform.position = wristPositions[index];
        forearmGO.transform.rotation = Quaternion.Euler(0, 0, targetPoseList[index][0] + targetPoseList[index][1]); // rotate to align the pose
        forearmGO.transform.Translate(new Vector3(0, 0.07f, 0), Space.Self); // offset the model

        wristHandGO.transform.position = wristPositions[index];
        wristHandGO.transform.rotation = Quaternion.Euler(0, 0, targetPoseList[index][0] + targetPoseList[index][1]); // rotate to align the pose
        
        float q = 180.0f + targetPoseList[index][2];
        wristHandGO.transform.Rotate(new Vector3(0, q, 0), Space.Self); 
    }
    #endregion


    #region public methods
    /// <summary>
    /// Add the joint pose
    /// </summary>
    /// <param >
    /// <returns 
    public void AddJointPose(string type, float[] angle)
    {
        
        switch (type)
        {
            case ADD_SFE_POSE:
                foreach (float value in angle)
                {
                    qShoulderFlexionExtension.Add(value);
                }
                break;
            case ADD_EFE_POSE:
                foreach (float value in angle)
                {
                    qElbowFlexionExtension.Add(value);
                }
                break;
            case ADD_WPS_POSE:
                foreach (float value in angle)
                {
                    qWristPronationSupination.Add(value);
                }
                break;

        }
        

    }

    public void AddJointPose(float[] angle)
    {
        qUpperLimb.Add(angle);
    }



    /// <summary>
    /// Spawn the boottle grid
    /// </summary>
    /// <param >
    /// <returns bool reached>
    public void SpawnTargetGrid()
    {
        GenerateTargetLocations();
        
        for (int i = 0; i <= targetPositions.Count-1; i++)
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
            for (int j = 0; j <= targetRotations.Count-1; j++)
            {

                if (targetType == TargetType.Bottle)
                {
                    // Spawn a new bottle with this as parent
                    GameObject target = Instantiate(reachBottlePrefab, this.transform);
                    // Move the local position of the ball.
                    target.transform.localPosition = targetPositions[i];
                    target.transform.Rotate(targetRotations[j]);
                    // Add bottle to collection
                    ReachBottleManager manager = target.GetComponent<ReachBottleManager>();
                    bottles.Add(manager);

                    // Eable the in hand bottle
                    bottleInHand.SetActive(true);
                    manager.SetBottlInHand(this.bottleInHand); // Set in hand bottle gameobject 
                }

                
 
            }
        }

        InitialiseLimb();

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
        

        switch (targetType)
        {
            case TargetType.Bottle:
                bottles[index].SetSelected();
                break;
            case TargetType.Ball:
                balls[index].SetSelected();
                break;
        }

        hasSelected = true;
        selectedTouched = false;
        ShowLimbPose(index);
    }

    /// <summary>
    /// Clears the ball selection
    /// </summary>
    public void ResetTargetSelection()
    {
        switch (targetType)
        {
            case TargetType.Bottle:
                foreach (ReachBottleManager bottle in bottles)
                {
                    bottle.ClearSelection();
                }
                break;
            case TargetType.Ball:
                foreach (TouchyBallManager ball in balls)
                {
                    ball.ClearSelection();
                }
                break;
        }
               
        hasSelected = false;
        selectedTouched = false;
    }
    #endregion
}
