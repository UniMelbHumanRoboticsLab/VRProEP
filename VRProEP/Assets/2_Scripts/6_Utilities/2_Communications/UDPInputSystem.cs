using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;


//
// Customised input system to replace or work as a supplementary to the VIVE controller through UDP
//

public static class UDPInputSystem
{
    public enum InputType { UDPClothespinButton }
    private static List<UDPClothespinManager> udpPinList = new List<UDPClothespinManager>();

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
        switch (type)
        {
            case InputType.UDPClothespinButton:
                if (channel > (udpPinList.Count - 1))
                    throw new ArgumentOutOfRangeException("The requested input channel is out of range!");
                else
                    return udpPinList[channel].GetClothespinState();
                
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
