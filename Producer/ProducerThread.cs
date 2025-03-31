using System;
using System.Collections.Generic;
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

            thread = new Thread(Run);
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
                        VideoRequest videoRequest = new VideoRequest(portNumber, videoName);
                        var result = Program.SendVideoRequest(id, videoRequest);

                        if (result != null)
                        {
                            state = State.WAITING_FOR_CONSUMER;
                        }
                        else
                        {
                            Thread.Sleep(1000);
                        }

                        break;

                    case State.WAITING_FOR_CONSUMER:
                        try
                        {
                            // Wait for the consumer to connect to this port
                            listener = new TcpListener(IPAddress.Any, portNumber);
                            listener.Start();
                            Console.WriteLine($"Producer Thread {id} is waiting for consumer connection on port {portNumber}...");

                            consumerThreadClient = listener.AcceptTcpClient();
                            consumerThreadStream = consumerThreadClient.GetStream();

                            Console.WriteLine($"Producer Thread {id} connected to consumer.");

                            state = State.CURRENTLY_CONNECTED;
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error in Producer Thread {id}: " + ex.Message);
                        }
                        break;

                    case State.CURRENTLY_CONNECTED:
                        //try
                        {
                            using FileStream fileStream = File.OpenRead(fullPath);
                            fileStream.CopyTo(consumerThreadStream);
                            Console.WriteLine($"Producer Thread {id} finished sending {videoName}.");

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
                        //catch (Exception ex)
                        //{
                        //    Console.WriteLine($"Error sending video: {ex.Message}");
                        //}
                        break;
                }
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
