using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Consumer
{
    class ConsumerThread
    {
        public uint id;

        private TcpClient producerThreadClient;
        private NetworkStream producerThreadStream;

        private Thread thread;

        public ConsumerThread(uint id)
        {
            this.id = id;
        }

        public void Run()
        {
            while (Program.hasVideosToSend || Program.videoRequestQueue.Count > 0)
            {
                // Get video request from the shared queue
                VideoRequest request = Program.videoRequestQueue.Dequeue();
                if (request == null)
                {
                    // Sleep briefly and retry if queue is empty
                    Console.WriteLine($"[Consumer Thread {id}] Queue is empty. Retrying...");
                    Thread.Sleep(1000);
                    continue;
                }
                Console.WriteLine($"[Consumer Thread {id}] Dequeued video request for {request.videoName}");

                try
                {
                    // Connect to the corresponding producer thread
                    producerThreadClient = new TcpClient();
                    producerThreadClient.Connect(Program.producerIPAddress, request.producerPort);
                    producerThreadStream = producerThreadClient.GetStream();

                    Console.WriteLine($"[Consumer Thread {id}] Connected to Producer at port {request.producerPort} for video {request.videoName}");

                    // Receive the video
                    ReceiveVideo(request.videoName);

                    // Close the stream and connection
                    producerThreadStream.Close();
                    producerThreadClient.Close();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Consumer Thread {id}] Error connecting to producer at port {request.producerPort}: {ex.Message}");
                }

                // Give back the slot to the queue
                Program.videoRequestQueue.IncrementSlotBack();
            }
        }

        private void ReceiveVideo(string videoName)
        {
            try
            {
                // Check if the file already exists
                if (File.Exists(System.IO.Path.Combine(Program.videoFolder, videoName)))
                {
                    // If the file exists, add a number to the file name
                    int i = 1;
                    string fileNameWithoutExtension = System.IO.Path.GetFileNameWithoutExtension(videoName);
                    string fileExtension = System.IO.Path.GetExtension(videoName);
                    while (File.Exists(System.IO.Path.Combine(Program.videoFolder, videoName)))
                    {
                        videoName = fileNameWithoutExtension + "_" + i + fileExtension;
                        i++;
                    }

                    Console.WriteLine($"[Consumer Thread {id}] File {videoName} already exists. Renaming to: " + videoName);
                }

                // Create the file path
                string filePath = System.IO.Path.Combine(Program.videoFolder, videoName);

                // Receive file, decompress it, and save it
                using GZipStream gzipStream = new GZipStream(producerThreadStream, CompressionMode.Decompress, true);
                using FileStream fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
                //producerThreadStream.CopyTo(fileStream);

                gzipStream.CopyTo(fileStream);
                fileStream.Flush();

                // Log the sizes
                long decompressedFileSize = new FileInfo(filePath).Length;

                Console.WriteLine($"[Consumer Thread {id}] Received video {videoName} with {decompressedFileSize} bytes");

                // Add the video to the list
                Task.Run(() => MainWindow.AddVideoToList(videoName));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Consumer Thread {id}] Error receiving video: " + ex.Message);
                MessageBox.Show("Error receiving video: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void Start()
        {
            thread = new Thread(Run);
            thread.Start();
        }

        public void Join()
        {
            if (thread != null && thread.IsAlive)
                thread.Join();
        }
    }
}
