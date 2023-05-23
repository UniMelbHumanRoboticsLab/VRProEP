using System.Collections;
using System.Collections.Generic;
using System.IO.Ports;
using System.IO;
using System.Text;
using System;
using System.Net.Sockets;
using UnityEngine;
using System.Threading;

public class TCPDeviceManager : RunAbleThread
{
    protected int port;
    protected string ipAddress;
    protected string receivedData;

    protected TcpClient clientSocket;
    protected NetworkStream networkStream;
    protected StreamReader reader;
    protected StreamWriter writer;

    private bool connected = false;

    //
    /// <summary>
    /// Constructor
    /// </summary>
    public TCPDeviceManager(string ipAddress, int port)
    {
        this.ipAddress = ipAddress;
        this.port = port;
    }

    //
    /// <summary>
    /// Overide the run thread
    /// </summary>
    protected override void Run()
    {
        Connect(this.ipAddress, this.port);
        while (Running)
        {
            try
            {
                receivedData = reader.ReadLine();
                Debug.Log("TCP devive at:" + ipAddress +  ", received: " + receivedData);
            }
            catch (System.TimeoutException) { }
            catch (ThreadAbortException) { return; }
        }
        clientSocket.Close();
        clientSocket.Dispose();
    }


    //
    /// <summary>
    /// Connect to the device
    /// </summary>
    protected bool Connect(string ipAddress, int port)
    {
        try
        {
            //Establish TCP/IP connection to server using URL entered
            clientSocket = new TcpClient(ipAddress, port);

            //Set up communication streams
            networkStream = clientSocket.GetStream();
            reader = new StreamReader(networkStream, Encoding.ASCII);
            writer = new StreamWriter(networkStream, Encoding.ASCII);

            //Get initial response from server and display
            //.ReadLine();
            //reader.ReadLine();   //get extra line terminator

            connected = true;   //indicate that we are connected

            return connected;
        }
        catch (Exception connectException)
        {
            //connection failed, display error message
            Debug.Log("Could not connect.\n" + connectException.Message);
            connected = false;
            return connected;
        }
    }
}
