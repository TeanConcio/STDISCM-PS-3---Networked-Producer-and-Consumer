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
        public string hash;

        public VideoRequest(ushort producerPort, string videoName, string hash)
        {
            this.producerPort = producerPort;
            this.videoName = videoName;
            this.hash = hash;
        }

        public static string ComputeHash(string path)
        {
            using var sha = System.Security.Cryptography.SHA256.Create();
            using var stream = System.IO.File.OpenRead(path);
            byte[] hash = sha.ComputeHash(stream);
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
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

            // Get hash
            byte[] hashLenBytes = new byte[4];
            stream.Read(hashLenBytes, 0, 4);
            int hashLen = BitConverter.ToInt32(hashLenBytes, 0);
            byte[] hashBytes = new byte[hashLen];
            stream.Read(hashBytes, 0, hashLen);
            string hash = Encoding.UTF8.GetString(hashBytes);

            return new VideoRequest(producerThreadPortNumber, videoName, hash);
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

            // Add hash length (4 bytes)
            byte[] hashBytes = Encoding.UTF8.GetBytes(videoRequest.hash);
            bytes.AddRange(BitConverter.GetBytes(hashBytes.Length));

            // Add hash string
            bytes.AddRange(hashBytes);

            return bytes.ToArray();
        }
    }
}
