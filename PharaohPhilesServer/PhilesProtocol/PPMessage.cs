using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PharaohPhilesServer.PhilesProtocol
{
    class PPMessage
    {
        private const int HeaderSize = 9;

        public bool ConnectionEstablished { get; set; }
        public int SenderJobNumber { get; set; }
        public int ReceiverJobNumber { get; set; }
        public byte[] Message { get; set; }


        public PPMessage()
        {
            ConnectionEstablished = false;
            SenderJobNumber = -1;
            ReceiverJobNumber = -1;
            Message = null;
        }

        public PPMessage(bool connectionEstablished, int senderJobNumber, int receiverJobNumber)
        {
            ConnectionEstablished = connectionEstablished;
            SenderJobNumber = senderJobNumber;
            ReceiverJobNumber = receiverJobNumber;
            Message = null;
        }

        public PPMessage(bool connectionEstablished, int senderJobNumber, int receiverJobNumber, byte[] message)
        {
            ConnectionEstablished = connectionEstablished;
            SenderJobNumber = senderJobNumber;
            ReceiverJobNumber = receiverJobNumber;
            Message = message;
        }

        public PPMessage(byte[] encodedData)
        {
            ConnectionEstablished = false;
            SenderJobNumber = -1;
            ReceiverJobNumber = -1;
            Message = null;

            decodeData(encodedData);
        }

        public byte[] GetEncodedData()
        {
            try
            {
                int messageLength = 0;
                if (Message != null)
                    messageLength = Message.Length;

                int encodedSize = messageLength + HeaderSize;
                byte[] connectionEstablishedBytes = BitConverter.GetBytes(ConnectionEstablished);
                byte[] senderJobNumberBytes = BitConverter.GetBytes(SenderJobNumber);
                byte[] receiverJobNumberBytes = BitConverter.GetBytes(ReceiverJobNumber);
                byte[] encodedData = new byte[encodedSize];
                int offset = 0;
                Array.Copy(connectionEstablishedBytes, 0, encodedData, offset, connectionEstablishedBytes.Length);
                offset += connectionEstablishedBytes.Length;
                Array.Copy(senderJobNumberBytes, 0, encodedData, offset, senderJobNumberBytes.Length);
                offset += senderJobNumberBytes.Length;
                Array.Copy(receiverJobNumberBytes, 0, encodedData, offset, receiverJobNumberBytes.Length);
                offset += receiverJobNumberBytes.Length;
                if (messageLength > 0)
                    Array.Copy(Message, 0, encodedData, offset, Message.Length);

                return encodedData;
            }
            catch (Exception ex)
            {
                Core.HandleEx("PPMessage:GetEncodedData", ex);
                return null;
            }
        }

        private void decodeData(byte[] encodedData)
        {
            try
            {
                // Determine size of encoded data.
                int offset = 0;

                // Get header information.
                ConnectionEstablished = BitConverter.ToBoolean(encodedData, offset);
                offset += sizeof(bool);
                SenderJobNumber = BitConverter.ToInt32(encodedData, offset);
                offset += sizeof(int);
                ReceiverJobNumber = BitConverter.ToInt32(encodedData, offset);
                offset += sizeof(int);

                // Prepare data.
                Message = new byte[encodedData.Length-offset];
                Array.Copy(encodedData, offset, Message, 0, encodedData.Length-offset);
            }
            catch (Exception ex)
            {
                Core.HandleEx("PPMessage:decodeData", ex);
            }
        }
    }
}
