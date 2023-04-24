using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRProEP.Utilities;

public class UDPClothespinManager : UDPSensorManager
{
    private const int SAMPLE_INTERVAL = 5;
    public UDPClothespinManager(string ipAddress, int port): base(ipAddress, port, "ClothesPinWiFi", SAMPLE_INTERVAL) {}

    public enum ClothespinState { Open, Pinch, Null};

    private ClothespinState currentState = ClothespinState.Null;
    public ClothespinState CurrentState { get => currentState; }
    private ClothespinState prevState = ClothespinState.Null;
    public ClothespinState PrevState { get => prevState; }


    public ClothespinState GetClothespinState()
    {
        prevState = currentState;
        // Get current sensor data from memory.
        float[] value = GetCurrentSensorValues();
        //Debug.Log("Reading length: " + value.Length + ". First value: " + value[0] );
        ClothespinState state = ClothespinState.Open;

        if (value[0] == 1)
            state = ClothespinState.Pinch;

        currentState = state;
        return state;
    }



}
