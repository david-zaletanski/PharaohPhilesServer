using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using PharaohPhilesServer.Server;
using PharaohPhilesServer.PhilesProtocol;

namespace PharaohPhilesServer.PhilesProtocol.PhilesJobs
{
    class ClientDirectoryRequestJob : PhilesJob
    {
        public string RemoteDirectory { get; set; }

        private ClientDirectoryRequestState state;

        public ClientDirectoryRequestJob(object[] param)
            : base()
        {
            if (param != null && param.Length >= 1)
                RemoteDirectory = (string)param[0];
            else
                RemoteDirectory = "";
            state = ClientDirectoryRequestState.CDR_SEND_DIRECTORY;
        }

        private int TotalBytes = 0;
        private int BytesRead = 0;
        private StringBuilder ResponseBuilder = null;
        public override void ProcessMessage(byte[] data, AClient client)
        {
            try
            {
                if (state == ClientDirectoryRequestState.CDR_SEND_DIRECTORY)
                {
                    byte[] bDirectory = ASCIIEncoding.ASCII.GetBytes(RemoteDirectory);
                    client.Send(new PPMessage(true, this.JobNumber, this.RemoteJobNumber, bDirectory).GetEncodedData());
                    //client.Send(EncapsulateDataWithSize(bDirectory));
                    state = ClientDirectoryRequestState.CDR_GET_RESULTS;
                }
                else if (state == ClientDirectoryRequestState.CDR_GET_RESULTS)
                {
                    string response = ASCIIEncoding.ASCII.GetString(data, 0, data.Length);
                    Core.Output("SERVER DIRECTORY LISTING\n------------------------\n" + response);
                    CompleteJob();
                }
            }
            catch (Exception ex)
            {
                Core.HandleEx("ClientDirectoryRequestJob:ProcessMessage", ex);
            }
        }

        public override void CompleteJob()
        {
            Core.Output("Client Directory Request Job " + JobNumber + " complete.");
            base.CompleteJob();
        }

        enum ClientDirectoryRequestState
        {
            CDR_SEND_DIRECTORY,
            CDR_GET_RESULTS
        }
    }
}
