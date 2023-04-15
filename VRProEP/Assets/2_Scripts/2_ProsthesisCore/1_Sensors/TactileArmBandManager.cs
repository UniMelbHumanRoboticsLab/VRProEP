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
    public class TactileArmBandManager : SerialDeviceManager
    {
        // Sensor settings
        private int chNum = 0;

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
        public TactileArmBandManager(string com, int baudratre) : base(com, baudratre)
        {

        }

        //
        // Override the thread method
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
                            csvString.Append(receivedData);
                            csvString.Append(Environment.NewLine);
                        }
                        else
                        {
                            Debug.Log("Serial received: " + receivedData);
                        }

                    }
                    catch (System.TimeoutException) { }
                    catch (ThreadAbortException) { return; }
                }
                port.Close();
            }
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
        }





    }
}
