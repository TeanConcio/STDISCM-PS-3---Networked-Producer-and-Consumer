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
        public uint id;
        private string videoFolderPath;
        public ushort portNumber;

        // Video names
        private string[] videoNames;
        private uint currentVideoIndex = 0;

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
            try
            {
                for (; currentVideoIndex < videoNames.Length; currentVideoIndex++)
                {
                    string fullPath = videoNames[currentVideoIndex];
                    string videoName = Path.GetFileName(fullPath);

                    VideoRequest videoRequest = new VideoRequest(portNumber, videoName);
                    VideoRequest result = Program.SendVideoRequest(videoRequest);

                    if (result != null)
                    {
                        // Wait for the consumer to connect to this port
                        TcpListener listener = new TcpListener(IPAddress.Any, portNumber);
                        listener.Start();
                        Console.WriteLine($"Producer Thread {id} is waiting for consumer connection on port {portNumber}...");

                        consumerThreadClient = listener.AcceptTcpClient();
                        consumerThreadStream = consumerThreadClient.GetStream();

                        Console.WriteLine($"Producer Thread {id} connected to consumer. Sending video...");

                        // Send video data
                        SendVideo(fullPath);

                        // Close connection after sending
                        consumerThreadStream.Close();
                        consumerThreadClient.Close();
                        listener.Stop();

                        Console.WriteLine($"Producer Thread {id} finished sending {videoName}.");
                    }
                    else
                    {
                        Console.WriteLine($"Producer Thread {id} dropped video {videoName} due to queue full.");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in Producer Thread {id}: " + ex.Message);
            }
        }

        private void SendVideo(string fullPath)
        {
            try
            {
                using FileStream fileStream = File.OpenRead(fullPath);
                fileStream.CopyTo(consumerThreadStream);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending video: {ex.Message}");
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
