using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.IO;

namespace SetDPI
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 3)
            {
                DisplayHelp();
                return;
            }

            float dpiX, dpiY;
            if (!float.TryParse(args[0], out dpiX) || !float.TryParse(args[1], out dpiY))
            {
                DisplayHelp();
                return;
            }

            for (int i = 2; i < args.Length; i++)
            {
                string path = ".";
                string pattern = args[i];

                // determine whether a directory is part of the path provided
                int lastbackslash = args[i].LastIndexOf('\\');
                if (lastbackslash >= 0)
                {
                    path = args[i].Substring(0, lastbackslash);
                    pattern = args[i].Substring(lastbackslash + 1);
                }

                if (!Directory.Exists(path))
                {
                    Console.WriteLine("Warning: Directory {0} does not exist. Skipping.", path);
                }
                else
                {
                    string[] files = Directory.GetFiles(path, pattern);
                    if (files.Length < 0)
                    {
                        Console.WriteLine("Warning: No files matching the pattern {0} in the directory {1}. Skipping.", pattern, path);
                    }
                    else
                    {
                        foreach (string file in files)
                        {
                            try
                            {
                                Bitmap image = new Bitmap(file);
                                float dpiXoriginal = image.HorizontalResolution;
                                float dpiYoriginal = image.VerticalResolution;
                                Bitmap newimage = new Bitmap(image);
                                image.Dispose();

                                newimage.SetResolution(dpiX, dpiY);
                                newimage.Save(file);
                                newimage.Dispose();

                                Bitmap newimagecheck = new Bitmap(file);
                                float dpiXnew = newimagecheck.HorizontalResolution;
                                float dpiYnew = newimagecheck.VerticalResolution;
                                newimagecheck.Dispose();

                                Console.WriteLine("{0} - DPI (x,y): was ({1},{2}), now ({3},{4})", file, dpiXoriginal, dpiYoriginal, dpiXnew, dpiYnew);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine("Warning: Failed to set DPI for image {0} ({1}).", file, ex.ToString());
                            }
                        }
                    }
                }
            }

        }

        static void DisplayHelp()
        {
            Console.WriteLine("SetDPI.exe usage:");
            Console.WriteLine("  SetDPI dpiX dpiY filepattern [filepattern [filepattern...]]");
        }
    }
}
