using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

using PharaohPhilesServer.Server;

namespace PharaohPhilesServer.PhilesProtocol
{
    class PhilesJob
    {
        public int RemoteJobNumber { get; set; }
        public int JobNumber { get; set; }

        public PhilesJob()
        {
            JobNumber = -1;
            RemoteJobNumber = -1;
        }

        public virtual void ProcessMessage(byte[] data, AClient client)
        {
            Core.Output("Completed job " + JobNumber + ".", System.Drawing.Color.Green);
            CompleteJob();
        }

        public virtual void CompleteJob()
        {
            if (OnJobComplete != null)
                OnJobComplete(JobNumber);
        }

        public delegate void JobCompleteDelegate(int jNumber);
        public event JobCompleteDelegate OnJobComplete;

    }
}
