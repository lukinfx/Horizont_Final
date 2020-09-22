using BitMiracle.LibTiff.Classic;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReadGeoTiff
{
    class Trash
    {
        private static void ReadTiff(string fn)
        {
            Tiff tiff = Tiff.Open(fn, "r");
            if (tiff == null)
                return;

            //Get the image size
            int imageWidth = tiff.GetField(TiffTag.IMAGEWIDTH)[0].ToInt();
            int imageHeight = tiff.GetField(TiffTag.IMAGELENGTH)[0].ToInt();

            //Get the tile size
            int tileWidth = tiff.GetField(TiffTag.TILEWIDTH)[0].ToInt();
            int tileHeight = tiff.GetField(TiffTag.TILELENGTH)[0].ToInt();
            int tileSize = tiff.TileSize();

            //Pixel depth
            int depth = tileSize / (tileWidth * tileHeight);

            byte[] buffer = new byte[tileSize];

            for (int y = 0; y < imageHeight; y += tileHeight)
            {
                for (int x = 0; x < imageWidth; x += tileWidth)
                {
                    //Read the value and store to the buffer
                    tiff.ReadTile(buffer, 0, x, y, 0, 0);

                    for (int kx = 0; kx < tileWidth; kx++)
                    {
                        for (int ky = 0; ky < tileHeight; ky++)
                        {
                            //Calculate the index in the buffer
                            int startIndex = (kx + tileWidth * ky) * depth;
                            if (startIndex >= buffer.Length)
                                continue;

                            //Calculate pixel index
                            int pixelX = x + kx;
                            int pixelY = y + ky;
                            if (pixelX >= imageWidth || pixelY >= imageHeight)
                                continue;

                            //Get the value for the target pixel
                            double value = BitConverter.ToSingle(buffer, startIndex);
                        }
                    }
                }
            }

            tiff.Close();
        }


        static void Main2(string[] args)
        {
            string inputFileName = @"c:\temp\ElevationMap\N050E017\ALPSMLC30_N050E017_DSM.tif";
            string outputFileName = @"c:\temp\ElevationMap\N050E017\ALPSMLC30_N050E017_DSM2.txt";

            ReadTiff(inputFileName);

            var gt = new GeoTIFFBad(inputFileName);
            StreamWriter sw = new StreamWriter(outputFileName);

            for (int i = 0; i < gt.NHeight; i++)
            {
                StringBuilder sb = new StringBuilder();

                for (int j = 0; j < gt.NWidth; j++)
                {
                    var x = gt.HeightMap[i, j];
                    sb.Append($"{x:D4} ");
                }

                sw.WriteLine(sb.ToString());
            }
            sw.Close();

        }
        /*static void Main(string[] args)
        {
            
            string inputFileName = @"c:\temp\ElevationMap\N050E017\ALPSMLC30_N050E017_DSM.tif";
            string outputFileName = @"c:\temp\ElevationMap\N050E017\ALPSMLC30_N050E017_DSM.txt";


            using (Tiff tiff = Tiff.Open(inputFileName, "r"))
            {
                int width = tiff.GetField(TiffTag.IMAGEWIDTH)[0].ToInt();
                int height = tiff.GetField(TiffTag.IMAGELENGTH)[0].ToInt();
                double dpiX = tiff.GetField(TiffTag.XRESOLUTION)[0].ToDouble();
                double dpiY = tiff.GetField(TiffTag.YRESOLUTION)[0].ToDouble();

                byte[] scanline = new byte[tiff.ScanlineSize()];
                ushort[] scanline16Bit = new ushort[tiff.ScanlineSize() / 2];

                StreamWriter sw = new StreamWriter(outputFileName);

                for (int i = 0; i < height; i++)
                {
                    tiff.ReadScanline(scanline, i); //Loading ith Line                        
                    MultiplyScanLineAs16BitSamples(scanline, scanline16Bit, 1, i, sw);
                }

                sw.Close();
            }
        }

        private static void MultiplyScanLineAs16BitSamples(byte[] scanline, ushort[] temp, ushort factor, int row, StreamWriter sw)
        {
            if (scanline.Length % 2 != 0)
            {
                // each two bytes define one sample so there should be even number of bytes
                throw new ArgumentException();
            }

            Buffer.BlockCopy(scanline, 0, temp, 0, scanline.Length);

            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < temp.Length; i++)
            {
                temp[i] *= factor;
                //Console.WriteLine("Row:" + row.ToString() + "Column:" + (i / 2).ToString() + "Value:" + temp[i].ToString());
                sb.Append($"{temp[i]:D4} ");
            }

            sw.WriteLine(sb.ToString());
        }*/





        /*
        int buffersize = 1000000;
        using (Tiff tiff = Tiff.Open(fileName, "r"))
        {
            int nooftiles = tiff.GetField(TiffTag.TILEBYTECOUNTS).Length;
            int width = tiff.GetField(TiffTag.TILEWIDTH)[0].ToInt();
            int height = tiff.GetField(TiffTag.TILELENGTH)[0].ToInt();
            byte[] buffer = new byte[buffersize];

            for (int i = 0; i < nooftiles; i++)
            {
                int size = tiff.ReadEncodedTile(i, buffer, 0, buffersize);
                float[,] data = new float[width, height];
                Buffer.BlockCopy(buffer, 0, data, 0, size); // Convert byte array to x,y array of floats (height data)
                // Do whatever you want with the height data (calculate hillshade images etc.)
            }
        */
    }
}
