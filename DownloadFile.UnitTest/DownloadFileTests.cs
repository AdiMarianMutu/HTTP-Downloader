using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Text;
using System.Diagnostics;
using HTTP_Downloader;

namespace DownloadFile.UnitTest {
    [TestClass]
    public class DownloadFileTests {
        // N.B: You need at least a 6Mbit/s speed connection to be able to pass the 'speed throttling' tests
        // Link where you can find the files used for the tests: https://www.thinkbroadband.com/download

        string OutputPath          = String.Format(@"{0}\{1}\", Directory.GetCurrentDirectory(), "Unit Tests");
        // 5MB File Test
        const string URL_5MBFile   = "http://ipv4.download.thinkbroadband.com/5MB.zip";
        const string Name_5MBFile  = "5MBFile_UnitTest.bin";
        // 10MB File Test
        const string URL_10MBFile  = "http://ipv4.download.thinkbroadband.com/10MB.zip";
        const string Name_10MBFile = "10MBFile_UnitTest.bin";
        // 20MB File Test
        const string URL_20MBFile  = "http://ipv4.download.thinkbroadband.com/20MB.zip";
        const string Name_20MBFile = "20MBFile_UnitTest.bin";

        /// <summary>
        /// Creates the directory where the test files will be downloaded and delete the old test files
        /// </summary>
        /// <param name="fullFilePath">The full local path of the current file being tested</param>
        private void VerifyResources(string fullFilePath) {
            if (File.Exists(fullFilePath))
                File.Delete(fullFilePath);

            if (!Directory.Exists(OutputPath))
                Directory.CreateDirectory(OutputPath);
        }

        /// <summary>
        /// Returns the digest hash derived from the test file using the MD5 algorithm
        /// </summary>
        /// <param name="fullFilePath">The full local path of the current file being tested</param>
        private string GetMD5DigestHash(string fullFilePath) {
            byte[] hash = (new System.Security.Cryptography.MD5CryptoServiceProvider()).ComputeHash(File.ReadAllBytes(fullFilePath));
            var    sb   = new StringBuilder();

            for (int i = 0; i < hash.Length; i++)
                sb.Append(hash[i].ToString("x2"));

            return sb.ToString();
        }

        /*********************************************************************************************************************************************************/

        [TestMethod]
        public void VerifyIntegrity_5MBFile() {
            // Arrange
            string       fullPath           = String.Format("{0}{1}", OutputPath, Name_5MBFile);
            const string expected_md5Digest = "b3215c06647bc550406a9c8ccc378756";

            VerifyResources(fullPath);

            // Act
            Program.HTTPDownloader.DownloadFile(URL_5MBFile, OutputPath, Name_5MBFile);

            // Assert
            Assert.AreEqual(expected_md5Digest, GetMD5DigestHash(fullPath));
        }

        [TestMethod]
        public void VerifyIntegrity_10MBFile() {
            // Arrange
            string       fullPath           = String.Format("{0}{1}", OutputPath, Name_10MBFile);
            const string expected_md5Digest = "3aa55f03c298b83cd7708e90d289afbd";

            VerifyResources(fullPath);

            // Act
            Program.HTTPDownloader.DownloadFile(URL_10MBFile, OutputPath, Name_10MBFile);

            // Assert
            Assert.AreEqual(expected_md5Digest, GetMD5DigestHash(fullPath));
        }

        [TestMethod]
        public void VerifyIntegrity_20MBFile() {
            // Arrange
            string       fullPath           = String.Format("{0}{1}", OutputPath, Name_20MBFile);
            const string expected_md5Digest = "9017804333c820e3b4249130fc989e00";

            VerifyResources(fullPath);

            // Act
            Program.HTTPDownloader.DownloadFile(URL_20MBFile, OutputPath, Name_20MBFile);

            // Assert
            Assert.AreEqual(expected_md5Digest, GetMD5DigestHash(fullPath));
        }

        /*********************************************************************************************************************************************************/

        [TestMethod]
        public void VerifyThrottleFeature_5MBFile_1Mbit() {
            // Arrange
            string       fullPath              = String.Format("{0}{1}", OutputPath, Name_5MBFile);
            long[]       expected_timingResult = { 36 * 1000, 46 * 1000}; // If the program has access to the full speed of your internet connection (in this case 1Mbit/s)
                                                                          // the download should take ~41 seconds.
                                                                          // But in a real world scenario, the connection speed can change second by second,
                                                                          // so in order to claim the test a success, the download should finish between 36 and 46 seconds.
            var          stopWatch             = new Stopwatch();

            VerifyResources(fullPath);

            // Act
            stopWatch.Start();
            Program.HTTPDownloader.DownloadFile(URL_5MBFile, OutputPath, Name_5MBFile, 125000); // The download speed will be limited to 1Mbit/s
            stopWatch.Stop();

            // Assert
            Assert.IsTrue(stopWatch.ElapsedMilliseconds >= expected_timingResult[0] && stopWatch.ElapsedMilliseconds <= expected_timingResult[1]);
        }

        [TestMethod]
        public void VerifyThrottleFeature_10MBFile_2Mbit() {
            // Arrange
            string       fullPath              = String.Format("{0}{1}", OutputPath, Name_10MBFile);
            long[]       expected_timingResult = { 36 * 1000, 46 * 1000 }; // If the program has access to the full speed of your internet connection (in this case 2Mbit/s)
                                                                           // the download should take ~41 seconds.
                                                                           // But in a real world scenario, the connection speed can change second by second,
                                                                           // so in order to claim the test a success, the download should finish between 36 and 46 seconds.
            var stopWatch                      = new Stopwatch();

            VerifyResources(fullPath);

            // Act
            stopWatch.Start();
            Program.HTTPDownloader.DownloadFile(URL_10MBFile, OutputPath, Name_10MBFile, 250000); // The download speed will be limited to 2Mbit/s
            stopWatch.Stop();

            // Assert
            Assert.IsTrue(stopWatch.ElapsedMilliseconds >= expected_timingResult[0] && stopWatch.ElapsedMilliseconds <= expected_timingResult[1]);
        }

        [TestMethod]
        public void VerifyThrottleFeature_20MBFile_4Mbit() {
            // Arrange
            string       fullPath              = String.Format("{0}{1}", OutputPath, Name_20MBFile);
            long[]       expected_timingResult = { 36 * 1000, 46 * 1000 }; // If the program has access to the full speed of your internet connection (in this case 4Mbit/s)
                                                                           // the download should take ~41 seconds.
                                                                           // But in a real world scenario, the connection speed can change second by second,
                                                                           // so in order to claim the test a success, the download should finish between 36 and 46 seconds.
            var          stopWatch             = new Stopwatch();

            VerifyResources(fullPath);

            // Act
            stopWatch.Start();
            Program.HTTPDownloader.DownloadFile(URL_20MBFile, OutputPath, Name_20MBFile, 500000); // The download speed will be limited to 4Mbit/s
            stopWatch.Stop();

            // Assert
            Assert.IsTrue(stopWatch.ElapsedMilliseconds >= expected_timingResult[0] && stopWatch.ElapsedMilliseconds <= expected_timingResult[1]);
        }
    }
}