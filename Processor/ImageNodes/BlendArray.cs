﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using ImageProcessor;
using ImageProcessor.Imaging;
using ImageProcessor.Imaging.Formats;
using PluginTypes;

namespace ImageNodes
{
    public class BlendArray : IProcessPlugin
    {
        #region Node settings
        public int InputQty => 1;
        public int OutputQty => 1;
        public string Name => "Image Blend";
        public string Description => "Blends all images in an array and overlays them";
        public string OutputType(int index)
        {
            if (index == 0) return "jpg";
            if (index == 1) return "string";
            return "";
        }
        public string OutputName(int index)
        {
            if (index == 0) return "Image";
            if (index == 1) return "File Name";
            return "";
        }
        public string InputType(int index) { return ""; }
        public string InputName(int index) { return ""; }
        #endregion

        public List<byte[]> ProcessData(List<byte[]> input)
        {
            int imageQty = BitConverter.ToInt32(input[0], 0),
                opacity = 100 / imageQty;

            //Work out required size of the final image
            Size largestSize = new Size(0, 0);
            for (var i = 1; i < input.Count; i++)
            {
                using (MemoryStream inStream = new MemoryStream(input[i]))
                {   //only use the size to save on memory
                    Size imageSize = Image.FromStream(inStream).Size;

                    if (imageSize.Width > largestSize.Width) largestSize.Width = imageSize.Width;
                    if (imageSize.Height > largestSize.Height) largestSize.Height = imageSize.Height;
                }
            }

            ImageFactory image = new ImageFactory();
            image.Format(new JpegFormat());
            image.Quality(100);
            image.Load(new Bitmap(largestSize.Width, largestSize.Height));

            for (var i = 1; i < input.Count; i++)
            {
                ImageLayer layer = new ImageLayer { Opacity = opacity };
                using (MemoryStream inStream = new MemoryStream(input[i]))
                    layer.Image = Image.FromStream(inStream);
                layer.Size = layer.Image.Size;

                image.Overlay(layer);
            }

            using (MemoryStream outStream = new MemoryStream())
            {
                image.Save(outStream);
                image.Dispose();

                List<byte[]> output = new List<byte[]>();
                output.Add(outStream.ToArray());
                return output;
            }
        }
    }
}
