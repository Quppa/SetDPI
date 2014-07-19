namespace SetDPI
{
    using System;
    using System.IO;

    public class Program
    {
        public static void Main(string[] args)
        {
            if (args.Length < 3)
            {
                DisplayHelp();
                return;
            }

            float newppix, newppiy;
            if (!float.TryParse(args[0], out newppix) || !float.TryParse(args[1], out newppiy))
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
                                byte[] filebytes = File.ReadAllBytes(file);

                                int[] ppm = PNG.GetPPM(filebytes);

                                double ppix = 0, ppiy = 0;

                                if (ppm != null)
                                {
                                    ppix = PNG.PPMtoPPI(ppm[0]);
                                    ppiy = PNG.PPMtoPPI(ppm[1]);
                                }

                                byte[] updatedbytes = PNG.SetDPI(newppix, newppiy, filebytes);

                                File.WriteAllBytes(file, updatedbytes);

                                Console.WriteLine("{0} - DPI (x,y): was ({1},{2}), now ({3},{4})", file, ppix, ppiy, newppix, newppiy);
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
