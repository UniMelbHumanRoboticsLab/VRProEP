using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SerialTester : MonoBehaviour
{

    private SerialDeviceManager serialDevice;

    // Start is called before the first frame update
    void Start()
    {
        serialDevice = new SerialDeviceManager("COM7", 115200);
        serialDevice.Start();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnApplicationQuit()
    {
        serialDevice.Stop();
    }
}
