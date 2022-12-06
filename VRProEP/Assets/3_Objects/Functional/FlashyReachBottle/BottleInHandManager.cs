using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// GameMaster includes
using VRProEP.ExperimentCore;
using VRProEP.GameEngineCore;
using VRProEP.ProsthesisCore;
using VRProEP.Utilities;

public class BottleInHandManager : MonoBehaviour
{

    [SerializeField] [Range(-0.1f, 0.1f)] private float xOffest; //0.02f
    [SerializeField] [Range (-0.1f,0.1f)] private float yOffest; //-0.05f
    [SerializeField] [Range(-0.1f, 0.1f)] private float zOffest; //0f
   

    // Update is called once per frame
    // Attach the bottle to the hand
    void Update()
    {
        if (AvatarSystem.IsAvatarAvaiable)
        {
            GameObject wristPoint;
            //Debug.Log("Avatar find, attach bottle to hand!");
            wristPoint = GameObject.FindGameObjectWithTag("BottleAnchor");

            transform.rotation = wristPoint.transform.rotation;  // Orientation
            transform.Rotate(Vector3.forward, 90f, Space.Self);
            transform.Rotate(Vector3.up, -180f, Space.Self);
            transform.position = wristPoint.transform.position;
            transform.Translate(new Vector3(xOffest + SaveSystem.ActiveUser.handLength, yOffest, zOffest), Space.Self);
 
        }
    }
}
