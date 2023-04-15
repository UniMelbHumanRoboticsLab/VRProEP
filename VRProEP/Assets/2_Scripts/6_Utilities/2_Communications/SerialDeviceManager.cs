using System.Collections;
using System.Collections.Generic;
using System.IO.Ports;
using UnityEngine;
using System.Threading;

public class SerialDeviceManager : RunAbleThread
{
    private SerialPort port;

    public SerialDeviceManager(string com, int baudrate)
    {
        InitPort(com, baudrate);
        port.Open();
        
    }

    //
    // Overide the run thread
    //
    protected override void Run()
    {
        while (Running)
        {
            try
            {
                string data = port.ReadLine();
                Debug.Log("Serial received: " + data);
            }
            catch (System.TimeoutException) { }
            catch (ThreadAbortException) { return; }
        }
        port.Close();
    }


    //
    // Setup serial port
    //
    private void InitPort(string com, int baudrate)
    {
        // Connect to the first
        port = new SerialPort(com);

        // Set the baud rate, parity, data bits, and stop bits
        port.BaudRate = baudrate;
        port.Parity = Parity.None;
        port.DataBits = 8;
        port.StopBits = StopBits.One;
    }

}
