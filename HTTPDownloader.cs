using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Diagnostics;

public static class HTTPDownloader {
    /// <summary>
    /// Keeps track of the current amount of threads which are busy downloading a file
    /// </summary>
    public static byte Current_NumberOfConcurrentThreads;

    /// <summary>
    /// Maximum amount of possible simultaneous downloads
    /// </summary>
    public static byte Maximum_NumberOfConcurrentThreads = 1;

    /// <summary>
    /// Returns the response stream from the request
    /// </summary>
    private static Stream GetResponseStream(ref HttpWebRequest request) {
        return request.GetResponse().GetResponseStream();
    }

    /// <summary>
    /// Downloads a file using the HTTP Protocol. Returns true if the operation succeed
    /// </summary>
    /// <param name="url">URL of the file to be downloaded</param>
    /// <param name="outputDirectory">Directory where to save the downloaded file</param>
    /// <param name="fileName">The local name of the downloaded file</param>
    /// <param name="maximum_DownloadSpeedBytes">The maximum speed in bytes per second</param>
    public static void DownloadFile(string url, string outputDirectory, string fileName, uint maximum_DownloadSpeedBytes = uint.MaxValue) {
        // Used to measure the elapsed time of the downloaded file
        Stopwatch _stopWatch = new Stopwatch();
                  _stopWatch.Start();

        try {
            string filePath = String.Format("{0}\\{1}", outputDirectory.TrimEnd('\\'), fileName);

            if (!File.Exists(filePath)) {
                Console.WriteLine("[INFO]: Downloading the '{0}' file...", fileName);

                // We create the request by using the 'get' method
                HttpWebRequest httpWebRequest        = (HttpWebRequest)WebRequest.Create(url);
                               httpWebRequest.Method = WebRequestMethods.Http.Get;
                // We need to set also an User Agent to avoid a 403 error response from the server
                               httpWebRequest.UserAgent = ".NET Framework HTTP Protocol Download File Test";
                // By default the maximum connection allowed at once is limited to 2, we need to extend it using the value of the 'Maximum_NumberOfConcurrentThreads'
                ServicePointManager.DefaultConnectionLimit = Maximum_NumberOfConcurrentThreads;

                // Gets the 'Stream' from the response
                using (Stream stream = GetResponseStream(ref httpWebRequest)) {
                    // Initializes the 'FileStream' which will write the data into the file
                    using (FileStream fileStream = new FileStream(filePath, FileMode.CreateNew, FileAccess.Write, FileShare.Read)) {
                        // Initializes the buffer which will contain the received data
                        const int buffSize     = 4096;
                        byte[]    buff         = new byte[buffSize];
                        // The amount of data read from the chunk
                        int       bytesRead    = 0;
                        // The total amount of data which was read until the reaching of the speed limit (if the case)
                        uint      totBytesRead = 0;

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
                }
            } else
                Console.WriteLine("[WARNING]: A file with the same name as the '{0}' already exists!", fileName);
        } catch (Exception e) {
            Console.WriteLine("[EXCEPTION : {0}]: Unable to download the '{1}' file!", e.Message, fileName);
        }
        _stopWatch.Stop();

        // Now the main thread knows that a thread is free to download a new file
        Current_NumberOfConcurrentThreads--;

        Console.WriteLine("[INFO]: Download of the '{0}' file completed |> ELAPSED: {1}", fileName, _stopWatch.Elapsed);
    }
}