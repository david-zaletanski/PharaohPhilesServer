using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PharaohPhilesServer
{
    public class Core
    {
        public static frmMain MainForm;

        public static void Output(string message)
        {
            MainForm.Output(message);
        }
        public static void Output(string message, System.Drawing.Color c)
        {
            MainForm.Output(message, c);
        }

        public static void HandleEx(string Sender, Exception ex)
        {
            Output("EXCEPTION (" + Sender + "):"+"\n\t" + ex.Message, System.Drawing.Color.Red);
        }
        public static void HandleEx(string Sender, Exception ex, string comment)
        {
            Output("EXCEPTION (" + Sender + "):",System.Drawing.Color.Red);
            Output("\t"+comment,System.Drawing.Color.Orange);
            Output("\t"+ex.Message, System.Drawing.Color.Red);
        }

        // This function is run on application exit. Contains all cleanup instructions.
        public static void OnExit()
        {
            // Stop the server. This also disconnects from all clients.
            MainForm.StopServer();
        }
    }
}
