using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using NetMQ;

public static class ZMQSystem
{

    public enum SocketType { Pusher, Puller, Requester}
    private static List<ZMQPusher> pusherList = new List<ZMQPusher>();
    private static List<int> pusherPortList = new List<int>();

    private static List<ZMQPuller> pullerList = new List<ZMQPuller>();
    private static List<int> pullerPortList = new List<int>();


    private static List<ZMQRequester> requesterList = new List<ZMQRequester>();
    private static List<int> requesterPortList = new List<int>();

    

    public static void AddZMQSocket(int port, SocketType type)
    {
        switch (type)
        {
            case SocketType.Pusher:
                ZMQPusher pusher = new ZMQPusher(port);
                pusherList.Add(pusher);
                pusherPortList.Add(port);
                pusher.Start();
                break;
            case SocketType.Puller:
                ZMQPuller puller = new ZMQPuller(port);
                pullerList.Add(puller);
                pullerPortList.Add(port);
                puller.Start();
                break;
            case SocketType.Requester:
                ZMQRequester requester = new ZMQRequester(port);
                requesterList.Add(requester);
                requesterPortList.Add(port);
                requester.Start();
                break;
            
        }
        
    }


    public static void CloseZMQSocket(int port, SocketType type)
    {
        switch (type)
        {
            case SocketType.Pusher:
                ZMQPusher pusher = pusherList[pusherPortList.IndexOf(port)];
                pusher.Stop();
                break;

            case SocketType.Puller:
                ZMQPuller puller = pullerList[pullerPortList.IndexOf(port)];
                puller.Stop();
                break;

            case SocketType.Requester:
                ZMQRequester requester = requesterList[requesterPortList.IndexOf(port)];
                requester.Stop();
                break;

        }


    }


    public static void CloseAllZMQSocket()
    {
        foreach (ZMQPusher pusher in pusherList)
        {
            pusher.Stop();
        }

        foreach (ZMQPuller puller in pullerList)
        {
            puller.Stop();
        }

        foreach (ZMQRequester requester in requesterList)
        {
            requester.Stop();
        }
    }
    public static void AddPushData(int port, float[] data)
    {
        ZMQPusher pusher = pusherList[pusherPortList.IndexOf(port)];
        pusher.newData(data);
    }

    public static double[] AddRequest(int port, float[] data)
    {
        ZMQRequester requester = requesterList[requesterPortList.IndexOf(port)];
        requester.newData(data);
        
        double[] response = requester.GetReceiveData();
        //float[] response = temp.Cast<float>().ToArray();

        return response;
    }

    public static float[] GetLatestPulledData(int port)
    {
        try
        {
            ZMQPuller puller = pullerList[pullerPortList.IndexOf(port)];
            return puller.GetReceivedData();
        }
        catch (System.ArgumentOutOfRangeException e)
        {
            return null;
        }
            
        
        
    }
}
