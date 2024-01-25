using System;
using System.IO.Compression;
using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Tar;

namespace UnityPackageExtractor
{
    internal class Program
    {
        private static string tmpFolder = "./Temp";

        static void Main(string[] args)
        {
            if (args.Length == 0 || args[0] == "-h")
            {
                DisplayHelp();
                return;
            }

            string inputFile = "";
            string outputDirectory = "";
            bool skipMeta = false;
            bool extractPreviews = false;

            if(args.Length == 1 && File.Exists(args[0]))
            {
                inputFile = args[0];
                outputDirectory = "./";
                skipMeta = false;
                extractPreviews = false;
            }
            else
            {
                for (int arg = 0; arg < args.Length; arg++)
                {
                    switch (args[arg])
                    {
                        case "-h":
                            DisplayHelp();
                            return;
                        case "-i":
                            arg++;
                            inputFile = args[arg];
                            break;
                        case "-o":
                            arg++;
                            outputDirectory = args[arg];
                            break;
                        case "--skipmeta":
                            skipMeta = true;
                            break;
                        case "--extractpreviews":
                            extractPreviews = true;
                            break;
                    }
                }
            }

            if (string.IsNullOrEmpty(inputFile) || !File.Exists(inputFile))
            {
                Console.WriteLine("Input file does not exist");
                return;
            }

            if(string.IsNullOrEmpty(inputFile) || !Directory.Exists(outputDirectory))
            {
                Console.WriteLine("Output directory does not exist");
                return;
            }

            Console.WriteLine("Extracting");
            try
            {
                Directory.CreateDirectory(tmpFolder);
                ExtractTarGZ(inputFile, tmpFolder);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return;
            }

            Console.WriteLine("Organizing");
            var packageFiles = Directory.GetDirectories(tmpFolder, "*", SearchOption.TopDirectoryOnly);
            if(packageFiles.Length > 0)
            {
                int fileCount = 0;
                int total = packageFiles.Length;

                foreach (var packageFile in packageFiles)
                {
                    fileCount++;

                    string assetFilePath = Path.Combine(packageFile, "asset");
                    string assetMetaFilePath = Path.Combine(packageFile, "asset.meta");
                    string assetPathFilePath = Path.Combine(packageFile, "pathname");
                    string assetPreviewFilePath = Path.Combine(packageFile, "preview.png");

                    if (!File.Exists(assetFilePath) || !File.Exists(assetMetaFilePath) || !File.Exists(assetPathFilePath))
                        continue;

                    string assetPath = File.ReadLines(assetPathFilePath).First();
                    if(string.IsNullOrEmpty(assetPath))
                        continue;

                    assetPath = Path.Combine(outputDirectory, assetPath);
                    string assetDirectory = Path.GetDirectoryName(assetPath);
                    if (!Directory.Exists(assetDirectory))
                        Directory.CreateDirectory(assetDirectory);

                    File.Copy(assetFilePath, assetPath);

                    if (!skipMeta)
                        File.Copy(assetMetaFilePath, assetPath + ".meta");

                    if (extractPreviews && File.Exists(assetPreviewFilePath))
                        File.Copy(assetPreviewFilePath, assetPath + "-preview.png");

                    Console.WriteLine((fileCount / (float)total).ToString("0.00%"));
                }
            }

            Console.WriteLine("Cleaning up");
            Directory.Delete(tmpFolder, true);

            Console.WriteLine("done.");
        }

        private static void DisplayHelp()
        {
            Console.Write(@"UnityPackageExtractor help:

-h: show this
-i: input file
-o: output folder
--skipmeta: ignore meta files
--extractpreviews : force extract preview images

exemple:
./UnityPackageExtractor.exe -i ./asset.unitypackage -o ./extracted --skipmeta

You can also drop the package on the executable to extract it in place.
");
        }

        public static void ExtractTarGZ(string gzArchiveName, string outputPath)
        {
            Stream inStream = File.OpenRead(gzArchiveName);
            Stream gzipStream = new GZipInputStream(inStream);

            TarArchive tarArchive = TarArchive.CreateInputTarArchive(gzipStream, System.Text.Encoding.ASCII);
            tarArchive.ExtractContents(outputPath, false);

            gzipStream.Close();
            inStream.Close();
            tarArchive.Close();
        }
    }
}