using System.Collections;
using System.Collections.Generic;
using System.IO.Ports;
using UnityEngine;
using System.Threading;

public class SerialDeviceManager : RunAbleThread
{
    protected int baudrate;
    protected string com;
    protected string receivedData;

    public SerialDeviceManager(string com, int baudrate)
    {
        this.com = com;
        this.baudrate = baudrate;
    }

    //
    // Overide the run thread
    //
    protected override void Run()
    {
        using (SerialPort port = new SerialPort())
        {
            InitPort(port, com, baudrate);
            port.Open();
            while (Running)
            {
                try
                {
                    receivedData = port.ReadLine();
                    Debug.Log("Serial received: " + receivedData);
                }
                catch (System.TimeoutException) { }
                catch (ThreadAbortException) { return; }
            }
            port.Close();
        }
           
    }


    //
    // Setup serial port
    //
    protected void InitPort(SerialPort port, string com, int baudrate)
    {
        // Set the baud rate, parity, data bits, and stop bits
        port.PortName = com;
        port.BaudRate = baudrate;
        port.Parity = Parity.None;
        port.DataBits = 8;
        port.StopBits = StopBits.One;
    }

}
