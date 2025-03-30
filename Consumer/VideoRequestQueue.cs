using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Consumer
{
    class VideoRequestQueue
    {
        private uint maxQueueSize => Program.maxQueueSize;
        private readonly ConcurrentQueue<VideoRequest> queue;

        public VideoRequestQueue()
        {
            queue = new ConcurrentQueue<VideoRequest>();
        }

        public VideoRequest Enqueue(VideoRequest videoRequest)
        {
            if (queue.Count < maxQueueSize)
            {
                queue.Enqueue(videoRequest);
                Console.WriteLine($"Added video request from {videoRequest.producerPort} for {videoRequest.videoName} to queue");
                return videoRequest;
            }
            else
            {
                Console.WriteLine($"Queue is full. Could not add video request from {videoRequest.producerPort} for {videoRequest.videoName}");
                return null;
            }
        }

        public VideoRequest Dequeue()
        {
            if (queue.TryDequeue(out VideoRequest videoRequest))
            {
                return videoRequest;
            }
            else
            {
                return null;
            }
        }
    }
}
