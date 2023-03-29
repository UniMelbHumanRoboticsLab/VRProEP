using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class ReachHandManager : MonoBehaviour
{
    public enum ReachHandState { Idle, Selected, Correct, Wrong }

    [Header("Objects")]
    [SerializeField]
    private GameObject hand;
    
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


    private ReachHandState handState = ReachHandState.Idle;
    
    private Renderer[] allRenderer;
    private Renderer handRenderer;
    private Renderer baseRenderer;
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

    void Update()
    {
       
        //Debug.Log(BottleState);
        //Debug.Log(bottleState);
        /*
            if (CheckReaching())
                Debug.Log("The bottle is inside of the area");
            else
                Debug.Log("The bottle is outside of the area");
            */
    }

    void Awake()
    {
        hand = GameObject.FindGameObjectWithTag("Hand");
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
        Debug.Log("Collide");
        // Check if hand is gone to return to idle.
        if (other.tag == "Hand")
        {
            //hand = GameObject.FindGameObjectWithTag("Hand");
            //Debug.Log("Collide");
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
        //bool orientationReachedPrev = Mathf.Abs(errorAngVec.x) < rotationTolerance.x && Mathf.Abs(errorAngVec.y) < rotationTolerance.y && Mathf.Abs(errorAngVec.z) < rotationTolerance.z;
        bool reachedPrev = positionReachedPrev & orientationReachedPrev;

        // Current
        this.errorPos = this.transform.position - hand.transform.position;

        // Just check frontal orientation
        /*
        Quaternion targetRotation = this.gameObject.transform.rotation;
        Quaternion bottleInHandRotation = Quaternion.Inverse(targetRotation) * bottleInHand.transform.rotation;
        Vector3 targetAxis = Vector3.up;
        Vector3 bottleAxis = bottleInHandRotation * Vector3.up;
        this.errorAng = Vector2.Angle( new Vector2(targetAxis.y, targetAxis.z), new Vector2(bottleAxis.y, bottleAxis.z));
        */

        // Quaternion angle difference method
        Quaternion targetRotation = this.gameObject.transform.localRotation;
        Quaternion bottleInHandRotation = hand.transform.localRotation;
        Quaternion relative = Quaternion.Inverse(bottleInHandRotation) * targetRotation;
        //this.errorAng = Quaternion.Angle(targetRotation, bottleInHandRotation) / 2.0f;
        this.errorAng = 2.0f * Mathf.Rad2Deg * Mathf.Atan2(Mathf.Sqrt(relative.x * relative.x + relative.y * relative.y + relative.z * relative.z),relative.w);
        if (errorAng > 180.0f)
            errorAng = 360.0f - errorAng;

        /*
        Quaternion targetRotation = this.gameObject.transform.localRotation;
        Matrix4x4 m1 = Matrix4x4.Rotate(targetRotation);
        Quaternion bottleInHandRotation = bottleInHand.transform.localRotation;
        Matrix4x4 m2 = Matrix4x4.Rotate(bottleInHandRotation);
        Vector3 temp = 0.5f * (Vector3.Cross(m1.GetColumn(0),m2.GetColumn(0)) + Vector3.Cross(m1.GetColumn(1), m2.GetColumn(1)) + Vector3.Cross(m1.GetColumn(2), m2.GetColumn(2)));
        this.errorAngVec.x = Mathf.Rad2Deg * Mathf.Asin(temp.x);
        this.errorAngVec.y = Mathf.Rad2Deg * Mathf.Asin(temp.y);
        this.errorAngVec.z = Mathf.Rad2Deg * Mathf.Asin(temp.z);
        */

        positionReached = Mathf.Abs(errorPos.x) < positionTolerance.x && Mathf.Abs(errorPos.y) < positionTolerance.y && Mathf.Abs(errorPos.z) < positionTolerance.z;
        orientationReached =  Mathf.Abs(errorAng) <= rotationToleranceAngle;
        //orientationReached = Mathf.Abs(errorAngVec.x) < rotationTolerance.x && Mathf.Abs(errorAngVec.y) < rotationTolerance.y && Mathf.Abs(errorAngVec.z) < rotationTolerance.z;
        reached = positionReached & orientationReached;

        // Debug Log
        Debug.Log(errorPos);
        //Debug.Log(errorAng);

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

    #region public methods
    /// <summary>
    /// Set the in hand bottle gameobject
    /// </summary>
    /// <param GameObject bottleInHand>
    /// <returns>
    public void SetBottlInHand(GameObject bottleInHand)
    {
        this.hand = bottleInHand;
    }


    /// <summary>
    /// Set bottle the selected one.
    /// </summary>
    public void SetSelected()
    {
        if (resetCoroutine != null)
            StopCoroutine(resetCoroutine);

        handState = ReachHandState.Selected;
        handRenderer.material.color = selectedColor;
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
