using System;
using System.Collections.Generic;
using System.Linq;
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

        }

        private void SendVideo()
        {
            try
            {
                string videoName = videoNames[currentVideoIndex];

                Console.WriteLine($"Producer Thread {id} is sending video {videoName}");

                using FileStream fileStream = File.OpenRead(videoFolderPath + "/" + videoName);

                // Send the file data
                fileStream.CopyTo(consumerThreadStream);
                Console.WriteLine("Video sent to consumer.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
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
