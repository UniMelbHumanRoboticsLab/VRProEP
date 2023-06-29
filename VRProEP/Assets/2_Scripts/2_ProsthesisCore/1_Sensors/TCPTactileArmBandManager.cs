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
        private int minDataNum;
        private int maxDataNum;

        public int MinDataNum { get => minDataNum;  set { minDataNum = value; } }
        public int MaxDataNum { get => maxDataNum;  set { maxDataNum = value; } }

        // Saving file
        private string fileName;
        private string fileHeader;
        
        
        public string FileName { get => fileName; set { fileName = value; } }
        public string FileHeader { get => fileHeader; set { fileHeader = value; } }
        private StringBuilder csvString = new StringBuilder();

        // Flags
        private bool recording = false;

        //
        // Constructor
        //
        public TCPTactileArmBandManager(string ipAddress, int port) : base(ipAddress, port)
        {
            this.minDataNum = 0;
            this.maxDataNum = 10000;
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
                    float[] data = Array.ConvertAll(receivedData.Split(','), float.Parse);


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
            base.reader.Close();
            base.writer.Close();
            base.networkStream.Close();
            base.clientSocket.Close();
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
            Debug.Log("Armband recorded: " + recordDataNum + "rows of data.");

            if (recordDataNum < minDataNum || recordDataNum > maxDataNum)
                Debug.LogWarning("Data file: " + fileName + " may not be logged correctly!" + " Only " + recordDataNum + " rows recorded!");
        }





    }
}
