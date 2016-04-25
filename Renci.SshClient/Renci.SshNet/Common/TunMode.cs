using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Renci.SshNet.Common
{
    /// <summary>
    /// Tun mode specified by openssh in which the device will operate.
    /// </summary>
    public  enum TunMode {
        POINTOPOINT = 0x01,  // layer 3
        ETHERNET  =  0x02,   // layer 2
    }
}
