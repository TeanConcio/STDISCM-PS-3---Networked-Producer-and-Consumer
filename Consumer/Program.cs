﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Windows;
using System.Collections.Concurrent;

namespace Consumer
{
    class Program
    {
        // Default values for configurations
        private const uint DEFAULT_NUMBER_OF_CONSUMER_THREADS = 4;
        private const uint DEFAULT_MAX_QUEUE_SIZE = 4;
        private static readonly IPAddress DEFAULT_PRODUCER_IP_ADDRESS = IPAddress.Parse("127.0.0.1");
        private const ushort DEFAULT_PRODUCER_PORT_NUMBER = 9000;

        // Configurations
        public static uint numConsumerThreads = DEFAULT_NUMBER_OF_CONSUMER_THREADS;
        public static uint maxQueueSize = DEFAULT_MAX_QUEUE_SIZE;
        public static IPAddress producerIPAddress = DEFAULT_PRODUCER_IP_ADDRESS;
        public static ushort producerPortNumber = DEFAULT_PRODUCER_PORT_NUMBER;

        public const string videoFolder = "./downloaded_videos";

        // Producer variables
        private static TcpClient producerClient;
        private static NetworkStream producerStream;

        // Consumer Threads
        private static ConsumerThread[] consumerThreads;

        // Video Request Queue
        public static VideoRequestQueue videoRequestQueue;

        // Variables
        public static bool hasVideosToSend = false;

        public static HashSet<string> existingVideoHashes = new HashSet<string>();

        public static void GetConfig()
        {
            Console.WriteLine("[Main Thread] Getting Configurations from config.txt");
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
                    case "NUMBER_OF_CONSUMER_THREADS (C)":
                        if (!uint.TryParse(parts[1].Trim(), out uint tempNumConsumerThreads) || tempNumConsumerThreads > int.MaxValue || tempNumConsumerThreads < 1)
                        {
                            Console.WriteLine($"[Main Thread] Error: Invalid Number of Instances. Setting Number of Instances to {DEFAULT_NUMBER_OF_CONSUMER_THREADS}.");
                            hasErrorWarning = true;
                        }
                        else
                        {
                            // Check if very large number of instances
                            if (tempNumConsumerThreads >= 10)
                            {
                                Console.WriteLine($"[Main Thread] Warning: Very large number of threads. Please expect a very long initialization time.");
                                hasErrorWarning = true;
                            }

                            numConsumerThreads = tempNumConsumerThreads;
                        }
                        break;

                    case "MAX_QUEUE_SIZE (Q)":
                        if (!uint.TryParse(parts[1].Trim(), out uint tempMaxQueueSize) || tempMaxQueueSize > int.MaxValue || tempMaxQueueSize < 1)
                        {
                            Console.WriteLine($"[Main Thread] Error: Invalid Max Queue Size. Setting Max Queue Size to {DEFAULT_MAX_QUEUE_SIZE}.");
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
                            Console.WriteLine($"[Main Thread] Error: Invalid Producer IP Address. Setting Producer IP Address to {DEFAULT_PRODUCER_IP_ADDRESS}.");
                            hasErrorWarning = true;
                        }
                        else
                        {
                            producerIPAddress = tempProducerIPAddress;
                        }
                        break;

                    case "PRODUCER_PORT_NUMBER":
                        if (!ushort.TryParse(parts[1].Trim(), out ushort tempConsumerPortNumber) || tempConsumerPortNumber > ushort.MaxValue || tempConsumerPortNumber < 1)
                        {
                            Console.WriteLine($"[Main Thread] Error: Invalid Producer Port Number. Setting Producer Port Number to {DEFAULT_PRODUCER_PORT_NUMBER}.");
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
            Console.WriteLine("[Main Thread] Number of Consumer Threads: " + numConsumerThreads);
            Console.WriteLine("[Main Thread] Max Queue Size: " + maxQueueSize);
            Console.WriteLine("[Main Thread] Producer IP Address: " + producerIPAddress);
            Console.WriteLine("[Main Thread] Producer Port Number: " + producerPortNumber);

            Console.WriteLine();
            Console.WriteLine();
        }

        public static void Initialize()
        {
            consumerThreads = new ConsumerThread[numConsumerThreads];

            for (ushort i = 0; i < numConsumerThreads; i++)
            {
                consumerThreads[i] = new ConsumerThread(i);
            }

            // Initialize video request queue
            videoRequestQueue = new VideoRequestQueue();

            // Create video folder if it does not exist
            if (!Directory.Exists(videoFolder))
            {
                Directory.CreateDirectory(videoFolder);
            }

            // Load existing video hashes from the video folder
            foreach (string file in Directory.GetFiles(videoFolder))
            {
                string hash = VideoRequest.ComputeHash(file);
                existingVideoHashes.Add(hash);
            }
        }

        public static bool ConnectToProducer()
        {
            if (producerStream != null)
            {
                Console.WriteLine("[Main Thread] Already connected to producer");
                MessageBox.Show("Already connected to producer", "Connection", MessageBoxButton.OK, MessageBoxImage.Information);
                return false;
            }

            try
            {
                // Connect to the producer
                producerClient = new TcpClient();
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10)); // Set timeout to 10 seconds
                Console.WriteLine("[Main Thread] Connecting to producer...");
                var connectTask = producerClient.ConnectAsync(producerIPAddress, (int)producerPortNumber);
                if (!connectTask.Wait(TimeSpan.FromSeconds(10)))
                {
                    throw new TimeoutException("Connection to producer timed out.");
                }
                producerStream = producerClient.GetStream();
                Console.WriteLine("[Main Thread] Connected to producer");

                MessageBox.Show("Connected to producer", "Connection", MessageBoxButton.OK, MessageBoxImage.Information);

                return true;
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("[Main Thread] Error: Connection timed out");
                MessageBox.Show("Error: Connection timed out", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine("[Main Thread] Error: " + ex.Message);
                MessageBox.Show("Error: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        public static bool StartDownloadingVideos()
        {
            if (producerStream == null)
            {
                Console.WriteLine("[Main Thread] Please connect to the producer first");
                MessageBox.Show("Please connect to the producer first", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            try
            {
                Console.WriteLine("[Main Thread] Starting video downloads...");

                // Start consumer threads
                hasVideosToSend = true;
                foreach (ConsumerThread consumerThread in consumerThreads)
                {
                    consumerThread.Start();
                }

                // Send single byte to producer to start sending videos
                producerStream.WriteByte(1);

                // Start receiving video requests
                ReceiveVideoRequests();

                // Disconnect from producer
                producerStream.Close();
                producerClient.Close();

                // Reset producer variables
                producerStream = null;
                producerClient = null;

                // Join consumer threads
                foreach (ConsumerThread consumerThread in consumerThreads)
                {
                    consumerThread.Join();
                }

                Console.WriteLine("[Main Thread] Download complete");
                MessageBox.Show("Download complete", "Download", MessageBoxButton.OK, MessageBoxImage.Information);

                // Reload video list
                //MainWindow.ReloadVideoList();

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("[Main Thread] Error downloading videos: " + ex.Message);
                MessageBox.Show("Error downloading videos: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        public static void ReceiveVideoRequests()
        {
            hasVideosToSend = true;

            while (hasVideosToSend)
            {
                try
                {
                    // First byte says if there are still videos to download
                    // 0 - No more videos to download
                    // 1 - There are still videos to download
                    if (producerStream.ReadByte() == 0)
                    {
                        hasVideosToSend = false;
                        break;
                    }

                    // Receive video request
                    VideoRequest videoRequest = VideoRequest.Decode(producerStream);
                    Console.WriteLine($"[Main Thread] Received request: video = {videoRequest.videoName}, port = {videoRequest.producerPort}");


                    if (existingVideoHashes.Contains(videoRequest.hash))
                    {
                        Console.WriteLine($"[Main Thread] Duplicate video detected (hash: {videoRequest.hash}). Rejecting {videoRequest.videoName}.");
                        producerStream.WriteByte(2); // new response code for "duplicate"
                        continue; // skip enqueue
                    }

                    // Add video request to video request queue
                    var added = videoRequestQueue.Enqueue(videoRequest);

                    // If successfully added, reply to producer with response byte
                    // 0 - Not added
                    // 1 - Added
                    if (added != null)
                    {
                        existingVideoHashes.Add(videoRequest.hash);
                        producerStream.WriteByte(1);
                    }
                    else
                    {
                        producerStream.WriteByte(0);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("[Main Thread] Error receiving video request: " + ex.Message);
                    MessageBox.Show("Error: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}
