﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kontract.Interface;
using System.Drawing;
using Kontract.IO;
using System.IO;

namespace Kontract.Image.Format
{
    public class Palette : IImageFormat
    {
        IImageFormat paletteFormat;
        List<Color> colors;
        byte[] colorBytes;

        ByteOrder byteOrder;

        public int BitDepth { get; }

        public string FormatName { get; }

        public Palette(byte[] paletteData, IImageFormat paletteFormat, int indexDepth, ByteOrder byteOrder = ByteOrder.LittleEndian)
        {
            if (indexDepth % 4 != 0) throw new Exception("IndexDepth has to be dividable by 4.");

            this.byteOrder = byteOrder;

            BitDepth = indexDepth;
            FormatName = "Paletted " + paletteFormat.FormatName;

            this.paletteFormat = paletteFormat;
            colors = paletteFormat.Load(colorBytes).ToList();
        }

        public IEnumerable<Color> Load(byte[] data)
        {
            using (var br = new BinaryReaderX(new MemoryStream(data), true, byteOrder))
                while (true)
                    switch (BitDepth)
                    {
                        case 4:
                            yield return colors[br.ReadNibble()];
                            break;
                        case 8:
                            yield return colors[br.ReadByte()];
                            break;
                        default:
                            throw new Exception($"BitDepth {BitDepth} not supported!");
                    }
        }

        public byte[] Save(IEnumerable<Color> colors)
        {
            var redColors = CreatePalette(colors.ToList());

            var ms = new MemoryStream();
            using (var bw = new BinaryWriterX(ms, true, byteOrder))
                foreach (var color in colors)
                    switch (BitDepth)
                    {
                        case 4:
                            bw.WriteNibble(redColors.FindIndex(c => c == color));
                            break;
                        case 8:
                            bw.Write((byte)redColors.FindIndex(c => c == color));
                            break;
                        default:
                            throw new Exception($"BitDepth {BitDepth} not supported!");
                    }

            return ms.ToArray();
        }

        List<Color> CreatePalette(List<Color> colors)
        {
            List<Color> reducedColors = new List<Color>();
            foreach (var color in colors)
                if (!reducedColors.Exists(c => c == color)) reducedColors.Add(color);

            colorBytes = paletteFormat.Save(reducedColors);

            return reducedColors;
        }

        public static byte[] CreatePalette(Bitmap bmp, IImageFormat format)
        {
            List<Color> reducedColors = new List<Color>();
            for (int y = 0; y < bmp.Height; y++)
                for (int x = 0; x < bmp.Width; x++)
                    if (!reducedColors.Exists(c => c == bmp.GetPixel(x, y))) reducedColors.Add(bmp.GetPixel(x, y));

            return format.Save(reducedColors);
        }
    }
}