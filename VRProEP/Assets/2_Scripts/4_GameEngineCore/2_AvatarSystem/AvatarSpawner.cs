﻿using System.Collections;
using System.IO;
using UnityEngine;

namespace VRProEP.GameEngineCore
{
    /// <summary>
    /// Avatar objects should be placed within: /Resources/Avatars/
    /// </summary>
    public class AvatarSpawner
    {
        private AvatarObjectData activeResidualLimbData;
        private AvatarObjectData activeSocketData;
        private AvatarObjectData activeElbowData_Upper;
        private AvatarObjectData activeElbowData_Lower;
        private AvatarObjectData activeForearmData;
        private AvatarObjectData activeHandData;

        private const float objectGap = 0.0f; // Helps with the gap between certain objects to avoid overlapping issues.

        private readonly string resourcesDataPath = Application.dataPath + "/Resources/Avatars";
        
        public void SpawnTranshumeralAvatar(UserData userData, AvatarData avatarData)
        {
            LoadResidualLimb(avatarData.residualLimbType);
            LoadSocket(avatarData.socketType);
            LoadElbow(avatarData.elbowType, userData.upperArmLength);
            LoadForearm(avatarData.forearmType, userData.upperArmLength, userData.forearmLength);
            // LoadHand
        }

        public void SpawnTransradialAvatar(UserData userData, AvatarData avatarData)
        {
            // Load
            // Customize
            throw new System.NotImplementedException("Transradial avatars not yet implemented.");
        }

        /// <summary>
        /// Loads and instantiates a residual limb avatar prefab from Resources/Avatars/ResidualLimbs.
        /// The prefab must include the tag "ResidualLimbAvatar". Loads by name.
        /// </summary>
        /// <param name="rlType">The name of the prefab residual limb avatar to be loaded.</param>
        /// <returns>The instantiated residual limb GameObject.</returns>
        private GameObject LoadResidualLimb(string rlType)
        {
            // Load Avatar object to set as parent.
            GameObject avatarGO = GameObject.FindGameObjectWithTag("Avatar");
                
            // Load residual limb from avatar folder and check whether successfully loaded.
            GameObject residualLimbPrefab = Resources.Load<GameObject>("Avatars/ResidualLimbs/" + rlType);
            if (residualLimbPrefab == null)
                throw new System.Exception("The requested residual limb prefab was not found.");

            // Load the residual object info.
            string objectPath = resourcesDataPath + "/ResidualLimbs/" + rlType + ".json";
            string objectDataAsJson = File.ReadAllText(objectPath);
            activeResidualLimbData = JsonUtility.FromJson<AvatarObjectData>(objectDataAsJson);
            if (activeResidualLimbData == null)
                throw new System.Exception("The requested residual limb information was not found.");

            // Instantiate with tracker as parent.
            GameObject residualLimbGO = Object.Instantiate(residualLimbPrefab, new Vector3(0.0f, -activeResidualLimbData.dimensions.x / 2.0f, 0.0f), Quaternion.identity, avatarGO.transform);
            
            // Make sure the loaded residual limb has a the follower script and set the offset
            ResidualLimbFollower follower = residualLimbGO.GetComponent<ResidualLimbFollower>();
            // If it wasn't found, then add it.
            if (follower == null)
                residualLimbGO.AddComponent<ResidualLimbFollower>();

            follower.offset = new Vector3(0.0f, -activeResidualLimbData.dimensions.x / 2.0f, 0.0f);

            return residualLimbGO;
        }

        /// <summary>
        /// Loads and instantiates a socket avatar prefab from Resources/Avatars/Sockets.
        /// The prefab must include the tag "Socket". Loads by name.
        /// </summary>
        /// <param name="socketType">The name of the prefab socket avatar to be loaded.</param>
        /// <returns>The instantiated socket GameObject.</returns>
        private GameObject LoadSocket(string socketType)
        {
            // Need to attach to ResidualLimbAvatar, so find that first and get its Rigidbody.
            GameObject residualLimbGO = GameObject.FindGameObjectWithTag("ResidualLimbAvatar");
            Rigidbody residualLimbRB = residualLimbGO.GetComponent<Rigidbody>();

            // Load socket from avatar folder and check whether successfully loaded.
            GameObject socketPrefab = Resources.Load<GameObject>("Avatars/Sockets/" + socketType);
            if (socketPrefab == null)
                throw new System.Exception("The requested socket prefab was not found.");

            // Get parent prosthesis manager
            GameObject prosthesisManagerGO = GameObject.FindGameObjectWithTag("ProsthesisManager");

            // Load socket object info
            string objectPath = resourcesDataPath + "/Sockets/" + socketType + ".json";
            string objectDataAsJson = File.ReadAllText(objectPath);
            activeSocketData = JsonUtility.FromJson<AvatarObjectData>(objectDataAsJson);
            if (activeSocketData == null)
                throw new System.Exception("The requested socket information was not found.");

            // Instantiate with prosthesis manager as parent.
            GameObject socketGO = Object.Instantiate(socketPrefab, new Vector3(0.0f, -(activeResidualLimbData.dimensions.x + (activeSocketData.dimensions.x/2.0f) + objectGap), 0.0f), Quaternion.identity, prosthesisManagerGO.transform);
            
            // Attach the socket to the residual limb through a fixed joint.
            FixedJoint socketFixedJoint = socketGO.GetComponent<FixedJoint>();
            // If no fixed joint was found, then add it.
            if (socketFixedJoint == null)
                socketFixedJoint = socketGO.AddComponent<FixedJoint>();
            // Connect
            socketFixedJoint.connectedBody = residualLimbRB;
            return socketGO;
        }

        /// <summary>
        /// Loads and instantiates an elbow avatar prefab from Resources/Avatars/Elbows. Loads by name.
        /// The prefab is composed of 3 parts:
        /// The parent elbow empty GameObject which includes the tag "Elbow". 
        /// The upper arm part of the elbow device, which includes the tag "Elbow_Upper".
        /// The lower arm part of the elbow device, which includes the tag "Elbow_Lower".
        /// </summary>
        /// <param name="elbowType">The name of the prefab socket avatar to be loaded.</param>
        /// <returns>The instantiated socket GameObject.</returns>
        private GameObject LoadElbow(string elbowType, float upperArmLength)
        {
            // Need to attach to Socket, so find that first and get its Rigidbody.
            GameObject socketGO = GameObject.FindGameObjectWithTag("Socket");
            Rigidbody socketRB = socketGO.GetComponent<Rigidbody>();

            // Load elbow components from avatar folder and check whether successfully loaded.
            GameObject elbowPrefab = Resources.Load<GameObject>("Avatars/Elbows/" + elbowType);
            if (elbowPrefab == null)
                throw new System.Exception("The requested elbow prefab was not found.");

            // Get parent prosthesis manager
            GameObject prosthesisManagerGO = GameObject.FindGameObjectWithTag("ProsthesisManager");

            // Load elbow objects info
            string objectPath_Upper = resourcesDataPath + "/Elbows/" + elbowType + "_Upper.json";
            string objectPath_Lower = resourcesDataPath + "/Elbows/" + elbowType + "_Lower.json";
            string objectDataAsJson_Upper = File.ReadAllText(objectPath_Upper);
            string objectDataAsJson_Lower = File.ReadAllText(objectPath_Lower);
            activeElbowData_Upper = JsonUtility.FromJson<AvatarObjectData>(objectDataAsJson_Upper);
            activeElbowData_Lower = JsonUtility.FromJson<AvatarObjectData>(objectDataAsJson_Upper);

            if (activeElbowData_Upper == null || activeElbowData_Lower == null)
                throw new System.Exception("The requested elbow information was not found.");

            // Instantiate with prosthesis manager as parent.
            GameObject elbowGO = Object.Instantiate(elbowPrefab, new Vector3(0.0f, -(upperArmLength - (1.5f*activeElbowData_Upper.dimensions.x)), 0.0f), Quaternion.identity, prosthesisManagerGO.transform);
                       
            // Attach the socket to thre residual limb through a fixed joint.
            // Get the elbow upper part that needs to be attached to the socket
            FixedJoint elbowFixedJoint = elbowGO.GetComponentInChildren<FixedJoint>();
            // If no fixed joint was found, then add it.
            if (elbowFixedJoint == null)
            {
                GameObject elbow_Upper = GameObject.FindGameObjectWithTag("Elbow_Upper");
                elbowFixedJoint = elbow_Upper.AddComponent<FixedJoint>();

            }
            // Connect
            elbowFixedJoint.connectedBody = socketRB;
            return elbowGO;
        }


        /// <summary>
        /// Loads and instantiates a forearm avatar prefab from Resources/Avatars/Forearms.
        /// The prefab must include the tag "Forearm". Loads by name.
        /// </summary>
        /// <param name="forearmType">The name of the prefab forearm avatar to be loaded.</param>
        /// <returns>The instantiated forearm GameObject.</returns>
        private GameObject LoadForearm(string forearmType, float upperArmLength, float lowerArmLength)
        {
            // Need to attach to Elbow_Lower, so find that first and get its Rigidbody.
            GameObject elbowLowerGO = GameObject.FindGameObjectWithTag("Elbow_Lower");
            Rigidbody elbowLowerRB = elbowLowerGO.GetComponent<Rigidbody>();

            // Load forearm from avatar folder and check whether successfully loaded.
            GameObject forearmPrefab = Resources.Load<GameObject>("Avatars/Forearms/" + forearmType);
            if (forearmPrefab == null)
                throw new System.Exception("The requested socket prefab was not found.");

            // Get parent prosthesis manager
            GameObject prosthesisManagerGO = GameObject.FindGameObjectWithTag("ProsthesisManager");

            // Load forearm object info
            string objectPath = resourcesDataPath + "/Forearms/" + forearmType + ".json";
            string objectDataAsJson = File.ReadAllText(objectPath);
            activeForearmData = JsonUtility.FromJson<AvatarObjectData>(objectDataAsJson);
            if (activeForearmData == null)
                throw new System.Exception("The requested forearm information was not found.");

            // Instantiate with prosthesis manager as parent.
            GameObject forearmGO = Object.Instantiate(forearmPrefab, new Vector3(0.0f, -(upperArmLength + lowerArmLength - (activeForearmData.dimensions.x / 2.0f)), 0.0f), Quaternion.identity, prosthesisManagerGO.transform);
            //GameObject forearmGO = Object.Instantiate(forearmPrefab, new Vector3(0.0f, -1.0f, 0.0f), Quaternion.identity, prosthesisManagerGO.transform);
            // Debug.Log( "ua: " + upperArmLength + ", la: " + lowerArmLength + ", fa: " + (activeForearmData.dimensions.x));

            // Attach the socket to the residual limb through a fixed joint.
            FixedJoint forearmFixedJoint = forearmGO.GetComponent<FixedJoint>();
            // If no fixed joint was found, then add it.
            if (forearmFixedJoint == null)
                forearmFixedJoint = forearmGO.AddComponent<FixedJoint>();
            // Connect
            forearmFixedJoint.connectedBody = elbowLowerRB;
            return forearmGO;
        }
    }

}