using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

using PharaohPhilesServer.Server;

namespace PharaohPhilesServer.PhilesProtocol
{
    class ServerFileUploadJob : PhilesJob
    {
        long filesize;
        string filename;

        public ServerFileUploadJob() : base()
        {
            filesize = 0;
            filename = "";
        }

        long bytesread = 0;
        FileStream fs = null;
        public override void ProcessMessage(byte[] data, AClient client)
        {
            try
            {
                if (filesize == 0)
                {
                    filesize = BitConverter.ToInt64(data,0);
                }
                else if (filename == "")
                {
                    filename = ASCIIEncoding.ASCII.GetString(data);
                    fs = new FileStream(Settings.GetDefaultUploadLocation() + filename, FileMode.CreateNew);
                }
                else
                {
                    fs.Write(data, 0, data.Length);
                    bytesread += data.Length;

                    if (bytesread == filesize)
                    {
                        fs.Close();
                        CompleteJob();
                    }
                }
            }
            catch (Exception ex)
            {
                Core.HandleEx("ServerFileUploadJob", ex);
            }
        }

        public override void CompleteJob()
        {
            Core.Output("ServerFileUploadJob " + this.JobNumber + " complete.");
            base.CompleteJob();
        }
    }
}
