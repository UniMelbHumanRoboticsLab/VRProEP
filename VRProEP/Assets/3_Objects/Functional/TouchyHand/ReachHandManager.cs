using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class ReachHandManager : MonoBehaviour
{
    public enum ReachHandState { Idle, Selected, Correct, Wrong }

    [Header("Objects")]
    [SerializeField]
    private GameObject avatarHand;
    
    [Header("Tolerance")]
    [SerializeField]
    Vector3 positionTolerance;
    [SerializeField]
    Vector3 rotationTolerance;
    [SerializeField]
    float rotationToleranceAngle;

    [Header("Colour configuration")]
    [SerializeField]
    private Color idleColor;
    [SerializeField]
    private Color selectedColor;
    [SerializeField]
    private Color correctColor;
    [SerializeField]
    private Color wrongColor;
    private Renderer handRenderer;

    // State
    private ReachHandState handState = ReachHandState.Idle;
    public ReachHandState HandState { get => handState; }


    // Reset coroutine
    private Coroutine resetCoroutine;
    private bool isWaiting = false;

    //Constants
    private float WAIT_SECONDS = 1.0f;
    private float MIN_HOLD_TIME = 0.1f;

    // Error
    private Vector3 errorPos;
    public Vector3 ErrorPos { get => errorPos; }
    private float errorAng;
    public float ErrorAng { get => errorAng; }
    private Vector3 errorAngVec;
    public Vector3 ErrorAngVec { get => errorAngVec; }

    private float holdTime;

    // Trial complete flag
    [SerializeField]
    private bool reachedFinal = false;
    public bool ReachedFinal { get => reachedFinal; set { reachedFinal = value; } }

    void Update()
    {
       
    }

    void Awake()
    {
        avatarHand = GameObject.FindGameObjectWithTag("Hand");
        handRenderer = GetComponentInChildren<SkinnedMeshRenderer>();

        handRenderer.material.color = idleColor;
    }

    /// <summary>
    /// Trigger when bottle is entering
    /// </summary>
    /// <param >
    /// <returns >
    private void OnTriggerStay(Collider other)
    {
        //Debug.Log("Collide");

        // Check if hand is gone to return to idle.
        if (other.tag == "ThumbFingerCollider" || other.tag == "IndexFingerCollider")
        {
            //Debug.Log("Hand collide");
            if (CheckReached())
            {
                switch (handState)
                {
                    case ReachHandState.Idle:
                        handState = ReachHandState.Wrong;
                        handRenderer.material.color = wrongColor;
                        break;
                    case ReachHandState.Selected:
                        handState = ReachHandState.Correct;
                        handRenderer.material.color = correctColor;
                        break;
                    case ReachHandState.Correct:
                        reachedFinal = true;
                        break;
                    case ReachHandState.Wrong:

                        break;
                }
            }
        }
    }

    /// <summary>
    /// Trigger when bottle is leaving
    /// </summary>
    /// <param >
    /// <returns >
    private void OnTriggerExit(Collider other)
    {
        // Check if hand is gone to return to idle.
        if (other.tag == "Hand" && !isWaiting)
        {
            
            //resetCoroutine = StartCoroutine(ReturnToIdle(WAIT_SECONDS));
            
        }
    }

    #region private methods
    /// <summary>
    /// Check if the bottle reaches the target postion within the error tolerance.
    /// </summary>
    /// <param >
    /// <returns bool reached>
    private bool CheckReached()
    {
        bool reachedFinal = false;

        bool positionReached = false;
        bool orientationReached = false;
        bool reached = false;

        // Previous
        bool positionReachedPrev = Mathf.Abs(errorPos.x) < positionTolerance.x && Mathf.Abs(errorPos.y) < positionTolerance.y && Mathf.Abs(errorPos.z) < positionTolerance.z;
        bool orientationReachedPrev = Mathf.Abs(errorAng) <= rotationToleranceAngle;
        bool reachedPrev = positionReachedPrev & orientationReachedPrev;

        // Current
        this.errorPos = this.transform.position - avatarHand.transform.position;

        // Quaternion angle difference method
        Quaternion targetRotation = this.gameObject.transform.rotation;
        Quaternion avatarHandRotation = avatarHand.transform.rotation;
        Quaternion relative = Quaternion.Inverse(avatarHandRotation) * targetRotation;
        this.errorAng = 2.0f * Mathf.Rad2Deg * Mathf.Atan2(Mathf.Sqrt(relative.x * relative.x + relative.y * relative.y + relative.z * relative.z),relative.w);
        if (errorAng > 180.0f)
            errorAng = 360.0f - errorAng;


        positionReached = Mathf.Abs(errorPos.x) < positionTolerance.x && Mathf.Abs(errorPos.y) < positionTolerance.y && Mathf.Abs(errorPos.z) < positionTolerance.z;
        orientationReached =  Mathf.Abs(errorAng) <= rotationToleranceAngle;
        reached = positionReached & orientationReached;

        // Debug Log
        //Debug.Log(errorPos.ToString("F3"));
        //Debug.Log(errorAng.ToString("F3"));

        if (reachedPrev && reached)
            holdTime += Time.fixedDeltaTime;
        else
            holdTime = 0.0f;

        reachedFinal = reached && holdTime > MIN_HOLD_TIME;
            
        return reachedFinal;
    }

    /// <summary>
    /// Wait some seconds before returning to idle state.
    /// If it was selected in the middle of the wait, then just stay selected.
    /// </summary>
    /// <param name="waitSeconds">The number of seconds to wait.</param>
    /// <returns>IEnumerator used for the Coroutine.</returns>
        
    private IEnumerator ReturnToIdle(float waitSeconds)
    {
        isWaiting = true;       
        yield return new WaitForSecondsRealtime(waitSeconds);
        handState = ReachHandState.Idle;
        handRenderer.material.color = idleColor;

        isWaiting = false;
       // Debug.Log(bottleRenderer.material.color);
    }
    #endregion

    #region public methods
    /// <summary>
    /// Set bottle the selected one.
    /// </summary>
    public void SetSelected()
    {
        if (resetCoroutine != null)
            StopCoroutine(resetCoroutine);

        handState = ReachHandState.Selected;
        handRenderer.material.color = selectedColor;
        reachedFinal = false;
    }

    /// <summary>
    /// Resets the selection to idle.
    /// </summary>
    public void ClearSelection()
    {
        if (resetCoroutine != null)
            StopCoroutine(resetCoroutine);

        resetCoroutine = StartCoroutine(ReturnToIdle(0.1f));
    }

    #endregion




}
