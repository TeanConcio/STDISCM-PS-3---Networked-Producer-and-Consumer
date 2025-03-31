using System.Net.Sockets;
using System.Net;
using System.Text;

namespace Producer
{
    class Program
    {
        // Default values for configurations
        private const uint DEFAULT_NUMBER_OF_PRODUCER_THREADS = 4;
        private const ushort DEFAULT_PRODUCER_PORT_NUMBER = 9000;

        // Configurations
        private static uint numProducerThreads = DEFAULT_NUMBER_OF_PRODUCER_THREADS;
        private static ushort producerPortNumber = DEFAULT_PRODUCER_PORT_NUMBER;

        private const string mainVideoFolderPath = "./video_folders";
        private const string videoFolderSuffix = "/video_folder_";

        // Consumer variables
        private static IPAddress consumerIPAddress;
        private static ushort consumerPortNumber;
        private static TcpClient consumerClient;
        private static NetworkStream consumerStream;

        // Producer Threads
        private static ProducerThread[] producerThreads;

        private static readonly object streamLock = new object();

        // Thread to check if all producer threads are finished
        private static Thread producerThreadStatusChecker = new Thread(() =>
        {
            while (true)
            {
                bool allFinished = true;
                foreach (ProducerThread producerThread in producerThreads)
                {
                    if (producerThread.state != ProducerThread.State.FINISHED)
                    {
                        allFinished = false;
                        break;
                    }
                }
                if (allFinished)
                {
                    break;
                }
            }
            // Send signal to consumer that all videos are sent
            lock (streamLock)
            {
                // First byte says if there are still videos to download
                // 0 - No more videos to download
                // 1 - There are still videos to download
                consumerStream.WriteByte(0);

                // Close the stream and connection
                consumerStream.Close();
                consumerClient.Close();
            }
        });

        private static void Main()
        {
            GetConfig();

            Initialize();

            ConnectToConsumer();

            StartSendingVideoRequests();

            Console.WriteLine("Press any key to exit.");
            Console.ReadKey();
        }

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
                    case "NUMBER_OF_PRODUCER_THREADS (P)":
                        if (!uint.TryParse(parts[1].Trim(), out uint tempNumProducerThreads) || tempNumProducerThreads > int.MaxValue || tempNumProducerThreads < 1)
                        {
                            Console.WriteLine($"Error: Invalid Number of Instances. Setting Number of Instances to {DEFAULT_NUMBER_OF_PRODUCER_THREADS}.");
                            hasErrorWarning = true;
                        }
                        else
                        {
                            // Check if very large number of instances
                            if (tempNumProducerThreads >= 50)
                            {
                                Console.WriteLine($"Warning: Very large number of threads. Please expect a very long initialization time.");
                                hasErrorWarning = true;
                            }

                            numProducerThreads = tempNumProducerThreads;
                        }
                        break;

                    case "PRODUCER_PORT_NUMBER":
                        if (!ushort.TryParse(parts[1].Trim(), out ushort tempPortNumber) || tempPortNumber > ushort.MaxValue || tempPortNumber < 1)
                        {
                            Console.WriteLine($"Error: Invalid Program Port Number. Setting Producer Port Number to {DEFAULT_PRODUCER_PORT_NUMBER}.");
                            hasErrorWarning = true;
                        }
                        else
                        {
                            producerPortNumber = tempPortNumber;
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
            Console.WriteLine("Number of Program Threads: " + numProducerThreads);
            Console.WriteLine("Program Port Number: " + producerPortNumber);

            Console.WriteLine();
            Console.WriteLine();
        }

        public static void Initialize()
        {
            producerThreads = new ProducerThread[numProducerThreads];

            for (ushort i = 0; i < numProducerThreads; i++)
            {
                string videoFolderPath = mainVideoFolderPath + videoFolderSuffix + i;
                producerThreads[i] = new ProducerThread(i, videoFolderPath, (ushort)(producerPortNumber + i + 1));
            }
        }    

        private static void ConnectToConsumer()
        {
            try
            {
                // Listen for consumer
                TcpListener listener = new TcpListener(IPAddress.Any, (int)producerPortNumber);
                listener.Start();
                Console.WriteLine("Program is waiting for connection...");

                // Accept consumer connection
                consumerClient = listener.AcceptTcpClient();
                if (consumerClient == null)
                {
                    Console.WriteLine("Error: Consumer not connected.");
                    return;
                }
                consumerStream = consumerClient.GetStream();
                consumerIPAddress = ((IPEndPoint)consumerClient.Client.RemoteEndPoint).Address;
                consumerPortNumber = (ushort)((IPEndPoint)consumerClient.Client.RemoteEndPoint).Port;
                Console.WriteLine($"Consumer connected from {consumerIPAddress}:{consumerPortNumber}");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }
        }

        private static void StartSendingVideoRequests()
        {
            try
            {
                // Receive single byte from consumer to signal that it is ready
                byte[] readySignal = new byte[1];
                _ = consumerStream.Read(readySignal, 0, readySignal.Length);

                // Start producer threads
                foreach (ProducerThread producerThread in producerThreads)
                {
                    producerThread.Start();
                }

                // Start producer thread status checker
                producerThreadStatusChecker.Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }
        }

        public static VideoRequest SendVideoRequest(uint threadID, VideoRequest videoRequest)
        {
            lock (streamLock)
            {
                Console.WriteLine($"Producer Thread {threadID} is sending request: video = {videoRequest.videoName}, port = {videoRequest.producerPort}");
                // First byte says if there are still videos to download
                // 0 - No more videos to download
                // 1 - There are still videos to download
                consumerStream.WriteByte(1);

                // Send video request
                byte[] videoRequestBytes = VideoRequest.Encode(videoRequest);
                consumerStream.Write(videoRequestBytes, 0, videoRequestBytes.Length);

                // Get response byte from consumer
                // 0 - Not added
                // 1 - Added
                byte[] response = new byte[1];
                _ = consumerStream.Read(response, 0, response.Length);

                if (response[0] == 1)
                {
                    Console.WriteLine($"Video request for {videoRequest.videoName} sent to consumer.");
                    return videoRequest;
                }
                else
                {
                    Console.WriteLine($"Error: Video request for {videoRequest.videoName} not sent to consumer.");
                    return null;
                }
            }
        }
    }
}