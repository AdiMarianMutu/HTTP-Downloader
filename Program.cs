using System;
using System.IO;
using System.Threading;
using System.Diagnostics;
using System.Net;

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
        }
        
        public static class HTTPDownloader {
            public static byte Current_NumberOfConcurrentThreads = 0; // Keeps track of the current amount of threads which are busy downloading a file
            public static byte Maximum_NumberOfConcurrentThreads = 1; // Maximum amount of possible simultaneous downloads

            /// <summary>
            /// Downloads a file using the HTTP Protocol. Returns true if the operation succeed
            /// </summary>
            /// <param name="url">URL of the file to be downloaded</param>
            /// <param name="outputDirectory">Directory where to save the downloaded file</param>
            /// <param name="fileName">The local name of the downloaded file</param>
            /// <param name="maximum_DownloadSpeedBytes">The maximum speed in bytes per second</param>
            public static bool DownloadFile(string url, string outputDirectory, string fileName, uint maximum_DownloadSpeedBytes = uint.MaxValue) {
                // Used to measure the elapsed time of the downloaded file
                Stopwatch _stopWatch = new Stopwatch();
                _stopWatch.Start();

                bool operationResult = false;
                try {
                    string filePath = String.Format("{0}\\{1}", outputDirectory.TrimEnd('\\'), fileName);

                    if (!File.Exists(filePath)) {
                        Console.WriteLine("[INFO]: Downloading the '{0}' file...", fileName);

                        // We create the request by using the 'get' method
                        HttpWebRequest httpWebRequest           = (HttpWebRequest)WebRequest.Create(url);
                                       httpWebRequest.Method    = WebRequestMethods.Http.Get;
                        // We need to set also an User Agent to avoid a 403 error response from the server
                                       httpWebRequest.UserAgent = ".NET Framework HTTP Protocol Download File Test";
                        // By default the maximum connection allowed at once is limited to 2, we need to extend it using the value of the 'Maximum_NumberOfConcurrentThreads'
                        ServicePointManager.DefaultConnectionLimit = Maximum_NumberOfConcurrentThreads;

                        // Retrieve the response from the request
                        using (HttpWebResponse httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse()) {
                            // Gets the 'Stream' from the response
                            using (Stream stream = httpWebResponse.GetResponseStream()) {
                                // Initializes the buffer which will contain the received data
                                const int buffSize     = 4096;
                                byte[]    buff         = new byte[buffSize];
                                // The amount of data read from the chunk
                                int       bytesRead    = 0;
                                // The total amount of data which was read until the reaching of the speed limit (if the case)
                                uint      totBytesRead = 0;

                                // Initializes the 'FileStream' which will write the data into the file
                                using (FileStream fileStream = new FileStream(filePath, FileMode.CreateNew, FileAccess.Write, FileShare.Read)) {
                                    // This variable will contain the ticks passed from receiving the chunks of data until the speed limit will be reached within a second
                                    long ticksPassedReachedSpeedLimit = DateTime.Now.Ticks;

                                    while ((bytesRead = stream.Read(buff, 0, buffSize)) != 0) {
                                        fileStream.Write(buff, 0, bytesRead);
                                        totBytesRead += (uint)bytesRead;

                                        // When the 'totBytesRead' value reaches the maximum download speed limit within a second,
                                        // we use 'Thread.Sleep()' to throttle the speed which the file are being written.
                                        // The value passed to 'Thread.Sleep()' is the amount of ms left within the current second
                                        // The formula used for the throttling speed feature is the below:
                                        // c - ((x - y) / c1) = ms
                                        // Where:
                                        // c  => 1000 milliseconds (constant)
                                        // x  => 'DateTime.Now' in ticks
                                        // y  => 'ticksPassedReachedSpeedLimit'
                                        // c1 => 10000 milliseconds (constant)
                                        // ms => The value passed to 'Thread.Sleep()'
                                        if (totBytesRead > maximum_DownloadSpeedBytes && (DateTime.Now.Ticks - ticksPassedReachedSpeedLimit) <= TimeSpan.TicksPerSecond) {
                                            Thread.Sleep((int)(1000 - ((DateTime.Now.Ticks - ticksPassedReachedSpeedLimit) / 10000)));

                                            totBytesRead = 0;
                                            ticksPassedReachedSpeedLimit = DateTime.Now.Ticks;
                                        }
                                    }
                                }

                                // If the length of the downloaded file and the length of the file on the server are equal, the download was a success
                                if ((new FileInfo(filePath)).Length == httpWebResponse.ContentLength)
                                    operationResult = true;
                                else
                                    Console.WriteLine("[ERROR]: The download of the '{0}' file isn't complete!", fileName);
                            }
                        }
                    } else
                        Console.WriteLine("[WARNING]: A file with the same name as the '{0}' already exists!", fileName);
                } catch (Exception e) {
                    Console.WriteLine("[EXCEPTION : {0}]: Unable to download the '{1}' file!", e.Message, fileName);
                }
                _stopWatch.Stop();

                // Now the main thread knows that a thread is free to download a new file
                Current_NumberOfConcurrentThreads--;
                if (operationResult == true)
                    Console.WriteLine("[INFO]: Download of the '{0}' file completed |> ELAPSED: {1}", fileName, _stopWatch.Elapsed);

                return operationResult;
            }
        }

        static void Main(string[] args) {

            /*args = new string[8];
            args[0] = "-f";
            args[1] = @"C:\Users\Mutu.A\Desktop\urls.txt"; // URLs list file
            args[2] = "-o";
            args[3] = @"C:\Users\Mutu.A\Desktop\test";     // Output directory
            args[4] = "-n";
            args[5] = "4";                                 // Number of concurrent threads
            args[6] = "-l";
            args[7] = "0";                                 // Speed limit*/

            string URLsFileList = "";
            string DownloadedFiles_OutputDirectory = "";
            uint   Maximum_DownloadSpeedBytes = uint.MaxValue;

            // Parsing the given arguments
            for (int i = 0; i < args.Length; i++) {
                if (args[i] == "-f") {
                    try {
                        if (!String.IsNullOrEmpty(args[i + 1])) {
                            if (args[i + 1].StartsWith("\"")) {
                                for (int j = i + 1; j < args.Length; j++) {
                                    URLsFileList += args[j];

                                    if (args[j].EndsWith("\""))
                                        break;
                                }
                            } else
                                URLsFileList = args[i + 1];
                        }
                    } catch {
                        _Debug.WriteFatalError("The '-f' parameter is empty!");
                        break;
                    }
                } else if (args[i] == "-o") {
                    try {
                        if (!String.IsNullOrEmpty(args[i + 1])) {
                            if (args[i + 1].StartsWith("\"")) {
                                for (int j = i + 1; j < args.Length; j++) {
                                    DownloadedFiles_OutputDirectory += args[j];

                                    if (args[j].EndsWith("\""))
                                        break;
                                }
                            } else
                                DownloadedFiles_OutputDirectory = args[i + 1];
                        }
                    } catch {
                        _Debug.WriteFatalError("The '-o' parameter is empty!");
                        break;
                    }
                } else if (args[i] == "-n") {
                    try {
                        if (!String.IsNullOrEmpty(args[i + 1])) {
                            if (!Byte.TryParse(args[i + 1], out HTTPDownloader.Maximum_NumberOfConcurrentThreads)) {
                                _Debug.WriteFatalError("The '-n' parameter must be an (unsigned) integer (8-bit)!");
                                break;
                            } else {
                                if (HTTPDownloader.Maximum_NumberOfConcurrentThreads == 0)
                                    HTTPDownloader.Maximum_NumberOfConcurrentThreads = 1;
                            }
                        }
                    } catch {
                        _Debug.WriteFatalError("The '-n' parameter is empty!");
                        break;
                    }
                } else if (args[i] == "-l") {
                    try {
                        if (!String.IsNullOrEmpty(args[i + 1])) {
                            if (!UInt32.TryParse(args[i + 1], out Maximum_DownloadSpeedBytes)) {
                                _Debug.WriteFatalError("The '-l' parameter must be an (unsigned) integer (32-bit)");
                                break;
                            } else {
                                if (Maximum_DownloadSpeedBytes < 62500 && Maximum_DownloadSpeedBytes != 0) // Minimum speed throttling : 0.5Mbit/s
                                    Maximum_DownloadSpeedBytes = 62500;
                                else if (Maximum_DownloadSpeedBytes == 0) // 0 = no speed limit
                                    Maximum_DownloadSpeedBytes = uint.MaxValue;
                            }
                        }
                    } catch {
                        _Debug.WriteFatalError("The '-l' parameter is empty!");
                        break;
                    }
                }
            }
            if (String.IsNullOrEmpty(URLsFileList))
                _Debug.WriteFatalError("You must add the full path where the URLs list file is!");
            if (String.IsNullOrEmpty(DownloadedFiles_OutputDirectory))
                _Debug.WriteFatalError("You must add the full path where the downloaded files would be saved!");

            Console.WriteLine("\nMaximum download speed: {0:0.0} Mbit/s => {1} bytes/s\n", Maximum_DownloadSpeedBytes / 125000F, Maximum_DownloadSpeedBytes);

            _Debug._Stopwatch.Start();
            // Parsing the file which contains the URLs list
            // Before starting: We verify the existence of the 'Output Directory'/'URLs List File'
            if (!Directory.Exists(DownloadedFiles_OutputDirectory))
                _Debug.WriteFatalError("The output directory where the downloaded files should be saved doesn't exists!");

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
