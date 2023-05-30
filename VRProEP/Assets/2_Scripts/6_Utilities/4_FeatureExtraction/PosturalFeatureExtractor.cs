using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * Static class for postural feature extraction using trackers' position and quaternion readings
 * 
 */

public static class PosturalFeatureExtractor
{
    private static float trunkFE = 0; // Trunk flexion extension, Postivie: bend trunk forward
    private static float trunkLRB = 0; // Trunk left right bending, Postivie: bend trunk to right
    private static float trunkR = 0; // Trunk Rotation, Postivie: rotate trunk to right

    private static float shoulderFE = 0; // Shoulder flexion extension, Postivie: flex forward
    private static float shoulderABD = 0; // Shoulder adducation abduction, Postivie: towards the body
    private static float shoulderR = 0; // Shoulder internal rotation, Postivie: rotate inwards

    private static float scapularDE = 0; // Scapular depression elevation, Postivie: upward
    private static float scapularPR = 0; // Scapular protraction retraction, Postivie: forward

    private static float elbowFE = 0; //Elbow flexion and extension, Positive: towards body, Zero: fully relax

    private static float wristPS = 0; // Wrist pronation supination, Positive: inner rotation towards body, Zero: neutral pose
    private static float wristFE = 0; // Currently not available
    private static float wristAA = 0; // Currently not availabel


    private static float sgn = 1;

    public static float getShoulderFE()
    {
        return shoulderFE;
    }


    public static float[] ExtractTrunkPose(Quaternion qC7TrackerRef, Quaternion qC7Tracker)
    {
        Vector3 initialTrunkAxial =  Vector3.up;
        Vector3 initialTrunkLateral = Vector3.right;

        Vector3 currentTrunkAxial = Quaternion.Inverse(qC7TrackerRef) * qC7Tracker * Vector3.up;
        Vector3 currentTrunkLateral = Quaternion.Inverse(qC7TrackerRef) * qC7Tracker * Vector3.right;

        //Vector3 initialTrunkAxial = qC7TrackerRef * Vector3.up;
        //Vector3 initialTrunkLateral = qC7TrackerRef * Vector3.right;

        //Vector3 currentTrunkAxial = qC7Tracker * Vector3.up;
        //Vector3 currentTrunkLateral = qC7Tracker * Vector3.right;


        //Debug.Log(currentTrunkSaggital);
        //Debug.Log(initialTrunkSaggital);

        // Trunk Flexion/extension
        Vector2 vYZ1 = new Vector2(currentTrunkAxial.y, currentTrunkAxial.z);
        Vector2 vYZ2 = new Vector2(initialTrunkAxial.y, initialTrunkAxial.z);
        if (vYZ1.y > 0) sgn = -1; else sgn = 1;
        trunkFE = sgn * Vector2.Angle(vYZ1, vYZ2) * Mathf.Deg2Rad;
        //Debug.Log("Trunk FE: " + Mathf.Rad2Deg * trunkFE);

        // Trunk Left right bending
        Vector2 vXY1 = new Vector2(currentTrunkLateral.x, currentTrunkLateral.y);
        Vector2 vXY2 = new Vector2(initialTrunkLateral.x, initialTrunkLateral.y);
        if (vXY1.y > 0) sgn = -1; else sgn = 1;
        trunkLRB = sgn * Vector2.Angle(vXY1, vXY2) * Mathf.Deg2Rad;
        //Debug.Log("Trunk LRB: " + Mathf.Rad2Deg * trunkLRB);

        // Trunk Rotation
        Vector2 vXZ1 = new Vector2(currentTrunkLateral.x, currentTrunkLateral.z);
        Vector2 vXZ2 = new Vector2(initialTrunkLateral.x, initialTrunkLateral.z);
        if (vXZ1.y > 0) sgn = 1; else sgn = -1;
        trunkR = sgn * Vector2.Angle(vXZ1, vXZ2) * Mathf.Deg2Rad;
        //Debug.Log("Trunk R: " + Mathf.Rad2Deg * trunkR);
        


        float[] pose = new float[3] { trunkFE, trunkLRB, trunkR };
        return pose;
        //Quaternion relativeRot = qC7Tracker * Quaternion.Inverse(qC7TrackerRef);
    }

    public static float[] ExtractShoulderPose(Quaternion qC7Tracker, Quaternion qUpperarmTracker)
    {   

        // Caculate shoulder angles in C7's frame
        Quaternion relativeRot = Quaternion.Inverse(qC7Tracker);
        Vector3 currentShoulderAxial = relativeRot * (qUpperarmTracker * Vector3.up);
        Vector3 currentTrunkAxial = -Vector3.up;
        Vector3 currentShoulderLateral = relativeRot * (qUpperarmTracker * Vector3.forward);
        Vector3 currentTrunkLateral = -Vector3.right;

        // Debug.Log(currentShoulderAxial);

        // Shoulder Flexion / extension
        Vector2 vYZ1 = new Vector2(currentShoulderAxial.y, currentShoulderAxial.z);
        Vector2 vYZ2 = new Vector2(currentTrunkAxial.y, currentTrunkAxial.z);
        if (vYZ1.y > 0) sgn = -1; else sgn = 1;
        shoulderFE = sgn * Vector2.Angle(vYZ1, vYZ2) * Mathf.Deg2Rad;
        //Debug.Log("Shoudler FE: " + Mathf.Rad2Deg * shoulderFE);

        // Shoulder Adduction / Abdictopm
        Vector2 vXY1 = new Vector2(currentShoulderAxial.x, currentShoulderAxial.y);
        Vector2 vXY2 = new Vector2(currentTrunkAxial.x, currentTrunkAxial.y);
        if (vXY1.x > 0) sgn = -1; else sgn = 1;
        shoulderABD = sgn * Vector2.Angle(vXY1, vXY2) * Mathf.Deg2Rad;
        //Debug.Log("Shoudler ABD: " + Mathf.Rad2Deg * shoulderABD);

        //Shoulder Rotation
        Vector2 vXZ1 = new Vector2(currentShoulderLateral.x, currentShoulderLateral.z);
        Vector2 vXZ2 = new Vector2(currentTrunkLateral.x, currentTrunkLateral.z);
        if (vXZ1.y > 0) sgn = 1; else sgn = -1;
        shoulderR = sgn * Vector2.Angle(vXZ1, vXZ2) * Mathf.Deg2Rad;
        //Debug.Log("Shoudler R: " + Mathf.Rad2Deg * shoulderR);



        float[] pose = new float[3] { shoulderFE, shoulderABD, shoulderR };
        return pose;
    }




    public static float[] ExtractScapularPose(Vector3 c7TrackerRef, Vector3 shoulderTrackerRef, Vector3 c7Tracker,  Vector3 shoulderTracker, Quaternion qC7Tracker)
    {
        Vector3 initialAcromion = shoulderTrackerRef - c7TrackerRef;
        Vector3 currrentAcromion = shoulderTracker - c7Tracker;

        Vector3 diff = currrentAcromion - initialAcromion;
        Quaternion relativeRot = Quaternion.Inverse(qC7Tracker);
        diff = relativeRot * diff;
        //Debug.Log(diff);

        // Scapular depression / elevation
        scapularDE = - diff.y;
        //Debug.Log("Scapular DE Vec: " + 100* scapularDE);

        // Scapular protraction / retraction
        scapularPR = diff.z;
        //Debug.Log("Scapular PR Vec: " + 100* scapularPR);

        float[] pose = new float[2] { scapularDE, scapularPR };
        return pose;
    }


    public static float[] ExtractScapularPose(Quaternion qC7TrackerRef, Quaternion qShoulderTrackerRef, Quaternion qC7Tracker, Quaternion qShoulderTracker, float shoulderBreadth)
    {
        // Convert to C7 frame
        Quaternion relativeRot = Quaternion.Inverse(qC7Tracker);
        Quaternion relativeRotInit = Quaternion.Inverse(qC7TrackerRef);
        Vector3 currentAcromionAxial = relativeRot * (qShoulderTracker * Vector3.up); // Vector along c7 to shoulder acromion
        Vector3 initialAcromionAxial = relativeRotInit * (qShoulderTrackerRef * Vector3.up);

        // Scapular depression / elevation
        Vector2 vXY1 = new Vector2(currentAcromionAxial.x, currentAcromionAxial.y);
        Vector2 vXY2 = new Vector2(initialAcromionAxial.x, initialAcromionAxial.y);
        if (vXY1.y > vXY2.y) sgn = -1; else sgn = 1;
        float angleDE = Vector2.Angle(vXY1, vXY2);
        scapularDE = sgn * (shoulderBreadth / 2.0f) * Mathf.Sin(angleDE * Mathf.Deg2Rad);
        //Debug.Log(angleDE);
        //Debug.Log("Scapular DE Quat: " + 100 * scapularDE);

        // Scapular protraction / retraction
        Vector2 vXZ1 = new Vector2(currentAcromionAxial.x, currentAcromionAxial.z);
        Vector2 vXZ2 = new Vector2(initialAcromionAxial.x, initialAcromionAxial.z);
        if (vXZ1.y > vXZ2.y) sgn = 1; else sgn = -1;
        float anglePR = Vector2.Angle(vXZ1, vXZ2);
        scapularPR =  sgn * (shoulderBreadth / 2.0f) * Mathf.Sin(anglePR * Mathf.Deg2Rad);
        //Debug.Log(anglePR);
        //Debug.Log("Scapular PR Quat: " + 100 * scapularPR);


        float[] pose = new float[2] { scapularDE, scapularPR };
        return pose;
    }


    public static float[] ExtractElbowPose(Quaternion qUpperarmTracker, Quaternion qForearmTracker)
    {

        // Caculate elbow angles in upperarm's frame
        Quaternion relativeRot = Quaternion.Inverse(qUpperarmTracker);
        Vector3 currentForearmrAxial = relativeRot * (qForearmTracker * Vector3.up);
        Vector3 currentShoulderAxial = -Vector3.up;
        Vector3 currentForearmLateral = relativeRot * (qForearmTracker * Vector3.forward);
        Vector3 currentShoulderLateral = Vector3.forward;

        // Elbow Flexion / extension
        Vector2 vYZ1 = new Vector2(currentForearmrAxial.x, currentForearmrAxial.y);
        Vector2 vYZ2 = new Vector2(currentShoulderAxial.x, currentShoulderAxial.y);
        elbowFE = Vector2.Angle(vYZ1, vYZ2) * Mathf.Deg2Rad;
        //Debug.Log("Elbow FE: " + Mathf.Rad2Deg * elbowFE);


        float[] pose = { elbowFE };
        return pose;
    }


    public static float[] ExtractWristPose(Quaternion qUpperarmTracker, Quaternion qHandTracker)
    {
        // Need a dummy tracker which is on the real forearm rigid body not on the hand
        Quaternion qForearmTracker = qUpperarmTracker * Quaternion.Euler(0, 0, -elbowFE * Mathf.Rad2Deg);

        // Caculate elbow angles in upperarm's frame
        Quaternion relativeRot = Quaternion.Inverse(qForearmTracker);

        Vector3 currentHandAxial = relativeRot * (qHandTracker * Vector3.up);
        Vector3 currentForearmAxial = -Vector3.up;

        Vector3 currentHandLateral = relativeRot * (qHandTracker * Vector3.forward);
        Vector3 currentForearmLateral = Vector3.forward;

        // Wrist pronation /supination
        Vector2 vXZ1 = new Vector2(currentHandLateral.x, currentHandLateral.z);
        Vector2 vXZ2 = new Vector2(currentForearmLateral.x, currentForearmLateral.z);
        if (vXZ1.x > 0) sgn = -1; else sgn = 1;
        wristPS = sgn * Vector2.Angle(vXZ1, vXZ2) * Mathf.Deg2Rad;

        //Debug.Log("Wrist PS: " + Mathf.Rad2Deg * wristPS);


        float[] pose = { wristPS };
        return pose;
    }





}
