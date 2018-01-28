using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using System.Net;
using System.Net.Sockets;
using PharaohPhilesServer.Server;
using PharaohPhilesServer.PhilesProtocol;

namespace PharaohPhilesServer.TClient
{
    public partial class frmTClient : Form
    {
        AClient Client;
        PhilesClientProtocol PCP;

        public frmTClient()
        {
            InitializeComponent();
            PCP = new PhilesClientProtocol();
            Socket underlying = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            Client = new AClient(underlying,false);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            // Connect To Server
            if (button1.Text == "Connect")
            {
                if (Client.Connect(new IPEndPoint(System.Net.IPAddress.Loopback, 4150)))
                {
                    Client.OnClientDisconnect += new AClient.ClientDisconnectDelegate(Client_OnClientDisconnect);
                    Client.OnDataRead += new AClient.DataReadDelegate(Client_OnDataRead);
                    label2.Text = "YES";
                    button1.Text = "Disconnect";
                }
            }
            else if (button1.Text == "Disconnect")
            {
                Client.Close();
                label2.Text = "NO";
                button1.Text = "Connect";
            }
        }

        void Client_OnClientDisconnect(AClient c)
        {
            label2.Text = "NO";
            button1.Text = "Connect";
        }

        void Client_OnDataRead(byte[] data, AClient client)
        {
            PCP.ReceiveData(data, client);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            // Start PhilesJob Connection Test
            PCP.BeginJob(PhilesJobType.JOB_CLIENT_PHILES, Client, null);
        }

        private void frmTClient_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Close connection.
            Client.Close();
            label2.Text = "NO";
            button1.Text = "Connect";
        }

        private void button3_Click(object sender, EventArgs e)
        {
            // Client directory request job.
            PCP.BeginJob(PhilesJobType.JOB_CLIENT_DIRECTORY_LISTING, Client, new object[] { textBox1.Text });
        }

        private void button4_Click(object sender, EventArgs e)
        {
            // Send random data.
            Random rnd = new Random();

            for (int i = 0; i < 50; i++)
            {
                int dataSize = rnd.Next(50, 10000);
                Core.Output("Sending data packet that is " + dataSize + " bytes.");
                byte[] data = new byte[dataSize];
                Client.Send(data);
            }
        }

        private void button5_Click(object sender, EventArgs e)
        {
            PCP.BeginJob(PhilesJobType.JOB_CLIENT_FILE_UPLOAD, Client, new object[] { textBox2.Text });
        }
    }
}
