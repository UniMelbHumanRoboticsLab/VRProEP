using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRProEP.Utilities;

public class UDPClothespinManager : UDPSensorManager
{
    public UDPClothespinManager(string ipAddress, int port): base(ipAddress, port, "ClothesPinWiFi") {}

    public enum ClothespinState { Open, Pinch};

    public ClothespinState GetClothespinState()
    {
        // Get current sensor data from memory.
        float[] value = GetCurrentSensorValues();
        //Debug.Log("Reading length: " + value.Length + ". First value: " + value[0] );
        ClothespinState state = ClothespinState.Open;

        if (value[0] == 1)
            state = ClothespinState.Pinch;

        return state;
    }



}
