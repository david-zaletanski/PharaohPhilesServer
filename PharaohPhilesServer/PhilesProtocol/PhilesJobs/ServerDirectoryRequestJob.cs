using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

using PharaohPhilesServer.Server;

namespace PharaohPhilesServer.PhilesProtocol
{
    class ServerDirectoryRequestJob : PhilesJob
    {
        private ServerDirectoryRequestState State { get; set; }

        public ServerDirectoryRequestJob() : base()
        {
            State = ServerDirectoryRequestState.SDR_GET_FOLDER_NAME;
        }

        public override void ProcessMessage(byte[] data, AClient client)
        {
            try
            {

                if (State == ServerDirectoryRequestState.SDR_GET_FOLDER_NAME)
                {
                    // Determine folder to give information on.
                    string folder = ASCIIEncoding.ASCII.GetString(data);
                    Core.Output("Server listing directory info for folder: '" + folder + "'");

                    // Compile a list of subfolders and files, package with string length
                    // So the client can watch out for multiple packets.
                    string response = getDirectoryInformation(folder);
                    byte[] responseBytes = ASCIIEncoding.ASCII.GetBytes(response);
                    client.Send(new PPMessage(true, this.JobNumber, this.RemoteJobNumber, responseBytes).GetEncodedData());
                    CompleteJob();
                }
            }
            catch (Exception ex)
            {
                Core.HandleEx("ServerDirectoryRequestJob", ex);
            }
        }

        public override void CompleteJob()
        {
            Core.Output("ServerDirectoryRequestJob " + this.JobNumber + " complete.");
            base.CompleteJob();
        }

        private string getDirectoryInformation(string dir)
        {
            StringBuilder sb = new StringBuilder();
            foreach (string s in Directory.GetDirectories(dir))
                sb.Append(s + "\n");
            foreach (string s in Directory.GetFiles(dir))
                sb.Append(s + "\n");
            return sb.ToString().Trim();
        }

        enum ServerDirectoryRequestState
        {
            SDR_GET_FOLDER_NAME
        }
    }
}
