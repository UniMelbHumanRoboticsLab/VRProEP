using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRProEP.ProsthesisCore;

public class SerialTester : MonoBehaviour
{

    private SerialDeviceManager serialDevice;
    private TactileArmBandManager armBand;
    private int iteration = 1;
    // Start is called before the first frame update
    void Start()
    {
        armBand = new TactileArmBandManager("COM7", 115200);
        armBand.StartAcquisition();
       
        //serialDevice = new SerialDeviceManager("COM7", 115200);
        //serialDevice.Start();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            Debug.Log("Start recording tactile armband data.");
            armBand.FileName = "C://i_" + iteration + ".csv";
            armBand.StartRecording();
        }
        else if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            Debug.Log("Stop recording tactile armband data.");
            armBand.StopRecording();
            iteration++;
        }


    }

    private void OnApplicationQuit()
    {
        armBand.StopAcquisition();
        //serialDevice.Stop();
    }
}
