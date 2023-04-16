using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UDPClothespinTester : MonoBehaviour
{
    private UDPClothespinManager udpClopin;
    private string ipAddress = "192.168.137.75";
    private int port = 2390;
    // Start is called before the first frame update
    void Start()
    {
        udpClopin = new UDPClothespinManager(ipAddress, port);
    }

    // Update is called once per frame
    void Update()
    {
        UDPClothespinManager.ClothespinState state = udpClopin.GetClothespinState();
        Debug.Log("Current clothespin state: " + state.ToString());
    }

    private void OnApplicationQuit()
    {
        udpClopin.StopSensorReading();
    }
}
