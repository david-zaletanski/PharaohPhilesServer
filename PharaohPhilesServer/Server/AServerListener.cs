using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Threading;
using System.Net;
using System.Net.Sockets;

namespace PharaohPhilesServer.Server
{
    class AServerListener
    {
        private Socket ListenSocket;
        private ManualResetEvent ManualReset;

        public AServerListener()
        {
            ManualReset = new ManualResetEvent(false);
        }

        public void Start(IPEndPoint ipEnd)
        {
            // Setup our socket to be in listen mode, and create the async callback.
            ListenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                ListenSocket.Bind(ipEnd);
                ListenSocket.Listen(10);
                ListenSocket.BeginAccept(new AsyncCallback(acceptCallback), ListenSocket);
                Core.Output("Server has begun accepting connections.");
            }
            catch (Exception ex)
            {
                Core.HandleEx("AServerListener:Start", ex);
            }
        }

        private void acceptCallback(IAsyncResult ar)
        {
            try
            {
                // Accept the new socket, pop it off in an event for the server to handle, and get ready
                // to accept a new one.
                Socket ListenSocket = (Socket)ar.AsyncState;
                Socket nSocket = ListenSocket.EndAccept(ar);

                if (OnClientConnect != null)
                    OnClientConnect(nSocket);

                ListenSocket.BeginAccept(new AsyncCallback(acceptCallback), ListenSocket);
            }
            catch (ObjectDisposedException)
            {
                // This exception is thrown when the listen socket is closed.
                Core.Output("Server is no longer accepting connections.");
            }
            catch (Exception ex)
            {
                Core.HandleEx("AServerListener:acceptCallback", ex);
            }
        }

        public delegate void ClientConnectDelegate(Socket nSocket);
        public event ClientConnectDelegate OnClientConnect;

        public void Stop()
        {
            try
            {
                ListenSocket.Close();
                ListenSocket = null;
            }
            catch (Exception ex)
            {
                Core.HandleEx("AServerListener:Stop", ex);
            }
        }
    }
}
