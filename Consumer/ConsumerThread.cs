using System;
using System.Collections.Generic;
using System.IO;
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

        private string videoName;

        private Thread thread;

        public ConsumerThread(uint id)
        {
            this.id = id;

            thread = new Thread(Run);
        }

        public void Run()
        {
            while (true)
            {
                // Get video request from the shared queue
                VideoRequest request = Program.videoRequestQueue.Dequeue();

                if (request == null)
                {
                    // Sleep briefly and retry if queue is empty
                    Thread.Sleep(100);
                    continue;
                }

                this.videoName = request.videoName;

                try
                {
                    // Connect to the corresponding producer thread
                    producerThreadClient = new TcpClient();
                    producerThreadClient.Connect(Program.producerIPAddress, request.producerPort);
                    producerThreadStream = producerThreadClient.GetStream();

                    Console.WriteLine($"Consumer Thread {id} connected to Producer at port {request.producerPort} for video {request.videoName}");

                    // Receive the video
                    ReceiveVideo();

                    // Close the stream and connection
                    producerThreadStream.Close();
                    producerThreadClient.Close();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Consumer Thread {id} failed to connect to producer at port {request.producerPort}: {ex.Message}");
                }
            }
        }


        private void ReceiveVideo()
        {
            try
            {
                Console.WriteLine($"Producer Thread {id} is sending video {this.videoName}");

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

                    Console.WriteLine("File already exists. Renaming to: " + videoName);
                }

                // Create the file path
                string filePath = System.IO.Path.Combine(Program.videoFolder, videoName);

                // Receive the file data
                using FileStream fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
                producerThreadStream.CopyToAsync(fileStream);

                Console.WriteLine("Download complete");
                MessageBox.Show("Download complete", "Download", MessageBoxButton.OK, MessageBoxImage.Information);

                // Add the video to the list
                MainWindow.AddVideoToList(videoName);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
                MessageBox.Show("Error: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void Start()
        {
            thread.Start();
        }

        public void Join()
        {
            thread.Join();
        }
    }
}
