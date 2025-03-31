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
        private static uint maxQueueSize => Program.maxQueueSize;
        private uint currentQueueSize = VideoRequestQueue.maxQueueSize;

        private readonly ConcurrentQueue<VideoRequest> queue;

        public uint Count => (uint)queue.Count;

        private object queueLock = new object();

        public VideoRequestQueue()
        {
            queue = new ConcurrentQueue<VideoRequest>();
        }

        public VideoRequest Enqueue(VideoRequest videoRequest)
        {
            lock (queueLock)
            {
                if (queue.Count < currentQueueSize)
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
        }

        public VideoRequest Dequeue()
        {
            lock (queueLock)
            {
                if (queue.TryDequeue(out VideoRequest videoRequest))
                {
                    //// Remove the slot from the queue
                    //if (currentQueueSize > 0)
                    //    currentQueueSize--;

                    return videoRequest;
                }
                else
                {
                    return null;
                }
            }
        }

        //public void IncrementSlotBack()
        //{
        //    lock (queueLock)
        //    {
        //        if (currentQueueSize < maxQueueSize)
        //            currentQueueSize++;
        //    }
        //}
    }
}
