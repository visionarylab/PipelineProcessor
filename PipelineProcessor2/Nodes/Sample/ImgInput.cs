﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace PipelineProcessor2.Nodes
{
    public class ImgInput : IInputPlugin
    {
        public int InputQty => 0;
        public int OutputQty => 1;

        public IEnumerable RetrieveData(string path)
        {
            if (!Directory.Exists(path)) yield break;

            foreach (string filePath in Directory.EnumerateFiles(path))
            {
                string fileName = Path.GetFileName(filePath);
                if (fileName.EndsWith(".jpg"))
                {
                    List<byte[]> output = new List<byte[]>();
                    try
                    {
                        output.Add(File.ReadAllBytes(filePath));
                    }
                    catch (IOException io)
                    {
                        Console.WriteLine(io);
                    }

                    yield return output;
                }
            }
        }

        public string PluginInformation(PluginInformationRequests request, int index)
        {
            if (request == PluginInformationRequests.Name) return "Image Import";
            else if (request == PluginInformationRequests.Description) return "Imports all images of a given path";
            else if (request == PluginInformationRequests.OutputName)
            {
                if (index == 0) return "image";
            }
            else if (request == PluginInformationRequests.OutputType)
            {
                if (index == 0) return "jpg";
            }

            return "";
        }
    }
}
