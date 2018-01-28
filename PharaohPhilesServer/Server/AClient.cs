using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.IO;

namespace PharaohPhilesServer.Server
{
    class AClientStateObject
    {
        public Socket WorkSocket = null;
        public const int BufferSize = 1024;
        public byte[] buffer = new byte[BufferSize];

        public bool ReadingData = false;
        public int DataLength = 0;
        public int BytesRead = 0;
        public MemoryStream MStream = new MemoryStream();

        public void Reset()
        {
            // Reset our packet reading object.
            ReadingData = false;
            DataLength = 0;
            BytesRead = 0;

            // Reset our read buffer
            byte[] buffer = MStream.GetBuffer();
            Array.Clear(buffer, 0, buffer.Length);
            MStream.Position = 0;
            MStream.SetLength(0);
        }
    }

    class AClient
    {
        private const int DATA_LENGTH_SIZE = 4;
        private static PharaohPhilesServer.PhilesProtocol.PhilesServerProtocol ServerProtocol = new PhilesProtocol.PhilesServerProtocol();

        private static int ClientIDCounter = 0;
        public int ClientID { get; private set; }
        public bool IsConnected
        {
            get
            {
                try
                {
                    return !(ClientSocket.Poll(1, SelectMode.SelectRead) && ClientSocket.Available == 0);
                }
                catch (Exception ex)
                {
                    Core.HandleEx("AClient:IsConnected", ex);
                    return false;
                }
            }
        }

        private Socket ClientSocket;

        public AClient(Socket nSocket, bool beginReceive)
        {
            ClientID = ClientIDCounter;
            ClientIDCounter++;

            ClientSocket = nSocket;

            // Setup read data callback.
            if (beginReceive)
            {
                AClientStateObject ACS = new AClientStateObject();
                ACS.WorkSocket = ClientSocket;
                ClientSocket.BeginReceive(ACS.buffer, 0, AClientStateObject.BufferSize, 0, new AsyncCallback(ReadCallback), ACS);
            }
        }

        public bool Connect(IPEndPoint ipEP)
        {
            try
            {
                ClientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                IAsyncResult result = ClientSocket.BeginConnect(ipEP, new AsyncCallback(ConnectCallback), ClientSocket);
                result.AsyncWaitHandle.WaitOne(5000, true);
                bool success = ClientSocket.Connected;
                if (!success)
                    ClientSocket.Close();
                return success;
            }
            catch (Exception ex)
            {
                Core.HandleEx("AClient:Connect", ex);
                return false;
            }
        }

        private void ConnectCallback(IAsyncResult ar)
        {
            try
            {
                Socket ClientSocket = (Socket)ar.AsyncState;
                ClientSocket.EndConnect(ar);

                AClientStateObject ACS = new AClientStateObject();
                ACS.WorkSocket = ClientSocket;
                ClientSocket.BeginReceive(ACS.buffer, 0, AClientStateObject.BufferSize, 0, new AsyncCallback(ReadCallback), ACS);
            }
            catch (ObjectDisposedException ex)
            {
                Core.Output("Client could not connect.");
            }
            catch (SocketException ex)
            {
                Core.Output("Client could not connect.");
            }
            catch (Exception ex)
            {
                Core.HandleEx("AClient:ConnectCallback", ex);
            }
        }

        public void Send(byte[] data)
        {
            try
            {
                // Do not let it send data that is larger than int.MaxValue
                // This data should be broken up into smaller chunks by the user.
                if (data.LongLength > int.MaxValue-sizeof(int))
                {
                    throw new Exception("Attempt to send data greater than the maximum size (int.MaxValue-4)");
                }

                // Append the size of the data to the front of the message.
                byte[] datasize = BitConverter.GetBytes(data.Length);
                byte[] finaldata = new byte[datasize.Length + data.Length];
                Array.Copy(datasize, 0, finaldata, 0, datasize.Length);
                Array.Copy(data, 0, finaldata, datasize.Length, data.Length);

                ClientSocket.BeginSend(finaldata, 0, finaldata.Length, SocketFlags.None, new AsyncCallback(SendCallback), ClientSocket);
            }
            catch (ObjectDisposedException ex)
            {
                // Thrown upon closing client.
            }
            catch (Exception ex)
            {
                Core.HandleEx("AClient:Send", ex);
            }
        }

        private void SendCallback(IAsyncResult ar)
        {
            try
            {
                Socket ClientSocket = (Socket)ar.AsyncState;
                int bytesSent = ClientSocket.EndSend(ar);
            }
            catch (ObjectDisposedException ex)
            {
                // Thrown upon closing client.
            }
            catch (Exception ex)
            {
                Core.HandleEx("AClient:SendCallback", ex);
            }
        }

        // Example of useage: http://msdn.microsoft.com/en-us/library/fx6588te.aspx
        
        private void ReadCallback(IAsyncResult ar)
        {
            try
            {
                AClientStateObject ACS = (AClientStateObject)ar.AsyncState;
                Socket ClientSocket = ACS.WorkSocket;

                int bytesRead = ClientSocket.EndReceive(ar);
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

                                int dataLength = BitConverter.ToInt32(dataLengthBytes, 0);
                                if (dataLength == 0)
                                    Core.Output("STOPPP");
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
                            //if (OnDataRead != null)
                                //OnDataRead(ACS.MStream.ToArray(), this);

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

                Core.Output("MSTREAM: " + ACS.MStream.Length);

                // Get more data.
                ClientSocket.BeginReceive(ACS.buffer, 0, AClientStateObject.BufferSize, 0,
                    new AsyncCallback(ReadCallback), ACS);
            }
            catch (ObjectDisposedException ex)
            {
                // Thrown upon closing client.
            }
            catch (SocketException ex)
            {
                // When client runs in the problems, it likely means a disconnect, so cleanup socket.
                Close();
            }
            catch (Exception ex)
            {
                Core.HandleEx("AClient:ReadCallback", ex);
            }
        }

        public delegate void DataReadDelegate(byte[] data, AClient client);
        public event DataReadDelegate OnDataRead;

        public static AClient StartClientConnection(Socket nSocket)
        {
            try
            {
                AClient a = new AClient(nSocket,true);

                // Perform startup communications before being officially added to the server.
                // Or null if it does not pass for a new connection.

                return a;
            }
            catch (Exception ex)
            {
                Core.HandleEx("AClient:StartClientConnection", ex);
                return null;
            }
        }

        public void Close()
        {
            try
            {
                IAsyncResult result = ClientSocket.BeginDisconnect(false, new AsyncCallback(DisconnectCallback), ClientSocket);
                bool success = result.AsyncWaitHandle.WaitOne(5000, true);
                if (!success)
                    Core.Output("WARNING: AClient was unable to disconnect before timeout.");
                ClientSocket.Shutdown(SocketShutdown.Both);
                ClientSocket.Close();
            }
            catch (ObjectDisposedException ex)
            {
                // If the socket is already disposed, we have nothing to worry about.
            }
            catch (Exception ex)
            {
                Core.HandleEx("AClient:Close", ex);
            }
            finally
            {
                if (OnClientDisconnect != null)
                    OnClientDisconnect(this);
            }
        }

        private void DisconnectCallback(IAsyncResult ar)
        {
            try
            {
                Socket ClientSocket = (Socket)ar.AsyncState;
                ClientSocket.EndDisconnect(ar);
            }
            catch (ObjectDisposedException ex)
            {
                // Thrown upon closing client.
            }
            catch (Exception ex)
            {
                Core.HandleEx("AClient:DisconnectCallback", ex);
            }
        }

        public delegate void ClientDisconnectDelegate(AClient c);
        public event ClientDisconnectDelegate OnClientDisconnect;

        #region Equality Overloads

        public override bool Equals(object obj)
        {
            // If parameter is null return false
            if (obj == null)
                return false;

            // If paramter cannot be cast to PhilesClient return false
            AClient c = obj as AClient;
            if ((System.Object)c == null)
                return false;

            // Return true if the fields match
            return (this.ClientID == c.ClientID);
        }

        public bool Equals(AClient obj)
        {
            // If parameter is null return false
            if ((object)obj == null)
                return false;

            // Return true if the fields match
            return (this.ClientID == obj.ClientID);
        }

        public override int GetHashCode()
        {
            return ClientID;
        }

        public static bool operator ==(AClient a, AClient b)
        {
            // If both are null, or both are the same instance, return true.
            if (System.Object.ReferenceEquals(a, b))
                return true;

            // If one is null, but not both, return false.
            if (((object)a == null) || ((object)b == null))
                return false;

            // Return true if both fields match.
            return (a.ClientID == b.ClientID);
        }

        public static bool operator !=(AClient a, AClient b)
        {
            return !(a == b);
        }

        #endregion

        #region Old ReadCallback Code

        /*if (bytesRead > 0)
                {
                    // Process the read data.
                    //if (OnDataRead != null)
                        //OnDataRead(ACS.buffer, this);

                    // WARNING: Inherently, the maximum packet size is 2 GB. This is because of data length cast from LONG to INT

                    // THANKS TO : http://vadmyst.blogspot.com/2008/03/part-2-how-to-transfer-fixed-sized-data.html

                    int offset = 0;
                    while (offset < bytesRead)
                    {
                        // Do we know how long the data to be read is?
                        if (ACS.DataLength == 0)
                        {
                            // Nope, not yet.
                            int bytesInMemoryStream = (int)ACS.MStream.Length; // Any size bytes left over in memory stream.
                            int bytesNeededFromPacket = DATA_LENGTH_SIZE - bytesInMemoryStream;
                            int bytesRemainingFromPacket = bytesRead - offset;

                            // Have we received enough bytes to determine data size?
                            if (bytesNeededFromPacket <= bytesRemainingFromPacket)
                            {
                                // Yes.
                                byte[] dataLengthBytes = new byte[DATA_LENGTH_SIZE];
                                if (bytesInMemoryStream > 0)
                                {
                                    // Read the part from the stream.
                                    ACS.MStream.Read(dataLengthBytes, 0, bytesInMemoryStream);
                                    // Read the other part from the received buffer.
                                    Array.Copy(ACS.buffer, offset, dataLengthBytes, bytesInMemoryStream, bytesNeededFromPacket);
                                    offset += bytesNeededFromPacket;
                                }
                                else
                                {
                                    // We read from bytes received alone.
                                    Array.Copy(ACS.buffer, offset, dataLengthBytes, 0, bytesNeededFromPacket);
                                    offset += bytesNeededFromPacket;
                                }
                                ACS.DataLength = BitConverter.ToInt64(dataLengthBytes, 0);
                                if (ACS.DataLength <= 0)
                                    Core.Output("STOPPP!!");
                                continue;
                            }
                            else
                            {
                                // No
                                // Write what data length bytes we can to memory. Will have to wait for
                                // the next packet to have enough bytes to determine data size.
                                ACS.MStream.Write(ACS.buffer, offset, bytesRemainingFromPacket);
                                offset += bytesRemainingFromPacket;
                                continue;
                            }
                        }
                        else
                        {
                            // We know how long the data to be received is.
                            long dataBytesNeeded = ACS.DataLength - ACS.BytesRead;
                            int dataLeft = bytesRead - offset;

                            // Can we finish reading data?
                            if (dataLeft >= dataBytesNeeded)
                            {
                                // Yes!
                                ACS.MStream.Write(ACS.buffer, offset, (int)dataBytesNeeded);
                                offset += (int)dataBytesNeeded;
                                ACS.BytesRead += dataBytesNeeded;

                                if (ACS.BytesRead == ACS.DataLength)
                                {
                                    // We have received the end of the data packet.
                                    Core.Output("HURRAH! We finished transfering a data packet that was " + ACS.DataLength + " bytes!");
                                    if (OnDataRead != null)
                                        OnDataRead(ACS.MStream.ToArray(), this);

                                    ACS.Reset();
                                    continue;
                                }
                            }
                            else
                            {
                                // No we cannot finish reading data just yet. Add what we have to memory.
                                ACS.MStream.Write(ACS.buffer, offset, dataLeft);
                                ACS.BytesRead += dataLeft;
                                offset += dataLeft;
                                continue;
                            }
                        }
                    }
                    
                }*/

        #endregion
    }
}
