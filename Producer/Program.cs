using System.Net.Sockets;
using System.Net;

namespace STDISCM_PS_3___Networked_Producer_and_Consumer
{
    class Producer
    {
        // Default values for configurations
        private const uint DEFAULT_NUMBER_OF_PRODUCER_THREADS = 4;
        private const uint DEFAULT_PRODUCER_PORT_NUMBER = 9000;

        // Configurations
        private static uint numProducerThreads = DEFAULT_NUMBER_OF_PRODUCER_THREADS;
        private static uint portNumber = DEFAULT_PRODUCER_PORT_NUMBER;

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
                    case "NUMBER_OF_PRODUCER_THREADS (C) = ":
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

                    case "PRODUCER_PORT_NUMBER = ":
                        if (!ushort.TryParse(parts[1].Trim(), out ushort tempPortNumber) || tempPortNumber > ushort.MaxValue || tempPortNumber < 1)
                        {
                            Console.WriteLine($"Error: Invalid Producer Port Number. Setting Procuder Port Number to {DEFAULT_PRODUCER_PORT_NUMBER}.");
                            hasErrorWarning = true;
                        }
                        else
                        {
                            portNumber = tempPortNumber;
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
            Console.WriteLine("Number of Producer Threads: " + numProducerThreads);
            Console.WriteLine("Producer Port Number: " + portNumber);

            Console.WriteLine();
            Console.WriteLine();
        }

        static void Main()
        {
            int port = 9000;
            string videoPath = "./video_folders/video_folder_1/test1.mp4"; // Make sure this file exists

            try
            {
                TcpListener listener = new TcpListener(IPAddress.Any, port);
                listener.Start();
                Console.WriteLine("Producer is waiting for connection...");

                using TcpClient client = listener.AcceptTcpClient();

                Console.WriteLine("Consumer connected. Sending video...");

                using NetworkStream stream = client.GetStream();
                using FileStream fileStream = File.OpenRead(videoPath);

                fileStream.CopyTo(stream);
                Console.WriteLine("Video sent to consumer.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }

            Console.WriteLine("Press any key to exit.");
            Console.ReadKey();
        }
    }
}