using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.IO.Ports;
using System.Threading;
using System.IO;
using UnityEngine;
using System.Linq;

namespace VRProEP.ProsthesisCore
{
    public class TCPTactileArmBandManager : TCPDeviceManager
    {
        // Sensor settings
        private int tactileCh = 0;
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
        private bool success = true;
        private bool setOffset = true;
        public bool SetOffset { get => setOffset; set { setOffset = value; } }

        // Offsets
        private List<List<float>> offsetBuffer = new List<List<float>>();
        private List<float> offset = new List<float>();

        //
        // Constructor
        //
        public TCPTactileArmBandManager(int tactileCh, string ipAddress, int port) : base(ipAddress, port)
        {
            this.tactileCh = tactileCh;
            offset = Enumerable.Repeat(0f, tactileCh).ToList();
            minDataNum = 0;
            maxDataNum = 10000;
            setOffset = true;
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
                    //string arrayAsString = string.Join(", ", data);
                    //Debug.Log("Data: " + arrayAsString);


                    if (recording)
                    {
                        //Debug.Log("TCP devive at:" + ipAddress + ", received: " + receivedData);

                        // Fix the offset issue of the forearm band
                        for (int i = 1; i <= tactileCh; i++)
                        {
                            if (data[i] < -10.0f)
                                data[i] = data[i] + 2 * 7900;
                        }

                        // Offset the data using offset values of relax gesture
                        List<float> tempoffsetData = new List<float>();
                        for (int i = 1; i <= tactileCh; i++)
                        {
                            tempoffsetData.Add((float)Math.Round(data[i] - offset[i-1]));
                        }
                        string offsetData = string.Join(",", tempoffsetData);
                        
                        
                        csvString.Append(receivedData + "," + offsetData);
                        

                        //csvString.Append(receivedData);
                        csvString.Append(Environment.NewLine);
                        recordDataNum++;

                        if (setOffset)
                        {
                            List<float> temp = new List<float>();
                            temp = data.OfType<float>().ToList().GetRange(1, tactileCh);
                            if(!temp.Contains(0.0f))
                                offsetBuffer.Add(temp);
                        }
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
        public bool StopRecording()
        {
            recording = false;
            File.WriteAllText(fileName, csvString.ToString());
            Debug.Log("Armband recorded: " + recordDataNum + "rows of data.");

            if (recordDataNum < minDataNum || recordDataNum > maxDataNum)
            {
                Debug.LogWarning("Data file: " + fileName + " may not be logged correctly!" + " Only " + recordDataNum + " rows recorded!");

                success = false;
                if (setOffset && offsetBuffer != null)
                {
                    offsetBuffer.Clear();
                    Debug.LogWarning("Fail to record offsets value" + fileName);
                }
            }
            else
            {
                success = true;

                if (setOffset && offsetBuffer != null)
                {
                    offset.Clear();
                    
                    for (int i = 0; i < offsetBuffer[0].Count; i++)
                    {
                        List<float> temp = new List<float>();
                        for (int j = 0; j < offsetBuffer.Count; j++)
                        {
                            temp.Add(offsetBuffer[j][i]);
                        }
                        //temp.Sort();
                        offset.Add(temp.Average());
                    }

                    string arrayAsString = string.Join(", ", offset);
                    Debug.Log("Set offsets: " + arrayAsString);

                    setOffset = false;
                }

            }

            

            return success;
        }






    }
}
