using System;
using System.IO;
using System.Threading;
using System.Diagnostics;

namespace HTTP_Downloader {
    public class Program {

        public static class _Debug {
            // Used to track the time elapsed for the whole work
            public static Stopwatch _Stopwatch = new Stopwatch();

            /// <summary>
            /// Writes to the console the given message
            /// </summary>
            /// <param name="message">The string to be written to the console</param>
            public static void WriteFatalError(string message) {
                Console.WriteLine("{0}\nPress any key to exit...", message);

                Console.ReadKey();
                Environment.Exit(1);
            }

            /// <summary>
            /// Parse an argument and returns his value
            /// </summary>
            /// <param name="parameter">The character which identifies the start of the argument (Like '-f')</param>
            /// <param name="args">The array which contains the arguments</param>
            public static string GetArgument(char parameter, string[] args) {
                string argValue = null;

                for (int i = 0; i < args.Length; i++) {
                    if (args[i] == $"-{parameter}") {
                        try {
                            if (!String.IsNullOrEmpty(args[i + 1])) {
                                if (args[i + 1].StartsWith("\"")) {
                                    for (int j = i + 1; j < args.Length; j++) {
                                        argValue += args[j];

                                        if (args[j].EndsWith("\""))
                                            break;
                                    }
                                } else
                                    argValue = args[i + 1];
                            }
                        } catch {
                            WriteFatalError($"The '{parameter}' parameter is empty!");
                            break;
                        }

                        break;
                    }
                }

                return argValue;
            }
        }
        
        static void Main(string[] args) {
            args = new string[8];
            args[0] = "-f";
            args[1] = @"C:\Users\Mutu.A\Desktop\urls.txt"; // URLs list file
            args[2] = "-o";
            args[3] = @"C:\Users\Mutu.A\Desktop\test";     // Output directory
            args[4] = "-n";
            args[5] = "2";                                 // Number of concurrent threads
            args[6] = "-l";
            args[7] = "0";                                 // Speed limit

            // Parsing the given arguments
            string URLsFileList                    = _Debug.GetArgument('f', args);
            string DownloadedFiles_OutputDirectory = _Debug.GetArgument('o', args);
            uint   Maximum_DownloadSpeedBytes      = uint.MaxValue;

            if (!UInt32.TryParse(_Debug.GetArgument('l', args), out Maximum_DownloadSpeedBytes))
                _Debug.WriteFatalError("The '-l' parameter must be an (unsigned) integer (32-bit)");
            else
                if (Maximum_DownloadSpeedBytes < 62500 && Maximum_DownloadSpeedBytes != 0) // Minimum speed throttling : 0.5Mbit/s
                    Maximum_DownloadSpeedBytes = 62500;
                else if (Maximum_DownloadSpeedBytes == 0) // 0 = no speed limit
                    Maximum_DownloadSpeedBytes = uint.MaxValue;

            if (!Byte.TryParse(_Debug.GetArgument('n', args), out HTTPDownloader.Maximum_NumberOfConcurrentThreads))
                _Debug.WriteFatalError("The '-n' parameter must be an (unsigned) integer (8-bit)!");
            else
                if (HTTPDownloader.Maximum_NumberOfConcurrentThreads == 0)
                    HTTPDownloader.Maximum_NumberOfConcurrentThreads = 1;
            
            if (String.IsNullOrEmpty(URLsFileList))
                _Debug.WriteFatalError("You must add the full path where the URLs list file is!");
            if (String.IsNullOrEmpty(DownloadedFiles_OutputDirectory))
                _Debug.WriteFatalError("You must add the full path where the downloaded files would be saved!");

            Console.WriteLine("\nMaximum download speed: {0:0.0} Mbit/s => {1} bytes/s\n", Maximum_DownloadSpeedBytes / 125000F, Maximum_DownloadSpeedBytes);

            _Debug._Stopwatch.Start();
            // Parsing the file which contains the URLs list
            // Before starting: We verify the existence of the 'Output Directory'/'URLs List File'
            if (!Directory.Exists(DownloadedFiles_OutputDirectory))
                _Debug.WriteFatalError("The output directory where the downloaded files would be saved doesn't exists!");

            if (File.Exists(URLsFileList)) {
                // Enumerating the lines in the file
                foreach (string _s in File.ReadAllLines(URLsFileList)) {
                    if (!String.IsNullOrEmpty(_s)) {
                        string url      = _s.Substring(0, _s.IndexOf(' '));  // Gets the URL from the current line
                        string fileName = _s.Substring(_s.IndexOf(' ') + 1); // Gets the local file name from the current line

                        // If the working threads are less than the maximum working threads allowed...
                        if (HTTPDownloader.Current_NumberOfConcurrentThreads < HTTPDownloader.Maximum_NumberOfConcurrentThreads) {
                            // We start a new working thread where the file should be downloaded
                            // Passing as parameters the URL, Output Directory, Local File Name and optionally the maximum speed limit
                            Thread downloadFileThread = new Thread(() => HTTPDownloader.DownloadFile(url, DownloadedFiles_OutputDirectory, fileName, Maximum_DownloadSpeedBytes));
                                   downloadFileThread.Start();

                            // We need to keep track of the active working threads to simulate a queue list
                            HTTPDownloader.Current_NumberOfConcurrentThreads++;
                        }

                        // Until all threads are busy, we block the foreach loop
                        while (HTTPDownloader.Current_NumberOfConcurrentThreads >= HTTPDownloader.Maximum_NumberOfConcurrentThreads)
                            // Used to avoid overload of the CPU
                            Thread.Sleep(1);
                    }
                }
            } else
                _Debug.WriteFatalError("Unable to find the file which contains the URLs list!");

            // Blocking the main thread
            while (true) {
                if (HTTPDownloader.Current_NumberOfConcurrentThreads == 0) {
                    _Debug._Stopwatch.Stop();
                    Console.WriteLine("\n\nTime elapsed for the whole work: {0}\n", _Debug._Stopwatch.Elapsed);

                    break;
                }

                Thread.Sleep(1);
            }
            
            Console.WriteLine("Done! Press any key to exit...");
            Console.ReadKey();
        }
    }
}