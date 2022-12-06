using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class ReachBottleManager : MonoBehaviour
{
    public enum ReachBottleState { Idle, Selected, Correct, Wrong }

    [Header("Objects")]
    [SerializeField]
    private GameObject bottleInHand;
    
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


    private ReachBottleState bottleState = ReachBottleState.Idle;
    
    private Renderer[] allRenderer;
    private Renderer bottleRenderer;
    private Renderer baseRenderer;
    public ReachBottleState BottleState { get => bottleState; }


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
        bottleInHand = GameObject.FindGameObjectWithTag("Bottle");
        bottleRenderer = GetComponent<Renderer>();
        //Debug.Log(bottleRenderer.ToString());
        allRenderer = GetComponentsInChildren<Renderer>();
        //bottleRenderer = allRenderer[0];
        baseRenderer = allRenderer[0];

        bottleRenderer.material.color = idleColor;
        baseRenderer.material.color = idleColor;
    }

    /// <summary>
    /// Trigger when bottle is entering
    /// </summary>
    /// <param >
    /// <returns >
    private void OnTriggerStay(Collider other)
    {
        // Check if hand is gone to return to idle.
        if (other.tag == "Bottle")
        {
            bottleInHand = GameObject.FindGameObjectWithTag("Bottle");
            //Debug.Log("Collide");
            if (CheckReached())
            {
                switch (bottleState)
                {
                    case ReachBottleState.Idle:
                        bottleState = ReachBottleState.Wrong;
                        bottleRenderer.material.color = wrongColor;
                        baseRenderer.material.color = wrongColor;
                        break;
                    case ReachBottleState.Selected:
                        bottleState = ReachBottleState.Correct;
                        bottleRenderer.material.color = correctColor;
                        baseRenderer.material.color = correctColor;
                        break;
                    case ReachBottleState.Correct:

                        break;
                    case ReachBottleState.Wrong:

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
        if (other.tag == "Bottle" && !isWaiting)
        {
            
            resetCoroutine = StartCoroutine(ReturnToIdle(WAIT_SECONDS));
            
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
        this.errorPos = this.transform.position - bottleInHand.transform.position;

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
        Quaternion bottleInHandRotation = bottleInHand.transform.localRotation;
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
        //Debug.Log(bottleInHandRotation);
        //Debug.Log(bottleInHand.transform.position);
        //Debug.Log(postionError);
        Debug.Log(errorAng);

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
        bottleState = ReachBottleState.Idle;
        bottleRenderer.material.color = idleColor;
        baseRenderer.material.color = idleColor;

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
        this.bottleInHand = bottleInHand;
    }


    /// <summary>
    /// Set bottle the selected one.
    /// </summary>
    public void SetSelected()
    {
        if (resetCoroutine != null)
            StopCoroutine(resetCoroutine);

        bottleState = ReachBottleState.Selected;
        bottleRenderer.material.color = selectedColor;
        baseRenderer.material.color = selectedColor;
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
