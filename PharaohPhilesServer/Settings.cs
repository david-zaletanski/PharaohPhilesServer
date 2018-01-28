using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Net;

namespace PharaohPhilesServer
{
    class Settings
    {
        public static IPEndPoint GetListenEndPoint()
        {
            IPEndPoint d = new IPEndPoint(IPAddress.Any, 4150);

            return d;
        }

        public static string GetDefaultUploadLocation()
        {
            return @"C:\Users\MORPHEUS\Desktop\";
        }
    }
}
