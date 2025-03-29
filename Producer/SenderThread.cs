using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Producer
{
    class SenderThread
    {
        private uint id;
        private string videoPath;
        private string portNumber;

        private Thread thread;

        public SenderThread(uint id, string videoPath, string portNumber)
        {
            this.id = id;
            this.videoPath = videoPath;
            this.portNumber = portNumber;
        }
    }
}
