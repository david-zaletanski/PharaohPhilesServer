using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PharaohPhilesServer.Server
{
    class AClientReadCallbackHelper
    {
        // The number of bytes determining the size of the data.
        public const int DATA_LENGTH_SIZE = 4;

        /// <summary>
        /// Helper method to read incoming packet data. The packet begins with a
        /// LONG determining the size of the data, followed by the data itself. There
        /// are 4 scenarios to handle with this setup:
        /// 1. Receive a partial data length.
        /// 2. Receive the data length and partial data.
        /// 3. Receive data length, data, and a partial data length of the next message.
        /// 4. Receive data length, data, the data length of the next message, and
        ///    partial data of that next message.
        /// </summary>
        public static void HandleReadCallback(AClientStateObject ACS, int bytesRead)
        {
            try
            {
                int offset = 0;

                while (offset < bytesRead)
                {
                    int bytesRemaining = bytesRead - offset;

                    if (!ACS.ReadingData)
                    {
                        // Data length is unknown.
                        int bytesInMemoryStream = (int)ACS.MStream.Length;
                        int bytesNeededFromPacket = DATA_LENGTH_SIZE - bytesInMemoryStream;

                        if (bytesInMemoryStream > 0)
                        {
                            // We have partial data length bytes in memory stream.
                            if (bytesNeededFromPacket <= bytesRemaining)
                            {
                                // There is enough data length bytes in packet to complete data length.
                                byte[] dataLengthBytes = new byte[DATA_LENGTH_SIZE];
                                if (bytesInMemoryStream >= DATA_LENGTH_SIZE)
                                    Core.Output("This should never happen, check it out!");
                                ACS.MStream.Read(dataLengthBytes, 0, bytesInMemoryStream);
                                Array.Copy(ACS.buffer, offset, dataLengthBytes, bytesInMemoryStream, bytesNeededFromPacket);
                                offset += bytesNeededFromPacket;
                                ACS.DataLength = BitConverter.ToInt32(dataLengthBytes, 0);
                                ACS.ReadingData = true;
                            }
                            else
                            {
                                // Not enough bytes in packet to complete data length. Put them in the memory stream.
                                ACS.MStream.Write(ACS.buffer, offset, bytesRemaining);
                                offset += bytesRemaining;
                            }
                        }
                        else
                        {
                            // We have no partial data length bytes. All must be in packet.
                            if (bytesNeededFromPacket <= bytesRemaining)
                            {
                                // We have enough data length bytes in packet to complete data length.
                                byte[] dataLengthBytes = new byte[DATA_LENGTH_SIZE];
                                Array.Copy(ACS.buffer, offset, dataLengthBytes, 0, bytesNeededFromPacket);
                                offset += bytesNeededFromPacket;
                                ACS.DataLength = BitConverter.ToInt32(dataLengthBytes, 0);
                                ACS.ReadingData = true;
                            }
                            else
                            {
                                // Not enough bytes in packet to complete data length. Put them in the memory stream.
                                ACS.MStream.Write(ACS.buffer, offset, bytesRemaining);
                                offset += bytesRemaining;
                            }
                        }
                    }
                    else
                    {
                        // Data length is known.
                        if (ACS.BytesRead + bytesRemaining >= ACS.DataLength)
                        {
                            // We have enough bytes to complete reading the data.
                            int bytesToRead = ACS.DataLength - ACS.BytesRead;
                            ACS.MStream.Write(ACS.buffer, offset, bytesToRead);
                            ACS.BytesRead += bytesToRead;
                            offset += bytesToRead;

                            Core.Output("Data transmission successful, received " + ACS.MStream.Length + " bytes.");
                            byte[] message = new byte[ACS.DataLength];
                            ACS.MStream.Read(message, 0, ACS.DataLength);

                            // TODO: Set off OnDataRead event and handle received data!

                            ACS.Reset();
                        }
                        else
                        {
                            // Not enough data to complete message. Add remainder to memory stream.
                            ACS.MStream.Write(ACS.buffer, offset, bytesRemaining);
                            ACS.BytesRead += bytesRemaining;
                            offset += bytesRemaining;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Core.HandleEx("AClientReadCallbackHelper:HandleCallback", ex);
            }
        }
    }
}
