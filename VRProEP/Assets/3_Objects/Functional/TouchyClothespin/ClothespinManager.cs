using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRProEP.ProsthesisCore;

public class ClothespinManager : MonoBehaviour
{
    //
    // Flow control state
    //
    public enum ClothespinState { Idle, Selected, InHand, LeaveInit, ReachTarget, Wrong }
    private ClothespinState pinState;
    public ClothespinState PinState { get => pinState; }
    // Grasp State
    private bool tempGrasped;

    //
    // Transform control
    //
    private Vector3 initPosition;
    private Quaternion initRotation;
    private Vector3 finalPosition;
    private Quaternion finalRotation;
    [SerializeField]
    private Vector3 posTol;
    public Vector3 PosTol { get => posTol; set { posTol = value; } }
    [SerializeField]
    private float angTol;
    public float AngTol { get => angTol; set { angTol = value; } }

    // Error of task execution
    private Vector3 errorPos;
    public Vector3 ErrorPos { get => errorPos; }

    private float errorAng;
    public float ErrorAng { get => errorAng; }

    private ACESHandAnimation handManager;

    //
    // Color and display
    //
    [Header("Colour configuration")]
    [SerializeField]
    private Color idleColour;
    public  Color IdleColour { get => idleColour; }
    [SerializeField]
    private Color selectedColour;
    public Color SelectedColour { get => selectedColour;  }
    [SerializeField]
    private Color correctColour;
    public Color CorrectColour { get => correctColour;  }
    [SerializeField]
    private Color movingColour;
    public Color MovingColour { get => movingColour;  }
    [SerializeField]
    private Color wrongColour;
    public Color WrongColour { get => wrongColour;  }
    private Renderer[] pinRenderer;


    //
    // Animation
    //
    [SerializeField]
    private Animator animator;

    private float openLevel;

    [SerializeField]
    private float deltaLevel = 0.01f;
    [SerializeField]
    private float maxLevel = 1.0f;
    [SerializeField]
    private float minLevel = 0.0f;
    [SerializeField]
    private float initialLevel = 0.5f;
    [SerializeField]
    private float springCoeff = 4.0f;

    //
    // State control
    //
    private bool indexFingerTouched = false;
    private bool thumbFingerTouched = false;
    private bool touchRod = true;
    private int iGrasped = 0;
    private int iNotGrasped = 0;

    [SerializeField]
    private int maxI = 1000;


    [SerializeField]
    private bool reachedFinal = false;
    public bool ReachedFinal { get => reachedFinal; set { reachedFinal = value; } }

    // Start is called before the first frame update
    void Start()
    {
        minLevel = initialLevel;
        animator = GetComponent<Animator>();
        openLevel = initialLevel;
        animator.SetFloat("InputAxis1", openLevel);
        pinRenderer = GetComponentsInChildren<Renderer>();
        handManager = GameObject.FindGameObjectWithTag("Hand").GetComponentInChildren<ACESHandAnimation>(); 
    }

    // Collider
    private void OnTriggerEnter(Collider other)
    {
        // Check if touched by subject
        //other.tag == "IndexFingerCollider" ||
        if (other.CompareTag("IndexFingerCollider"))
        {
            indexFingerTouched = true;
            //Debug.Log("Index touched!");
        }

        if (other.CompareTag("ThumbFingerCollider"))
        {
            thumbFingerTouched = true;
            //Debug.Log("Thumb touched!");
        }

        if (other.CompareTag("ClothespinRackRod"))
        {
            touchRod = true;
            //Debug.Log("Rod touched!");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // Check if touched by subject
        //other.tag == "IndexFingerCollider" ||
        if (other.CompareTag("IndexFingerCollider"))
        {
            indexFingerTouched = false;
            //Debug.Log("Index leave!");
        }

        if (other.CompareTag("ThumbFingerCollider"))
        {
            thumbFingerTouched = false;
            //Debug.Log("Thumb leave!");
        }

        if (other.CompareTag("ClothespinRackRod"))
        {
            touchRod = false;
            //Debug.Log("Rod leave!");
        }
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        tempGrasped = indexFingerTouched && thumbFingerTouched;
        if (tempGrasped)
        {
            iGrasped++;
            iGrasped = Mathf.Min(maxI, iGrasped);
            iNotGrasped = 0;
        } 
        else
        {
            iNotGrasped++;
            iNotGrasped = Mathf.Min(maxI, iNotGrasped);
            iGrasped = 0;
        }
            
        switch (pinState)
        {
            case ClothespinState.Idle:
                if (tempGrasped)
                {
                    ChangeClothespinColor(wrongColour);
                    OpenClothespin();
                    pinState = ClothespinState.Wrong;
                }
                else
                {
                    ChangeClothespinColor(idleColour);
                    CloseClothespin();
                    pinState = ClothespinState.Idle;
                }
                
                break;
            case ClothespinState.Selected:
                if (tempGrasped)
                {
                    ChangeClothespinColor(correctColour);
                    OpenClothespin();
                    pinState = ClothespinState.InHand;
                }
                else
                {
                    ChangeClothespinColor(selectedColour);
                    CloseClothespin();
                    pinState = ClothespinState.Selected;
                    
                }
                break;
            case ClothespinState.InHand:

                FollowHand(true);
                OpenClothespin();

                
                if (!tempGrasped)
                {
                    ChangeClothespinColor(selectedColour);
                    CloseClothespin();
                    FollowHand(false);
                    SetTransform(initPosition, initRotation);
                    pinState = ClothespinState.Selected;
                    Debug.Log("Lost grasp");
                    break;
                }

                if (openLevel == maxLevel && !CheckAtTargetTransform(initPosition, initRotation, posTol * 4, 360)) // Fully opened or not touching the rod, go to next state
                {
                    ChangeClothespinColor(movingColour);
                    pinState = ClothespinState.LeaveInit;
                    break;
                }
                    
                break;
            case ClothespinState.LeaveInit:

                OpenClothespin();

                // Leaving the initial position & rotation
                //if (!CheckAtTargetTransform(initPosition, initRotation, posTol, angTol))
                //{
                    //ChangeClothespinColor(movingColour);
                //}

                if (!indexFingerTouched && !thumbFingerTouched && handManager.State == ACESHandAnimation.HandStates.Open)
                {
                    CloseClothespin();
                    if (openLevel == minLevel)
                    {
                        ChangeClothespinColor(selectedColour);
                        FollowHand(false);
                        SetTransform(initPosition, initRotation);
                        pinState = ClothespinState.Selected;
                        Debug.Log("Lost grasp");
                        break;
                    }
                }
                    

                // Reaching the final position & rotation
                if (CheckAtTargetTransform(finalPosition, finalRotation, posTol, angTol) && !touchRod)
                {
                    pinState = ClothespinState.ReachTarget;
                }
                
                break;
            case ClothespinState.ReachTarget:
                if ((!indexFingerTouched || !thumbFingerTouched) && handManager.State == ACESHandAnimation.HandStates.Open)
                {
                    CloseClothespin();
                }
                    
                    
                if (CheckAtTargetTransform(finalPosition, finalRotation, posTol, angTol))
                {
                    ChangeClothespinColor(correctColour);
                    if (openLevel == initialLevel)
                    {
                        FollowHand(false);
                        CloseClothespin();
                        this.initPosition = GetPosition();
                        this.initRotation = GetRotation();
                        reachedFinal = true;
                        pinState = ClothespinState.Idle;
                        Debug.Log("Complte back to normal");
                        break;
                    }
                }
                else
                {
                    pinState = ClothespinState.LeaveInit;
                    ChangeClothespinColor(movingColour);
                    break;
                }
 
                break;
            case ClothespinState.Wrong:
                if (tempGrasped)
                {
                    OpenClothespin();
                }
                else
                {
                    pinState = ClothespinState.Idle;
                    ChangeClothespinColor(idleColour);
                    CloseClothespin();
                    break;
                }
                break;
        }
        
    }


    //
    // Public methods
    //
    #region public methods

    //
    // Set the clothespin to selected state.
    //
    public void SetSelect()
    {
        pinState = ClothespinState.Selected;
    }

    //
    // Set the clothespin transform
    //
    public void SetTransform(Vector3 position, Quaternion rotation)
    {
        this.transform.parent.parent.position = position;
        this.transform.parent.parent.rotation = rotation;
    }

    //
    // Get the clothespin position
    //
    public Vector3 GetPosition()
    {
        return this.transform.parent.parent.position;
    }

    //
    // Get the clothespin rotation
    //
    public Quaternion GetRotation()
    {
        return this.transform.parent.parent.rotation;
    }

    //
    // Set the target init transform of the clothespin
    //
    public void SetInitTransform(Vector3 initPosition, Quaternion initRotation)
    {
        this.initPosition = initPosition;
        this.initRotation = initRotation;
    }
    //
    // Set the target final transform of the clothespin
    //
    public void SetFinalTransform(Vector3 finalPosition, Quaternion finalRotation)
    {
        this.finalPosition = finalPosition;
        this.finalRotation = finalRotation;
    }

    #endregion


    //
    // Private methods
    //
    #region private emthods

    //
    // Set the clothespin to follow the hand
    //
    private void FollowHand(bool follow)
    {
        if (follow)
            this.transform.parent.parent.SetParent(GameObject.FindGameObjectWithTag("Hand").transform);
        else
            this.transform.parent.parent.SetParent(null);
        
    }


    //
    // Check if the clothespin is at the 
    //
    private bool CheckAtTargetTransform(Vector3 targetPosition, Quaternion targetRotation, Vector3 posTol, float angTol)
    {
        Transform current = this.transform.parent.parent; // two layer hiearchy

        // Position
        Vector3 errorPos = new Vector3(Mathf.Abs(current.position.x - targetPosition.x),
                                        Mathf.Abs(current.position.y - targetPosition.y),
                                        Mathf.Abs(current.position.z - targetPosition.z));

        bool posReached = errorPos.x < posTol.x & errorPos.y < posTol.y & errorPos.z < posTol.z;

        // Angular
        Quaternion relative = Quaternion.Inverse(current.rotation) * targetRotation;
        float errorAng = 2.0f * Mathf.Rad2Deg * Mathf.Atan2(Mathf.Sqrt(relative.x * relative.x + relative.y * relative.y + relative.z * relative.z), relative.w);
        if (errorAng > 180.0f)
            errorAng = 360.0f - errorAng;
        bool angReached = Mathf.Abs(errorAng) <= angTol;
        

        this.errorPos = new Vector3 (errorPos.x, errorPos.y, errorPos.z);
        this.errorAng = errorAng;

        //Debug.Log("Peg angular error:" + angTol);
        //angReached = true;
        return posReached & angReached;
    }


    private void ChangeClothespinColor(Color color)
    {
        foreach (Renderer renderer in pinRenderer)
        {
            renderer.material.color = color;
        }
    }

    private void OpenClothespin()
    {
        openLevel += deltaLevel * (1 - (1.0f * iGrasped / (1.0f * maxI)));

        if (openLevel > maxLevel)
        {
            openLevel = maxLevel;
            
        }

        else if (openLevel < minLevel)
        {
            openLevel = minLevel;
            
        }
            
        animator.SetFloat("InputAxis1", openLevel);
    }

    private void CloseClothespin()
    {

        openLevel -= springCoeff * (1.0f * iNotGrasped/ (1.0f * maxI)); // soringcoeff to control bounce back

        if (openLevel > maxLevel)
        {
            openLevel = maxLevel;
            
        }
        else if (openLevel < minLevel)
        {
            openLevel = minLevel;
            
        }  
        animator.SetFloat("InputAxis1", openLevel);
    }
    #endregion





}
