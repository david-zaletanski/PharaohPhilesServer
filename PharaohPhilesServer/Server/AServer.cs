using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Threading;
using System.Net;
using System.Net.Sockets;

using PharaohPhilesServer.PhilesProtocol;

namespace PharaohPhilesServer.Server
{
    public class AServer
    {
        public int ClientCount
        {
            get
            {
                return Clients.Count;
            }
        }

        private AServerListener ASListener;
        private List<AClient> Clients;
        private Thread ConnectivityCheckThread;
        private bool CheckConnectivity;
        private PhilesServerProtocol Protocol;

        public AServer()
        {
            Clients = new List<AClient>();
        }

        public void Start(IPEndPoint ipEnd)
        {
            try
            {
                Protocol = new PhilesServerProtocol();
                ASListener = new AServerListener();
                ASListener.OnClientConnect += new AServerListener.ClientConnectDelegate(ASListener_OnClientConnect);
                ASListener.Start(ipEnd);
                ConnectivityCheckThread = new Thread(new ThreadStart(connectivityCheckThread));
                ConnectivityCheckThread.Start();
            }
            catch (Exception ex)
            {
                Core.HandleEx("AServer:Start", ex);
            }
        }

        void connectivityCheckThread()
        {
            CheckConnectivity = true;
            while (CheckConnectivity) // WHY WONT TIS UPDATE?
            {
                int i = 0;
                while(i < Clients.Count)
                {
                    try
                    {
                        AClient a = Clients[i];
                        if (!a.IsConnected)
                        {
                            // Closing the connection on our side automatically removes it from client list.
                            a.Close();
                        }
                        else
                        {
                            i++;
                        }
                    }
                    catch (Exception ex)
                    {
                        Core.HandleEx("AServer:connectivityCheckThread", ex);
                    }
                }
                
                Thread.Sleep(500);
            }
        }

        // Handles New Clients
        void ASListener_OnClientConnect(Socket nSocket)
        {
            try
            {
                AClient NewClient = AClient.StartClientConnection(nSocket);
                if (NewClient != null)
                {
                    NewClient.OnDataRead += new AClient.DataReadDelegate(NewClient_OnDataRead);
                    NewClient.OnClientDisconnect += new AClient.ClientDisconnectDelegate(NewClient_OnClientDisconnect);
                    Clients.Add(NewClient);

                    if (OnClientConnect != null)
                        OnClientConnect();
                }
            }
            catch (Exception ex)
            {
                Core.HandleEx("AServer:ASListener_OnClientConnect", ex);
            }
        }

        void NewClient_OnDataRead(byte[] data, AClient client)
        {
            Protocol.ReceiveData(data, client);
        }

        // A client disconnects.
        void NewClient_OnClientDisconnect(AClient c)
        {
            Clients.Remove(c);

            if (OnClientDisconnect != null)
                OnClientDisconnect();
        }

        public delegate void ClientConnectDelegate();
        public event ClientConnectDelegate OnClientConnect;
        public delegate void ClientDisconnectDelegate();
        public event ClientDisconnectDelegate OnClientDisconnect;

        public void Stop()
        {
            try
            {
                // Stop polling connections to check if their still connected.
                CheckConnectivity = false;

                // Stop listening for new connections.
                ASListener.Stop();

                // Disconnect from clients.
                int i = 0;
                while (i < Clients.Count)
                {
                    Clients[i].Close();
                }
            }
            catch (Exception ex)
            {
                Core.HandleEx("AServer:Stop", ex);
            }
        }
    }
}
