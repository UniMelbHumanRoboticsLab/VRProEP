using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class CSVReader // Read csv file data to an array
{

    
    public float[][] ReadCSVData(string filePath)
    {
        float[][] csvData;

        if (File.Exists(filePath))
        {
            string[] lines = File.ReadAllLines(filePath);

            // Initialize the 2D array with appropriate dimensions
            csvData = new float[lines.Length][];

            for (int i = 0; i < lines.Length; i++)
            {
                string[] values = lines[i].Split(',');
                csvData[i] = new float[values.Length];

                for (int j = 0; j < values.Length; j++)
                {
                    if (float.TryParse(values[j], out float floatValue))
                    {
                        csvData[i][j] = floatValue;
                    }
                    else
                    {
                        Debug.LogError("Error parsing float at row " + i + " and column " + j);
                    }
                }
            }

            return csvData;
        }
        else
        {
            Debug.LogError("CSV file not found at path: " + filePath);
            return null;
        }

        
    }
     
}
