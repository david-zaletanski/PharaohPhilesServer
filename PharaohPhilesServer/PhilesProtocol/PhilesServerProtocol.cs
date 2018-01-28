using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using PharaohPhilesServer.Server;

namespace PharaohPhilesServer.PhilesProtocol
{
    class PhilesServerProtocol
    {
        private PhilesJobManager Jobs;

        public PhilesServerProtocol()
        {
            Jobs = new PhilesJobManager();
        }

        public void ReceiveData(byte[] encodedData, AClient client)
        {
            PPMessage message = new PPMessage(encodedData);
            if (message.ConnectionEstablished)
            {
                // If we receive a message for a job that doesn't exist.
                if (Jobs[message.ReceiverJobNumber] == null)
                    return; // TODO: Tell client to cancel its job.

                // Otherwise give the job the data to be processed.
                Jobs[message.ReceiverJobNumber].ProcessMessage(message.Message, client);
            }
            else
            {
                // Create a new job depending on what type the client requests.
                PhilesJobType newJobType = (PhilesJobType)message.ReceiverJobNumber;
                int newJobNumber = Jobs.AddJob(newJobType);
                Jobs[newJobNumber].RemoteJobNumber = message.SenderJobNumber;

                // Send a blank response to client signaling its okay to start transmission.
                PPMessage serverResponse = new PPMessage(true, newJobNumber, message.SenderJobNumber);
                client.Send(serverResponse.GetEncodedData());
            }
        }
    }
}
