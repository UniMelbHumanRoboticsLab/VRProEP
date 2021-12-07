using AsyncIO;
using NetMQ;
using NetMQ.Sockets;
using UnityEngine;
using System;
/// <summary>
/// Stream data to python and receive the response - now in a sync way, maybe will consider async way
/// </summary>
public class ZMQRequester : RunAbleThread
{
    /// <summary>
    /// 
    /// </summary>
    private byte[] sendData;
    private byte[] receiveData;
    private bool newDataFlag = false;
    private int port = 5555;
    /*
    public byte[] SendData
    {
        get { return sendData; }
        set { sendData = value; }
    }

    public byte[] ReceiveData
    {
        get { return receiveData; }
        set { receiveData = value; }
    }

    */

    //
    // Constructor
    //

    public ZMQRequester(int port)
    {
        this.port = port;
    }

    public ZMQRequester(int port, byte[] data)
    {
        this.port = port;
        this.newData(data);
    }

    public ZMQRequester(int port, float[] data)
    {
        this.port = port;
        this.newData(data);
    }

    //
    // Method to set new sending data
    //
    // for byte array data
    public void newData(byte[] data)
    {
        sendData = data;
        newDataFlag = true;
    }
    // for float array data
    public void newData(float[] data)
    {
        byte[] byteArray = new byte[data.Length * sizeof(float)];
        Buffer.BlockCopy(data, 0, byteArray, 0, byteArray.Length);
        sendData = byteArray; // assign the senData
        newDataFlag = true;
    }



    // Overide the run thread
    protected override void Run()
    {
        ForceDotNet.Force(); // this line is needed to prevent unity freeze after one use, not sure why yet
        using (RequestSocket client = new RequestSocket())
        {
            string addr = "tcp://localhost:" + port;
            client.Connect(addr);
            while (Running)
            {
                if (newDataFlag)
                {
                    Debug.Log("ZMQRequester-> Data Sent through ZMQ.");
                    client.SendFrame(sendData);
                    newDataFlag = false;

                    byte[] response = null;
                    double[] parsedResponse = null;
                    bool gotResponse = false;
                    while (Running)
                    {
                        gotResponse = client.TryReceiveFrameBytes(out response); // this returns true if it's successful
                        if (gotResponse) break;
                    }

                    if (gotResponse)
                    {
                        parsedResponse = parseReceivedData(response);
                        string responseStr = null;
                        foreach (double element in parsedResponse)
                        {
                            responseStr += element.ToString() + ",";
                        }


                        Debug.Log("ZMQRequester<- Received: " + responseStr);

                    }
                }

            }

        }

        NetMQConfig.Cleanup(); // this line is needed to prevent unity freeze after one use, not sure why yet
    }

    private double[] parseReceivedData(byte[] receivedData) // From python/matlab it's 64 bit float (double)
    {
        double[] floatArray = new double[receivedData.Length / sizeof(double)];
        Buffer.BlockCopy(receivedData, 0, floatArray, 0, receivedData.Length);

        return floatArray;
    }


}