using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using PharaohPhilesServer.PhilesProtocol;
using PharaohPhilesServer.Server;

namespace PharaohPhilesServer.PhilesProtocol.PhilesJobs
{
    class ClientPhilesJob : PhilesJob
    {
        public ClientPhilesJob()
            : base()
        {
        }

        public override void ProcessMessage(byte[] data, AClient client)
        {
            // Signal server to end its job.
            client.Send(new PPMessage(true, this.JobNumber, this.RemoteJobNumber).GetEncodedData());
            Core.Output("Client Philes Job " + JobNumber + " complete.");
            CompleteJob();
        }

        public override void CompleteJob()
        {
            base.CompleteJob();
        }
    }
}
