using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.IO;

using PharaohPhilesServer.Server;
using PharaohPhilesServer.PhilesProtocol;

namespace PharaohPhilesServer.PhilesProtocol.PhilesJobs
{
    class ClientFileUploadJob : PhilesJob
    {
        public string FileName { get; set; }

        public ClientFileUploadJob(object[] param)
            : base()
        {
            if (param != null && param.Length >= 1)
                FileName = (string)param[0];
            else
                FileName = "";
        }

        private int TotalBytes = 0;
        private int BytesRead = 0;
        private StringBuilder ResponseBuilder = null;
        public override void ProcessMessage(byte[] data, AClient client)
        {
            try
            {
                FileInfo fi = new FileInfo(FileName);
                if (fi.Exists)
                {
                    // Send Filesize
                    long filesize = fi.Length;
                    client.Send(new PPMessage(true, JobNumber, RemoteJobNumber, BitConverter.GetBytes(filesize)).GetEncodedData());

                    // Send Filename
                    string filename = fi.Name;
                    client.Send(new PPMessage(true, JobNumber, RemoteJobNumber, ASCIIEncoding.ASCII.GetBytes(filename)).GetEncodedData());

                    // Send File
                    FileStream fs = File.OpenRead(FileName);
                    byte[] buffer = new byte[2048]; // should be correct size to fit in packets, although doesnt really matter
                    long bytessent = 0;
                    while (bytessent < filesize)
                    {
                        if (filesize - bytessent >= buffer.Length)
                        {
                            fs.Read(buffer, 0, buffer.Length);
                            client.Send(new PPMessage(true, JobNumber, RemoteJobNumber, buffer).GetEncodedData());
                            bytessent += buffer.Length;
                        }
                        else
                        {
                            int bytesread = fs.Read(buffer, 0, (int)(filesize - bytessent));
                            byte[] final = new byte[filesize - bytessent];
                            Array.Copy(buffer, 0, final, 0, bytesread);
                            client.Send(new PPMessage(true, JobNumber, RemoteJobNumber, final).GetEncodedData());
                            bytessent += final.Length;
                        }
                    }
                    fs.Close();
                }
                else
                {
                    Core.Output("File does not exist! '" + FileName + "'");
                    CompleteJob();
                }
                
            }
            catch (Exception ex)
            {
                Core.HandleEx("ClientFileUploadJob:ProcessMessage", ex);
            }
        }

        public override void CompleteJob()
        {
            Core.Output("Client File Upload Job " + JobNumber + " complete.");
            base.CompleteJob();
        }
    }
}
