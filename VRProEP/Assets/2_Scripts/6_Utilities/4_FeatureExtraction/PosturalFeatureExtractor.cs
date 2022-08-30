using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * Static class for postural feature extraction using trackers' position and quaternion readings
 * 
 */

public static class PosturalFeatureExtractor
{
    public static float[] extractTrunkPose(Quaternion reference, Quaternion C7)
    {
        float[] pose = new float[3];

        return pose;
    }

    public static float[] extractAcromionPose(Quaternion reference, Quaternion SA)
    {
        float[] pose = new float[2];

        return pose;
    }

    public static float[] extractAcromionPose(Vector3 reference, Vector3 SA)
    {
        float[] pose = new float[2];

        return pose;
    }

    public static float[] extractShoulderPose(Quaternion reference, Quaternion UA)
    {
        float[] pose = new float[3];

        return pose;
    }


}
