using Kinect_Data_acquisition;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

using System.Runtime.Serialization.Formatters.Binary;
using Microsoft.Kinect;
using Microsoft.Kinect.Face;

namespace BinaryExtrator
{
    class Program
    {
      
        private static string colorPath;
        private static string depthPath;
        private static string depthCSVPath;
        private static string facePointsCSVPath;

        static void Main(string[] args)
        {
            var formatter = new BinaryFormatter();

            string filename = "C:\\Users\\Administrator\\source\\repos\\Kinect Data acquisition\\Kinect Data acquisition\\bin\\x64\\Release\\data.bin";
          
          

            {
                if (args.Length == 1)
                {
                    filename = args[0];
                }
                else
                {
                    Console.WriteLine(args.Length);
                }
                System.Console.WriteLine(System.AppDomain.CurrentDomain.BaseDirectory);
                colorPath = System.IO.Path.Combine(Environment.CurrentDirectory, "ColorImages");
                if (!Directory.Exists(colorPath))
                {
                    Directory.CreateDirectory(colorPath);
                }
                depthPath = System.IO.Path.Combine(Environment.CurrentDirectory, "DepthImages");
                if (!Directory.Exists(depthPath))
                {
                    Directory.CreateDirectory(depthPath);
                }
                depthCSVPath = System.IO.Path.Combine(Environment.CurrentDirectory, "DepthCSV");
                if (!Directory.Exists(depthCSVPath))
                {
                    Directory.CreateDirectory(depthCSVPath);
                }
                facePointsCSVPath = System.IO.Path.Combine(Environment.CurrentDirectory, "FacePointsCSV");
                if (!Directory.Exists(facePointsCSVPath))
                {
                    Directory.CreateDirectory(facePointsCSVPath);
                }
                using (var stream = new FileStream(filename, FileMode.Open))
                {
                    while (true)
                    {
                        object obj = null;
                        try
                        {
                            obj = formatter.Deserialize(stream);
                        }
                        catch (Exception)
                        {
                            break;
                        }
                        if (obj == null)
                        {
                            break;
                        }
                        else if (obj is AbstractDepthFrame)
                        {
                            AbstractDepthFrame tmp = (AbstractDepthFrame)obj;
                            string depthtmp = System.IO.Path.Combine(depthPath, tmp.content.ToString());
                            if (!Directory.Exists(depthtmp))
                            {
                                Directory.CreateDirectory(depthtmp);
                            }
                            string depthcsvtmp = System.IO.Path.Combine(depthCSVPath, tmp.content.ToString());
                            if (!Directory.Exists(depthcsvtmp))
                            {
                                Directory.CreateDirectory(depthcsvtmp);
                            }
                            string file = System.IO.Path.Combine(depthtmp, string.Format("depth_{0}.png", tmp.arrivedTime.ToString("yyyy-MM-dd-HH-mm-ss-ffff")));
                            string csvPath = System.IO.Path.Combine(depthcsvtmp, string.Format("depth_{0}.csv", tmp.arrivedTime.ToString("yyyy-MM-dd-HH-mm-ss-ffff")));

                            using (var bmap = new Bitmap(512, 424, System.Drawing.Imaging.PixelFormat.Format16bppRgb565))
                            {
                                var bdata = bmap.LockBits(
                                new System.Drawing.Rectangle(0, 0, 512, 424), ImageLockMode.WriteOnly, bmap.PixelFormat);
                                IntPtr ptr = bdata.Scan0;
                                short[] tmpBuffer = new short[tmp.buffer.Length];
                                for (int i = 0; i < tmp.buffer.Length; i++)
                                {
                                    tmpBuffer[i] = (short)tmp.buffer[i];
                                }
                                Marshal.Copy(tmpBuffer, 0, ptr, tmp.buffer.Length);
                                bmap.UnlockBits(bdata);
                                bmap.Save(file, ImageFormat.Png);
                            }

                            StringBuilder sb = new StringBuilder();
                            for (int i = 0; i < 424; i++)
                            {
                                string[] tmp_line = new string[512];
                                for (int j = 0; j < 512; j++)
                                {
                                    int count_depth = j + i * 512;
                                    int test = tmp.buffer[count_depth];
                                    tmp_line[j] = test.ToString();
                                }
                                sb.AppendLine(string.Join(",", tmp_line));
                            }
                            File.WriteAllText(csvPath, sb.ToString());
                        }
                        else if (obj is AbstractColorFrame)
                        {
                            AbstractColorFrame tmp = (AbstractColorFrame)obj;
                            string colortmp = System.IO.Path.Combine(colorPath, tmp.content.ToString());
                            if (!Directory.Exists(colortmp))
                            {
                                Directory.CreateDirectory(colortmp);
                            }
                            
                            using (var bmap = new Bitmap(1920, 1080, System.Drawing.Imaging.PixelFormat.Format32bppRgb))
                            {
                                var bdata = bmap.LockBits(
                                new System.Drawing.Rectangle(0, 0, 1920, 1080), ImageLockMode.WriteOnly, bmap.PixelFormat);
                                IntPtr ptr = bdata.Scan0;
                                Marshal.Copy(tmp.buffer, 0, ptr, tmp.buffer.Length);
                                bmap.UnlockBits(bdata);
                                var tmp_path = System.IO.Path.Combine(colortmp, string.Format("color_{0}.png", tmp.arrivedTime.ToString("yyyy-MM-dd-HH-mm-ss-ffff")));
                                bmap.Save(tmp_path, ImageFormat.Png);
                            }
                        }
                        else if (obj is AbstractFacePointsFrame)
                        {
                            AbstractFacePointsFrame tmp = (AbstractFacePointsFrame)obj;
                            string facetmp = System.IO.Path.Combine(facePointsCSVPath, tmp.content.ToString());
                            if (!Directory.Exists(facetmp))
                            {
                                Directory.CreateDirectory(facetmp);
                            }
                            string csvPath = System.IO.Path.Combine(facetmp, string.Format("facePoints_{0}.csv", tmp.arrivedTime.ToString("yyyy-MM-dd-HH-mm-ss-ffff")));

                            StringBuilder sb = new StringBuilder();
                            for (int i = 0; i < tmp.buffer.Length; i++)
                            {
                                string[] tmp_line = new string[3];
                                tmp_line[0] = tmp.buffer[i].X.ToString();
                                tmp_line[1] = tmp.buffer[i].Y.ToString();
                                tmp_line[2] = tmp.buffer[i].Z.ToString();
                                sb.AppendLine(string.Join(",", tmp_line));
                            }
                            File.WriteAllText(csvPath, sb.ToString());
                        }
                    }
                }
            }
        }
    }
}
