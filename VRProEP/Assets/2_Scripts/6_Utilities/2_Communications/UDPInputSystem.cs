using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Collections;


//
// Customised input system to replace or work as a supplementary to the VIVE controller through UDP
//

public static class UDPInputSystem
{
    public enum InputType { UDPClothespinButton }
    private static List<UDPClothespinManager> udpPinList = new List<UDPClothespinManager>();
    public static int UDPPinCount {get=> udpPinList.Count; }

    //
    // Add an input source to the system
    //
    public static void AddInput(InputType type, UDPClothespinManager pin)
    {
        udpPinList.Add(pin);
    }

    //
    // Get an input state
    //
    public static object GetInput(InputType type, int channel)
    {
        //if (udpPinList.Count == 0)
            //return UDPClothespinManager.ClothespinState.Null;

        switch (type)
        {
            case InputType.UDPClothespinButton:
                if (channel > (udpPinList.Count - 1))
                {
                    //Debug.LogWarning("The requested input channel is out of range!");
                    return UDPClothespinManager.ClothespinState.Null;
                }
                else
                    return udpPinList[channel].CurrentState;
                
            default:
                return null;
        }
    }

    //
    // Get an input state, a time step before
    //
    public static object GetPrevInput(InputType type, int channel)
    {
        //if (udpPinList.Count == 0)
            //return UDPClothespinManager.ClothespinState.Null;

        switch (type)
        {
            case InputType.UDPClothespinButton:
                if (channel > (udpPinList.Count - 1))
                {
                    //Debug.LogWarning("The requested input channel is out of range!");
                    return UDPClothespinManager.ClothespinState.Null;
                }
                else
                    return udpPinList[channel].PrevState;

            default:
                return null;
        }
    }




    //
    // Close all input.
    //
    public static void CloseInput()
    {
        if (udpPinList.Count == 0)
            throw new ArgumentOutOfRangeException("No UDP input source has been added!");
        else
        {
            foreach (UDPClothespinManager pin in udpPinList)
                pin.StopSensorReading();
        }
    }
        



}
