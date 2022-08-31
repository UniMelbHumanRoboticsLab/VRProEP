using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * Static class for postural feature extraction using trackers' position and quaternion readings
 * 
 */

public static class PosturalFeatureExtractor
{
    private static float trunkFE = 0;
    private static float trunkLRB = 0;
    private static float trunkR = 0;

    private static float shoulderFE = 0;
    private static float shoulderABD = 0;
    private static float shoulderR = 0;

    private static float scapularDE = 0;
    private static float scapularPR = 0;

    private static GameObject tempRefGO = new GameObject("TestRef");
    private static GameObject tempGO = new GameObject("Test");

    public static float[] extractTrunkPose(Quaternion qC7TrackerRef, Quaternion qC7Tracker)
    {

        Vector3 initialTrunkAxial = qC7TrackerRef * Vector3.up;
        Vector3 initialTrunkLateral = qC7TrackerRef * Vector3.right;
        
        Vector3 currentTrunkAxial = qC7Tracker * Vector3.up;
        Vector3 currentTrunkLateral = qC7Tracker * Vector3.right;
        //Debug.Log(currentTrunkSaggital);
        //Debug.Log(initialTrunkSaggital);

        // Trunk Flexion/extension
        Vector2 vYZ1 = new Vector2(currentTrunkAxial.y, currentTrunkAxial.z);
        Vector2 vYZ2 = new Vector2(initialTrunkAxial.y, initialTrunkAxial.z);
        trunkFE = Vector2.Angle(vYZ1, vYZ2);
        //Debug.Log("Trunk FE: " + trunkFE);

        // Trunk Left right bending
        Vector2 vXY1 = new Vector2(currentTrunkLateral.x, currentTrunkLateral.y);
        Vector2 vXY2 = new Vector2(initialTrunkLateral.x, initialTrunkLateral.y);
        trunkLRB = Vector2.Angle(vXY1, vXY2);
        //Debug.Log("Trunk LRB: " + trunkLRB);

        // Trunk Rotation
        Vector2 vXZ1 = new Vector2(currentTrunkLateral.x, currentTrunkLateral.z);
        Vector2 vXZ2 = new Vector2(initialTrunkLateral.x, initialTrunkLateral.z);
        trunkR = Vector2.Angle(vXZ1, vXZ2);
        //Debug.Log("Trunk R: " + trunkR);
        


        float[] pose = new float[3] { trunkFE, trunkLRB, trunkR };
        return pose;
        //Quaternion relativeRot = qC7Tracker * Quaternion.Inverse(qC7TrackerRef);
    }

    public static float[] extractShoulderPose(Quaternion qC7TrackerRef, Quaternion qC7Tracker, Quaternion qUpperarmTrackerRef, Quaternion qUpperarmTracker)
    {
        //Quaternion relativeRotRef = qUpperarmTrackerRef * Quaternion.Inverse(qC7TrackerRef);

        //qUpperarmTrackerRef *= relativeRotRef;
        //tempRefGO.transform.rotation = qUpperarmTrackerRef;
        

        // Caculate shoulder angles in C7's frame
        Quaternion relativeRot = Quaternion.Inverse(qC7Tracker);
        Vector3 currentShoulderAxial = relativeRot * (qUpperarmTracker * Vector3.up);
        Vector3 currentTrunkAxial = Vector3.up;
        Vector3 currentShoulderLateral = relativeRot * (qUpperarmTracker * Vector3.forward);
        Vector3 currentTrunkLateral = -Vector3.right;

        // Debug.Log(currentShoulderAxial);

        // Shoulder Flexion / extension
        Vector2 vYZ1 = new Vector2(currentShoulderAxial.y, currentShoulderAxial.z);
        Vector2 vYZ2 = new Vector2(currentTrunkAxial.y, currentTrunkAxial.z);
        shoulderFE = Vector2.Angle(vYZ1, vYZ2);
        Debug.Log("Shoudler FE: " + shoulderFE);

        // Shoulder Adduction / Abdictopm
        Vector2 vXY1 = new Vector2(currentShoulderAxial.x, currentShoulderAxial.y);
        Vector2 vXY2 = new Vector2(currentTrunkAxial.x, currentTrunkAxial.y);
        shoulderABD = Vector2.Angle(vXY1, vXY2);
        Debug.Log("Shoudler ABD: " + shoulderABD);

        //Shoulder Rotation
        Vector2 vXZ1 = new Vector2(currentShoulderLateral.x, currentShoulderLateral.z);
        Vector2 vXZ2 = new Vector2(currentTrunkLateral.x, currentTrunkLateral.z);
        shoulderR = Vector2.Angle(vXZ1, vXZ2);
        Debug.Log("Shoudler R: " + shoulderR);



        float[] pose = new float[3] { shoulderFE, shoulderABD, shoulderR };
        return pose;
    }



    public static float[] extractRotationalPose(Quaternion c7TrackerRef, Quaternion c7Tracker, Quaternion qUpperarmTrackerRef, Quaternion qUpperarmTracker)
    {

        Vector3 initialTrunkSaggital = c7TrackerRef * Vector3.up; 
        Vector3 initialTrunkLateral = c7TrackerRef * Vector3.right; 
        Quaternion relativeRotC7 = c7Tracker * Quaternion.Inverse(c7TrackerRef);
        Vector3 currentTrunkSaggital = c7Tracker * Vector3.up;
        Vector3 currentTrunkLateral = c7Tracker * Vector3.right;
        //Debug.Log(currentTrunkSaggital);
        //Debug.Log(initialTrunkSaggital);

        // Trunk Flexion/extension
        Vector2 vYZ1 = new Vector2(currentTrunkSaggital.y, currentTrunkSaggital.z);
        Vector2 vYZ2 = new Vector2(initialTrunkSaggital.y, initialTrunkSaggital.z);
        trunkFE = Vector2.Angle(vYZ1, vYZ2);
        Debug.Log(trunkFE);

        // Trunk Left right bending
        Vector2 vXY1 = new Vector2(currentTrunkLateral.x, currentTrunkLateral.y);
        Vector2 vXY2 = new Vector2(initialTrunkLateral.x, initialTrunkLateral.y);
        trunkLRB = Vector2.Angle(vXY1, vXY2);
        //Debug.Log(trunkLRB);

        // Trunk Rotation
        Vector2 vXZ1 = new Vector2(currentTrunkLateral.x, currentTrunkLateral.z);
        Vector2 vXZ2 = new Vector2(initialTrunkLateral.x, initialTrunkLateral.z);
        trunkR = Vector2.Angle(vXZ1, vXZ2);
        //Debug.Log(trunkR);



        float[] pose = new float[3] {trunkFE, trunkLRB, trunkR};
        return pose;
    }



    public static float[] extractScapularPose(Vector3 c7TrackerRef, Vector3 c7Tracker, Vector3 shoulderTrackerRef, Vector3 shoulderTracker)
    {

        // Scapular depression / elevation
        scapularDE = shoulderTracker.x - shoulderTrackerRef.x;
        // Scapular protraction / retraction
        scapularPR = shoulderTracker.z - shoulderTrackerRef.z;

        float[] pose = new float[2] { scapularDE, scapularPR };
        return pose;
    }

    

   

    public static float[] extractScapularPose(Quaternion shoulderTrackerRef, Quaternion shoulderTracker)
    {
        float[] pose = new float[2];

        return pose;
    }

}
