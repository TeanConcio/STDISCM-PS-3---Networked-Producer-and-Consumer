using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Consumer
{
    class VideoRequest
    {
        public ushort producerPort;
        public string videoName;

        public VideoRequest(ushort producerPort, string videoName)
        {
            this.producerPort = producerPort;
            this.videoName = videoName;
        }

        // Function to decode VideoRequest from stream
        public static VideoRequest Decode(NetworkStream stream)
        {
            // Get producer thread port number
            byte[] portNumberBytes = new byte[sizeof(ushort)];
            _ = stream.Read(portNumberBytes, 0, portNumberBytes.Length);
            ushort producerThreadPortNumber = BitConverter.ToUInt16(portNumberBytes, 0);

            // Get video name length
            byte[] videoNameLengthBytes = new byte[sizeof(int)];
            _ = stream.Read(videoNameLengthBytes, 0, videoNameLengthBytes.Length);
            int videoNameLength = BitConverter.ToInt32(videoNameLengthBytes, 0);

            // Get video name
            byte[] videoNameBytes = new byte[videoNameLength];
            _ = stream.Read(videoNameBytes, 0, videoNameBytes.Length);
            string videoName = Encoding.UTF8.GetString(videoNameBytes);

            return new VideoRequest(producerThreadPortNumber, videoName);
        }

        // Function to encode VideoRequest to stream
        public static byte[] Encode(VideoRequest videoRequest)
        {
            List<byte> bytes = new List<byte>();

            // Add producer thread port number
            bytes.AddRange(BitConverter.GetBytes(videoRequest.producerPort));

            // Add video name length
            bytes.AddRange(BitConverter.GetBytes(videoRequest.videoName.Length));

            // Add video name
            bytes.AddRange(Encoding.UTF8.GetBytes(videoRequest.videoName));

            return bytes.ToArray();
        }
    }
}
