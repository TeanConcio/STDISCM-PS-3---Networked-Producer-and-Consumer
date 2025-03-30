using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Windows;

namespace Consumer
{
    class Program
    {
        // Default values for configurations
        private const uint DEFAULT_NUMBER_OF_CONSUMER_THREADS = 4;
        private const uint DEFAULT_MAX_QUEUE_SIZE = 4;
        private static readonly IPAddress DEFAULT_PRODUCER_IP_ADDRESS = IPAddress.Parse("127.0.0.1");
        private const uint DEFAULT_PRODUCER_PORT_NUMBER = 9000;

        // Configurations
        private static uint numConsumerThreads = DEFAULT_NUMBER_OF_CONSUMER_THREADS;
        private static uint maxQueueSize = DEFAULT_MAX_QUEUE_SIZE;
        private static IPAddress producerIPAddress = DEFAULT_PRODUCER_IP_ADDRESS;
        private static uint producerPortNumber = DEFAULT_PRODUCER_PORT_NUMBER;

        public const string videoFolder = "./downloaded_videos";

        // Producer variables
        private static TcpClient producerClient;
        private static NetworkStream producerStream;
        private static uint numProducerThreads = 0;


        public static void GetConfig()
        {
            Console.WriteLine("Getting Configurations from config.txt");
            Console.WriteLine();

            bool hasErrorWarning = false;

            string[] lines = System.IO.File.ReadAllLines("config.txt");
            foreach (string line in lines)
            {
                // Skip empty lines or comments (#)
                if (line.Trim() == "" || line.Trim().StartsWith("#"))
                {
                    continue;
                }

                // Split line by "="
                string[] parts = line.Split("=");

                switch (parts[0].Trim().ToUpper())
                {
                    case "NUMBER_OF_CONSUMER_THREADS (C) = ":
                        if (!uint.TryParse(parts[1].Trim(), out uint tempNumConsumerThreads) || tempNumConsumerThreads > int.MaxValue || tempNumConsumerThreads < 1)
                        {
                            Console.WriteLine($"Error: Invalid Number of Instances. Setting Number of Instances to {DEFAULT_NUMBER_OF_CONSUMER_THREADS}.");
                            hasErrorWarning = true;
                        }
                        else
                        {
                            // Check if very large number of instances
                            if (tempNumConsumerThreads >= 50)
                            {
                                Console.WriteLine($"Warning: Very large number of threads. Please expect a very long initialization time.");
                                hasErrorWarning = true;
                            }

                            numConsumerThreads = tempNumConsumerThreads;
                        }
                        break;

                    case "MAX_QUEUE_SIZE (Q) = ":
                        if (!uint.TryParse(parts[1].Trim(), out uint tempMaxQueueSize) || tempMaxQueueSize > int.MaxValue || tempMaxQueueSize < 1)
                        {
                            Console.WriteLine($"Error: Invalid Max Queue Size. Setting Max Queue Size to {DEFAULT_MAX_QUEUE_SIZE}.");
                            hasErrorWarning = true;
                        }
                        else
                        {
                            maxQueueSize = tempMaxQueueSize;
                        }
                        break;

                    case "PRODUCER_IP_ADDRESS":
                        if (!IPAddress.TryParse(parts[1].Trim(), out IPAddress tempProducerIPAddress))
                        {
                            Console.WriteLine($"Error: Invalid Producer IP Address. Setting Producer IP Address to {DEFAULT_PRODUCER_IP_ADDRESS}.");
                            hasErrorWarning = true;
                        }
                        else
                        {
                            producerIPAddress = tempProducerIPAddress;
                        }
                        break;

                    case "PRODUCER_PORT_NUMBER = ":
                        if (!ushort.TryParse(parts[1].Trim(), out ushort tempConsumerPortNumber) || tempConsumerPortNumber > ushort.MaxValue || tempConsumerPortNumber < 1)
                        {
                            Console.WriteLine($"Error: Invalid Producer Port Number. Setting Producer Port Number to {DEFAULT_PRODUCER_PORT_NUMBER}.");
                            hasErrorWarning = true;
                        }
                        else
                        {
                            producerPortNumber = tempConsumerPortNumber;
                        }
                        break;
                }
            }

            // If there is an error, print a line
            if (hasErrorWarning)
            {
                Console.WriteLine();
            }

            // Print Configurations
            Console.WriteLine("Number of Consumer Threads: " + numConsumerThreads);
            Console.WriteLine("Max Queue Size: " + maxQueueSize);
            Console.WriteLine("Producer IP Address: " + producerIPAddress);
            Console.WriteLine("Producer Port Number: " + producerPortNumber);

            Console.WriteLine();
            Console.WriteLine();
        }

        public static void ConnectToProducer()
        {
            try
            {
                // Connect to the producer
                producerClient = new TcpClient();
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10)); // Set timeout to 10 seconds
                Console.WriteLine("Connecting to producer...");

                producerClient.ConnectAsync(producerIPAddress, (int)producerPortNumber).WaitAsync(cts.Token);
                producerStream = producerClient.GetStream();
                Console.WriteLine("Connected to producer");

                // Receive the number of producer threads
                byte[] numProducerThreadsBytes = new byte[4];
                _ = producerStream.ReadAsync(numProducerThreadsBytes, 0, numProducerThreadsBytes.Length);
                numProducerThreads = BitConverter.ToUInt32(numProducerThreadsBytes, 0);
                Console.WriteLine($"Received number of producer threads ({numProducerThreads})");

                // Initialize the consumer threads

                MessageBox.Show("Connected to producer", "Connection", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Error: Connection timed out");
                MessageBox.Show("Error: Connection timed out", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
                MessageBox.Show("Error: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public static void DownloadVideo()
        {
            if (producerStream == null)
            {
                Console.WriteLine("Please connect to the producer first");
                MessageBox.Show("Please connect to the producer first", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                Console.WriteLine("Downloading video...");

                // Ensure the video folder exists
                if (!Directory.Exists(videoFolder))
                {
                    Directory.CreateDirectory(videoFolder);
                }

                // Read the file name length
                byte[] fileNameLengthBytes = new byte[4];
                _ = producerStream.ReadAsync(fileNameLengthBytes, 0, fileNameLengthBytes.Length);
                int fileNameLength = BitConverter.ToInt32(fileNameLengthBytes, 0);

                // Read the file name
                byte[] fileNameBytes = new byte[fileNameLength];
                _ = producerStream.ReadAsync(fileNameBytes, 0, fileNameBytes.Length);
                string fileName = Encoding.UTF8.GetString(fileNameBytes);

                // Check if the file already exists
                if (File.Exists(System.IO.Path.Combine(videoFolder, fileName)))
                {
                    // If the file exists, add a number to the file name
                    int i = 1;
                    string fileNameWithoutExtension = System.IO.Path.GetFileNameWithoutExtension(fileName);
                    string fileExtension = System.IO.Path.GetExtension(fileName);
                    while (File.Exists(System.IO.Path.Combine(videoFolder, fileName)))
                    {
                        fileName = fileNameWithoutExtension + "_" + i + fileExtension;
                        i++;
                    }

                    Console.WriteLine("File already exists. Renaming to: " + fileName);
                }

                // Create the file path
                string filePath = System.IO.Path.Combine(videoFolder, fileName);

                // Receive the file data
                using FileStream fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
                producerStream.CopyToAsync(fileStream);

                Console.WriteLine("Download complete");
                MessageBox.Show("Download complete", "Download", MessageBoxButton.OK, MessageBoxImage.Information);

                // Add the video to the list
                STDISCM_PS_3___Networked_Producer_and_Consumer.MainWindow.AddVideoToList(fileName);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
                MessageBox.Show("Error: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
