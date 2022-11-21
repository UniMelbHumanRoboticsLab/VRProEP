using AsyncIO;
using NetMQ;
using NetMQ.Sockets;
using UnityEngine;
using System.Linq;
using System;

public class ZMQPuller : RunAbleThread
{
    private float[] receivedData;
    private int port;

    //
    // Constructor
    //
    public ZMQPuller(int port)
    {
        this.port = port;
    }

    //
    // Get the latest received data
    //
    public float[] GetReceivedData()
    {
        return receivedData;
    }

    //
    // Overide the run thread
    //
    protected override void Run()
    {
        ForceDotNet.Force(); // this line is needed to prevent unity freeze after one use, not sure why yet
        using (PullSocket puller = new PullSocket())
        {
            string addr = "tcp://localhost:" + port;
            puller.Bind(addr);
            while (Running)
            {
                byte[] data = null;
                var receiveFlag = puller.TryReceiveFrameBytes(out data);
                if (receiveFlag)
                {
                    receivedData = parseReceivedData(data);

                    /*
                    string responseStr = null;
                    foreach (float element in receivedData)
                    {
                        responseStr += element.ToString() + ",";
                    }
                    Debug.Log("ZMQPuller<- Received: " + responseStr);
                    */

                    /*
                    string responseStr = null;
                    foreach (byte element in data)
                    {
                        responseStr += element.ToString() + ",";
                    }
                    Debug.Log("ZMQPuller<- Received: " + responseStr);
                    */
                }

            }
            puller.Close();
            puller.Dispose();
            NetMQConfig.Cleanup(false);

        }
    }

    private float[] parseReceivedData(byte[] receivedData) // From python/matlab it's 64 bit float (double)
    {
        double[] tempArray = new double[receivedData.Length / sizeof(double)];
        Buffer.BlockCopy(receivedData, 0, tempArray, 0, receivedData.Length);
        float[] result = tempArray.Select(d => (float)d).ToArray();
        return result;
    }


}
