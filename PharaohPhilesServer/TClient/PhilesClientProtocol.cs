using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using PharaohPhilesServer.PhilesProtocol;
using PharaohPhilesServer.Server;

namespace PharaohPhilesServer.TClient
{
    class PhilesClientProtocol
    {
        private PhilesJobManager Jobs;

        public PhilesClientProtocol()
        {
            Jobs = new PhilesJobManager();
        }

        public void BeginJob(PhilesJobType jobType, AClient client, object[] param)
        {
            if (jobType == PhilesJobType.JOB_CLIENT_PHILES)
            {
                int nJobCode = Jobs.AddJob(PhilesJobType.JOB_CLIENT_PHILES);
                PPMessage beginJobMessage = new PPMessage(false, nJobCode, (int)PhilesJobType.JOB_PHILES);
                client.Send(beginJobMessage.GetEncodedData());
            }
            else if (jobType == PhilesJobType.JOB_CLIENT_DIRECTORY_LISTING)
            {
                int nJobCode = Jobs.AddJob(PhilesJobType.JOB_CLIENT_DIRECTORY_LISTING, param);
                PPMessage beginJobMessage = new PPMessage(false, nJobCode, (int)PhilesJobType.JOB_SERVER_DIRECTORY_LISTING);
                client.Send(beginJobMessage.GetEncodedData());
            }
            else if (jobType == PhilesJobType.JOB_CLIENT_FILE_UPLOAD)
            {
                int nJobCode = Jobs.AddJob(PhilesJobType.JOB_CLIENT_FILE_UPLOAD, param);
                PPMessage beginJobMessage = new PPMessage(false, nJobCode, (int)PhilesJobType.JOB_SERVER_FILE_UPLOAD);
                client.Send(beginJobMessage.GetEncodedData());
            }
        }

        public void ReceiveData(byte[] encodedData, AClient client)
        {
            PPMessage message = new PPMessage(encodedData);

            // Unless the server starts refusing requests in the future, this
            // should never be false.
            if (message.ConnectionEstablished)
            {
                if (Jobs[message.ReceiverJobNumber] == null)
                    return;
                if (Jobs[message.ReceiverJobNumber].RemoteJobNumber == -1)
                    Jobs[message.ReceiverJobNumber].RemoteJobNumber = message.SenderJobNumber;
                Jobs[message.ReceiverJobNumber].ProcessMessage(message.Message, client);
            }
        }
    }
}
