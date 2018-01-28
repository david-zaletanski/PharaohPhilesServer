using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using PharaohPhilesServer.Server;

namespace PharaohPhilesServer
{
    public partial class frmMain : Form
    {
        AServer Server;

        public frmMain()
        {
            InitializeComponent();
            Server = new AServer();
            Server.OnClientConnect += new AServer.ClientConnectDelegate(UpdateClientCountCallback);
            Server.OnClientDisconnect += new AServer.ClientDisconnectDelegate(UpdateClientCountCallback);
        }

        private delegate void UpdateClientCountDelegate();
        void UpdateClientCountCallback()
        {
            // Prevents this from throwing exceptions on application exit.
            if (this.IsDisposed)
                return;

            if (label3.InvokeRequired)
            {
                this.Invoke(new UpdateClientCountDelegate(UpdateClientCountCallback));
            }
            else
            {
                label3.Text = Server.ClientCount.ToString();
            }
        }

        #region Output

        public void Output(string message)
        {
            Output(message, Color.Black);
        }

        private delegate void OutputDelegate(string msg, Color c);
        public void Output(string message, Color c)
        {
            // Prevents messages on application exit from causing exceptions.
            if (this.IsDisposed)
                return;

            if (richTextBox1.InvokeRequired)
            {
                this.Invoke(new OutputDelegate(Output), new object[] { message, c });
            }
            else
            {
                int sPos = richTextBox1.TextLength;
                richTextBox1.AppendText(message + "\n");
                richTextBox1.Select(sPos, message.Length + 1);
                richTextBox1.SelectionColor = c;
                richTextBox1.Select(richTextBox1.TextLength, 0);
                richTextBox1.ScrollToCaret();
            }
        }

        #endregion

        public void StopServer()
        {
            Server.Stop();
        }

        private void SetStatus(string status)
        {
            label5.Text = status;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            // Start
            if (button1.Text == "Start")
            {
                SetStatus("ONLINE");
                Server.Start(Settings.GetListenEndPoint());
                button1.Text = "Stop";
            }
            else if (button1.Text == "Stop")
            {
                SetStatus("OFFLINE");
                Server.Stop();
                button1.Text = "Start";
            }
        }

        private void testClientToolStripMenuItem_Click(object sender, EventArgs e)
        {
            TClient.frmTClient tcl = new TClient.frmTClient();
            tcl.Show();
        }
    }
}
