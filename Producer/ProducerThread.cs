using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Producer
{
    class ProducerThread
    {
        public enum State
        {
            WAITING_FOR_RETRY,
            WAITING_FOR_CONSUMER,
            CURRENTLY_CONNECTED,
            FINISHED
        }

        public uint id;
        private string videoFolderPath;
        public ushort portNumber;
        public State state = State.WAITING_FOR_RETRY;

        // Video names
        private string[] videoNames;
        private uint currentVideoIndex = 0;

        // Consumer connection
        private TcpListener listener;
        private TcpClient consumerThreadClient;
        private NetworkStream consumerThreadStream;

        private Thread thread;

        public ProducerThread(uint id, string videoPath, ushort portNumber)
        {
            this.id = id;
            this.videoFolderPath = videoPath;
            this.portNumber = portNumber;

            // Get video names
            videoNames = Directory.GetFiles(videoFolderPath, "*.mp4");
        }

        public void Run()
        {
            while (state != State.FINISHED)
            {
                // Get current video name
                string fullPath = videoNames[currentVideoIndex];
                string videoName = Path.GetFileName(fullPath);

                switch (state)
                {
                    case State.WAITING_FOR_RETRY:
                        // Send video request
                        string hash = VideoRequest.ComputeHash(fullPath);
                        VideoRequest videoRequest = new VideoRequest(portNumber, videoName, hash);
                        var result = Program.SendVideoRequest(id, videoRequest);

                        switch (result)
                        {
                            case RequestResult.Accepted:
                                state = State.WAITING_FOR_CONSUMER;
                                break;

                            case RequestResult.QueueFull:
                                Console.WriteLine($"[Producer Thread {id}] Queue full. Retrying {videoName}...");
                                Thread.Sleep(1000);
                                break;

                            case RequestResult.Duplicate:
                                Console.WriteLine($"[Producer Thread {id}] Duplicate video. Skipping {videoName}.");
                                currentVideoIndex++;
                                state = (currentVideoIndex >= videoNames.Length) ? State.FINISHED : State.WAITING_FOR_RETRY;
                                break;
                        }
                        break;

                    case State.WAITING_FOR_CONSUMER:
                        try
                        {
                            // Wait for the consumer to connect to this port
                            listener = new TcpListener(IPAddress.Any, portNumber);
                            listener.Start();
                            Console.WriteLine($"[Producer Thread {id}] Waiting for consumer connection on port {portNumber}...");

                            consumerThreadClient = listener.AcceptTcpClient();
                            consumerThreadStream = consumerThreadClient.GetStream();

                            Console.WriteLine($"[Producer Thread {id}] Connected to consumer.");

                            state = State.CURRENTLY_CONNECTED;
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[Producer Thread {id}] Error connecting to consumer: " + ex.Message);
                        }
                        break;

                    case State.CURRENTLY_CONNECTED:
                        try
                        {
                            // Compress the file and capture the compressed size
                            using FileStream fileStream = File.OpenRead(fullPath);
                            using MemoryStream memoryStream = new MemoryStream();
                            using GZipStream gzipStream = new GZipStream(memoryStream, CompressionMode.Compress, true);

                            fileStream.CopyTo(gzipStream);
                            gzipStream.Flush();

                            // Get the compressed data and log the sizes
                            long originalFileSize = new FileInfo(fullPath).Length;
                            byte[] compressedData = memoryStream.ToArray();
                            long compressedFileSize = compressedData.Length;
                            Console.WriteLine($"[Producer Thread {id}] Compressed {videoName} (file size: {originalFileSize} bytes; Compressed size: {compressedFileSize} bytes)");

                            // Send the compressed data to the consumer
                            consumerThreadStream.Write(compressedData, 0, (int)compressedFileSize);
                            consumerThreadStream.Flush();


                            // Send the compressed data to the consumer
                            Console.WriteLine($"[Producer Thread {id}] Finished sending {videoName}");

                            // Close connection after sending
                            consumerThreadStream.Close();
                            consumerThreadClient.Close();
                            listener.Stop();

                            // Move to the next video
                            currentVideoIndex++;

                            // If there are no more videos, finish
                            if (currentVideoIndex >= videoNames.Length)
                            {
                                state = State.FINISHED;
                            }
                            else
                            {
                                state = State.WAITING_FOR_RETRY;
                            }

                            break;
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[Producer Thread {id}] Error sending video: {ex.Message}");
                        }
                        break;
                }
            }
        }

        public void Start()
        {
            currentVideoIndex = 0;
            state = State.WAITING_FOR_RETRY;

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
