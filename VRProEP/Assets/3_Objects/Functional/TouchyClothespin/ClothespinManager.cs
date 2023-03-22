using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClothespinManager : MonoBehaviour
{
    //
    // Flow control state
    //
    public enum ClothespinState { Idle, Selected, InHand, LeaveInit, ReachTarget, Wrong}
    private ClothespinState pinState;
    // Grasp State
    private bool grasped;

    //
    // Transform control
    //
    private Vector3 initPosition;
    private Quaternion initRotation;
    private Vector3 finalPosition;
    private Quaternion finalRotation;
    [SerializeField]
    private Vector3 posTol { get; set; }
    [SerializeField]
    private float angTol { get; set; }

    //
    // Color and display
    //
    [Header("Colour configuration")]
    [SerializeField]
    private Color idleColour;
    [SerializeField]
    private Color selectedColour;
    [SerializeField]
    private Color correctColour;
    [SerializeField]
    private Color movingColour;
    [SerializeField]
    private Color wrongColour;
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

    //
    // State control
    //
    private bool indexFingerTouched = false;
    private bool thumbFingerTouched = false;
    private bool touchRod = true;


    // Start is called before the first frame update
    void Awake()
    {
        minLevel = initialLevel;
        animator = GetComponent<Animator>();
        openLevel = initialLevel;
        animator.SetFloat("InputAxis1", openLevel);
        pinRenderer = GetComponentsInChildren<Renderer>();

        posTol = new Vector3(0.02f,0.02f,0.02f);
        angTol = 360.0f;
    }

    // Collider
    private void OnTriggerEnter(Collider other)
    {
        // Check if touched by subject
        //other.tag == "IndexFingerCollider" ||
        if (other.tag == "IndexFingerCollider")
        {
            indexFingerTouched = true;
            //Debug.Log("Index touched!");
        }

        if (other.tag == "ThumbFingerCollider")
        {
            thumbFingerTouched = true;
            //Debug.Log("Thumb touched!");
        }

        if (other.tag == "ClothespinRackRod")
        {
            touchRod = true;
            Debug.Log("Rod touched!");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // Check if touched by subject
        //other.tag == "IndexFingerCollider" ||
        if (other.tag == "IndexFingerCollider")
        {
            indexFingerTouched = false;
            //Debug.Log("Index leave!");
        }

        if (other.tag == "ThumbFingerCollider")
        {
            thumbFingerTouched = false;
            //Debug.Log("Thumb leave!");
        }

        if (other.tag == "ClothespinRackRod")
        {
            touchRod = false;
            Debug.Log("Rod leave!");
        }
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        grasped = indexFingerTouched && thumbFingerTouched;
        switch (pinState)
        {
            case ClothespinState.Idle:
                if (grasped)
                {
                    ChangeClothespinColor(wrongColour);
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
                if (grasped)
                {
                    ChangeClothespinColor(correctColour);
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
                if (grasped)
                {
                    OpenClothespin();
                    FollowHand(true);
                    if (CheckAtTargetTransform(initPosition,initRotation, posTol, angTol))
                    {
                        pinState = ClothespinState.LeaveInit;
                    }
                }
                else
                {
                    ChangeClothespinColor(selectedColour);
                    CloseClothespin();
                    FollowHand(false);
                    SetTransform(initPosition, initRotation);
                    pinState = ClothespinState.Selected;
                    
                }
                break;
            case ClothespinState.LeaveInit:
                if (grasped)
                {
                    OpenClothespin();
                    // Leaving the initial position & rotation
                    if (!CheckAtTargetTransform(initPosition, initRotation, posTol, angTol))
                    {
                        ChangeClothespinColor(movingColour);
                    }
                    // Reaching the final position & rotation
                    if (CheckAtTargetTransform(finalPosition, finalRotation, posTol, angTol) && !touchRod)
                    {
                        ChangeClothespinColor(correctColour);
                        pinState = ClothespinState.ReachTarget;
                    }

                }
                else
                {
                    ChangeClothespinColor(selectedColour);
                    CloseClothespin();
                    FollowHand(false);
                    SetTransform(initPosition, initRotation);
                    pinState = ClothespinState.Selected;
                }
                break;
            case ClothespinState.ReachTarget:
                if (grasped)
                {
                    ChangeClothespinColor(correctColour);
                }
                else
                {
                    if (CheckAtTargetTransform(finalPosition, finalRotation, posTol, angTol))
                    {
                        FollowHand(false);
                        CloseClothespin();
                        pinState = ClothespinState.Idle;
                    }
                }
                break;
            case ClothespinState.Wrong:
                if (grasped)
                {
                    OpenClothespin();
                }
                else
                {
                    pinState = ClothespinState.Idle;
                    ChangeClothespinColor(idleColour);
                    CloseClothespin();
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
    // Set the target init and final transform of the clothespin
    //
    public void SetTargetTransform(Vector3 initPosition, Quaternion initRotation, Vector3 finalPosition, Quaternion finalRotation)
    {
        this.initPosition = initPosition;
        this.initRotation = initRotation;
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
        bool posReached = Mathf.Abs(current.position.x - targetPosition.x) < posTol.x
                    & Mathf.Abs(current.position.y - targetPosition.y) < posTol.y
                    & Mathf.Abs(current.position.z - targetPosition.z) < posTol.z;


        // Angular
        Quaternion relative = Quaternion.Inverse(current.rotation) * targetRotation;
        float errorAng = 2.0f * Mathf.Rad2Deg * Mathf.Atan2(Mathf.Sqrt(relative.x * relative.x + relative.y * relative.y + relative.z * relative.z), relative.w);
        if (errorAng > 180.0f)
            errorAng = 360.0f - errorAng;
        bool angReached = Mathf.Abs(errorAng) <= angTol;
        angReached = true;

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
        openLevel += deltaLevel;
        if (openLevel > maxLevel)
        {
            openLevel = maxLevel;
            return;
        }

        else if (openLevel < minLevel)
        {
            openLevel = minLevel;
            return;
        }
            
        animator.SetFloat("InputAxis1", openLevel);
    }

    private void CloseClothespin()
    {
        openLevel -= deltaLevel * 2;
        if (openLevel > maxLevel)
        {
            openLevel = maxLevel;
            return;
        }
        else if (openLevel < minLevel)
        {
            openLevel = minLevel;
            return;
        }  
        animator.SetFloat("InputAxis1", openLevel);
    }
    #endregion





}
