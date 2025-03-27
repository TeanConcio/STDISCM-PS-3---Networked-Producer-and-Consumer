using System.Net.Sockets;
using System.Net;

namespace STDISCM_PS_3___Networked_Producer_and_Consumer
{
    class Producer
    {
        static void Main()
        {
            int port = 9000;
            string videoPath = "./video_folder_1/test1.mp4"; // Make sure this file exists

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