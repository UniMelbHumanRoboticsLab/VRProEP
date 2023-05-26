using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.IO.Ports;
using System.Threading;
using System.IO;
using UnityEngine;

namespace VRProEP.ProsthesisCore
{
    public class TCPTactileArmBandManager : TCPDeviceManager
    {
        // Sensor settings
        private int chNum = 0;
        private int recordDataNum = 0;
        // Saving file
        private string fileName;
        private string fileHeader;
        public string FileName { get => fileName; set { fileName = value; } }
        private StringBuilder csvString = new StringBuilder();

        // Flags
        private bool recording = false;

        //
        // Constructor
        //
        public TCPTactileArmBandManager(string ipAddress, int port) : base(ipAddress, port)
        {

        }

        //
        // Override the thread method
        //
        protected override void Run()
        {
            Connect(this.ipAddress, this.port);
            while (Running)
            {
                try
                {
                    receivedData = reader.ReadLine();
                    // Config sensor channel number and saving file heading
                    if (chNum == 0)
                    {
                        float[] data = Array.ConvertAll(receivedData.Split(','), float.Parse);
                        chNum = data.Length;
                        for (int i = 1; i <= chNum; i++)
                            fileHeader += "ch" + i + ",";
                    }

                    if (recording)
                    {
                        
                        //Debug.Log("TCP devive at:" + ipAddress + ", received: " + receivedData);
                        csvString.Append(receivedData);
                        csvString.Append(Environment.NewLine);
                        recordDataNum++;
                    }
                    else
                    {
                        //Debug.Log("TCP devive at:" + ipAddress + ", received: " + receivedData);
                    }

                }
                catch (System.TimeoutException) { }
                catch (ThreadAbortException) { return; }
            }
            clientSocket.Close();
            //clientSocket.Dispose();
  
        }

        //
        // Start acquisition
        //
        public void StartAcquisition()
        {
           
            this.Start();
        }


        //
        // Start acquisition
        //
        public void StopAcquisition()
        {
            
            this.Stop();
        }


        //
        // Start recording
        //
        public void StartRecording()
        {
            recordDataNum = 0;
            csvString = new StringBuilder();
            csvString.Append(fileHeader);
            csvString.Append(Environment.NewLine);
            recording = true;


        }


        //
        // Stop recording
        //
        public void StopRecording()
        {
            recording = false;
            File.WriteAllText(fileName, csvString.ToString());
            Debug.Log("Armband recorded: " + recordDataNum + "rows of data recorded!");
        }





    }
}
